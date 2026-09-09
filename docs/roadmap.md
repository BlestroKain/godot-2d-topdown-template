# Ruta ejecutable

Los documentos fuente enumeran objetivos. El avance comprobado vive en IMPLEMENTATION_STATUS.md.

Actualización 2026-09-09: implementadas las capas iniciales de los pasos 1–6 para un mapa técnico local; integración de dos procesos Godot verificada. Faltan latencia/jitter, exportación móvil y validación de movimiento bajo condiciones de producción antes de cerrar la columna vertebral como producto. La lista siguiente conserva el orden de trabajo, no implica que todas sus filas sigan sin empezar.

| Orden | Trabajo | Criterio de aceptación | Clasificación |
| --- | --- | --- | --- |
| 0 | Preparar repositorio, client/, licencias, canon y escena mínima | Import y arranque sin errores críticos; fuentes intactas | Preparación |
| 1 | Auditar código propio de GodotMMO para migración selectiva | Origen y dependencias revisados; build/tests reproducibles; sin código Intersect | TECHNICAL DEBT |
| 2 | Solución .NET y contratos mínimos | Compila; dominio/simulación sin Godot; IDs y mensajes probados | FUTURE FEATURE |
| 3 | Host y sesiones dev | Inicio/cierre ordenado, tick controlable, handshake/rechazos/desconexión probados | FUTURE FEATURE |
| 4 | NetworkBridge C# y UI GDScript | Conectar/desconectar, errores visibles y estado InWorld desde servidor | FUTURE FEATURE |
| 5 | PlayerView y movimiento | Input secuenciado, simulación servidor, ack real, prediction/reconciliation/replay | FUTURE FEATURE |
| 6 | Entidades remotas y AOI | Dos clientes Godot; interpolación, spawn/despawn, AOI y reconexión | FUTURE FEATURE |
| 7 | Consolidar fases A–D | Fixtures aisladas, World coordinador, comandos/eventos, contenido validado | TECHNICAL DEBT / FUTURE FEATURE |
| 8 | Persistencia mínima | Repositorios; schemas auth/content/game/audit; restauración validada y pruebas de integración | FUTURE FEATURE |
| 9 | Bases de items y economía E | Definition/Instance, una ubicación válida, atomicidad, concurrencia y audit | FUTURE FEATURE |
| 10 | Adaptación de interacción e inventario | UI envía intención; validaciones de owner, alcance, permisos y estado servidor | FUTURE FEATURE |
| 11 | Sistemas F y slice sistémico XXXVIII | Diseño de cada regla y pruebas; no rellenar fórmulas ausentes | FUTURE FEATURE / PENDING DESIGN |

## Puerta obligatoria: Vertical Slice 0

- Dos procesos Godot conectan al mismo servidor y reciben EntityId distintos.
- Entrada local responde inmediatamente; servidor decide posición; replay corrige discrepancias.
- Entidades remotas interpolan, aparecen/desaparecen y respetan AOI/instancia.
- Desconectar retira entidad; reconectar puede recrearla.
- Build .NET y pruebas de protocolo/movimiento/sesión pasan.
- Inputs malformados, secuencias inválidas y cambios locales de posición no alteran autoridad.
- No se guarda personaje, posición ni inventario autoritativos en archivos del cliente.
- Capacidades visuales del template conservadas; suavidad observada en ejecución, además de pruebas automatizadas.

No marcar esta puerta completa con snapshots artificiales, una escena de preparación o dos clientes de consola. Persistencia, combate, profesiones, mercado, guilds, quests y housing no son prerrequisitos para este primer hito.
