param(
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

if (-not $NoBuild) {
    Write-Host "[NuevoMMO] Compilando solución..."
    dotnet build NuevoMMO.sln -nologo
    if ($LASTEXITCODE -ne 0) { throw "Falló dotnet build." }
}

Write-Host "[NuevoMMO] Iniciando servidor con SQLite..."
dotnet run --project "Server/NuevoMMO.Server/NuevoMMO.Server.csproj" --no-build
