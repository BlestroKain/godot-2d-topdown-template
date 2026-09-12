# Arquitectura vigente

Leer primero [design.md](design.md). Esta página define las fronteras; [TECHNICAL_ARCHITECTURE.md](TECHNICAL_ARCHITECTURE.md) describe el objetivo y [IMPLEMENTATION_STATUS.md](IMPLEMENTATION_STATUS.md) registra exclusivamente el estado observado.

## Dependencias

`Core ← Network ← Server`

`Core ← Network ← Client/Godot C# (+ componentes GDScript existentes)`

`Core ← Network ← Editor/Godot C#`

Core no conoce Godot, Network, Server, Client, Editor, PostgreSQL ni transporte. Network conoce Core y contiene protocolo/transporte, nunca ejecución de gameplay. Server contiene las entidades runtime y la simulación autoritativa. Client y Editor son aplicaciones Godot separadas. **Superseded**: la división provisional `Contracts + Protocol + Domain + Simulation` como arquitectura pública; sus piezas se consolidan sin duplicar modelos.

Los addons Godot viven exclusivamente detrás de adaptadores de Client/Editor. Pueden representar estados, contenedores, abilities, diálogos o herramientas de authoring, pero no entran en Core/Network/Server ni aceptan resultados locales como verdad. Sus versiones/licencias se fijan en `Client/addons/addons.lock.json`.

## Autoridad

Input → Intent → Command → validación de sesión → validación de dominio → System → eventos → persistencia/replicación.

El cliente aplica una proyección autorizada: estado + AOI + visibilidad + conocimiento + permisos + relevancia. AOI, visibilidad y Simulation Tier son conceptos distintos. Snapshot inicial y deltas posteriores; no enviar secretos ni snapshots globales continuos.

Movimiento: tick fijo servidor, input secuenciado, acknowledgement del input realmente procesado, predicción inmediata y replay cliente; buffer de interpolación remota. El reloj/FPS cliente no fija la distancia permitida. 20 TPS es referencia técnica inicial, no balance.

## Adaptación del template

Conservar arte, escenas, escalado entero, cámara, UI, preferencias, localización, diálogos y máquinas de estados visuales. Adaptar controller, interacción, inventario y vida a comandos/proyecciones. Reemplazar autoridad de combate, loot y guardado local de personaje.

La escena MMO en C# no instancia el controller single-player, inventario, combate ni niveles de demo. Reutiliza los SpriteFrames extraídos de la escena original y sus preferencias. Las piezas originales permanecen disponibles para adaptación incremental. Véase CLASS_MAP.md y ADR-0002.

## Datos

ContentGuid + ContentKey estable/legible; DisplayName no es identidad. ContentPackage versionado y validado; servidor sin texturas; VisualKey resuelto por AssetRegistry cliente. World → Region → Map → Area → Entity; chunks son optimización.

PostgreSQL es destino de persistencia diseñado, no servicio instalado por esta preparación. Fronteras auth/content/game/audit; repositorios por responsabilidad, runtime en memoria, transacciones críticas atómicas e idempotentes cuando corresponda. No persistir cada frame.

## Alcance técnico

Monolito modular inicialmente. Sin microservicios, Kafka, Redis o Kubernetes por anticipación. Tokens dev solo Development/Test. Fixtures exclusivamente en server/Fixtures. Sistemas superiores esperan validación del slice y diseño suficiente.
