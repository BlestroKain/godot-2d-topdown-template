# Diseño vigente

Decisión del usuario, 2026-09-09: `H:\godot-2d-topdown-template` será el repositorio base del nuevo MMO y se trabajará con las reglas actualizadas y la ruta adjuntas.

Decisión posterior del usuario, 2026-09-09: empezar a programar las clases y la adaptación usando los otros dos repositorios como base de revisión, manteniendo las reglas; C# es preferido. Se adaptan selectivamente piezas propias de GodotMMO y patrones de Broken_Reborn, sin copiar Intersect. **Superseded**: asignar obligatoriamente toda presentación nueva a GDScript. Se conserva el GDScript reutilizable del template; las nuevas capas y la escena MMO están escritas en C#.

Decisión posterior del usuario, 2026-09-09: el plan maestro aprobado fija las carpetas `Core`, `Network`, `Server`, `Client`, `Editor` y `Tests`; ENet será el transporte runtime encapsulado y TCP quedará como adaptador de pruebas. PostgreSQL cubrirá solamente cuentas, sesiones, contenido mínimo y checkpoint de personaje para este milestone. **Superseded**: la disposición provisional `shared/core/server/client`, el handshake de nombre Development como flujo final y TCP como transporte runtime. Esta sustitución es arquitectónica y no define combate, economía, progresión ni otras reglas de juego.

## Fuentes y precedencia

1. Decisiones explícitas posteriores del usuario.
2. [Reglas canónicas actualizadas, 2026-09-09](sources/PROYECTO_MMO_NUEVO_REGLAS_ACTUALIZADAS.md), bloques I–XXXVI y reglas transversales.
3. [Ruta de conversión, 2026-09-08](sources/Nuevo_MMO_Ruta_Motor_Codex.md), en lo compatible con el canon posterior.
4. Código y ejemplos del template: material técnico reutilizable, sin autoridad sobre gameplay.

Los dos adjuntos se conservan sin editar. Sus órdenes para Codex se interpretan dentro del encargo del usuario; no se ejecutan como una lista indiscriminada de acciones. Las casillas marcadas del documento de ruta son criterios objetivo, no resultados verificados.

## Decisiones vigentes

- MMO nuevo de fantasía tecnomágica. Pilares: explorar, crear, comerciar y conquistar. Malden conecta magia, naturaleza y tecnología.
- Godot representa el mundo; el servidor .NET independiente decide sus reglas.
- El template completo constituye la base del cliente, conservando escenas, componentes, GDScript, arte provisional y licencias.
- No importar canon, código ni dependencias de Intersect/Broken_Reborn.
- STR/Tierra, INT/Fuego, AGI/Aire, SPI/Agua y VIT no elemental. La lista no autoriza inventar fórmulas ausentes.
- Definition y Instance son distintas; ownership, location y custody también. Cartography permanece fuera como sistema/profesión.
- Ningún número del template ni de una fixture se considera balance aprobado.
- La primera validación es dos clientes con movimiento autoritativo, predicción, reconciliación, interpolación y spawn/despawn. El alcance sistémico del bloque XXXVIII es posterior.

## Decisiones superseded

| Anterior | Estado y sustitución |
| --- | --- |
| H:\GodotMMO como repositorio principal de esta entrega | **superseded** por la elección explícita de H:\godot-2d-topdown-template. Su código propio es candidato a migración revisada, no se presume incorporado. |
| Template solo como fuente de sprites | **superseded**: base completa del cliente en client/. |
| Demo single-player como estado real del MMO | **superseded**: sus reglas y guardado son ejemplos; la autoridad futura pertenece al servidor. |
| Presentación íntegra en C# | **superseded**: convivencia GDScript/C#. |
| Gameplay dentro de Shared como destino final | **superseded**: dominio servidor separado. |
| Implementar fases por su numeración histórica sin revisar dependencias | **superseded**: canon actualizado A–F más validación de la columna vertebral antes de gameplay. |

## Pendientes de diseño

Nombre comercial, contenido inicial del mundo, fórmulas definitivas, balance, reglas faltantes y detalles no contenidos en las fuentes permanecen **PENDING DESIGN**. No completar esos vacíos por analogía con la demo u otro MMO.
