# GameData

Archivo: `Data/game.db` (SQLite). Es la fuente de verdad de contenido, al patrón Intersect/Broken Reborn:

- Una tabla por tipo: `Maps`, `Items`, `Mobs`, `Npcs`, `Resources`, `Techniques`, `Effects`, `Events`, `Quests`, `Tilesets`, etc.
- Columnas de identidad (`id`, `key`, `name`, `enabled`, `version`) y `json` con el grafo anidado de la Definition.
- `content_meta` guarda `format_version` y `package_version`.
- La tabla legacy `definitions` se sigue escribiendo para compatibilidad.

El Editor guarda un objeto al pulsar Guardar en su formulario (`Upsert` de esa fila). Archivo → Guardar reescribe el paquete completo.

El servidor carga a RAM al arrancar (`GameDataSqlite` → `DefinitionRegistry`). Los sistemas no consultan SQL por tick.

`auth.db`, `players.db` y `logs.db` son otras bases (cuentas, personajes, telemetría), no contenido de diseño.

JSON suelto queda como fixture de tests, no como archivo de edición.
