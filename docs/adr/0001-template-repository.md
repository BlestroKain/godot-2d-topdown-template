# ADR-0001 — Template como repositorio principal y cliente real

Fecha: 2026-09-09. Estado: aceptado por solicitud del usuario.

Se utiliza H:\godot-2d-topdown-template como base. El contenido Godot se mueve a client/ manteniendo rutas res://, licencias y recursos originales. Git permanece en la raíz.

GDScript conserva presentación; C# conecta al servidor independiente. Se prepara una escena inicial propia sin activar reglas single-player. No se reescribe el template completo.

Superseded: usar H:\GodotMMO como repositorio principal de esta entrega o importar únicamente sprites del template. El prototipo anterior conserva su contenido y cambios; cualquier migración de código propio requiere revisión y validación. No copiar Intersect.

Las casillas de los adjuntos describen metas, no verificaciones realizadas. Las decisiones futuras incompatibles deberán marcar este ADR como superseded.
