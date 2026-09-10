# NuevoMMO — Auditoría, reutilización y evolución del código

Documento operativo. Gobierna cómo se revisan y evolucionan las clases. El pedido explícito posterior del usuario prevalece.

## Objetivo

Revisar y evolucionar el proyecto clase por clase, aprovechando código y lógica útiles ya existentes. No rehacer el motor desde cero innecesariamente.

`No reinventar lo que ya funciona, pero tampoco deformar NuevoMMO para acomodar código viejo.`

`Reutilizar la solución, no heredar las limitaciones de la implementación original.`

## Arquitectura

Core define el mundo. Network define cómo viaja la información. Server decide la verdad. Client la representa. Editor modifica las definiciones del Core.

Core no depende de nadie. Network depende de Core. Server/Client/Editor dependen de Core + Network. Editor no depende del Server; si habla con un servidor, lo hace mediante Network.

`Definition describe. Entity/State existe. System decide. Application/Service coordina. Message/Packet comunica. View representa.`

## Reutilización

Está permitido y recomendado reutilizar lógica probada de Broken_Reborn, Intersect Engine, referencias aprobadas e implementaciones propias, siempre que sea legal y se adapte a los contratos de NuevoMMO.

Flujo: analizar → extraer lógica útil → quitar dependencias innecesarias → adaptar a contratos → integrar → probar.

NuevoMMO no se deforma para encajar código viejo. No heredar lore, clases, nombres o game design de Broken/Intersect.

## Política vigente para Definitions de contenido

Las Definitions son la fuente maestra editable del objeto y pueden ser ricas, siguiendo el valor práctico del modelo Intersect/Broken_Reborn. Deben contener toda la configuración necesaria para que Entity/System/Service no tenga que reconstruir datos de diseño dispersos.

Esto NO autoriza lógica runtime, acceso a DB, sockets, timers activos ni estado mutable de una instancia dentro de la Definition. La separación sigue siendo: la Definition configura; Entity/State conserva estado vivo; System decide y ejecuta.

Preferir bloques de configuración y diccionarios tipados cuando reduzcan rigidez: stats/modificadores, rangos de propiedades, requisitos, servicios, hooks, parámetros y referencias. Mantener IDs/ContentKeys para enlazar contenido y validar referencias en ContentPackage antes del runtime.

La adaptación actual toma de Intersect ideas como configuración completa de items, comportamiento/combate de NPCs, drops, cooldowns, stacking, permisos, vitales, scaling y recursos agotables, pero expresadas con los tipos/canon de NuevoMMO.

## Flujo por clase

1. Ruta exacta.
2. Responsabilidad.
3. Leer implementación completa.
4. Buscar usos.
5. Revisar dependencias y consumidores.
6. Qué está bien / problemas reales.
7. Referencia/reutilización si aplica.
8. Decisión.
9. Si hay cambios: código final completo, impacto, tests/compilación.
10. Siguiente clase.

No refactors masivos. No duplicar tipos. Validar temprano. El Server es autoritativo.

## Ya revisadas

DefinitionId, ContentKey, GameDefinition, DefinitionRegistry, ContentPackage, MapDefinition, BoundsData, Vector2Data, Vector2IntData, MapId.

Segunda pasada de Definitions ricas completada para: MobDefinition, NpcDefinition, ItemDefinition, ItemPropertyDefinition, ResourceDefinition y LootTableDefinition, junto con ObjectDefinitionModels y validación cruzada de referencias del ContentPackage.

## Siguiente

`RecipeDefinition.cs`
