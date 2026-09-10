# Dependencias

- Core no depende de Godot, Network, Server, Client ni Editor.
- Network depende de Core.
- Server depende de Core + Network.
- Client depende de Core + Network. Godot queda en el proyecto cliente.
- Editor depende de Core + Network.
- Server es autoridad absoluta. Client nunca decide estado real.
- Network nunca procesa gameplay.
- Editor nunca contiene lógica de simulación.
