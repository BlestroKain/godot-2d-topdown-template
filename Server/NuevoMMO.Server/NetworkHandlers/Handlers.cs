using NuevoMMO.Core;
using NuevoMMO.Network;
using NuevoMMO.Server.Database;
using NuevoMMO.Server.Security;
using NuevoMMO.Server.Services;
using NuevoMMO.Server.World;

namespace NuevoMMO.Server.NetworkHandlers;

public sealed class ServerPacketContext
{
    public required ConnectionId Connection { get; init; }
    public required PlayerSession Session { get; init; }
    public required Action<IPacket> Send { get; init; }
}

public sealed class ConnectHandler(WorldRuntime world, AuthorizationService authorization)
    : IPacketHandler<ServerPacketContext, ConnectRequest>
{
    public ValueTask HandleAsync(ServerPacketContext context, ConnectRequest packet, CancellationToken cancellationToken)
    {
        Demand(context, ServerAction.AcceptProtocol, authorization);
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
        Demand(context, ServerAction.Register, authorization);
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
        Demand(context, ServerAction.Login, authorization);
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
            worldAuthenticate(context, account.Id, session.Id, session.Token);
            context.Send(new LoginResult(true, "", account.Id, session.Id, session.Token));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            context.Send(new LoginResult(false, exception.Message, default, default, ""));
        }

        void worldAuthenticate(ServerPacketContext packetContext, AccountId accountId, SessionId sessionId, string token)
        {
            packetContext.Session.Account = accountId;
            packetContext.Session.Session = sessionId;
            packetContext.Session.SessionToken = token;
            packetContext.Session.State = PlayerSessionState.Authenticated;
        }
    }
}

public sealed class CharacterListHandler(CharacterService characters, AuthorizationService authorization)
    : IPacketHandler<ServerPacketContext, CharacterListRequest>
{
    public async ValueTask HandleAsync(ServerPacketContext context, CharacterListRequest packet, CancellationToken cancellationToken)
    {
        Demand(context, ServerAction.ListCharacters, authorization);
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
        Demand(context, ServerAction.CreateCharacter, authorization);
        AuthService.EnsureSession(context.Session, packet.Session, packet.SessionToken);
        if (!InputValidator.IsSafeName(packet.Name)) throw new ArgumentException("Nombre de personaje inválido.");
        var created = await characters.CreateAsync(context.Session.Account, packet.Name, cancellationToken);
        context.Send(new CharacterCreated(CharacterService.ToSummary(created)));
    }
}

public sealed class CharacterSelectHandler(
    WorldRuntime world,
    CharacterService characters,
    PersistenceService persistence,
    AuthorizationService authorization) : IPacketHandler<ServerPacketContext, CharacterSelectRequest>
{
    public async ValueTask HandleAsync(ServerPacketContext context, CharacterSelectRequest packet, CancellationToken cancellationToken)
    {
        Demand(context, ServerAction.SelectCharacter, authorization);
        AuthService.EnsureSession(context.Session, packet.Session, packet.SessionToken);
        var list = await characters.ListAsync(context.Session.Account, cancellationToken);
        var record = list.FirstOrDefault(character => character.Id == packet.Character)
            ?? throw new InvalidOperationException("Personaje inexistente.");
        var spawn = await persistence.LoadCharacterAsync(context.Session.Account, record, cancellationToken);
        var player = world.Join(context.Session, spawn);
        context.Send(new CharacterSelected(player.CharacterId));
        context.Send(new MapLoadPacket(world.Projection("dev-1"), player.Id, player.CharacterId));
    }
}

public sealed class MapReadyHandler(WorldRuntime world, AuthorizationService authorization)
    : IPacketHandler<ServerPacketContext, MapReadyRequest>
{
    public ValueTask HandleAsync(ServerPacketContext context, MapReadyRequest packet, CancellationToken cancellationToken)
    {
        Demand(context, ServerAction.EnterWorld, authorization);
        world.Activate(context.Session, packet.Instance);
        return ValueTask.CompletedTask;
    }
}

public sealed class MoveRequestHandler(WorldRuntime world, AuthorizationService authorization)
    : IPacketHandler<ServerPacketContext, MoveRequest>
{
    public ValueTask HandleAsync(ServerPacketContext context, MoveRequest packet, CancellationToken cancellationToken)
    {
        Demand(context, ServerAction.Move, authorization);
        var input = packet.Input ?? throw new InvalidDataException("Input de movimiento ausente.");
        var player = context.Session.Player ?? throw new InvalidOperationException("Jugador fuera del mundo.");
        if (!InputValidator.IsFiniteDirection(input.X, input.Y) || input.ClientTick < 0 ||
            input.Sequence != player.Inputs.LastAccepted + 1)
            throw new InvalidDataException("Input de movimiento inválido o fuera de orden.");
        world.SubmitMovement(context.Session, input);
        return ValueTask.CompletedTask;
    }
}

public sealed class PingHandler : IPacketHandler<ServerPacketContext, PingPacket>
{
    public ValueTask HandleAsync(ServerPacketContext context, PingPacket packet, CancellationToken cancellationToken)
    {
        var now = NetworkClock.Timestamp;
        context.Send(new PongPacket(packet.Nonce, packet.ClientSendTimestamp, now, now));
        return ValueTask.CompletedTask;
    }
}

public sealed class DisconnectHandler : IPacketHandler<ServerPacketContext, DisconnectRequest>
{
    public ValueTask HandleAsync(ServerPacketContext context, DisconnectRequest packet, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}

public static class ServerHandlerRegistry
{
    public static HandlerRegistry<ServerPacketContext> Create(
        WorldRuntime world,
        AuthService auth,
        CharacterService characters,
        PersistenceService persistence,
        AuthorizationService? authorization = null,
        AbuseDetector? abuse = null,
        AuthenticationSettings? settings = null,
        BanList? bans = null)
    {
        authorization ??= new AuthorizationService();
        abuse ??= new AbuseDetector();
        settings ??= new AuthenticationSettings();
        bans ??= new BanList();
        settings.Validate();

        var registry = new HandlerRegistry<ServerPacketContext>();
        registry.Register(new ConnectHandler(world, authorization));
        registry.Register(new RegisterHandler(auth, authorization, abuse, settings));
        registry.Register(new LoginHandler(auth, authorization, abuse, settings, bans));
        registry.Register(new CharacterListHandler(characters, authorization));
        registry.Register(new CharacterCreateHandler(characters, authorization));
        registry.Register(new CharacterSelectHandler(world, characters, persistence, authorization));
        registry.Register(new MapReadyHandler(world, authorization));
        registry.Register(new MoveRequestHandler(world, authorization));
        registry.Register(new PingHandler());
        registry.Register(new DisconnectHandler());
        return registry;
    }

    private static void Demand(ServerPacketContext context, ServerAction action, AuthorizationService authorization)
    {
        var decision = authorization.Authorize(context.Session, action);
        if (!decision.Allowed) throw new InvalidOperationException(decision.Reason);
    }
}
