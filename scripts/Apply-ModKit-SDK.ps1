param(
    [string]$ModKitPath
)

. (Join-Path $PSScriptRoot "Common.ps1")

if (-not $ModKitPath) {
    $ModKitPath = Get-ModKitPath
}

if (-not (Test-Path $ModKitPath)) {
    throw "Dungeons Mod Kit not found: $ModKitPath. Run ./scripts/Bootstrap-ModKit.ps1 first."
}

$root = Get-ProjectRoot
$source = Join-Path $root "sdk\modkit\Source\Dungeons"
$destination = Join-Path $ModKitPath "UE4Project\Source\Dungeons"

if (-not (Test-Path $source)) {
    throw "SDK overlay source is missing: $source"
}

New-Item -ItemType Directory -Force -Path $destination | Out-Null

Copy-Item -Path (Join-Path $source "MCDQoLInventoryStubs.h") -Destination $destination -Force
Copy-Item -Path (Join-Path $source "MCDQoLInventoryStubs.cpp") -Destination $destination -Force

Write-Host "[OK] Applied Minecraft Dungeons QoL editor reflection stubs."
Write-Host "     Source:      $source"
Write-Host "     Destination: $destination"
