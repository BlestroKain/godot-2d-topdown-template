# Arquitectura

Core define el mundo. Network define cómo viaja la información. Server decide la verdad. Client representa esa verdad. Editor crea el contenido.

```text
Definitions → Server Entities → States → Packets → Client GameState → Views
```

Dependencias:

```text
Core
  ↑
Network
 ↑   ↑
Server Client
   ↑
 Editor
```
