param(
    [string]$PaksPath,
    [string]$PakPath
)

. (Join-Path $PSScriptRoot "Common.ps1")

if (-not $PakPath) {
    $PakPath = Get-DistPakPath
}

if (-not (Test-Path $PakPath)) {
    throw "Pak not found: $PakPath. Run ./scripts/Build.ps1 first."
}

$paks = Find-McdPaksPath -Override $PaksPath
$mods = Join-Path $paks "~mods"
New-Item -ItemType Directory -Force -Path $mods | Out-Null

$target = Join-Path $mods "MinecraftDungeonsQoL.pak"
Copy-Item -Force -Path $PakPath -Destination $target

Write-Host "[OK] Installed: $target"
Write-Host "Blueprint Loader must also be installed separately."
