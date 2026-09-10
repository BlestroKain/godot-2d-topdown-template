using NuevoMMO.Core;
using NuevoMMO.Network;
using NuevoMMO.Server.Database;
using NuevoMMO.Server.Services;
using NuevoMMO.Server.World;

namespace NuevoMMO.Server.NetworkHandlers;

public sealed class ServerPacketContext
{
    public required ConnectionId Connection { get; init; }
    public required PlayerSession Session { get; init; }
    public required Action<IPacket> Send { get; init; }
}

public sealed class ConnectHandler : IPacketHandler<ServerPacketContext, ConnectRequest>
{
    public ValueTask HandleAsync(ServerPacketContext context, ConnectRequest packet, CancellationToken cancellationToken)
    {
        if (context.Session.State != PlayerSessionState.Connected) throw new InvalidOperationException("Connect repetido.");
        if (!HandshakeRules.IsCompatible(packet.ProtocolVersion)) throw new InvalidDataException("Protocolo incompatible.");
        context.Session.State = PlayerSessionState.ProtocolAccepted;
        context.Send(new ConnectionAccepted(context.Connection, NetworkClock.Timestamp, ProtocolVersion.Current));
        return ValueTask.CompletedTask;
    }
}

public sealed class RegisterHandler(AuthService auth) : IPacketHandler<ServerPacketContext, RegisterRequest>
{
    public async ValueTask HandleAsync(ServerPacketContext context, RegisterRequest packet, CancellationToken cancellationToken)
    {
        if (context.Session.State != PlayerSessionState.ProtocolAccepted) throw new InvalidOperationException("Registro fuera de orden.");
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

public sealed class LoginHandler(AuthService auth) : IPacketHandler<ServerPacketContext, LoginRequest>
{
    public async ValueTask HandleAsync(ServerPacketContext context, LoginRequest packet, CancellationToken cancellationToken)
    {
        if (context.Session.State != PlayerSessionState.ProtocolAccepted) throw new InvalidOperationException("Login fuera de orden.");
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

public sealed class CharacterListHandler(CharacterService characters) : IPacketHandler<ServerPacketContext, CharacterListRequest>
{
    public async ValueTask HandleAsync(ServerPacketContext context, CharacterListRequest packet, CancellationToken cancellationToken)
    {
        AuthService.EnsureSession(context.Session, packet.Session, packet.SessionToken);
        var list = await characters.ListAsync(context.Session.Account, cancellationToken);
        context.Send(new CharacterListResult(list.Select(CharacterService.ToSummary).ToArray()));
    }
}

public sealed class CharacterCreateHandler(CharacterService characters) : IPacketHandler<ServerPacketContext, CreateCharacterRequest>
{
    public async ValueTask HandleAsync(ServerPacketContext context, CreateCharacterRequest packet, CancellationToken cancellationToken)
    {
        AuthService.EnsureSession(context.Session, packet.Session, packet.SessionToken);
        var created = await characters.CreateAsync(context.Session.Account, packet.Name, cancellationToken);
        context.Send(new CharacterCreated(CharacterService.ToSummary(created)));
    }
}

public sealed class CharacterSelectHandler(WorldRuntime world, CharacterService characters, PersistenceService persistence)
    : IPacketHandler<ServerPacketContext, CharacterSelectRequest>
{
    public async ValueTask HandleAsync(ServerPacketContext context, CharacterSelectRequest packet, CancellationToken cancellationToken)
    {
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

public sealed class MapReadyHandler(WorldRuntime world) : IPacketHandler<ServerPacketContext, MapReadyRequest>
{
    public ValueTask HandleAsync(ServerPacketContext context, MapReadyRequest packet, CancellationToken cancellationToken)
    {
        world.Activate(context.Session, packet.Instance);
        return ValueTask.CompletedTask;
    }
}

public sealed class MoveRequestHandler(WorldRuntime world) : IPacketHandler<ServerPacketContext, MoveRequest>
{
    public ValueTask HandleAsync(ServerPacketContext context, MoveRequest packet, CancellationToken cancellationToken)
    {
        world.SubmitMovement(context.Session, packet.Input);
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
    public static HandlerRegistry<ServerPacketContext> Create(WorldRuntime world, AuthService auth, CharacterService characters,
        PersistenceService persistence)
    {
        var registry = new HandlerRegistry<ServerPacketContext>();
        registry.Register(new ConnectHandler());
        registry.Register(new RegisterHandler(auth));
        registry.Register(new LoginHandler(auth));
        registry.Register(new CharacterListHandler(characters));
        registry.Register(new CharacterCreateHandler(characters));
        registry.Register(new CharacterSelectHandler(world, characters, persistence));
        registry.Register(new MapReadyHandler(world));
        registry.Register(new MoveRequestHandler(world));
        registry.Register(new PingHandler());
        registry.Register(new DisconnectHandler());
        return registry;
    }
}
