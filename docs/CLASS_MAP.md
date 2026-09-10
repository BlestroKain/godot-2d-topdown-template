# Clases de la primera base MMO

| Proyecto | Clases principales | Responsabilidad |
| --- | --- | --- |
| NuevoMMO.Core | IDs, GameDefinition y derivadas, States, Stats, ItemInstance, DefinitionRegistry, Vector2Data | Contratos y datos compartidos; sin Godot ni gameplay |
| NuevoMMO.Network | PacketId/Header, PacketCodec, INetworkTransport, EnetTransport, PacketDispatcher, NetworkClock | Protocolo, serialización y transporte; sin gameplay |
| NuevoMMO.Server | Entity/LivingEntity/Player/Mob, WorldRuntime, MovementSystem, ServerHost, Auth/Character services, repositorios | Autoridad, simulación, persistencia y host |
| NuevoMMO.Client.Core | GameConnection, ClientGameState, LocalMovementPrediction, Interpolation, ClientEntityManager | Estado visual, predicción y conexión; sin Godot |
| NuevoMMO.Client | NetworkBridge, MmoGame, WorldPresentation, PlayerView, views, input, debug | Representación Godot |
| NuevoMMO.Editor | Definition editors, MapEditor, validators, history | Contenido; sin simulación |
| Tests | Suite de consola + ClientSmokeTest | Protocolo, movimiento, registry, persistencia y TCP real |

Feature: movimiento de desarrollo. **Owner**: MovementSystem del servidor. **Client-only**: entrada, cámara, animación, predicción e interpolación. **Shared**: IDs, vectores y DTOs. **GameData**: mapa, mob y parámetros sintéticos de `Server/Fixtures/movement.json`. **GameState**: entidades/sesiones en memoria. **Replication**: snapshot inicial, upserts/despawns, corrección privada. **Persistence**: Account/Character/posición en memoria; schema PostgreSQL preparado. **Audit**: no hay acciones económicas ni de producción en este slice.
