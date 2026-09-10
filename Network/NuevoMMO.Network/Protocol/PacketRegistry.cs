namespace NuevoMMO.Network;

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
