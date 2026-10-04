. (Join-Path $PSScriptRoot "Common.ps1")

$root = Get-ProjectRoot
$modKit = Get-ModKitPath

if (-not (Test-Path $modKit)) {
    throw "Dungeons Mod Kit not found. Run ./scripts/Bootstrap-ModKit.ps1 first."
}

$sourceDir = Join-Path $root "sdk\modkit\Source\Dungeons"
$targetDir = Join-Path $modKit "UE4Project\Source\Dungeons"

if (-not (Test-Path $sourceDir)) {
    throw "Mirror API source directory not found: $sourceDir"
}

New-Item -ItemType Directory -Force -Path $targetDir | Out-Null

$ownedFiles = @(
    "MCDQoLInventoryStubs.h",
    "MCDQoLInventoryStubs.cpp"
)

foreach ($file in $ownedFiles) {
    $source = Join-Path $sourceDir $file
    if (-not (Test-Path $source)) {
        throw "Required mirror API file missing: $source"
    }

    Copy-Item -Force -Path $source -Destination (Join-Path $targetDir $file)
}

# Clean obsolete project-owned mirror filenames from earlier research passes.
foreach ($obsolete in @("MCDQoLGameAPI.h", "MCDQoLGameAPI.cpp")) {
    $path = Join-Path $targetDir $obsolete
    if (Test-Path $path) {
        Remove-Item -Force $path
    }
}

Write-Host "[OK] Synced canonical Dungeons reflection mirror into the Mod Kit project."
