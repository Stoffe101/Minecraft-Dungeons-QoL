. (Join-Path $PSScriptRoot "Common.ps1")

$root = Get-ProjectRoot
$modKit = Get-ModKitPath

if (-not (Test-Path $modKit)) {
    throw "Dungeons Mod Kit not found. Run ./scripts/Bootstrap-ModKit.ps1 first."
}

$sourceDir = Join-Path $root "sdk\Source\Dungeons"
$targetDir = Join-Path $modKit "UE4Project\Source\Dungeons"

if (-not (Test-Path $sourceDir)) {
    throw "Mirror API source directory not found: $sourceDir"
}

New-Item -ItemType Directory -Force -Path $targetDir | Out-Null

$ownedFiles = @(
    "MCDQoLGameAPI.h",
    "MCDQoLGameAPI.cpp"
)

foreach ($file in $ownedFiles) {
    $source = Join-Path $sourceDir $file
    if (-not (Test-Path $source)) {
        throw "Required mirror API file missing: $source"
    }

    Copy-Item -Force -Path $source -Destination (Join-Path $targetDir $file)
}

Write-Host "[OK] Synced verified Dungeons mirror API into the Mod Kit project."
