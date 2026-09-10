# Client

Representación, input, predicción y vistas Godot.

- Packets actualizan ClientGameState. Views consumen State.
- Local player: predicción + reconciliación. Remotos: interpolación.
- La UI no habla con Network; NetworkBridge traduce paquetes a estado.
- El template Godot se conserva para arte, cámara, tiles y GDScript reutilizable.
