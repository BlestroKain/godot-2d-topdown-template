# Procedencia de la adaptación

Revisión: 2026-09-09. Repositorio de destino: H:\godot-2d-topdown-template.

| Fuente revisada | Uso en esta entrega |
| --- | --- |
| H:\GodotMMO\shared\Transport\Frames.cs | Adaptación propia del framing limitado sobre Stream; nuevo codec/contratos independientes |
| H:\GodotMMO\client\GameConnection.cs | Adaptación propia de conexión TCP, timeout, un escritor y cancelación; sin gameplay, tokens ni estado persistente local |
| H:\GodotMMO\server\Game.Server.Domain\MovementInputBuffer.cs | Adaptación propia de cola por sesión; secuencias ahora contiguas y ack al procesar |
| H:\GodotMMO\server\Services\MovementService.cs | Referencia para separar simulación del arribo de paquetes y normalizar diagonales; sistema mínimo nuevo |
| H:\GodotMMO\server\Entities\PlayerSession.cs | Referencia para separar conexión y entidad; sin trasladar proxies de inventario/stats |
| H:\Broken_Reborn\Intersect.Server.Core\Entities\Entity.cs y Player.cs | Referencia conceptual de entidad y ciclo de actualización; implementación propia pequeña, sin copiar el modelo ni gameplay |
| H:\Broken_Reborn\Intersect.Server.Core\Networking\PacketHandler.cs | Referencia conceptual del límite de entrada; router/handlers nuevos separados del transporte |
| client/entities/player/player.tscn | SpriteFrames originales extraídos reproduciblemente a template_player_frames.tres; solo idle/walk activados |
| client/scripts/autoloads/Globals.gd y escenas de preferencias | Reutilizados para ajustes; GameState sigue bloqueado localmente |
| peter-kish/gloot `6b09b87` (MIT) | Addon vendorizado 3.0.2; `ServerInventoryProjection` lo alimenta exclusivamente desde snapshots autoritativos |
| derkork/godot-statecharts `76d226a` (MIT) | Addon vendorizado 0.22.5; flujo visual Frontend/World/SettingsOverWorld declarado en escena y coordinado desde C# |
| shomykohai/quest-system `d1d933c` (MIT) | Patrones de Resources, pools, señales y localización; integración directa diferida para no duplicar QuestSystem autoritativo |
| Relintai/entity_spell_system `3c240ff` (MIT) | Referencia de ResourceDB/IDs/abilities; sin dependencia por migración Godot 4 inconclusa |
| Quaint-Studios/Reia `82fd21f` (AGPL-3.0) | Referencia arquitectónica solamente; no se copió código |

El nuevo código no referencia archivos fuera del repositorio. GodotMMO y Broken_Reborn no son dependencias de compilación ni runtime. Se conserva la licencia del template y sus componentes. La inspección conceptual no autoriza a importar reglas de juego.
