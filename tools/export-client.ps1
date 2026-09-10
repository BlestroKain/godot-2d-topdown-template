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
    if (-not $godotCmd) { $godotCmd = Get-Command "Godot_v4.7.1-stable_mono_win64_console.exe" -ErrorAction SilentlyContinue }
    if ($godotCmd) { $GodotExe = $godotCmd.Source }
}
if ($GodotExe -like "*_console.exe") {
    $guiCandidate = $GodotExe -replace "_console\.exe$", ".exe"
    if (Test-Path $guiCandidate) { $GodotExe = $guiCandidate }
}
if (-not $GodotExe -or -not (Test-Path $GodotExe)) {
    throw "Indica Godot .NET 4.7.1 con -GodotExe o la variable GODOT_EXE."
}

New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

$templateDir = Join-Path $env:APPDATA "Godot\export_templates\4.7.1.stable.mono"
New-Item -ItemType Directory -Path $templateDir -Force | Out-Null
$debugTemplate = Join-Path $templateDir "windows_debug_x86_64.exe"
$releaseTemplate = Join-Path $templateDir "windows_release_x86_64.exe"
if (-not (Test-Path $debugTemplate) -or -not (Test-Path $releaseTemplate)) {
    Write-Host "Plantillas Windows x64 ausentes; se usa el editor Godot .NET como plantilla de desarrollo."
    Copy-Item $GodotExe $debugTemplate -Force
    Copy-Item $GodotExe $releaseTemplate -Force
}
$consoleGodot = Join-Path (Split-Path $GodotExe) (([IO.Path]::GetFileNameWithoutExtension($GodotExe)) + "_console.exe")
if (Test-Path $consoleGodot) {
    Copy-Item $consoleGodot (Join-Path $templateDir "windows_debug_x86_64_console.exe") -Force
    Copy-Item $consoleGodot (Join-Path $templateDir "windows_release_x86_64_console.exe") -Force
}

Write-Host "Compilando NuevoMMO.Client..."
dotnet build (Join-Path $client "NuevoMMO.Client.csproj") -c Debug --nologo
if ($LASTEXITCODE -ne 0) { throw "Compilación C# del cliente falló." }

Write-Host "Importando proyecto Godot..."
& $GodotExe --headless --path $client --import --quit
if ($LASTEXITCODE -ne 0) { throw "Godot --import falló." }

Write-Host "Exportando Windows Desktop (debug)..."
& $GodotExe --headless --path $client --export-debug WindowsDesktop $exe
if ($LASTEXITCODE -ne 0) { throw "Godot --export-debug falló." }
if (-not (Test-Path $exe)) { throw "No se generó $exe" }

$enet = Join-Path $client "mmo/core/bin/Debug/net8.0/enet.dll"
if (Test-Path $enet) { Copy-Item $enet $outputDir -Force }

Write-Host ""
Write-Host "Cliente exportado en: $outputDir"
Write-Host "Ejecutable: $exe"
Write-Host "Puedes abrir dos copias del exe, cada una con un nombre distinto, contra el servidor en 127.0.0.1:7777."
