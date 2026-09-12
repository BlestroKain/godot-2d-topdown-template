# Diseño vigente

Decisión del usuario, 2026-09-09: `H:\godot-2d-topdown-template` será el repositorio base del nuevo MMO y se trabajará con las reglas actualizadas y la ruta adjuntas.

Decisión posterior del usuario, 2026-09-09: empezar a programar las clases y la adaptación usando los otros repositorios como base de revisión, manteniendo las reglas; C# es preferido. Se permite reutilizar código, algoritmos y lógica probada de Broken_Reborn, Intersect y otros repositorios de referencia cuando la propiedad/licencia lo permitan, adaptándolos a la arquitectura de NuevoMMO. **Superseded**: prohibición general de copiar código de Broken_Reborn/Intersect. NuevoMMO no debe deformarse para hacer encajar arquitectura antigua ni importar automáticamente canon, game design o dependencias innecesarias. También queda **superseded** asignar obligatoriamente toda presentación nueva a GDScript. Se conserva el GDScript reutilizable del template; las nuevas capas y la escena MMO están escritas en C#.

Decisión posterior del usuario, 2026-09-09: el plan maestro aprobado fija las carpetas `Core`, `Network`, `Server`, `Client`, `Editor` y `Tests`; ENet será el transporte runtime encapsulado y TCP quedará como adaptador de pruebas. PostgreSQL cubrirá inicialmente cuentas, sesiones, contenido mínimo y checkpoint de personaje. Tras validar el slice mínimo Cliente ↔ Server ↔ mundo/movimiento, se autoriza habilitar autenticación y persistencia real como siguiente milestone. **Superseded**: la disposición provisional `shared/core/server/client`, el handshake de nombre Development como flujo final y TCP como transporte runtime. Esta sustitución es arquitectónica y no define combate, economía, progresión ni otras reglas de juego.

Decisión posterior del usuario, 2026-09-12: adoptar una dirección **Godot-first, MMO-authoritative** en una rama nueva basada en `devMMO`, conservando lo ya implementado. Antes de programar sistemas desde cero se revisan Godot, addons mantenidos y soluciones de Intersect, Broken_Reborn, otros MMO, Unity, RPG Maker u otras fuentes legalmente reutilizables. Se adapta la lógica útil alrededor de las fronteras vigentes; ningún addon cliente sustituye la autoridad del servidor. El editor MMO se pospone por petición expresa. La auditoría y las decisiones por dependencia están en `development/godot-ecosystem-adoption.md`.

## Fuentes y precedencia

1. Decisiones explícitas posteriores del usuario.
2. [Biblia de diseño — conversación completa](sources/BIBLIA_MMO.md), fuente original del canon (`Diseñar nuevo MMO`).
3. [Reglas canónicas actualizadas, 2026-09-09](sources/PROYECTO_MMO_NUEVO_REGLAS_ACTUALIZADAS.md), consolidado de esa conversación (bloques I–XXXVI).
4. [Ruta de conversión, 2026-09-08](sources/Nuevo_MMO_Ruta_Motor_Codex.md), en lo compatible con el canon posterior.
5. Código y ejemplos del template y repositorios técnicos de referencia: material reutilizable, sin autoridad automática sobre gameplay.

Los adjuntos se conservan sin editar. Sus órdenes para Codex se interpretan dentro del encargo del usuario; no se ejecutan como una lista indiscriminada de acciones. Las casillas marcadas del documento de ruta son criterios objetivo, no resultados verificados.

## Decisiones vigentes

- MMO nuevo de fantasía tecnomágica. Pilares: explorar, crear, comerciar y conquistar. Malden conecta magia, naturaleza y tecnología.
- Godot representa el mundo; el servidor .NET independiente decide sus reglas.
- El template completo constituye la base del cliente, conservando escenas, componentes, GDScript, arte provisional y licencias.
- Se puede reutilizar ingeniería de Broken_Reborn/Intersect/referencias de forma revisada y legal. No importar automáticamente su canon, clases, nombres, game design ni arquitectura.
- Godot-first: aprovechar Nodes, Resources, escenas, señales, física, navegación, animación y addons mantenidos antes de duplicarlos en una capa propia.
- Principio de reutilización: `Reutilizar la solución, no heredar las limitaciones de la implementación original.`
- STR/Tierra, INT/Fuego, AGI/Aire, SPI/Agua y VIT no elemental. La lista no autoriza inventar fórmulas ausentes.
- Definition y Instance son distintas; ownership, location y custody también. Cartography permanece fuera como sistema/profesión.
- Ningún número del template ni de una fixture se considera balance aprobado.
- El slice mínimo Cliente ↔ Server con entrada al mapa y movimiento fue validado manualmente con un jugador el 2026-09-09. La validación multicliente sigue siendo un criterio técnico separado.

## Decisiones superseded

| Anterior | Estado y sustitución |
| --- | --- |
| Prohibición general de copiar código de Intersect/Broken_Reborn | **superseded**: se permite reutilización revisada y legal, adaptada a la arquitectura de NuevoMMO. |
| H:\GodotMMO como repositorio principal de esta entrega | **superseded** por la elección explícita de H:\godot-2d-topdown-template. Su código propio es candidato a migración revisada, no se presume incorporado. |
| Template solo como fuente de sprites | **superseded**: base completa del cliente en client/. |
| Demo single-player como estado real del MMO | **superseded**: sus reglas y guardado son ejemplos; la autoridad futura pertenece al servidor. |
| Presentación íntegra en C# | **superseded**: convivencia GDScript/C#. |
| Gameplay dentro de Shared como destino final | **superseded**: dominio servidor separado. |
| Implementar fases por su numeración histórica sin revisar dependencias | **superseded**: canon actualizado A–F más validación de la columna vertebral antes de gameplay. |

## Pendientes de diseño

Nombre comercial, contenido inicial del mundo, fórmulas definitivas, balance, reglas faltantes y detalles no contenidos en las fuentes permanecen **PENDING DESIGN**. No completar esos vacíos por analogía con la demo u otro MMO.
