$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot
dotnet run --project (Join-Path $repoRoot 'Server/NuevoMMO.Server/NuevoMMO.Server.csproj')
if ($LASTEXITCODE -ne 0) { throw 'El servidor terminó con error.' }
