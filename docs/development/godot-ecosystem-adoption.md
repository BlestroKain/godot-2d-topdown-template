# Adopción del ecosistema Godot y fuentes externas

Revisión: 2026-09-12. Rama: `feature/godot-first-systems`, basada en `devMMO`.

## Regla operativa

`Godot-first, MMO-authoritative.`

Antes de crear un sistema desde cero se revisa, en este orden: capacidades nativas de Godot,
Asset Library/addons mantenidos, repositorios Godot 4, implementaciones MMO/RPG de otros
motores y, por último, implementación propia. Reutilizar no cambia la autoridad: Server decide,
Client representa y los gestos locales se convierten en comandos.

Un candidato pasa por: versión compatible, licencia, actividad/mantenimiento, superficie de
acoplamiento, soporte de exportación, capacidad de encapsularlo y compatibilidad con autoridad
servidor. Las dependencias directas se fijan por revisión; nunca se consume `master` flotante.

## Decisiones de la auditoría

| Candidato | Revisión observada | Decisión | Uso en NuevoMMO |
| --- | --- | --- | --- |
| Dialogue Manager | 3.10.1, MIT | ADOPTADO | Authoring y presentación de diálogos; la ejecución que cambia gameplay debe pedir comandos al servidor. |
| GLoot | 3.0.2 / `6b09b87`, Godot 4.7, MIT | ADOPTADO CON ADAPTADOR | Contenedores, stacks, slots y UI del cliente. `ServerInventoryProjection` reconstruye GLoot desde snapshots; GLoot no concede ni mueve ítems autoritativamente. |
| Godot State Charts | 0.22.5 / `76d226a`, Godot 4, MIT | ADOPTADO | Estados jerárquicos de presentación. Primer uso real: frontend, mundo y preferencias sobre el mundo. |
| shomykohai/quest-system | 2.0.2 / `d1d933c`, Godot 4.4+, MIT | ADAPTAR, NO ACTIVAR AÚN | Reutilizar Resources, pools, señales y localización para authoring/proyección. Su manager local no sustituye `QuestSystem`/`EventRuntime` del servidor y sus IDs `int` no sustituyen `DefinitionId`. |
| OctoD/godot-gameplay-abilities | `6d1ce71`, MIT | SPIKE NATIVO PENDIENTE | Reutilizar Ability/Container/Runtime y hooks para presentación de técnicas. Es GDExtension C++; requiere binarios reproducibles para 4.7.1 y plataformas objetivo antes de entrar a runtime. |
| LimboAI | `3f14ea4`, Godot 4.7, licencia MIT-style | AUTHORING/REFERENCIA | Los mobs siguen siendo autoritativos en Server. Evaluar su editor de BT/HSM para producir definiciones neutrales cuando se retome el editor; no ejecutar decisiones de AI solo en cliente. |
| Relintai/entity_spell_system | `3c240ff`, MIT | REFERENCIA ARQUITECTÓNICA | Extraer patrones de ResourceDB, IDs compactos, abilities/effects/targeting y separación cliente-servidor. No integrar el módulo: la migración a Godot 4 quedó inconclusa y exige recompilar Godot. |
| Quaint-Studios/Reia | `82fd21f`, Godot 4.7, AGPL-3.0 | REFERENCIA, SIN COPIA | Estudiar composición de escenas y separación de netcode. No copiar código al proyecto sin una decisión explícita de licencia AGPL. |
| godot-addons/godot-quest-system | Godot 3 | DESCARTADO | Obsoleto para el cliente 4.7 y con persistencia incompleta. |
| Escandiuzzi/Inventory-Manager-Asset-Godot | 2019 | REFERENCIA SUPERADA | Su patrón Node/señales fue útil; GLoot cubre una superficie mayor y mantenida. |

## Fuentes no Godot

Intersect, Broken_Reborn, Unity, RPG Maker y otros MMO/RPG pueden aportar algoritmos,
contratos, flujos de editor, tablas de datos y casos límite. Cada adopción debe registrar ruta o
URL, licencia, lógica extraída, cambios y pruebas. No se importan lore, balance, clases ni una
arquitectura completa por comodidad.

## Integración inicial

- El frontend ya usa Godot State Charts para separar `Frontend`, `World` y
  `SettingsOverWorld`; el C# conserva coordinación y la escena declara transiciones.
- El inventario C# conserva la durabilidad recibida por red y la proyecta a un `Inventory` con
  `GridConstraint` de GLoot. `ServerInventoryGrid` usa el drag-and-drop de GLoot únicamente
  para emitir `MoveInventoryItemRequest`; no modifica la proyección local. Doble clic/clic
  derecho emite equipar/desequipar y cada operación queda pendiente hasta un nuevo snapshot.
  Los gestos se serializan de uno en uno mientras no existe correlación de comandos de inventario.
- El servidor valida el reordenamiento, conserva identidad/equipo, persiste el nuevo orden y
  responde siempre con `InventorySnapshotPacket`, incluso al rechazar, para reconciliar la UI.
- La proyección de equipo cliente conserva el índice de slots múltiples (anillos y trofeos).
- El parche local de firma de señal requerido por GLoot está declarado en
  `Client/addons/addons.lock.json`.
- Las revisiones y licencias vendorizadas se registran en `Client/addons/addons.lock.json`.

## Siguiente lote autorizado por esta dirección

1. Validar el circuito GLoot con Godot 4.7.1 Mono y dos procesos cliente; la implementación y
   las pruebas de contrato ya están preparadas, pero la verificación runtime no se declara
   cerrada sin ejecutar Godot.
2. Crear proyección de quests basada en Resources/señales sin manager autoritativo local.
3. Hacer spike reproducible de Gameplay Abilities para Windows/Linux/Android; si no pasa CI,
   portar su modelo a Nodes/Resources sin dependencia binaria.
4. Llevar estados de casting, stun, muerte y minijuegos de profesión a state charts donde
   reduzcan código sin duplicar reglas de servidor.
5. Posponer el editor MMO como pidió el usuario; cuando se retome, evaluar LimboAI y
   `EditorPlugin` como herramientas de authoring, no como autoridad runtime.
