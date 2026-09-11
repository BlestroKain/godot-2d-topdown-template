using NuevoMMO.Network;
using NuevoMMO.Server.Database;
using NuevoMMO.Server.Security;
using NuevoMMO.Server.Services;
using NuevoMMO.Server.Systems;
using NuevoMMO.Server.World;

namespace NuevoMMO.Server.NetworkHandlers;

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

/// <summary>
/// Composition root for authoritative packet handlers. Kept in its own file so feature-specific
/// handler edits cannot accidentally remove the registry tail from Handlers.cs.
/// </summary>
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
        BanList? bans = null,
        ProgressionSystem? progression = null,
        CombatSystem? combat = null)
    {
        authorization ??= new AuthorizationService();
        abuse ??= new AbuseDetector();
        settings ??= new AuthenticationSettings();
        bans ??= new BanList();
        progression ??= world.Systems?.Progression ?? new ProgressionSystem();
        combat ??= world.Systems?.Combat ?? new CombatSystem();
        settings.Validate();

        var registry = new HandlerRegistry<ServerPacketContext>();
        registry.Register(new ConnectHandler(world, authorization));
        registry.Register(new RegisterHandler(auth, authorization, abuse, settings));
        registry.Register(new LoginHandler(auth, authorization, abuse, settings, bans));
        registry.Register(new CharacterListHandler(characters, authorization));
        registry.Register(new CharacterCreateHandler(characters, authorization));
        registry.Register(new CharacterSelectHandler(world, characters, persistence, progression, authorization));
        registry.Register(new MapReadyHandler(world, authorization));
        registry.Register(new MoveRequestHandler(world, authorization));
        registry.Register(new AllocateAttributeHandler(progression, persistence, authorization));
        registry.Register(new DevelopmentAttackHandler(world, combat, progression, persistence, authorization));
        registry.Register(new BasicAttackHandler(world, authorization));
        registry.Register(new UseTechniqueHandler(world, authorization));
        registry.Register(new InteractHandler(world, authorization));
        registry.Register(new SetTargetHandler(world, authorization));
        registry.Register(new PingHandler());
        registry.Register(new DisconnectHandler());
        return registry;
    }
}
