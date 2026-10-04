. (Join-Path $PSScriptRoot "Common.ps1")

$modKit = Get-ModKitPath
if (-not (Test-Path $modKit)) {
    return
}

$content = Join-Path $modKit "UE4Project\Content"
$paths = @(
    (Join-Path $content "Mods\CameraCoords"),
    (Join-Path $content "BPLoader\Menu\CameraCoords.umap"),
    (Join-Path $content "Fonts\MinecraftFive.uasset"),
    (Join-Path $content "Fonts\NewFive.uasset")
)

foreach ($path in $paths) {
    if (Test-Path $path) {
        Remove-Item -Recurse -Force $path
    }
}

$marker = Join-Path $modKit ".mcdqol-ui-reference-staged"
if (Test-Path $marker) {
    Remove-Item -Force $marker
}

Write-Host "[OK] Cleared third-party UI reference assets from cookable Content."
