# Nuevo MMO

Repositorio principal del nuevo MMO, basado en Godot 2D Top-Down Template. Godot es el cliente; el servidor C#/.NET es independiente y autoritativo.

Arquitectura: `Core` → `Network` → `Server` / `Client` / `Editor`.

## Empezar

Abrir `Client/project.godot` en Godot .NET.

Servidor Development: abrir `NuevoMMO.sln`, establecer **NuevoMMO.Server** como proyecto de inicio y Compilar / Depurar. El ejecutable queda en `Server/NuevoMMO.Server/bin/Debug/net8.0/NuevoMMO.Server.exe`.

Desde la consola: `dotnet run --project Server/NuevoMMO.Server`.

Cliente exportado (Windows): `./tools/export-client.ps1`. El exe queda en `dist/client/NuevoMMO.Client.exe`. Puedes lanzar dos copias para probar conexiones.

Desde el editor: `Client/project.godot` → Proyecto → Exportar → Windows Desktop.

Desde PowerShell, editor: `./tools/run-client.ps1 -GodotExe <ruta-a-Godot>`.
Para importación headless: `./tools/verify-client.ps1 -GodotExe <ruta-a-Godot>`.

- [Diseño vigente](docs/design.md)
- [Arquitectura](docs/architecture/overview.md)
- [Ruta de trabajo](docs/roadmap.md)
- [Estado real y validación](docs/IMPLEMENTATION_STATUS.md)
- [Reglas para trabajar](AGENTS.md)

El primer hito es movimiento entre dos clientes con autoridad servidor, predicción, reconciliación, interpolación y un mob de fixture. Las fases completas de gameplay se implementarán después.

## Procedencia

Template base: stesproject/godot-2d-topdown-template, revisión `ea95789514fe28532d4ed444bdd06a9d4cddad31`. Se conserva [LICENSE](LICENSE), todos los avisos incluidos y el [README original con créditos](docs/TEMPLATE_README.md). Sus recursos y comportamientos de demo no son canon del MMO.
