$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot
dotnet run --project (Join-Path $repoRoot 'Editor/NuevoMMO.Editor.WinForms/NuevoMMO.Editor.WinForms.csproj') --launch-profile NuevoMMO.Editor.WinForms
if ($LASTEXITCODE -ne 0) { throw 'El editor terminó con error.' }
