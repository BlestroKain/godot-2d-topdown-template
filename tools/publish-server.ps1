param(
    [string]$Runtime = "win-x64",
    [string]$Output = "dist/server"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "Server/NuevoMMO.Server/NuevoMMO.Server.csproj"
$outputPath = Join-Path $root $Output

Write-Host "Publicando NuevoMMO.Server para $Runtime..."

dotnet publish $project `
    -c Release `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishTrimmed=false `
    -o $outputPath

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish falló con código $LASTEXITCODE."
}

Write-Host ""
Write-Host "Servidor publicado en: $outputPath"
Write-Host "Ejecutable esperado: $(Join-Path $outputPath 'NuevoMMO.Server.exe')"
Write-Host "Configuración: $(Join-Path $outputPath 'config.json')"
