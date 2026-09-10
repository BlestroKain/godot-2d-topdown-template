using NuevoMMO.Core;

namespace NuevoMMO.Network;

public interface IPacket { }

public enum PacketId : ushort
{
    ConnectRequest = 1, LoginRequest = 2, CharacterListRequest = 3, CreateCharacterRequest = 4,
    CharacterSelectRequest = 5, MapReadyRequest = 6, MoveRequest = 7, Ping = 8, DisconnectRequest = 9,
    ConnectionAccepted = 100, LoginResult = 101, CharacterListResult = 102, CharacterCreated = 103,
    CharacterSelected = 104, MapLoad = 105, SpawnEntity = 106, DespawnEntity = 107,
    EntityMoved = 108, EntityState = 109, ServerTime = 110, Pong = 111, Error = 112
}

public enum PacketDirection : byte { ClientToServer, ServerToClient, Bidirectional }
public readonly record struct PacketHeader(ushort Version, PacketId PacketId, uint PayloadLength)
{
    public const int Size = 12;
}

public sealed record ConnectRequest(string ClientVersion, ushort ProtocolVersion) : IPacket;
public sealed class LoginRequest(string username, string password) : IPacket
{
    public string Username { get; } = username;
    public string Password { get; } = password;
    public override string ToString() => $"LoginRequest {{ Username = {Username}, Password = [REDACTED] }}";
}
public sealed class CharacterListRequest(SessionId session, string sessionToken) : IPacket
{
    public SessionId Session { get; } = session;
    public string SessionToken { get; } = sessionToken;
    public override string ToString() => $"CharacterListRequest {{ Session = {Session}, SessionToken = [REDACTED] }}";
}
public sealed class CreateCharacterRequest(SessionId session, string sessionToken, string name) : IPacket
{
    public SessionId Session { get; } = session;
    public string SessionToken { get; } = sessionToken;
    public string Name { get; } = name;
    public override string ToString() => $"CreateCharacterRequest {{ Session = {Session}, SessionToken = [REDACTED], Name = {Name} }}";
}
public sealed class CharacterSelectRequest(SessionId session, string sessionToken, CharacterId character) : IPacket
{
    public SessionId Session { get; } = session;
    public string SessionToken { get; } = sessionToken;
    public CharacterId Character { get; } = character;
    public override string ToString() => $"CharacterSelectRequest {{ Session = {Session}, SessionToken = [REDACTED], Character = {Character} }}";
}
public sealed record MapReadyRequest(MapInstanceId Instance) : IPacket;
public sealed record InputFrame(long Sequence, long ClientTick, float X, float Y);
public sealed record MoveRequest(InputFrame Input) : IPacket;
public sealed record PingPacket(long Nonce, long ClientSendTimestamp) : IPacket;
public sealed record DisconnectRequest(string Reason) : IPacket;

public sealed record ConnectionAccepted(ConnectionId Connection, long ServerTimestamp, ushort ProtocolVersion) : IPacket;
public sealed class LoginResult(bool succeeded, string error, AccountId account, SessionId session, string sessionToken) : IPacket
{
    public bool Succeeded { get; } = succeeded;
    public string Error { get; } = error;
    public AccountId Account { get; } = account;
    public SessionId Session { get; } = session;
    public string SessionToken { get; } = sessionToken;
    public override string ToString() => $"LoginResult {{ Succeeded = {Succeeded}, Error = {Error}, Account = {Account}, Session = {Session}, SessionToken = [REDACTED] }}";
}
public sealed record CharacterSummary(CharacterId Id, string Name, DefinitionId MapDefinition, Vector2Data Position);
public sealed record CharacterListResult(CharacterSummary[] Characters) : IPacket;
public sealed record CharacterCreated(CharacterSummary Character) : IPacket;
public sealed record CharacterSelected(CharacterId Character) : IPacket;
public sealed record MapProjection(DefinitionId Definition, MapInstanceId Instance, ContentKey VisualKey,
    BoundsData Bounds, float MovementSpeed, int TickMilliseconds, string ContentVersion);
public sealed record MapLoadPacket(MapProjection Map, EntityId Self, CharacterId Character) : IPacket;
public sealed record SpawnEntityPacket(EntityState Entity) : IPacket;
public sealed record DespawnEntityPacket(EntityId Entity) : IPacket;
public sealed record EntityMovedPacket(EntityId Entity, Vector2Data Position, Vector2Data Velocity, long ServerTick) : IPacket;
public sealed record MovementCorrection(EntityId Self, Vector2Data Position, long LastProcessedInput);
public sealed record EntityStatePacket(long Tick, bool Full, MovementCorrection Correction,
    EntityState[] Upserts, EntityId[] Despawns) : IPacket;
public sealed record ServerTimePacket(long ServerTimestamp, long ServerTick) : IPacket;
public sealed record PongPacket(long Nonce, long ClientSendTimestamp, long ServerReceiveTimestamp, long ServerSendTimestamp) : IPacket;
public sealed record ErrorPacket(string Code, string Message, bool Fatal) : IPacket;

public static class PacketRegistry
{
    private static readonly IReadOnlyDictionary<Type, (PacketId Id, PacketDirection Direction)> ByType =
        new Dictionary<Type, (PacketId, PacketDirection)>
        {
            [typeof(ConnectRequest)] = (PacketId.ConnectRequest, PacketDirection.ClientToServer),
            [typeof(LoginRequest)] = (PacketId.LoginRequest, PacketDirection.ClientToServer),
            [typeof(CharacterListRequest)] = (PacketId.CharacterListRequest, PacketDirection.ClientToServer),
            [typeof(CreateCharacterRequest)] = (PacketId.CreateCharacterRequest, PacketDirection.ClientToServer),
            [typeof(CharacterSelectRequest)] = (PacketId.CharacterSelectRequest, PacketDirection.ClientToServer),
            [typeof(MapReadyRequest)] = (PacketId.MapReadyRequest, PacketDirection.ClientToServer),
            [typeof(MoveRequest)] = (PacketId.MoveRequest, PacketDirection.ClientToServer),
            [typeof(PingPacket)] = (PacketId.Ping, PacketDirection.ClientToServer),
            [typeof(DisconnectRequest)] = (PacketId.DisconnectRequest, PacketDirection.ClientToServer),
            [typeof(ConnectionAccepted)] = (PacketId.ConnectionAccepted, PacketDirection.ServerToClient),
            [typeof(LoginResult)] = (PacketId.LoginResult, PacketDirection.ServerToClient),
            [typeof(CharacterListResult)] = (PacketId.CharacterListResult, PacketDirection.ServerToClient),
            [typeof(CharacterCreated)] = (PacketId.CharacterCreated, PacketDirection.ServerToClient),
            [typeof(CharacterSelected)] = (PacketId.CharacterSelected, PacketDirection.ServerToClient),
            [typeof(MapLoadPacket)] = (PacketId.MapLoad, PacketDirection.ServerToClient),
            [typeof(SpawnEntityPacket)] = (PacketId.SpawnEntity, PacketDirection.ServerToClient),
            [typeof(DespawnEntityPacket)] = (PacketId.DespawnEntity, PacketDirection.ServerToClient),
            [typeof(EntityMovedPacket)] = (PacketId.EntityMoved, PacketDirection.ServerToClient),
            [typeof(EntityStatePacket)] = (PacketId.EntityState, PacketDirection.ServerToClient),
            [typeof(ServerTimePacket)] = (PacketId.ServerTime, PacketDirection.ServerToClient),
            [typeof(PongPacket)] = (PacketId.Pong, PacketDirection.ServerToClient),
            [typeof(ErrorPacket)] = (PacketId.Error, PacketDirection.ServerToClient)
        };

    public static (PacketId Id, PacketDirection Direction) Describe(IPacket packet) =>
        ByType.TryGetValue(packet.GetType(), out var value) ? value : throw new InvalidDataException("Tipo de paquete no registrado.");
    public static PacketDirection Direction(PacketId id) => ByType.Values.FirstOrDefault(value => value.Id == id).Direction;
}
