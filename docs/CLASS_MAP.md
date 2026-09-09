# Clases de la primera base MMO

| Proyecto | Clases principales | Responsabilidad |
| --- | --- | --- |
| NuevoMMO.Contracts | IDs tipados, WorldPosition, HandshakeRequest/Accepted/Rejected, MoveCommand, WorldSnapshot, EntityProjection, MovementCorrection | Comunicación y primitivas; sin reglas de gameplay |
| NuevoMMO.Protocol | PacketCodec, Frames | Serialización binaria explícita, versión, longitudes y validación estructural |
| NuevoMMO.Domain | PlayerEntity, MapDefinition, MovementInputBuffer | Estado de entidad/mapa e invariantes de input |
| NuevoMMO.Simulation | MovementSystem | Consumo de input por tick, normalización y límites del mapa |
| NuevoMMO.Application | WorldRuntime, WorldOptions, PlayerSession, CommandRouter, MovementCommandHandler, SpatialIndex, ReplicationSystem | Coordinación, sesiones, aplicación de comandos y proyección por AOI |
| NuevoMMO.Server | ServerHost, PeerConnection | Loopback TCP, handshake, rate limits, tick, backpressure y cierre |
| Fixtures (host dev/test) | DevelopmentWorldFactory | Composición con datos sintéticos; no ensamblada en Domain/Application |
| NuevoMMO.Client.Core | GameConnection, ClientWorldState, MovementPredictor, InterpolationBuffer | Conexión, caché visual, predicción/replay e interpolación; sin Godot |
| NuevoMMO.Client | NetworkBridge, MmoGame, WorldPresentation, PlayerView, AssetRegistry | Puente de señales, UI/input, cámara, sprites/animación |
| Tests | Suite de consola + ClientSmokeTest de Debug | Protocolo/movimiento/sesiones, TCP real y dos procesos Godot |

Feature: movimiento de desarrollo. **Owner**: MovementSystem del servidor. **Client-only**: entrada, cámara, animación, predicción e interpolación. **Shared**: IDs, vectores y DTOs. **GameData**: mapa y parámetros sintéticos de server/Fixtures/movement.json. **GameState**: entidades/sesiones en memoria. **Replication**: snapshot inicial, upserts/despawns, corrección privada. **Persistence**: ninguna todavía. **Audit**: no hay acciones económicas ni de producción en este slice.
