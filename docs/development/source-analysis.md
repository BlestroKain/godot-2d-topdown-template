# Source analysis

## Template Godot 2D top-down

KEEP: movimiento visual, mapas, cámara, sprites, escenas, input, tiles, SpriteFrames extraídos.
ADAPT: controller de jugador a intención de red; escena MMO C# separada.
REPLACE: autoridad de combate, loot y guardado local.
DISCARD: demo como canon de gameplay.
REFERENCE ONLY: HUD single-player, inventario local.

## Intersect Engine

KEEP as reference: separación Core/Server/Client/Editor, Definitions, GameData vs PlayerData, CRUD, Map Editor.
REPLACE: PacketHandler monolítico, dependencias MonoGame.
DISCARD: lore, sistemas de Broken, arquitectura antigua innecesaria.

## Netmaker

KEEP as reference: ENet wrapper, peer management, Handler → Service → Repository, account/character/map lifecycle.
REPLACE: `var_to_bytes` / hashes de funciones como protocolo.
DISCARD: todos los mensajes Reliable.
