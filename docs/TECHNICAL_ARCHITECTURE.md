# Objetivo técnico

Este documento no certifica implementación. Fuentes: [canon actualizado](sources/PROYECTO_MMO_NUEVO_REGLAS_ACTUALIZADAS.md) y [ruta cliente](sources/Nuevo_MMO_Ruta_Motor_Codex.md).

| Ubicación objetivo | Responsabilidad |
| --- | --- |
| client/ | Template Godot: nueva funcionalidad C#, componentes GDScript conservados |
| client/mmo/ | network, protocol, replication, prediction, presentation, adapters, state |
| shared/ | IDs, primitivas, DTOs, contratos y serialización |
| core/ | Domain, Simulation y World sin Godot ni infraestructura |
| server/ | Host, Application, Infrastructure y Persistence |
| server/Fixtures/ | Datos sintéticos exclusivos de Development/Test |
| content/ | Definitions, paquetes, validadores y compiler |
| tools/ | Arranque, validación y herramientas de contenido |
| tests/ | Dominio, protocolo, movimiento, sesiones y persistencia |

Una feature debe declarar: owner, client-only/server-authoritative/shared, GameData, GameState, persistencia, replicación y audit. Las reglas pendientes quedan explícitas; una interfaz no equivale a una feature funcional.

## Fases canónicas A–F

A. Retirar acoplamientos a fixtures productivas sin romper lo validado.
B. WorldRuntime, regiones, mapas/instancias, transiciones, chunks, streaming y AOI.
C. Router/handlers, eventos y proyección de replicación.
D. Identidad de contenido, paquetes, validadores, compiler, referencias y AssetRegistry.
E. Containers, ItemLocation, Ownership, InventoryTransaction, Wallet, Escrow y audit.
F. Gameplay sobre las bases anteriores y con diseño suficiente.

Se aplican incrementalmente. Primero validar la columna vertebral cliente-servidor descrita en el roadmap; no implementar A–F como un big-bang rewrite.
