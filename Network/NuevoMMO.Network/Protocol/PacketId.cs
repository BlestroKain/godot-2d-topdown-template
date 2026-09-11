namespace NuevoMMO.Network;

public enum PacketId : ushort
{
    ConnectRequest = 1, LoginRequest = 2, CharacterListRequest = 3, CreateCharacterRequest = 4,
    CharacterSelectRequest = 5, MapReadyRequest = 6, MoveRequest = 7, Ping = 8, DisconnectRequest = 9,
    RegisterRequest = 10, AllocateAttributeRequest = 11,
    ConnectionAccepted = 100, LoginResult = 101, CharacterListResult = 102, CharacterCreated = 103,
    CharacterSelected = 104, MapLoad = 105, SpawnEntity = 106, DespawnEntity = 107,
    EntityMoved = 108, EntityState = 109, ServerTime = 110, Pong = 111, Error = 112,
    RegisterResult = 113, PlayerStats = 114
}
