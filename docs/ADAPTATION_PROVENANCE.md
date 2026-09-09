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

El nuevo código no referencia archivos fuera del repositorio. GodotMMO y Broken_Reborn no son dependencias de compilación ni runtime. Se conserva la licencia del template y sus componentes. La inspección conceptual no autoriza a importar reglas de juego.
