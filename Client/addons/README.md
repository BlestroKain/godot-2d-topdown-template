# Addons de Godot

Las dependencias vendorizadas y sus revisiones están fijadas en
[`addons.lock.json`](addons.lock.json). Cada addon conserva su licencia dentro de su carpeta.

Regla del proyecto: un addon del cliente puede facilitar authoring, escenas, UI, animación o
proyecciones locales, pero no se convierte en autoridad de inventario, combate, quests,
economía, progresión ni persistencia. Las acciones del jugador se traducen a intenciones de
red y el estado solo cambia definitivamente cuando llega la proyección del servidor.

No actualizar una carpeta a `master` sin repetir la auditoría de compatibilidad, licencia,
cambios de API, exportación de plataformas y fronteras de autoridad.
