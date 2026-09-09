param([Parameter(Mandatory = $true)][string]$GodotExe)
$ErrorActionPreference = 'Stop'
$clientRoot = Join-Path (Split-Path $PSScriptRoot) 'client'
dotnet build (Join-Path $clientRoot 'NuevoMMO.Client.csproj') --nologo
if ($LASTEXITCODE -ne 0) { throw 'Compilación del cliente falló.' }
& $GodotExe --path $clientRoot
if ($LASTEXITCODE -ne 0) { throw 'Godot terminó con error.' }
