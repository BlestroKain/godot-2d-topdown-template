param([int]$Port = 7777)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot
dotnet run --project (Join-Path $repoRoot 'server/NuevoMMO.Server/NuevoMMO.Server.csproj') -- --development --port $Port
if ($LASTEXITCODE -ne 0) { throw 'El servidor terminó con error.' }
