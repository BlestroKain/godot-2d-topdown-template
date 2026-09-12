using NuevoMMO.Core;
using NuevoMMO.Network;
using NuevoMMO.Server.Database;
using NuevoMMO.Server.Entities;
using NuevoMMO.Server.Security;
using NuevoMMO.Server.Services;
using NuevoMMO.Server.Systems;
using NuevoMMO.Server.Telemetry;
using NuevoMMO.Server.World;

namespace NuevoMMO.Server.NetworkHandlers;

public sealed class ServerPacketContext
{
    public required ConnectionId Connection { get; init; }
    public required PlayerSession Session { get; init; }
    public required Action<IPacket> Send { get; init; }
}

internal static class ServerAuthorizationGuard
{
    public static void Demand(ServerPacketContext context, ServerAction action, AuthorizationService authorization)
    {
        var decision = authorization.Authorize(context.Session, action);
        if (!decision.Allowed) throw new InvalidOperationException(decision.Reason);
    }
}

public sealed class ConnectHandler(WorldRuntime world, AuthorizationService authorization)
    : IPacketHandler<ServerPacketContext, ConnectRequest>
{
    public ValueTask HandleAsync(ServerPacketContext context, ConnectRequest packet, CancellationToken cancellationToken)
    {
        ServerAuthorizationGuard.Demand(context, ServerAction.AcceptProtocol, authorization);
        if (!HandshakeRules.IsCompatible(packet.ProtocolVersion)) throw new InvalidDataException("Protocolo incompatible.");
        world.AcceptProtocol(context.Session);
        context.Send(new ConnectionAccepted(context.Connection, NetworkClock.Timestamp, ProtocolVersion.Current));
        return ValueTask.CompletedTask;
    }
}

public sealed class RegisterHandler(
    AuthService auth,
    AuthorizationService authorization,
    AbuseDetector abuse,
    AuthenticationSettings settings) : IPacketHandler<ServerPacketContext, RegisterRequest>
{
    public async ValueTask HandleAsync(ServerPacketContext context, RegisterRequest packet, CancellationToken cancellationToken)
    {
        ServerAuthorizationGuard.Demand(context, ServerAction.Register, authorization);
        var rate = abuse.Check($"register:{context.Connection}", Environment.TickCount64,
            settings.RegistrationAttemptsPerWindow, settings.RegistrationWindowMilliseconds);
        if (!rate.Allowed)
        {
            context.Send(new RegisterResult(false, "Demasiados intentos de registro. Intenta nuevamente más tarde.", default));
            return;
        }
        if (!InputValidator.IsSafeName(packet.Username, settings.MinimumUsernameLength, settings.MaximumUsernameLength) ||
            !InputValidator.IsBoundedText(packet.Password, settings.MinimumPasswordLength, settings.MaximumPasswordLength))
        {
            context.Send(new RegisterResult(false, "Usuario o contraseña no cumplen los requisitos.", default));
            return;
        }
        try
        {
            var account = await auth.RegisterAsync(packet.Username, packet.Password, cancellationToken);
            context.Send(new RegisterResult(true, "", account.Id));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            context.Send(new RegisterResult(false, exception.Message, default));
        }
    }
}

public sealed class LoginHandler(
    AuthService auth,
    AuthorizationService authorization,
    AbuseDetector abuse,
    AuthenticationSettings settings,
    BanList bans) : IPacketHandler<ServerPacketContext, LoginRequest>
{
    public async ValueTask HandleAsync(ServerPacketContext context, LoginRequest packet, CancellationToken cancellationToken)
    {
        ServerAuthorizationGuard.Demand(context, ServerAction.Login, authorization);
        var rate = abuse.Check($"login:{context.Connection}", Environment.TickCount64,
            settings.LoginAttemptsPerWindow, settings.LoginWindowMilliseconds);
        if (!rate.Allowed)
        {
            context.Send(new LoginResult(false, "Demasiados intentos de inicio de sesión. Intenta nuevamente más tarde.",
                default, default, ""));
            return;
        }
        if (!InputValidator.IsSafeName(packet.Username, settings.MinimumUsernameLength, settings.MaximumUsernameLength) ||
            !InputValidator.IsBoundedText(packet.Password, settings.MinimumPasswordLength, settings.MaximumPasswordLength))
        {
            context.Send(new LoginResult(false, "Credenciales inválidas.", default, default, ""));
            return;
        }
        if (bans.IsBanned(packet.Username, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), out _))
        {
            context.Send(new LoginResult(false, "Cuenta suspendida.", default, default, ""));
            return;
        }
        try
        {
            var (account, session) = await auth.LoginAsync(packet.Username, packet.Password, cancellationToken);
            context.Session.Account = account.Id;
            context.Session.Session = session.Id;
            context.Session.SessionToken = session.Token;
            context.Session.State = PlayerSessionState.Authenticated;
            context.Send(new LoginResult(true, "", account.Id, session.Id, session.Token));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            context.Send(new LoginResult(false, exception.Message, default, default, ""));
        }
    }
}

public sealed class CharacterListHandler(CharacterService characters, AuthorizationService authorization)
    : IPacketHandler<ServerPacketContext, CharacterListRequest>
{
    public async ValueTask HandleAsync(ServerPacketContext context, CharacterListRequest packet, CancellationToken cancellationToken)
    {
        ServerAuthorizationGuard.Demand(context, ServerAction.ListCharacters, authorization);
        AuthService.EnsureSession(context.Session, packet.Session, packet.SessionToken);
        var list = await characters.ListAsync(context.Session.Account, cancellationToken);
        context.Send(new CharacterListResult(list.Select(CharacterService.ToSummary).ToArray()));
    }
}

public sealed class CharacterCreateHandler(CharacterService characters, AuthorizationService authorization)
    : IPacketHandler<ServerPacketContext, CreateCharacterRequest>
{
    public async ValueTask HandleAsync(ServerPacketContext context, CreateCharacterRequest packet, CancellationToken cancellationToken)
    {
        ServerAuthorizationGuard.Demand(context, ServerAction.CreateCharacter, authorization);
        AuthService.EnsureSession(context.Session, packet.Session, packet.SessionToken);
        if (!InputValidator.IsSafeName(packet.Name)) throw new ArgumentException("Nombre de personaje inválido.");
        if (!CanonicalTraditions.IsValidAtCreate(packet.TraditionId))
            throw new ArgumentException("Tradición inválida. El personaje nace Novicio o con una Tradición publicada.");
        if (!CanonicalCharacterAppearance.IsSupported(packet.Appearance)) throw new ArgumentException("Apariencia inválida.");
        var created = await characters.CreateAsync(
            context.Session.Account,
            packet.Name,
            packet.TraditionId,
            packet.Appearance,
            cancellationToken);
        ServerLog.Info(
            $"Personaje creado account={context.Session.Account.Value:N} name={created.Name} id={created.Id.Value:N} " +
            $"tradition={CanonicalTraditions.DisplayName(created.TraditionId)} map={created.MapDefinition.Value} " +
            $"visual={created.Appearance.BaseVisual}");
        context.Send(new CharacterCreated(CharacterService.ToSummary(created)));
    }
}

public sealed class CharacterSelectHandler(
    WorldRuntime world,
    CharacterService characters,
    PersistenceService persistence,
    ProgressionSystem progression,
    AuthorizationService authorization) : IPacketHandler<ServerPacketContext, CharacterSelectRequest>
{
    public async ValueTask HandleAsync(ServerPacketContext context, CharacterSelectRequest packet, CancellationToken cancellationToken)
    {
        ServerAuthorizationGuard.Demand(context, ServerAction.SelectCharacter, authorization);
        AuthService.EnsureSession(context.Session, packet.Session, packet.SessionToken);
        var list = await characters.ListAsync(context.Session.Account, cancellationToken);
        var record = list.FirstOrDefault(character => character.Id == packet.Character)
            ?? throw new InvalidOperationException("Personaje inexistente.");
        var loaded = await persistence.LoadCharacterAsync(context.Session.Account, record, cancellationToken);
        var player = world.Join(context.Session, loaded.Spawn);
        progression.Initialize(player, loaded.Progression, preserveVitals: false);
        if (loaded.CurrentHealth is not null || loaded.CurrentMana is not null)
        {
            var health = Math.Clamp(loaded.CurrentHealth ?? player.MaxHealth, 0, player.MaxHealth);
            var mana = Math.Clamp(loaded.CurrentMana ?? player.MaxMana, 0, player.MaxMana);
            player.SetVitals(health, mana);
        }
        context.Send(new CharacterSelected(player.CharacterId));
        context.Send(world.PrepareMapLoad(context.Session, "dev-1"));
        context.Send(PlayerStatsProjection.Create(player, progression));
    }
}

public sealed class MapReadyHandler(WorldRuntime world, AuthorizationService authorization)
    : IPacketHandler<ServerPacketContext, MapReadyRequest>
{
    public ValueTask HandleAsync(ServerPacketContext context, MapReadyRequest packet, CancellationToken cancellationToken)
    {
        ServerAuthorizationGuard.Demand(context, ServerAction.EnterWorld, authorization);
        world.Activate(context.Session, packet.Instance);
        return ValueTask.CompletedTask;
    }
}

public sealed class MoveRequestHandler(WorldRuntime world, AuthorizationService authorization)
    : IPacketHandler<ServerPacketContext, MoveRequest>
{
    public ValueTask HandleAsync(ServerPacketContext context, MoveRequest packet, CancellationToken cancellationToken)
    {
        ServerAuthorizationGuard.Demand(context, ServerAction.Move, authorization);
        var input = packet.Input ?? throw new InvalidDataException("Input de movimiento ausente.");
        var player = context.Session.Player ?? throw new InvalidOperationException("Jugador fuera del mundo.");
        if (!InputValidator.IsFiniteDirection(input.X, input.Y) || input.ClientTick < 0 ||
            input.Sequence != player.Inputs.LastAccepted + 1)
            throw new InvalidDataException("Input de movimiento inválido o fuera de orden.");
        world.SubmitMovement(context.Session, input);
        return ValueTask.CompletedTask;
    }
}

public sealed class AllocateAttributeHandler(
    ProgressionSystem progression,
    PersistenceService persistence,
    AuthorizationService authorization) : IPacketHandler<ServerPacketContext, AllocateAttributeRequest>
{
    public async ValueTask HandleAsync(ServerPacketContext context, AllocateAttributeRequest packet, CancellationToken cancellationToken)
    {
        ServerAuthorizationGuard.Demand(context, ServerAction.AllocateAttribute, authorization);
        var player = context.Session.Player ?? throw new InvalidOperationException("Jugador fuera del mundo.");
        if (!Enum.IsDefined(packet.Attribute) || packet.Increments is < 1 or > 100)
            throw new InvalidDataException("Solicitud de atributo inválida.");
        try
        {
            progression.Allocate(player, packet.Attribute, packet.Increments);
            await persistence.SaveCharacterAsync(player, cancellationToken);
            context.Send(PlayerStatsProjection.Create(player, progression));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or OverflowException)
        {
            context.Send(new ErrorPacket("attribute_allocation", exception.Message, false));
            context.Send(PlayerStatsProjection.Create(player, progression));
        }
    }
}

/// <summary>
/// Harness de Development/Test. Ejecuta fórmulas técnicas sobre Mobs reales a través de CombatSystem;
/// no define targeting/rango de producción ni una ruta alternativa de daño.
/// </summary>
public sealed class DevelopmentAttackHandler(
    WorldRuntime world,
    CombatSystem combat,
    ProgressionSystem progression,
    PersistenceService persistence,
    AuthorizationService authorization) : IPacketHandler<ServerPacketContext, DevelopmentAttackRequest>
{
    public async ValueTask HandleAsync(ServerPacketContext context, DevelopmentAttackRequest packet, CancellationToken cancellationToken)
    {
        ServerAuthorizationGuard.Demand(context, ServerAction.UseTechnique, authorization);
        var player = context.Session.Player ?? throw new InvalidOperationException("Jugador fuera del mundo.");
        try
        {
            if (!Enum.IsDefined(packet.Attack) || packet.Target.Value <= 0)
                throw new InvalidDataException("Ataque de desarrollo inválido.");
            var currentMap = world.GetMap(player.MapInstanceId);
            if (!currentMap.Entities.TryGet(packet.Target, out var entity) || entity is not Mob target)
                throw new InvalidOperationException("El objetivo de prueba no es un mob disponible.");
            if (!target.IsAlive) throw new InvalidOperationException("El objetivo de prueba ya está derrotado.");

            var formula = packet.Attack switch
            {
                DevelopmentAttackKind.Basic => TrainingDummyFixture.BasicTestAttack(),
                DevelopmentAttackKind.Earth => TrainingDummyFixture.EarthTechnique(),
                DevelopmentAttackKind.Fire => TrainingDummyFixture.FireTechnique(),
                DevelopmentAttackKind.Air => TrainingDummyFixture.AirTechnique(),
                DevelopmentAttackKind.Water => TrainingDummyFixture.WaterTechnique(),
                DevelopmentAttackKind.NeutralStrength => TrainingDummyFixture.NeutralStrengthTechnique(),
                _ => throw new InvalidDataException("Ataque de desarrollo desconocido.")
            };

            var now = Environment.TickCount64;
            var result = combat.ExecuteAttributeDamage(player, target, formula, now);
            var meter = combat.Telemetry.Snapshot(target.Id, now) ?? new DamageMeterSnapshot(
                result.RawDamage, result.AppliedDamage, result.Element, result.Critical, 0, 0,
                result.AppliedDamage, result.AppliedDamage > 0 ? 1 : 0, result.Critical ? 1 : 0);
            context.Send(new CombatDebugPacket(
                target.Id, packet.Attack, formula.DamageType, formula.ScalingAttribute,
                result.RawDamage, result.ResistancePercent, result.AppliedDamage, result.Critical,
                target.Health, target.MaxHealth, meter.Dps5Seconds, meter.Dps10Seconds,
                meter.TotalDamage, meter.Hits, meter.CriticalHits));

            if (result.Killed && target.ExperienceReward > 0)
            {
                progression.GrantExperience(player, target.ExperienceReward);
                await persistence.SaveCharacterAsync(player, cancellationToken);
                context.Send(PlayerStatsProjection.Create(player, progression));
            }
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException or OverflowException or KeyNotFoundException)
        {
            context.Send(new ErrorPacket("development_combat", exception.Message, false));
        }
    }
}

public sealed class BasicAttackHandler(WorldRuntime world, AuthorizationService authorization)
    : IPacketHandler<ServerPacketContext, BasicAttackRequest>
{
    public ValueTask HandleAsync(ServerPacketContext context, BasicAttackRequest packet, CancellationToken cancellationToken)
    {
        ServerAuthorizationGuard.Demand(context, ServerAction.UseTechnique, authorization);
        var result = world.BasicAttack(context.Session, packet.Target);
        if (!result.Success) context.Send(new ErrorPacket("basic_attack", result.Message, false));
        else TechniqueFeedback.Send(context, world, packet.Target, result);
        return ValueTask.CompletedTask;
    }
}

public sealed class UseTechniqueHandler(WorldRuntime world, AuthorizationService authorization)
    : IPacketHandler<ServerPacketContext, UseTechniqueRequest>
{
    public ValueTask HandleAsync(ServerPacketContext context, UseTechniqueRequest packet, CancellationToken cancellationToken)
    {
        ServerAuthorizationGuard.Demand(context, ServerAction.UseTechnique, authorization);
        var result = world.UseTechnique(context.Session, packet.TechniqueId, packet.Target, packet.Point);
        if (!result.Success) context.Send(new ErrorPacket("use_technique", result.Message, false));
        else TechniqueFeedback.Send(context, world, packet.Target, result);
        return ValueTask.CompletedTask;
    }
}

public sealed class InteractHandler(WorldRuntime world, AuthorizationService authorization)
    : IPacketHandler<ServerPacketContext, InteractRequest>
{
    public ValueTask HandleAsync(ServerPacketContext context, InteractRequest packet, CancellationToken cancellationToken)
    {
        ServerAuthorizationGuard.Demand(context, ServerAction.Interact, authorization);
        var result = world.Interact(context.Session, packet.Target);
        if (!result.Success) context.Send(new ErrorPacket("interact", result.Message, false));
        else if (!string.IsNullOrWhiteSpace(result.Message)) context.Send(new ErrorPacket("interact_ok", result.Message, false));
        return ValueTask.CompletedTask;
    }
}

public sealed class SetTargetHandler(WorldRuntime world, AuthorizationService authorization)
    : IPacketHandler<ServerPacketContext, SetTargetRequest>
{
    public ValueTask HandleAsync(ServerPacketContext context, SetTargetRequest packet, CancellationToken cancellationToken)
    {
        ServerAuthorizationGuard.Demand(context, ServerAction.UseTechnique, authorization);
        try { world.SetTarget(context.Session, packet.Target); }
        catch (Exception exception) when (exception is InvalidOperationException or KeyNotFoundException)
        {
            context.Send(new ErrorPacket("target", exception.Message, false));
        }
        return ValueTask.CompletedTask;
    }
}

file static class TechniqueFeedback
{
    public static void Send(ServerPacketContext context, WorldRuntime world, EntityId targetId, TechniqueUseResult result)
    {
        var damage = result.Actions.Select(static action => action.Damage).FirstOrDefault(static value => value is not null);
        if (damage is null) return;
        var health = 0;
        var maxHealth = 0;
        if (context.Session.Player is { } player)
        {
            var currentMap = world.GetMap(player.MapInstanceId);
            if (currentMap.Entities.TryGet(targetId, out var entity) && entity is LivingEntity living)
            {
                health = living.Health;
                maxHealth = living.MaxHealth;
            }
        }
        context.Send(new CombatDebugPacket(
            targetId, DevelopmentAttackKind.Basic, damage.Element, PrimaryAttributeId.Strength,
            damage.RawDamage, damage.ResistancePercent, damage.AppliedDamage, damage.Critical,
            health, maxHealth, 0, 0, damage.AppliedDamage, damage.AppliedDamage > 0 ? 1 : 0, damage.Critical ? 1 : 0));
    }
}
