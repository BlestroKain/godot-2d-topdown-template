# Frontend / Lobby del MMO

Este módulo controla todo lo que ocurre antes de entrar al mapa. La referencia de UX es un MMO clásico tipo Dofus: pantallas completas, navegación por etapas y selección de personaje visual; los assets, materiales y composición pertenecen a la identidad tecnomágica propia del proyecto.

## Flujo actual

`Boot -> Login/Register -> Server Select -> Character Select -> Character Create -> Loading World -> InGame`

`Settings` puede abrirse tanto desde el frontend como desde el menú Escape ingame y utiliza el mismo `ClientSettingsStore`.

## Autoridad

- El cliente puede previsualizar y solicitar acciones, pero no crea cuentas/personajes por su cuenta.
- Registro y login son resueltos por `AuthService` en servidor.
- La sesión autenticada usa `SessionId + SessionToken`.
- La lista, creación y selección de personajes pasan por `CharacterService`/handlers del servidor.
- `GameConnection` mantiene una fase lobby request/response y solo inicia el receptor realtime después de `EnterWorldAsync`.
- `MapReady` sigue siendo el límite entre lobby y activación del mundo.

## Escenas y scripts

- `frontend_root.tscn`: composición editable de las pantallas.
- `FrontendFlowController.cs`: navegación y binding con `NetworkBridge`.
- `ClientSettingsStore.cs`: preferencias locales compartidas pre-game/ingame.
- `NetworkBridge.cs`: fachada Godot para lobby y runtime.
- `GameConnection.cs`: transporte TCP y protocolo.

## Creación de personaje

La primera integración crea un personaje real y persistente con nombre y el preset visual base del runtime. No se inventaron sliders/opciones cosméticas que todavía no tengan catálogo de sprites.

El siguiente paso visual es conectar un catálogo `CharacterAppearance` data-driven al renderer real/SubViewport y persistir, como mínimo, anatomía/preset corporal, rostro/cabello, ojos, orejas/cuernos o rasgos opcionales y pigmentos/colores. La selección visual NO representa raza y NO elige Tradición: la Tradición se adopta dentro del mundo.

## Servidores

La pantalla de selección existe desde ahora para no acoplar cuenta/personaje al mundo. El vertical slice actual expone un único endpoint técnico local; cuando exista un directorio de mundos, esta pantalla consumirá su listado en vez de cambiar el flujo de UI.

## Pendiente explícito

- Catálogo/persistencia de `CharacterAppearance` y preview real con `SubViewport`.
- Directorio real de mundos/servidores, población, región y latencia.
- Eliminar/renombrar personaje con confirmaciones y autoridad de servidor.
- Recuperación de cuenta/contraseña cuando exista el canal de recuperación seguro.
- Fondos/escenarios ilustrados y animados definitivos del frontend.
