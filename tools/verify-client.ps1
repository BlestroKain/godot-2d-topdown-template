param([Parameter(Mandatory = $true)][string]$GodotExe)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot
$clientRoot = Join-Path $repoRoot 'client'
$artifactRoot = Join-Path $repoRoot 'artifacts'
New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
dotnet build (Join-Path $clientRoot 'NuevoMMO.Client.csproj') --nologo
if ($LASTEXITCODE -ne 0) { throw 'Compilación del cliente falló.' }

foreach ($phase in @('import', 'smoke')) {
    $logPath = Join-Path $artifactRoot "client-$phase.log"
    $godotArgs = @('--headless', '--path', $clientRoot)
    if ($phase -eq 'import') { $godotArgs += @('--editor', '--import') }
    else { $godotArgs += @('--quit-after', '120') }
    & $GodotExe @godotArgs *> $logPath
    if ($LASTEXITCODE -ne 0) { throw "Godot falló en $phase. Ver $logPath" }
    $logText = Get-Content -LiteralPath $logPath -Raw
    # Known template shutdown diagnostic is reported, never silently counted as clean.
    $errors = @($logText -split '\r?\n' | Where-Object {
        $_ -match '(SCRIPT ERROR|ERROR):' -and $_ -notmatch '^ERROR: 1 resources still in use at exit'
    })
    if ($errors.Count -gt 0) { throw "Errores en ${phase}: $($errors -join [Environment]::NewLine)" }
    if ($logText -match '(leaked at exit|resources still in use at exit)') {
        Write-Warning "${phase}: recursos retenidos al cerrar Godot. Ver $logPath; no es validación libre de avisos."
    }
    Write-Output "${phase}: proceso terminado y sin errores de script/importación."
}
