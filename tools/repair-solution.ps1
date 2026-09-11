[CmdletBinding()]
param(
    [switch]$SkipClean
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root 'NuevoMMO.sln'
$coreProject = Join-Path $root 'Core/NuevoMMO.Core/NuevoMMO.Core.csproj'

function Invoke-Checked {
    param(
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)][scriptblock]$Action
    )

    Write-Host "`n==> $Label" -ForegroundColor Cyan
    & $Action
    if ($LASTEXITCODE -ne 0) {
        throw "$Label fallo con codigo de salida $LASTEXITCODE. Corrige ese primer error antes de revisar los errores en cascada de los proyectos consumidores."
    }
}

if (-not (Test-Path $solution)) {
    throw "No se encontro NuevoMMO.sln en $root. Ejecuta este script desde el repositorio."
}

if (-not (Test-Path $coreProject)) {
    throw "No se encontro el proyecto canonico Core/NuevoMMO.Core/NuevoMMO.Core.csproj."
}

Write-Host 'Comprobando SDK .NET...' -ForegroundColor Cyan
$sdkList = @(& dotnet --list-sdks 2>&1)
if ($LASTEXITCODE -ne 0) {
    throw 'No se pudo ejecutar dotnet. Instala el SDK .NET 8 x64 y vuelve a abrir Visual Studio.'
}

$dotnet8 = @($sdkList | Where-Object { $_ -match '^8\.0\.' })
if ($dotnet8.Count -eq 0) {
    throw "No hay ningun SDK .NET 8 instalado. SDK detectados:`n$($sdkList -join "`n")"
}

Write-Host "SDK .NET 8 detectado: $($dotnet8 -join ', ')" -ForegroundColor Green

if (Get-Command git -ErrorAction SilentlyContinue) {
    $tracked = @(& git -C $root ls-files 2>$null)
    if ($LASTEXITCODE -eq 0) {
        $lowercaseCore = @($tracked | Where-Object { $_ -cmatch '^core/' })
        if ($lowercaseCore.Count -gt 0) {
            throw "El indice de Git contiene una segunda raiz 'core/' en minuscula. Todo Core debe vivir en 'Core/NuevoMMO.Core'. Archivos detectados:`n$($lowercaseCore -join "`n")"
        }
    }
}

if (-not $SkipClean) {
    Write-Host "`n==> Limpiando caches de Visual Studio/MSBuild" -ForegroundColor Cyan

    $vs = Join-Path $root '.vs'
    if (Test-Path $vs) {
        Remove-Item $vs -Recurse -Force
    }

    $generated = @(Get-ChildItem -Path $root -Directory -Recurse -Force -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -in @('bin', 'obj') } |
        Sort-Object { $_.FullName.Length } -Descending)

    foreach ($directory in $generated) {
        if (Test-Path $directory.FullName) {
            Remove-Item $directory.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

$env:DOTNET_EnableWindowsTargeting = 'true'

Push-Location $root
try {
    Invoke-Checked 'Restaurando paquetes y grafo de proyectos' {
        dotnet restore $solution --force-evaluate --nologo
    }

    $projects = @(
        'Core/NuevoMMO.Core/NuevoMMO.Core.csproj',
        'Network/NuevoMMO.Network/NuevoMMO.Network.csproj',
        'Editor/NuevoMMO.Editor/NuevoMMO.Editor.csproj',
        'Server/NuevoMMO.Server/NuevoMMO.Server.csproj',
        'Editor/NuevoMMO.Editor.WinForms/NuevoMMO.Editor.WinForms.csproj'
    )

    foreach ($project in $projects) {
        Invoke-Checked "Compilando $project" {
            dotnet build $project -c Debug --no-restore --nologo
        }
    }

    Invoke-Checked 'Compilando solucion completa' {
        dotnet build $solution -c Debug --no-restore --nologo
    }
}
finally {
    Pop-Location
}

Write-Host "`nOK: referencias reconstruidas y solucion compilada." -ForegroundColor Green
Write-Host 'Cierra y vuelve a abrir Visual Studio si aun conserva diagnosticos activos antiguos.' -ForegroundColor Green
