$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot
$sourcePath = Join-Path $repoRoot 'client/entities/player/player.tscn'
$targetPath = Join-Path $repoRoot 'client/mmo/presentation/template_player_frames.tres'
$sourceText = Get-Content -LiteralPath $sourcePath -Raw
$atlases = [regex]::Matches($sourceText, '(?ms)^\[sub_resource type="AtlasTexture"[^\r\n]*\]\r?\n.*?(?=^\[)')
$frames = [regex]::Match($sourceText, '(?ms)^\[sub_resource type="SpriteFrames"[^\r\n]*\]\r?\n(.*?)(?=^\[)')
$texture = [regex]::Match($sourceText, '\[ext_resource type="Texture2D"[^\r\n]*path="res://entities/player/chara-hero.png"[^\r\n]*\]')
if (!$frames.Success -or !$texture.Success -or $atlases.Count -eq 0) { throw 'Estructura de animaciones del template no reconocida.' }
$textureLine = [regex]::Replace($texture.Value, ' uid="[^"]+"', '')
$parts = @(('[gd_resource type="SpriteFrames" load_steps=' + ($atlases.Count + 2) + ' format=3]'), '', $textureLine, '')
foreach ($atlas in $atlases) { $parts += $atlas.Value.TrimEnd() }
$parts += '[resource]'
$parts += $frames.Groups[1].Value.TrimEnd()
Set-Content -LiteralPath $targetPath -Value ($parts -join "`n")
Write-Output "SpriteFrames extraídos de la escena original: $($atlases.Count) atlas."
