# Packets

Client → Server: ConnectRequest, LoginRequest, CharacterListRequest, CreateCharacterRequest, CharacterSelectRequest, MapReadyRequest, MoveRequest, Ping, DisconnectRequest.

Server → Client: ConnectionAccepted, LoginResult, CharacterListResult, CharacterCreated, CharacterSelected, MapLoad, SpawnEntity, DespawnEntity, EntityMoved, EntityState, ServerTime, Pong, Error.

MovementSnapshot / EntityState / EntityMoved usan DeliveryMode UnreliableSequenced en ENet. Inventario, spawn y chat usan Reliable.
