# Estado real

Revisión: 2026-09-09. Checkpoint TCP del Vertical Slice 0 validado; migración a la arquitectura definitiva en curso.

## Incorporado y comprobado

- Template trasladado íntegramente a `client/`, conservando rutas `res://`, recursos, GDScript reutilizable, créditos y licencia.
- Solución C#/.NET 8 con contratos tipados, codec binario acotado y servidor independiente de Godot.
- Servidor autoritativo Development con tick fijo, input secuenciado, AOI, spawn/despawn y cierre ordenado.
- Cliente Godot C# con `NetworkBridge`, predicción/reconciliación local e interpolación remota.
- Escena MMO separada del controller, inventario, combate y guardado single-player del template.
- Build completo sin errores ni advertencias.
- 76 comprobaciones de protocolo, movimiento, sesiones, AOI y transporte TCP real.
- Prueba headless con dos procesos Godot: ambos ven movimiento local/remoto y el segundo observa el despawn del primero.

## En implementación por decisión posterior

- Consolidación en `Core`, `Network`, `Server`, `Client`, `Editor` y `Tests`.
- ENet como transporte runtime; TCP se conserva solo como adaptador de pruebas.
- Flujo de cuenta, sesión revocable, personaje y checkpoint de posición con PostgreSQL.
- Primer mapa visual dedicado, primer mob de fixture y Editor offline mínimo.
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
