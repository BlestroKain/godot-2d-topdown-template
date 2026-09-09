# ADR-0002 — Primera implementación autoritativa en C#

Fecha: 2026-09-09. Estado: implementado y sujeto a las limitaciones de IMPLEMENTATION_STATUS.md.

La funcionalidad nueva usa C#, incluida la escena de entrada MMO. Se conservan los componentes GDScript del template para reutilización. **Superseded**: exigir GDScript para toda UI/presentación nueva; el usuario expresó preferencia por C#.

Se adaptan selectivamente Frames, GameConnection y el patrón de MovementInputBuffer del código propio de GodotMMO. No se migra su solución completa: arrastra dependencias de gameplay en contratos y cambios sin validación. Se estudian Entity/Player/PacketHandler de Broken_Reborn como distribución conceptual de responsabilidades. No se copia código, tipos ni dependencias de Intersect.

Los proyectos Contracts, Protocol, Domain, Simulation, Application, Server, Client.Core, Client Godot y Tests separan responsabilidades. Domain/Simulation no referencian Godot ni implementación de red. Client.Core permite probar reconciliación sin Godot.

El protocolo binario usa magic NMMO, versión 1 propia, framing big-endian y payload explícito little-endian. **Superseded para este repositorio**: compatibilidad implícita con versiones del prototipo GodotMMO. No existe compatibilidad de wire con ese prototipo; mezclar clientes/servidores se rechaza.

Una conexión de desarrollo recibe CharacterId temporal y EntityId de ejecución. No representa cuenta autenticada ni creación definitiva de personaje. Una reconexión crea nuevas identidades y retorna al spawn sintético. El host solo admite --development/--test y loopback.

Movimiento consume exactamente un input por tick. Secuencias contiguas, cola máxima de 32 y dirección finita acotada. El ack confirma únicamente inputs consumidos. El cliente predice y reproduce pendientes tras corrección; la posición visual nunca entra en un comando.

Un mapa técnico usa índice espacial de celdas para AOI y distancia exacta para filtrar. Snapshot inicial, upserts/despawns posteriores y corrección privada en cada tick. Una cola de salida llena desconecta al cliente lento: no se pierden deltas silenciosamente. El handshake se encola antes de activar la replicación de la sesión.

Persistencia, colisiones de tilemap, mundo regional, autenticación, interacciones y gameplay quedan fuera de esta entrega. No se convierten datos del rectángulo técnico en reglas de juego.
