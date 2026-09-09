# Proyecto MMO Nuevo --- Reglas Canónicas Actualizadas

**Fecha:** 2026-09-09\
**Estado:** canon consolidado hasta el BLOQUE XXXVI\
**Motor:** Godot 4.x + C#/.NET

## 0. Reglas maestras

-   Proyecto MMO completamente nuevo. Broken_Reborn, Broken Bridge,
    Arovia, Baurah e Intersect no son canon.
-   Intersect es únicamente referencia conceptual; no es dependencia,
    fork ni fuente automática de reglas.
-   Inspiraciones: Dofus + Ragnarok Online como columna vertebral;
    Chrono Trigger para aventura/descubrimiento; Albion para
    economía/territorio/logística; Stardew Valley para profesiones;
    Fallout solo como referencia industrial/arcaica, nunca apocalipsis
    nuclear.
-   Pilares: **EXPLORAR / CREAR / COMERCIAR / CONQUISTAR**.
-   Malden es la energía fundamental que conecta magia, naturaleza y
    tecnología.
-   No inventar mecánicas o números donde el diseño siga pendiente.
-   Diseño indefinido = interfaz, placeholder, TODO o stub explícito.
-   El código existente no redefine el canon.
-   **Newest canon wins.**

# Canon por bloques

## I --- Constitución (001--012)

MMORPG de fantasía tecnomágica. Mundo estratificado históricamente.
Regiones con identidad física, ecológica, cultural, histórica y
económica. Profesiones, comercio, exploración y logística son estilos de
juego legítimos. Recursos desiguales generan especialización y comercio.
El territorio vale por infraestructura, rutas y recursos reales.

## II --- Malden y cosmología (013--021)

Malden es la energía fundamental. Fuego, Agua, Tierra y Aire son
manifestaciones fundamentales.

`MALDEN → CORRIENTES → NODOS → ELEMENTOS → MAGIA / NATURALEZA / TECNOLOGÍA`

Superficie, Profundidades y Alturas pertenecen al mismo mundo.
Tecnología antigua requiere conocimiento, profesiones, materiales,
energía, reparación e infraestructura.

## III --- Geografía (022--039)

Mundo regional abierto, viaje significativo, economía geográfica,
fronteras, regiones anómalas y señales diegéticas. El mundo no escala
alrededor del jugador. Peligro ≠ nivel.

## IV --- Sociedad e identidad (040--058)

No hay razas RPG clásicas. Anatomía fantástica no otorga estadísticas
raciales.

`ORIGEN + TRADICIÓN + PROFESIÓN + AFILIACIÓN`

Guild ≠ Faction. Cultura ≠ raza. Jugador = habitante, no elegido
universal.

## V --- Personaje y progresión (059--081)

Inicio tipo Novicio, sin Tradición. Aproximadamente 8--12 Tradiciones;
diez actualmente diseñadas. Sin trinidad rígida. Progresión
multidimensional, vertical inicialmente y horizontal posteriormente.
Equipo modifica mecánicas además de stats.

## VI --- Combate (082--108)

Tiempo real táctico, movimiento libre, targeting híbrido, ataque básico
relevante, recursos por Tradición, cooldowns moderados, kit limitado, CC
con contrajuego y alta legibilidad.

## VII --- Tradiciones (109--160)

Tradición = disciplina histórica con Fantasy + Mechanic + Visual. El
arma no define Tradición.

Nombres provisionales: 1. Veyrkan --- Pulso/Sobrecarga. 2. Mizram ---
Germinación/Red viva. 3. Ngomei --- Anclajes. 4. Tegra --- Trazado. 5.
Sagrel --- Trayectoria. 6. Kaelith --- Resonancia elemental. 7. Zoonai
--- Simbiosis con criaturas reales. 8. Sahjin --- Cadencia corporal. 9.
Khemra --- Circuitos/Tecnología Malden. 10. Nymbra --- Ecos/Percepción.

Atributos: - Tierra = STR - Fuego = INT - Aire = AGI - Agua = SPI - VIT
= no elemental - LUK no es primario.

## VIII --- Identidad visual (161--175)

`CUERPO + TRADICIÓN + EQUIPO/BUILD`. Elemento no es simple recolor. Cada
Tradición conserva silueta reconocible.

## IX --- Creación de personaje (176--197)

Sin selector racial. Anatomía libre controlada, sin cambios de
hitbox/stats. Marcas Malden, cuernos, pigmentos, prótesis, etc. Creación
previa a Tradición.

## X --- Historia y aprendizaje (198--223)

`TRADICIÓN = mecánica`, `ELEMENTO = filosofía/build`,
`ESCUELA = interpretación cultural`, `BUILD = configuración`.
Aprendizaje diegético mediante maestros, libros, reliquias, exploración
y conocimiento.

## XI --- Técnicas (224--256)

Aproximadamente 15--25 técnicas aprendibles por Tradición y 6--8 activas
como referencia. Variantes funcionales, maestría, especialización
horizontal y loadouts fuera de combate. Sin árbol rígido.

## XII --- Stats (257R--326R)

STR/INT/AGI/SPI/VIT son los únicos atributos distribuibles. VIT aporta
HP; SPI regula PM/Malden. Resistencias porcentuales. Terciarios mediante
fuentes. Cap de referencia Lv100, 3 puntos/nivel, ≈297 puntos Lv1→100.
Soft caps: ≤101 1:1; 102--200 2:1; 201--300 3:1; posteriores pendientes.

## XIII --- Equipamiento (327--354)

Arma no define Tradición. Durabilidad incluida. Slots: cabeza, torso,
manos, botas, arma principal, off-hand, amuleto, 2 anillos, espalda/capa
y 1 trofeo. Piernas retiradas; cinturón pendiente.

## XIV --- Itemización (355R--378R)

Nombre fijo + propiedades. Sin prefijos/sufijos aleatorios base.
`ItemDefinition ≠ ItemInstance`. Rareza ≠ calidad ≠ rolls ≠ poder.

## XV --- Muerte (379--408)

Penaliza expedición mediante posición, tiempo, durabilidad, consumibles
y oportunidad. Sin pérdida permanente de nivel/EXP/stats/maestría.
Inventario normal protegido. Vulnerable Cargo es explícito. PvP estándar
no es full loot.

## XVI --- Criaturas (409--463)

`Family → Species → Variant → Instance`. Criatura = habitante antes que
mob. Poblaciones con Simulation Tiers. Loot corporal y posesiones
separados. Domesticabilidad ≠ Zoonai.

## XVII --- PvE (464--531)

Dungeon = lugar real. Boss = culminación del lugar/sistema. Sin
autoscale universal. Minibosses y entorno enseñan. PvE grupal no
monopoliza mejor equipo.

## XVIII --- Profesiones (532--656)

Todas aprendibles, sin límite artificial de dos. Obtención: Minería,
Silvicultura, Botánica, Pesca, Caza. Producción: Herrería, Carpintería,
Textiles, Joyería, Alquimia, Cocina. Vida: Agricultura, Ganadería/Cría.
Especial: Restauración. Ingeniería Malden pendiente. Cartografía
eliminada.

## XIX --- Gathering (657--729)

Recurso ≠ nodo. Distribución por
geología/ecología/clima/Malden/historia. Maestría mejora identificación,
extracción, preservación y eficiencia.

## XX --- Crafting (730--818)

`Recipe + Materials + Artisan + Tools + Facility + Process → ItemInstance`.
Resultado controlado dentro de rangos válidos. Crafting no es
tragamonedas RNG.

## XXI --- Agricultura (819--933)

Ciclos biológicos persistentes con simulación server-side. Suelo, clima,
semillas, líneas, enfermedades, infraestructura, automatización y
trabajadores NPC. Husbandry ≠ Zoonai.

## XXII --- Economía (934--1015)

Player-driven/world-supported. Moneda principal con nombre pendiente.
Valor ≠ rareza. Precio ≠ poder. Faucets/sinks comprensibles. NPC
sostiene baseline.

## XXIII --- Comercio y logística (1016--1104)

Los bienes tienen ubicación. Distancia cuesta.
`Owner ≠ Location ≠ Custody`. Transporte mediante personas, animales,
carros, caravanas, barcos e infraestructura. Vulnerable Cargo conecta
economía y conflicto.

## XXIV --- Mercados regionales (1105--1201)

Mercados locales, buy/sell orders, escrow, partial fills, matching
atómico/auditable. Información remota ≠ teletransporte de bienes.

## XXV --- Exploración y conocimiento (1202--1289-W)

Cartografía eliminada.
`Unknown → Observed → Identified → Understood → Mastered`. Knowledge
persistente; Information puede envejecer. El mapa recuerda dónde;
Knowledge explica qué/cómo/por qué.

## XXVI --- Viaje (1290--1436)

Movement ≠ Travel ≠ Logistics. Sin fast travel universal. Rutas
conocidas y desarrolladas pueden volverse cómodas sin borrar geografía
económica.

## XXVII --- Mundo y regiones (1437--1670)

`World → Region → Map → Area → Entity/Object`. Region = contexto
sistémico; Map = espacio jugable. Chunks son técnicos. No
ZoneLevel/DangerLevel universal.

## XXVIII --- Profundidades y Alturas (1671--1883)

Superficie, Profundidades y Alturas son territorios persistentes del
mismo mundo, con economía, sociedades, ecología, infraestructura y
conexiones físicas. No son dungeons/endgame.

## XXIX --- Civilización, asentamientos y NPCs (1884--2106)

Un asentamiento existe porque alguien tiene razones para vivir allí. NPC
= habitante antes que tienda. Ciudades no son lobbies. Servicios
pertenecen a sistemas propietarios.

## XXX --- Sociedad y organizaciones (2107--2358)

`PARTY ≠ FRIEND ≠ GUILD ≠ FACTION ≠ AFFILIATION ≠ REPUTATION ≠ CHAT ≠ MAIL ≠ PERMISSION`.
Guild usa Wallet, Container y PermissionSystem transversales. Reputation
no es karma universal.

## XXXI --- Territorio, conflicto y ley

Canonizado. Territorio representa control de
espacio/infraestructura/rutas/recursos. PvP, guerra, asedio, crimen y
ley son contextuales. Sin SafeZones universales ni full loot estándar.
Guild/Faction/Territory/Authority son distintos. Law ≠ Moderation.

## XXXII --- Narrativa y estado

Canonizado. Quests observan sistemas y solicitan cambios a owners; no
mutan sistemas ajenos directamente.
`WorldState → NPC/Dialogue → Quest/Decision → System Action → WorldState`.

## XXXIII --- Mundo dinámico

Canonizado. Eventos, clima, poblaciones, recursos e infraestructura
evolucionan causalmente. `Abstract → Represented → Active`. Scheduler
administra cuándo; Systems deciden qué.

## XXXIV --- Vivienda y propiedad

Canonizado. Housing usa Ownership + Permissions comunes. No debe romper
geografía/logística ni sustituir ciudades/mercados.
Cosméticos/logros/colecciones sin power creep obligatorio.

## XXXV --- Presentación (3319--3825)

Canonizado. UI/UX, controles, accesibilidad, pixel art, audio y feedback
presentan sistemas sin sustituirlos. PC + móvil + controller desde
arquitectura inicial.

## XXXVI --- Arquitectura MMO (3826--5240)

Canonizado.

> Una Scene representa. Una Definition describe. Una Entity existe. Un
> System decide reglas. Un Message comunica. Un View muestra. Una
> Database persiste.

Godot = cliente. Servidor = C#/.NET independiente y autoritativo. Shared
sin Godot. Protocolo propio. Domain no depende de
Infrastructure/PostgreSQL/sockets/Godot. Monolito modular inicialmente.

Flujo:
`Input → Intent → Command → Session Validation → Domain Validation → System → Domain Events → Persistence → Replication`

Replication:
`Authoritative State + AOI + Visibility + Knowledge + Permissions + Relevance → Client Projection`

# Bloque transversal S --- Systems

Systems propietarios y separados para Character, Attributes, Tradition,
Technique, Combat, Death, Items, Containers, Equipment, Professions,
Gathering, Crafting, Agriculture, Economy, Trade, Market, Logistics,
Movement, Mounts, Vehicles, Travel, World, Region, Map, Environment,
Malden, Exploration, Knowledge, Quest, Dialogue, NPC, Creatures, Party,
Friends, Chat, Mail, Guild, Faction, Reputation, Affiliation, Territory,
PvP, War, Siege, Crime, Law, Dungeon, Loot, Housing, Achievements,
Cosmetics, Permissions, Ownership, Transactions, Scheduler, WorldEvents,
Replication y UI.

Cartography NO es sistema.

# Datos y bases de datos

Separación lógica obligatoria, inicialmente compatible con una sola
instancia PostgreSQL:

-   `auth.*` --- quién puede entrar: Account, credentials, sessions,
    tokens, security.
-   `content.*` --- qué puede existir: Definitions/GameData.
-   `game.*` --- qué existe ahora: Characters, ItemInstances,
    Containers, Wallets, Guilds, Markets, Knowledge, Progress,
    Territory, Region/World state.
-   `audit.*` --- qué ocurrió: transferencias, currency, market,
    treasury, admin, security.

Telemetry es diferente de Audit y puede migrar a infraestructura
especializada.

Regla: `CONTENT = WHAT CAN EXIST` `GAME = WHAT EXISTS NOW`
`AUTH = WHO MAY ENTER` `AUDIT = WHAT HAPPENED`

Player Data y World Data pueden separarse físicamente después, pero no
hace falta crear múltiples clusters desde el día uno.

# Reglas obligatorias para implementación/Codex

1.  Newest canon wins.
2.  No inventar gameplay pendiente.
3.  No copiar Intersect.
4.  Godot solo cliente.
5.  Server/Domain sin Godot.
6.  Server authoritative.
7.  Cliente envía intención, no resultados.
8.  Shared solo contratos/lógica realmente compartida.
9.  Definition ≠ Instance.
10. Entity/State = lo existente.
11. System = dueño de reglas.
12. DTO/Message ≠ Domain Entity.
13. Owner ≠ Location ≠ Custody.
14. ItemInstance tiene exactamente una ubicación válida.
15. Economía crítica: atomicidad + consistencia + idempotencia cuando
    corresponda + audit.
16. Wallet no es un `int Money` dentro de Character.
17. No float binario para dinero crítico.
18. No SQL disperso desde gameplay.
19. Repositories por responsabilidad/agregado, no por tabla
    automáticamente.
20. DB constraints refuerzan invariantes; no gameplay complejo en SQL
    triggers.
21. Runtime vive principalmente en memoria.
22. No persistir cada frame.
23. Strong persistence para currency, ownership, escrow, market fills y
    treasury.
24. Snapshot + Delta.
25. AOI ≠ Visibility ≠ Simulation Tier.
26. No enviar secretos al cliente innecesariamente.
27. Movement server-authoritative con prediction/interpolation cliente.
28. FPS/reloj cliente nunca alteran reglas.
29. Server time es autoridad.
30. Scheduler decide cuándo; System decide qué.
31. Chunk = optimización técnica.
32. Map ≠ Region.
33. Definition/State/Runtime separados.
34. WorldGraph representa conectividad.
35. ContentGuid + ContentKey como identidad estable.
36. Display name nunca es primary identity.
37. IDs runtime compactos pueden ser efímeros.
38. ContentPackage versionado/validado.
39. Server no carga texturas.
40. VisualKey → AssetRegistry client-side.
41. Content secreto puede ser server-only.
42. Invalid ContentPackage falla temprano.
43. Compiler/validators/editor son parte del motor.
44. Account ≠ Character.
45. Auth ≠ Game ≠ Content ≠ Audit.
46. Moderation ≠ Law.
47. Party ≠ Guild ≠ Faction.
48. Reputation ≠ Affiliation.
49. Guild treasury usa Wallet.
50. Guild storage usa Container.
51. TerritorySystem ≠ WarSystem ≠ LawSystem.
52. Quest no muta directamente Inventory/Wallet/etc.
53. NPCService no implementa el sistema servido.
54. Cartography permanece fuera.
55. Modular Monolith primero.
56. Microservices solo por necesidad real.
57. Un agregado mutable tiene una autoridad.
58. No Event Sourcing universal.
59. No CQRS universal.
60. Redis/Kafka/Kubernetes/Elasticsearch solo ante problema real.
61. Evitar SQL por entidad/tick.
62. Evitar world-wide loops.
63. Evitar snapshots completos continuos.
64. Domain tests sin Godot.
65. Persistence integration tests.
66. Economy concurrency/transaction tests.
67. Protocol serialization + malformed-input tests.
68. Clock controlable para tests.
69. Load testing con clientes simulados.
70. Toda creación/destrucción económica relevante tiene causa.
71. Audit económico preferentemente append-oriented.
72. Correcciones admin explícitas y auditadas.
73. No UPDATE manual de producción como workflow normal.
74. No passwords/tokens completos en logs.
75. TLS producción.
76. No criptografía propia.
77. Todo input del cliente es no confiable.
78. UI no es seguridad.
79. Rate limiting contextual.
80. Admin permissions explícitos.
81. Dev token/debug solo Development/Test.
82. Fixtures = test/dev, nunca canon.
83. Retirar `SliceFixture`, `FixtureAttributeCalculator`,
    `"fixture-slot"` y reglas equivalentes del runtime productivo.
84. `World.cs` es scaffolding y debe convertirse incrementalmente en
    coordinador.
85. No big-bang rewrite.
86. `TECHNICAL_ARCHITECTURE.md` = objetivo.
87. `IMPLEMENTATION_STATUS.md` = estado real.
88. Clasificar: PENDING DESIGN / TECHNICAL DEBT / FUTURE FEATURE /
    TEMPORARY FIXTURE.
89. Una feature debe declarar client-only, server-authoritative, shared,
    persistence, replication, GameData, GameState y audit.
90. Placement de datos: Definition→content; State→game;
    identity/security→auth; history→audit.
91. Placement de clases: Describe→Definition; Exists→Entity/State;
    Decides→System; Coordinates→Application/Runtime;
    Communicates→Message/DTO; Shows→View/ViewModel.
92. Una regla tiene un solo owner.
93. Derived data debe poder reconstruirse.
94. No recalcular todo cada frame.
95. Tooling es first-class.
96. Cambiar canon implica actualizar docs.
97. **Newest canon wins.**

# Fases técnicas vigentes

### A --- Debt removal

Retirar fixture coupling productivo manteniendo vertical slice.

### B --- World foundation

WorldRuntime, RegionDefinition/State/Runtime, MapManager, MapInstance,
transitions, chunks, streaming y AOI.

### C --- Commands/events

CommandRouter, ICommandHandler`<T>`{=html}, DomainEvent, EventDispatcher
y Replication projection.

### D --- Content

ContentGuid, ContentKey, ContentPackage, validators, compiler,
references y AssetRegistry.

### E --- Economía base

Container, ItemLocation, Ownership, InventoryTransaction, Wallet, Escrow
y Audit ledger.

### F --- Gameplay

Gathering, Crafting, Agriculture, Markets, Trade, Logistics, Professions
y sistemas siguientes sobre las bases anteriores.

# Reglas maestras finales

> El cliente puede sugerir; el servidor decide.

> Godot debe saber cómo mostrar el juego, no cómo funciona internamente
> el servidor.

> El servidor debe saber cómo funciona el juego, no cómo Godot dibuja
> sprites.

> Definition describe. Instance existe. System decide.

> La base de datos persiste consecuencias importantes; no dirige el
> gameplay frame a frame.

> Simulation Tier determina cuánto simulamos; AOI determina
> principalmente cuánto replicamos.

> MMO Scale ≠ Microservices.

> Un NPC es habitante antes que tienda. Una criatura es habitante antes
> que mob. Un dungeon es lugar antes que instancia. Una región existe
> por geografía, historia, ecología y economía antes que por nivel.

> El mapa recuerda dónde. Knowledge explica qué, cómo y por qué.

> El viaje conserva geografía. La logística convierte distancia en
> economía.

> El territorio vale por lo que permite controlar, producir, conectar o
> proteger.

> No inventar diseño pendiente para hacer avanzar el código.

> **El canon más reciente siempre prevalece.**

# Próximo bloque

## XXXVII --- Validación del Motor

Confrontar todo el canon con `GodotMMO`, detectar deuda/contradicciones,
actualizar arquitectura/documentación y generar roadmap ejecutable para
Codex.

## XXXVIII --- Vertical Slice

Integración real de:
`Account/Session → Character → World → Movement → NPC → Combat → Creature → Resource → Gathering → Profession → Crafting → ItemInstance → Inventory → Trade/Market → Quest/Knowledge → Persistence → Replication → Reconnect`
