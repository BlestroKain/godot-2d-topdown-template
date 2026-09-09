# NUEVO MMO — PLAN DE CONVERSIÓN DEL TEMPLATE GODOT A CLIENTE MMO

**Fecha:** 2026-09-08  
**Base cliente:** `stesproject/godot-2d-topdown-template`  
**Motor visual:** Godot 4.x  
**Backend:** C# / .NET  
**Arquitectura:** servidor autoritativo desacoplado del runtime de Godot  
**Objetivo de este documento:** servir como guía de implementación para Codex y como contrato técnico inicial del motor.

---

# 1. DECISIÓN PRINCIPAL

El proyecto parte del template:

`https://github.com/stesproject/godot-2d-topdown-template`

No se utilizará como motor MMO completo.

Se utilizará como:

- base visual;
- base de escenas;
- base de interacción;
- base de state machines;
- base de UI;
- base de controller;
- base de diálogos;
- base de preferencias;
- base de navegación y presentación 2D.

Encima y debajo de esta base se construirá nuestra arquitectura MMO.

Modelo conceptual:

```text
Godot 2D Top-Down Template
        ↓
CLIENTE / PRESENTACIÓN
        ↓
Capa MMO del cliente
        ↓
Protocol / Contracts
        ↓
Servidor autoritativo
        ↓
Domain + Simulation + World
        ↓
Persistence
```

El objetivo NO es convertir el template en un servidor.

El objetivo es convertirlo en el cliente de nuestro MMO.

---

# 2. FUENTES DE REFERENCIA

Se utilizarán tres fuentes con responsabilidades distintas.

## 2.1 Godot 2D Top-Down Template

Referencia principal para:

- movimiento visual;
- player controller;
- state machines;
- interacción;
- escenas;
- UI;
- diálogos;
- niveles;
- transiciones;
- preferencias;
- localización;
- presentación del mundo 2D;
- estructura de proyecto Godot.

## 2.2 Intersect / Broken_Reborn

Se utilizarán únicamente como referencias de ingeniería MMO para estudiar:

- separación Client / Server / Core;
- definiciones de contenido;
- entidades;
- paquetes;
- persistencia;
- inventario;
- items;
- mapas;
- sesiones;
- validación;
- patrones de autoridad;
- herramientas/editor;
- arquitectura de sistemas MMO maduros.

No se debe copiar directamente:

- arquitectura MMO Maker;
- modelos gigantes;
- singletons globales;
- dependencias históricas;
- lógica de juego dentro de packet handlers;
- diseño condicionado por editor;
- mapas o sistemas limitados por la arquitectura histórica de Intersect.

## 2.3 Documento Maestro del Nuevo MMO

Es la fuente autoritativa para:

- Malden;
- Tradiciones;
- atributos;
- combate;
- profesiones;
- recursos;
- crafting;
- economía;
- mundo;
- itemización;
- exploración;
- housing;
- presentación;
- reglas de gameplay.

Cuando Intersect o el template entren en conflicto con el Documento Maestro, gana el Documento Maestro.

---

# 3. PRINCIPIOS DE ARQUITECTURA

## 3.1 Servidor autoritativo

El cliente nunca será autoridad final sobre:

- posición;
- inventario;
- stats;
- HP;
- PM;
- equipo;
- loot;
- crafting;
- mercado;
- guild;
- profesión;
- quest;
- combate;
- economía;
- propiedades de items;
- ownership;
- permisos;
- mundo persistente.

El cliente puede:

- predecir;
- interpolar;
- renderizar;
- reproducir animaciones;
- mostrar UI;
- enviar comandos;
- mantener caché visual;
- guardar preferencias locales.

## 3.2 Godot no es el servidor

El servidor:

- no depende de SceneTree;
- no depende de CharacterBody2D;
- no depende de Node;
- no depende de AnimationTree;
- no depende de TileMap;
- no depende del runtime de Godot.

Godot es la capa de presentación del cliente.

## 3.3 Separación de responsabilidades

```text
CLIENT
  Input
  Rendering
  Animation
  UI
  Prediction
  Interpolation
  Local presentation state

SERVER
  Validation
  Simulation
  Authority
  Persistence
  Economy
  Combat
  Inventory
  World state
  Security
```

## 3.4 Definition != Instance

Regla obligatoria del motor:

```text
Definition = qué puede ser algo
Instance   = qué es esta copia concreta ahora
```

Ejemplo:

```text
ItemDefinition
  id
  name
  allowed_properties
  property_ranges

ItemInstance
  instance_id
  definition_id
  rolled_properties
  quality
  durability
  provenance
```

---

# 4. ESTRUCTURA PROPUESTA DEL REPOSITORIO

```text
NuevoMMO/
│
├── client/
│   │
│   ├── addons/
│   ├── components/
│   ├── dialogues/
│   ├── entities/
│   ├── items/
│   ├── particles/
│   ├── scenes/
│   ├── scripts/
│   ├── shaders/
│   ├── tilesets/
│   │
│   └── mmo/
│       ├── network/
│       ├── protocol/
│       ├── replication/
│       ├── prediction/
│       ├── presentation/
│       ├── adapters/
│       └── state/
│
├── server/
│   ├── NuevoMMO.Server/
│   ├── NuevoMMO.Application/
│   ├── NuevoMMO.Infrastructure/
│   └── NuevoMMO.Persistence/
│
├── core/
│   ├── NuevoMMO.Domain/
│   ├── NuevoMMO.Gameplay/
│   ├── NuevoMMO.Simulation/
│   └── NuevoMMO.World/
│
├── shared/
│   ├── NuevoMMO.Contracts/
│   ├── NuevoMMO.Protocol/
│   └── NuevoMMO.Primitives/
│
├── content/
│   ├── NuevoMMO.Content/
│   └── NuevoMMO.Content.Compiler/
│
├── tools/
├── tests/
└── docs/
```

---

# 5. RESPONSABILIDADES DE CADA CAPA

## 5.1 Client

Responsable de:

- input;
- cámara;
- sprites;
- animaciones;
- UI;
- audio;
- partículas;
- state machines visuales;
- interacción visual;
- prediction;
- reconciliation;
- interpolation;
- representación de entidades remotas;
- caché local de datos permitidos.

## 5.2 Shared

Contiene solo conceptos realmente compartidos.

Ejemplos:

```text
EntityId
CharacterId
ItemDefinitionId
ItemInstanceId
MapId
RegionId
Vector2 primitive
Enums
DTOs
Network messages
Serialization contracts
```

No meter gameplay aquí.

## 5.3 Domain

Contiene las reglas de negocio del MMO.

Ejemplos:

```text
Characters
Attributes
Items
Inventory
Equipment
Traditions
Techniques
Effects
Professions
Resources
Crafting
Creatures
Guilds
Economy
Ownership
Permissions
Containers
```

No depende de:

- Godot;
- PostgreSQL;
- networking;
- filesystem;
- UI.

## 5.4 Gameplay / Application

Casos de uso.

Ejemplos:

```text
MoveCharacterCommand
AttackCommand
EquipItemCommand
MoveItemCommand
GatherResourceCommand
CraftItemCommand
LearnTechniqueCommand
TradeCommand
InteractCommand
```

## 5.5 Simulation

Responsable de:

- tick fijo;
- movement simulation;
- combat processing;
- effects;
- AI;
- respawns;
- scheduled events;
- world clock;
- resource regeneration.

## 5.6 World

Responsable de:

- regiones;
- mapas;
- chunks;
- collision lógica;
- navegación;
- spawns;
- spatial queries;
- interest management;
- world events.

## 5.7 Persistence

Responsable de:

- PostgreSQL;
- repositorios;
- transacciones;
- migrations;
- mappings;
- carga/guardado de estado.

Separación conceptual:

```text
auth.*
content.*
game.*
audit.*
```

---

# 6. ESTRATEGIA GDSCRIPT + C#

No reescribir el template entero.

## Mantener GDScript para:

- UI;
- scenes;
- animation;
- visual state machines;
- interaction feedback;
- transitions;
- presentation;
- dialogue presentation;
- small scene-specific behavior.

## Usar C# para:

- network client;
- protocol;
- replication;
- prediction;
- reconciliation;
- client game state;
- shared contracts;
- server;
- domain;
- simulation;
- persistence.

Puente esperado:

```text
Player.gd
   ↓
NetworkBridge.cs
   ↓
Shared.Contracts
   ↓
Server
```

---

# 7. MAPA KEEP / ADAPT / REPLACE

## KEEP

Conservar con cambios mínimos:

- pixel-art rendering settings;
- integer scaling;
- camera/presentation;
- scene transitions;
- visual state machine pattern;
- dialogue presentation;
- loading screens;
- localization;
- preferences;
- debugger utilities;
- reusable UI components;
- level scene organization;
- effects and particles.

## ADAPT

Mantener concepto, cambiar autoridad:

### Player controller
De:
```text
Input → move_and_slide → posición final
```

A:
```text
Input
→ InputFrame
→ local prediction
→ MoveCommand
→ server
→ snapshot
→ reconciliation
```

### Inventory
De:
```text
Inventory node = estado real
```

A:
```text
Inventory UI/View
→ MoveItemRequest
→ server validates
→ InventorySnapshot / Delta
→ UI refresh
```

### Health
De:
```text
client subtracts damage
```

A:
```text
server damage calculation
→ HealthChanged
→ client renders
```

### Interaction
De:
```text
client executes interaction
```

A:
```text
client detects candidate
→ InteractRequest(EntityId)
→ server validates
→ interaction result
```

### Dialogue
Mantener presentación local, pero cualquier opción con consecuencias debe pasar por servidor.

## REPLACE

Reemplazar como fuente de verdad:

- local character save;
- local inventory persistence;
- local position persistence;
- local authoritative combat;
- local authoritative loot;
- local authoritative quest changes;
- local authoritative economy;
- local authoritative profession progress;
- local authoritative guild state.

---

# 8. SISTEMA DE RED INICIAL

Primer objetivo:

```text
Client
  ↓
HandshakeRequest
  ↓
Server
  ↓
HandshakeAccepted
  ↓
SessionCreated
```

Luego:

```text
InputFrame
MoveCommand
EntitySnapshot
WorldSnapshot
SpawnEntity
DespawnEntity
EntityStateChanged
Ping
Pong
Disconnect
```

No definir todavía el protocolo completo del MMO.

Primero validar la columna vertebral.

---

# 9. SIMULACIÓN Y TICK

Objetivo inicial:

```text
Server simulation: 20 TPS
Client rendering: independiente
```

Regla:

```text
SimulationTime != RenderFPS
```

El valor de 20 TPS es punto de partida técnico, no contrato final.

---

# 10. INTEREST MANAGEMENT

Debe existir temprano.

Cada jugador recibe únicamente entidades relevantes.

Concepto:

```text
WORLD
 ├── Player A
 │     └── AOI
 │          ├── mob
 │          ├── player
 │          └── resource
 │
 └── Player B
       └── AOI
```

Entidades potenciales:

- jugadores;
- mobs;
- NPCs;
- drops;
- recursos;
- projectiles;
- structures;
- pets;
- world objects.

---

# 11. PRIMER VERTICAL SLICE

El primer hito real del motor será:

```text
Server inicia
↓
Client Godot conecta
↓
Handshake
↓
Server crea PlayerEntity
↓
Client envía input
↓
Server simula movimiento
↓
Server envía snapshot
↓
Client interpola/reconcilia
↓
Segundo cliente entra
↓
Ambos jugadores se ven moverse
```

No implementar todavía:

- profesiones;
- mercado;
- guilds;
- crafting completo;
- Tradiciones completas;
- quests;
- housing.

Hasta que esta columna vertebral funcione.

---

# 12. RUTA DE TAREAS PARA CODEX

# FASE 0 — PREPARAR EL FORK

## Task 0.1 — Crear fork limpio del template
Objetivo:
- importar el template como `client/`;
- preservar licencia;
- confirmar que abre y ejecuta sin errores.

Done when:
- proyecto inicia;
- escena principal carga;
- no hay errores de import críticos.

## Task 0.2 — Renombrar proyecto
Cambiar:
- nombre del proyecto;
- namespace cuando aplique;
- referencias visuales del template;
- documentación raíz.

Done when:
- proyecto ya no presenta branding del template salvo licencia/créditos.

## Task 0.3 — Limpiar demo
Eliminar solo contenido claramente de ejemplo.

NO eliminar aún:
- state machines;
- interaction;
- dialogue;
- save/preferences;
- loading;
- localization;
- controllers;
- reusable scenes.

Done when:
- queda una pequeña escena funcional de prueba.

---

# FASE 1 — CREAR LA SOLUCIÓN .NET

## Task 1.1 — Crear solution
Crear:

```text
NuevoMMO.sln
```

Proyectos mínimos:

```text
NuevoMMO.Server
NuevoMMO.Domain
NuevoMMO.Simulation
NuevoMMO.Contracts
NuevoMMO.Persistence
NuevoMMO.Tests
```

Done when:
- `dotnet build` pasa limpio.

## Task 1.2 — Configurar dependencias

Permitido:

```text
Server → Domain
Server → Simulation
Server → Contracts
Server → Persistence

Simulation → Domain
Simulation → Contracts

Persistence → Domain

Tests → all required projects
```

No permitido:

```text
Domain → Godot
Domain → Persistence
Domain → Server
Domain → Networking implementation
```

Done when:
- referencias cumplen esta regla;
- no hay dependencia circular.

---

# FASE 2 — PRIMITIVES Y CONTRACTS

## Task 2.1 — IDs tipados

Implementar:

```text
EntityId
CharacterId
AccountId
MapId
RegionId
ItemDefinitionId
ItemInstanceId
```

Usar strongly typed IDs.

## Task 2.2 — Vector/Position compartido

Crear primitive independiente de Godot.

Ejemplo conceptual:

```csharp
public readonly record struct WorldPosition(float X, float Y);
```

## Task 2.3 — Mensajes iniciales

Crear contracts para:

```text
HandshakeRequest
HandshakeAccepted
HandshakeRejected
MoveCommand
PlayerSnapshot
SpawnEntity
DespawnEntity
Ping
Pong
```

Done when:
- contracts serializan/deserializan en tests.

---

# FASE 3 — SERVIDOR MÍNIMO

## Task 3.1 — Host

Crear ejecutable del servidor con:

- configuration;
- logging;
- cancellation;
- graceful shutdown.

## Task 3.2 — Game loop

Crear simulation loop fijo.

Objetivo inicial:
- 20 ticks/s;
- medir drift;
- no usar Godot.

## Task 3.3 — Session model

Crear:

```text
ConnectionId
PlayerSession
SessionState
```

Estados mínimos:

```text
Connected
Handshaking
AuthenticatedPlaceholder
InWorld
Disconnected
```

No implementar auth real todavía.

---

# FASE 4 — CLIENT NETWORK LAYER

## Task 4.1 — Crear `client/mmo/`

Subcarpetas:

```text
network/
protocol/
replication/
prediction/
state/
adapters/
```

## Task 4.2 — NetworkBridge.cs

Crear puente C# accesible desde GDScript.

Responsabilidades:

- conectar;
- desconectar;
- enviar command;
- recibir messages;
- exponer events/signals al cliente.

## Task 4.3 — Estado de conexión visual

Añadir UI mínima:

```text
Disconnected
Connecting
Connected
InWorld
```

Done when:
- Godot conecta al server y recibe handshake.

---

# FASE 5 — PLAYER ENTITY

## Task 5.1 — Server PlayerEntity

Crear entidad lógica:

```text
PlayerEntity
  EntityId
  CharacterId
  Position
  Velocity
  Facing
```

No stats aún.

## Task 5.2 — Client PlayerView

Reutilizar el controller/player scene del template.

Separar:

```text
PlayerView
PlayerInputController
PlayerPresentation
```

## Task 5.3 — RemotePlayerView

Crear representación para otro jugador.

Debe compartir:

- sprite;
- animation;
- facing;
- movement state.

No debe compartir input local.

Done when:
- una remote entity puede renderizarse desde un snapshot artificial.

---

# FASE 6 — MOVIMIENTO AUTORITATIVO

## Task 6.1 — InputFrame

El cliente produce input lógico.

Ejemplo:

```text
sequence
direction
timestamp/tick
run
```

## Task 6.2 — Prediction local

El jugador local debe moverse inmediatamente.

## Task 6.3 — Server simulation

El server:

- valida input;
- calcula movimiento;
- actualiza PlayerEntity.

## Task 6.4 — Snapshot

El server devuelve:

```text
server_tick
entity_id
position
velocity
last_processed_input
```

## Task 6.5 — Reconciliation

El cliente:

- compara predicción;
- corrige diferencias;
- reaplica inputs pendientes.

## Task 6.6 — Interpolation remota

Remote players usan buffer de snapshots.

Done when:
- dos clientes se ven mover suavemente;
- posición real vive en servidor;
- teleport manual del cliente no altera servidor.

---

# FASE 7 — SPAWN / DESPAWN / WORLD

## Task 7.1 — WorldState mínimo

Crear:

```text
WorldInstance
EntityRegistry
```

## Task 7.2 — Spawn / Despawn

Mensajes:

```text
SpawnEntity
DespawnEntity
```

## Task 7.3 — AOI inicial

Implementar interest management simple por distancia o grid.

No optimizar prematuramente.

Done when:
- entities fuera del AOI dejan de replicarse.

---

# FASE 8 — ADAPTAR INTERACTION SYSTEM

## Task 8.1 — Interactable detection

Conservar detection visual del template.

## Task 8.2 — InteractRequest

Enviar:

```text
player_entity_id
target_entity_id
interaction_type
```

## Task 8.3 — Server validation

Validar:

- distancia;
- estado;
- target existente;
- permisos básicos.

Done when:
- el cliente no puede interactuar con objeto fuera de rango alterando memoria local.

---

# FASE 9 — ADAPTAR SAVE SYSTEM

## Task 9.1 — Separar preferencias y GameState

Mantener local:

- audio;
- video;
- idioma;
- keybinds;
- UI preferences.

Eliminar del save local:

- posición autoritativa;
- inventario;
- stats;
- character state.

## Task 9.2 — Crear Persistence interfaces

Inicialmente:

```text
ICharacterRepository
IAccountRepository
```

Sin necesidad de implementar todos los sistemas.

---

# FASE 10 — POSTGRESQL MÍNIMO

## Task 10.1 — Migrations

Crear esquemas:

```text
auth
content
game
audit
```

## Task 10.2 — Tablas mínimas

```text
auth.accounts
game.characters
```

Campos mínimos de character:

```text
id
account_id
name
map_id
position_x
position_y
created_at
updated_at
```

## Task 10.3 — Character load/save

Flujo:

```text
session
↓
load character
↓
spawn entity
↓
play
↓
disconnect
↓
persist state
```

Done when:
- reconectar restaura posición validada del servidor.

---

# FASE 11 — PRIMERA CAPA DE DOMAIN

Solo después del networking.

Implementar:

```text
CharacterAttributes
HealthResource
MaldenResource
```

Primarios:

```text
STR
INT
AGI
SPI
VIT
```

No definir aún fórmulas finales.

Crear interfaces/configuración para no hardcodear balance definitivo.

---

# FASE 12 — ITEM FOUNDATION

Implementar:

```text
ItemDefinition
ItemInstance
ItemPropertyDefinition
ItemPropertyValue
ItemQuality
Durability
ItemProvenance
```

Regla:

```text
Definition != Instance
```

Luego:

```text
Container
Inventory
Equipment
```

El cliente recibe representación, no autoridad.

---

# FASE 13 — ADAPTAR INVENTARIO DEL TEMPLATE

Transformar inventario actual a:

```text
InventoryView
InventorySlotView
ItemTooltipView
DragDropController
```

Flujo:

```text
drag
↓
MoveItemRequest
↓
server
↓
InventoryDelta
↓
UI
```

Done when:
- editar el inventario solo en cliente no cambia el estado del servidor.

---

# FASE 14 — COMBAT SKELETON

Solo esqueleto.

Implementar:

```text
AttackCommand
Target validation
Range check
Cooldown placeholder
DamageResult
HealthChanged
EntityDied
```

No cerrar fórmulas.

Cliente:

- anima;
- muestra feedback;
- no calcula verdad final.

---

# FASE 15 — CONTENT SYSTEM

Crear loader para definitions.

Primeros tipos:

```text
ItemDefinition
CreatureDefinition
TechniqueDefinition
```

Flujo:

```text
JSON/YAML/data
↓
validator
↓
content registry
↓
server
```

Más adelante añadir Content Compiler.

---

# FASE 16 — TESTS OBLIGATORIOS

Añadir pruebas desde temprano.

Mínimo:

```text
StrongId tests
Protocol serialization tests
Movement determinism tests
Inventory ownership tests
ItemDefinition/Instance tests
Session tests
Persistence integration tests
```

---

# 13. REGLAS PARA CODEX

Codex debe seguir estas reglas durante implementación:

1. No reescribir todo el template a C#.
2. No mover lógica autoritativa al cliente.
3. No usar Godot como servidor MMO.
4. No introducir sistemas completos antes de terminar la columna vertebral.
5. No inventar mecánicas faltantes del Documento Maestro.
6. No copiar Intersect literalmente.
7. No colocar gameplay dentro de handlers de red.
8. No convertir `Shared` en un vertedero.
9. No acoplar `Domain` a PostgreSQL.
10. No acoplar `Domain` a Godot.
11. No meter balance definitivo todavía.
12. Mantener commits pequeños y temáticos.
13. Cada nueva capa debe incluir tests cuando tenga lógica determinista.
14. Preferir interfaces en fronteras de infraestructura.
15. Mantener autoridad del servidor incluso si el cliente predice.
16. Preservar la licencia MIT del template y sus avisos correspondientes.
17. Documentar decisiones arquitectónicas importantes en `docs/adr/`.

---

# 14. ADRs INICIALES A CREAR

```text
ADR-0001-template-as-client-base.md
ADR-0002-authoritative-server.md
ADR-0003-godot-not-server-runtime.md
ADR-0004-gdscript-csharp-split.md
ADR-0005-definition-vs-instance.md
ADR-0006-fixed-server-tick.md
ADR-0007-interest-management.md
ADR-0008-client-save-vs-server-persistence.md
```

---

# 15. PRIMERA META DE CODEX

La primera entrega no es combate, inventario ni profesión.

La primera entrega es:

```text
[✓] Template corre
[✓] Server corre
[✓] Client conecta
[✓] Handshake
[✓] PlayerEntity
[✓] InputFrame
[✓] Server movement
[✓] Snapshot
[✓] Local prediction
[✓] Reconciliation
[✓] Remote interpolation
[✓] Segundo cliente visible
```

Cuando esto funcione, el motor tiene una columna vertebral MMO real.

---

# 16. DEFINITION OF DONE DEL VERTICAL SLICE 0

El Vertical Slice 0 se considera completo cuando:

- dos clientes Godot pueden conectarse al mismo server;
- cada cliente recibe un `EntityId`;
- cada jugador puede moverse;
- el server es autoridad sobre la posición;
- el cliente local usa prediction;
- el server corrige posiciones inválidas;
- jugadores remotos interpolan suavemente;
- spawn/despawn funciona;
- desconectar elimina la entidad;
- reconectar puede recrear la entidad;
- no existe dependencia de Godot en `Domain` ni `Simulation`;
- el build .NET pasa;
- tests básicos pasan;
- el template conserva sus capacidades visuales reutilizables;
- el estado autoritativo no se guarda en archivos locales del cliente.

---

# 17. ORDEN INMEDIATO RECOMENDADO

Codex debe empezar exactamente aquí:

```text
01 Fork/import del template
02 Limpieza mínima
03 Solution .NET
04 Contracts + typed IDs
05 Server host
06 Fixed tick
07 NetworkBridge
08 Handshake
09 PlayerEntity
10 InputFrame
11 Server movement
12 Snapshot
13 Prediction
14 Reconciliation
15 RemotePlayerView
16 Segundo cliente
17 Spawn/despawn
18 AOI básico
19 Persistencia mínima
20 Stats/items foundation
```

No saltar a sistemas superiores hasta validar este flujo.

---

# FIN
