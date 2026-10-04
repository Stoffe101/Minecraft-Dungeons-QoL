. (Join-Path $PSScriptRoot "Common.ps1")

$root = Get-ProjectRoot
$modKit = Get-ModKitPath
$source = Join-Path $root "third_party\camera-coordinates-overlay\Content"
$target = Join-Path $modKit "UE4Project\Content"

if (-not (Test-Path $source)) {
    throw "Camera Coordinates Overlay reference assets are missing: $source"
}

if (-not (Test-Path $modKit)) {
    throw "Dungeons Mod Kit not found. Run ./scripts/Bootstrap-ModKit.ps1 first."
}

# These assets must retain their original /Game package paths to open correctly.
# They are development references only. Build.ps1 removes them before cooking.
Copy-Item -Path (Join-Path $source "*") -Destination $target -Recurse -Force

$marker = Join-Path $modKit ".mcdqol-ui-reference-staged"
Set-Content -Path $marker -Value "Camera Coordinates Overlay reference assets staged for editor inspection." -NoNewline

Write-Host "[OK] Staged Camera Coordinates Overlay reference assets."
Write-Host "     Development reference only. Build.ps1 removes them before cooking."
