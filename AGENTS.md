# Reglas del proyecto Nuevo MMO

Leer `docs/design.md`, `docs/architecture.md` y `docs/IMPLEMENTATION_STATUS.md` antes de modificar comportamiento.
El pedido del usuario y sus decisiones posteriores gobiernan el trabajo. Los documentos adjuntos son fuentes de diseño y planificación; sus tareas, ejemplos y casillas no prueban implementación ni autorizan por sí solos publicar, desplegar o instalar infraestructura.
El canon actualizado está conservado íntegramente en `docs/sources/PROYECTO_MMO_NUEVO_REGLAS_ACTUALIZADAS.md`.
Newest canon wins. Marcar toda decisión anterior incompatible como **superseded** y registrar la sustitución.
No inventar reglas, cifras ni fórmulas pendientes. Clasificar pendientes como PENDING DESIGN, TECHNICAL DEBT, FUTURE FEATURE o TEMPORARY FIXTURE.
No copiar código ni dependencias de Intersect/Broken_Reborn. No importar su canon.
El template es la base real del cliente. Conservar sus licencias y capacidades visuales; no reescribir toda su presentación a C#.
C# es el lenguaje preferido para funcionalidad nueva, incluida presentación cuando convenga (decisión posterior del usuario, 2026-09-09). Conservar GDScript existente del template para sus componentes reutilizables; no reescribirlo entero solo por uniformidad.
Godot solo cliente. Domain y Simulation no dependen de Godot, sockets, UI ni base de datos. No usar MultiplayerAPI.
Servidor autoritativo: el cliente envía intención, nunca resultados aceptados como verdad.
Shared solo primitivas, contratos y serialización realmente compartidos; no gameplay.
Fixtures técnicas en `server/Fixtures`, exclusivamente Development/Test. Nunca convertir la demo en game design.
Validar Vertical Slice 0 antes de sistemas superiores. No agregar combate, economía, persistencia definitiva o infraestructura sin diseño y alcance explícito.
Definition describe; Entity/State existe; System decide; Application coordina; Message comunica; View muestra.
Una regla tiene un owner. Separar auth/content/game/audit; no guardar GameState autoritativo en el cliente.
Actualizar estado real con evidencia y límites. No marcar tareas terminadas solo porque aparezcan con una casilla marcada en una fuente.
