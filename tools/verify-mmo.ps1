param([Parameter(Mandatory = $true)][string]$GodotExe, [switch]$Render)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot
$artifactRoot = Join-Path $repoRoot 'artifacts'
New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
Push-Location $repoRoot
try {
    dotnet build NuevoMMO.sln --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Build falló.' }
    dotnet run --project tests/NuevoMMO.Tests/NuevoMMO.Tests.csproj --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Pruebas .NET fallaron.' }
    & $GodotExe --headless --editor --path (Join-Path $repoRoot 'client') --import *> (Join-Path $artifactRoot 'mmo-import.log')
    if ($LASTEXITCODE -ne 0) { throw 'Importación falló.' }
    $port = 17777
    $serverDll = Join-Path $repoRoot 'server/NuevoMMO.Server/bin/Debug/net8.0/NuevoMMO.Server.dll'
    $server = Start-Process dotnet -ArgumentList @(('"' + $serverDll + '"'), '--test', '--port', $port) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $artifactRoot 'mmo-server.log') -RedirectStandardError (Join-Path $artifactRoot 'mmo-server-error.log')
    $clients = @()
    try {
        # The probe also checks that this process, rather than a pre-existing listener, owns the test port.
        $ready = $false
        for ($attempt = 0; $attempt -lt 40; $attempt++) {
            if ($server.HasExited) { throw 'Servidor de prueba terminó antes de conectar.' }
            if ((Get-Content (Join-Path $artifactRoot 'mmo-server.log') -Raw) -match 'escuchando') { $ready = $true; break }
            Start-Sleep -Milliseconds 100
        }
        if (!$ready) { throw 'Servidor no inició.' }
        foreach ($index in 1, 2) {
            $reportPath = Join-Path $artifactRoot "godot-client-$index.json"
            $godotArgs = @('--path', ('"' + (Join-Path $repoRoot 'client') + '"'), 'res://mmo/testing/client_smoke.tscn')
            if (!$Render) { $godotArgs += '--headless' }
            $godotArgs += @('--', "--name=Prueba$index", "--port=$port", "--direction=$(if ($index -eq 1) { 1 } else { -1 })", "--seconds=$(if ($index -eq 1) { 7 } else { 10 })", ('"--report=' + $reportPath + '"'))
            if ($Render) { $godotArgs += '"--screenshot=' + (Join-Path $artifactRoot "godot-client-$index.png") + '"' }
            $clients += Start-Process -FilePath $GodotExe -ArgumentList $godotArgs -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $artifactRoot "godot-client-$index.log") -RedirectStandardError (Join-Path $artifactRoot "godot-client-$index-error.log")
        }
        foreach ($process in $clients) {
            if (!$process.WaitForExit(30000)) { throw 'Cliente Godot no terminó.' }
            if ($process.ExitCode -ne 0) { throw 'Cliente Godot falló; revisar artifacts.' }
        }
        foreach ($index in 1, 2) {
            $report = Get-Content (Join-Path $artifactRoot "godot-client-$index.json") -Raw | ConvertFrom-Json
            if (!$report.passed) { throw "Integración Godot $index falló." }
            $errorLog = Get-Content (Join-Path $artifactRoot "godot-client-$index-error.log") -Raw
            if ($errorLog -match 'SCRIPT ERROR|System\..*Exception|ERROR:' -and $errorLog -notmatch '^ERROR: 1 resources still in use at exit') {
                # Keep the complete log review explicit; exit status alone is insufficient for Godot scripts.
                $unexpected = @($errorLog -split '\r?\n' | Where-Object { $_ -match 'SCRIPT ERROR|System\..*Exception|ERROR:' -and $_ -notmatch '1 resources still in use at exit' })
                if ($unexpected.Count -gt 0) { throw ($unexpected -join [Environment]::NewLine) }
            }
            $report | ConvertTo-Json -Compress
        }
        $second = Get-Content (Join-Path $artifactRoot 'godot-client-2.json') -Raw | ConvertFrom-Json
        if ($second.despawns -lt 1) { throw 'El segundo cliente no observó el despawn.' }
        Write-Output 'OK: dos procesos Godot, movimiento local/remoto y despawn verificados.'
    } finally {
        foreach ($process in $clients) { if (!$process.HasExited) { Stop-Process -Id $process.Id } }
        if (!$server.HasExited) { Stop-Process -Id $server.Id }
    }
} finally { Pop-Location }
