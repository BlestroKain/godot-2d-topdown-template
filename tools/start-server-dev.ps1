param(
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Write-Host "[NuevoMMO] Iniciando PostgreSQL..."
docker compose -f "Server/docker-compose.postgres.yml" up -d

Write-Host "[NuevoMMO] Esperando PostgreSQL saludable..."
$deadline = (Get-Date).AddSeconds(45)
do {
    $health = docker inspect --format='{{.State.Health.Status}}' nuevommo-postgres 2>$null
    if ($health -eq "healthy") { break }
    if ((Get-Date) -gt $deadline) { throw "PostgreSQL no quedó saludable dentro del tiempo esperado." }
    Start-Sleep -Seconds 1
} while ($true)

if (-not $NoBuild) {
    Write-Host "[NuevoMMO] Compilando solución..."
    dotnet build NuevoMMO.sln -nologo
    if ($LASTEXITCODE -ne 0) { throw "Falló dotnet build." }
}

Write-Host "[NuevoMMO] Iniciando servidor..."
dotnet run --project "Server/NuevoMMO.Server/NuevoMMO.Server.csproj" --no-build
