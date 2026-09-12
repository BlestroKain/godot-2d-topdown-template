namespace NuevoMMO.Network;

public static class PacketRegistry
{
    private static readonly IReadOnlyDictionary<Type, (PacketId Id, PacketDirection Direction)> ByType =
        new Dictionary<Type, (PacketId, PacketDirection)>
        {
            [typeof(ConnectRequest)] = (PacketId.ConnectRequest, PacketDirection.ClientToServer),
            [typeof(LoginRequest)] = (PacketId.LoginRequest, PacketDirection.ClientToServer),
            [typeof(RegisterRequest)] = (PacketId.RegisterRequest, PacketDirection.ClientToServer),
            [typeof(CharacterListRequest)] = (PacketId.CharacterListRequest, PacketDirection.ClientToServer),
            [typeof(CreateCharacterRequest)] = (PacketId.CreateCharacterRequest, PacketDirection.ClientToServer),
            [typeof(CharacterSelectRequest)] = (PacketId.CharacterSelectRequest, PacketDirection.ClientToServer),
            [typeof(MapReadyRequest)] = (PacketId.MapReadyRequest, PacketDirection.ClientToServer),
            [typeof(MoveRequest)] = (PacketId.MoveRequest, PacketDirection.ClientToServer),
            [typeof(PingPacket)] = (PacketId.Ping, PacketDirection.ClientToServer),
            [typeof(DisconnectRequest)] = (PacketId.DisconnectRequest, PacketDirection.ClientToServer),
            [typeof(AllocateAttributeRequest)] = (PacketId.AllocateAttributeRequest, PacketDirection.ClientToServer),
            [typeof(DevelopmentAttackRequest)] = (PacketId.DevelopmentAttackRequest, PacketDirection.ClientToServer),
            [typeof(BasicAttackRequest)] = (PacketId.BasicAttackRequest, PacketDirection.ClientToServer),
            [typeof(UseTechniqueRequest)] = (PacketId.UseTechniqueRequest, PacketDirection.ClientToServer),
            [typeof(InteractRequest)] = (PacketId.InteractRequest, PacketDirection.ClientToServer),
            [typeof(SetTargetRequest)] = (PacketId.SetTargetRequest, PacketDirection.ClientToServer),
            [typeof(EquipItemRequest)] = (PacketId.EquipItemRequest, PacketDirection.ClientToServer),
            [typeof(UnequipItemRequest)] = (PacketId.UnequipItemRequest, PacketDirection.ClientToServer),
            [typeof(MoveInventoryItemRequest)] = (PacketId.MoveInventoryItemRequest, PacketDirection.ClientToServer),
            [typeof(ConnectionAccepted)] = (PacketId.ConnectionAccepted, PacketDirection.ServerToClient),
            [typeof(LoginResult)] = (PacketId.LoginResult, PacketDirection.ServerToClient),
            [typeof(RegisterResult)] = (PacketId.RegisterResult, PacketDirection.ServerToClient),
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
            [typeof(ErrorPacket)] = (PacketId.Error, PacketDirection.ServerToClient),
            [typeof(PlayerStatsPacket)] = (PacketId.PlayerStats, PacketDirection.ServerToClient),
            [typeof(CombatDebugPacket)] = (PacketId.CombatDebug, PacketDirection.ServerToClient),
            [typeof(InventorySnapshotPacket)] = (PacketId.InventorySnapshot, PacketDirection.ServerToClient)
        };

    public static (PacketId Id, PacketDirection Direction) Describe(IPacket packet) =>
        ByType.TryGetValue(packet.GetType(), out var value) ? value : throw new InvalidDataException("Tipo de paquete no registrado.");
    public static PacketDirection Direction(PacketId id) => ByType.Values.FirstOrDefault(value => value.Id == id).Direction;
}
