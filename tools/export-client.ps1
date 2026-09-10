param(
    [string]$GodotExe = $env:GODOT_EXE,
    [string]$Output = "dist/client"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$client = Join-Path $root "Client"
$outputDir = Join-Path $root $Output
$exe = Join-Path $outputDir "NuevoMMO.Client.exe"

if (-not $GodotExe) {
    $godotCmd = Get-Command "Godot_v4.7.1-stable_mono_win64.exe" -ErrorAction SilentlyContinue
    if ($godotCmd) { $GodotExe = $godotCmd.Source }
}
if (-not $GodotExe -or -not (Test-Path $GodotExe)) {
    throw "Indica Godot .NET 4.7.1 con -GodotExe o la variable GODOT_EXE."
}

New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

Write-Host "Compilando NuevoMMO.Client..."
dotnet build (Join-Path $client "NuevoMMO.Client.csproj") -c Debug --nologo
if ($LASTEXITCODE -ne 0) { throw "Compilación C# del cliente falló." }

Write-Host "Importando proyecto Godot..."
& $GodotExe --headless --path $client --import --quit
if ($LASTEXITCODE -ne 0) { throw "Godot --import falló." }

Write-Host "Exportando Windows Desktop (debug)..."
& $GodotExe --headless --path $client --export-debug "Windows Desktop" $exe
if ($LASTEXITCODE -ne 0) { throw "Godot --export-debug falló." }
if (-not (Test-Path $exe)) { throw "No se generó $exe" }

Write-Host ""
Write-Host "Cliente exportado en: $outputDir"
Write-Host "Ejecutable: $exe"
Write-Host "Puedes abrir dos copias del exe, cada una con un nombre distinto, contra el servidor en 127.0.0.1:7777."
