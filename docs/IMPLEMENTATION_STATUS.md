# Estado real

Revisión: 2026-09-09. Arquitectura `Core / Network / Server / Client / Editor` creada sobre el template. El slice de movimiento autoritativo se migró al protocolo nuevo.

## Incorporado y comprobado

- Template trasladado íntegramente a `Client/`, conservando rutas `res://`, recursos, GDScript reutilizable, créditos y licencia.
- Solución C#/.NET 8 con `NuevoMMO.Core` sin Godot, `NuevoMMO.Network` (PacketId/codec/ENet + TCP de pruebas), `NuevoMMO.Server`, `NuevoMMO.Client.Core`, `NuevoMMO.Editor` y tests.
- Definitions, States, Stats, ItemInstance, Entity/LivingEntity/Player/Mob y DefinitionRegistry.
- Flujo Connect → Login → Character list/create/select → MapLoad → movimiento autoritativo.
- Servidor Development con tick fijo, input secuenciado, AOI, mob de fixture, persistencia en memoria y cierre ordenado.
- Cliente Godot C# con `NetworkBridge`, predicción/reconciliación local e interpolación remota.
- Editor offline mínimo sobre `Core.Definitions`.

## En implementación por decisión posterior

- ENet como transporte runtime de producción; TCP se conserva como adaptador de pruebas.
- PostgreSQL real para cuentas, sesiones y checkpoint; el schema SQL está preparado.
- Mapa visual dedicado importado desde TileMap, Editor Godot y validación de dos procesos Godot sobre el protocolo nuevo.
- CI de .NET, PostgreSQL y smoke Godot; releases únicamente mediante tag manual.

## Límites actuales

- El handshake vigente usa nombre Development y no es autenticación real.
- El mapa técnico actual usa límites rectangulares; todavía no importa colisiones de TileMap.
- El proceso Godot puede informar recursos retenidos del template al salir; no se considera limpieza cerrada hasta resolverlo.
- No se ha validado exportación Android/iOS ni un canal cifrado para login remoto.
- Combate, economía, guilds, quests, crafting, profesiones, PvP, loot y AI avanzada permanecen `PENDING DESIGN` o `FUTURE FEATURE`.

## Repetición de evidencia

```powershell
dotnet build NuevoMMO.sln -nologo
dotnet run --project tests/NuevoMMO.Tests/NuevoMMO.Tests.csproj --no-build
./tools/verify-mmo.ps1 -GodotExe <ruta-a-Godot-.NET-4.7.1>
```

La última ejecución registrada terminó con `OK: 76 comprobaciones` y `OK: dos procesos Godot, movimiento local/remoto y despawn verificados`.
