. (Join-Path $PSScriptRoot "Common.ps1")

$root = Get-ProjectRoot
$modKit = Get-ModKitPath
$source = Join-Path $root "mod\Content"
$destination = Join-Path $modKit "UE4Project\Content"

if (-not (Test-Path $modKit)) {
    throw "Dungeons Mod Kit not found. Run ./scripts/Bootstrap-ModKit.ps1 first."
}

if (-not (Test-Path $source)) {
    throw "Project mod Content directory does not exist: $source"
}

$sourceItems = @(Get-ChildItem -Force $source -ErrorAction SilentlyContinue)
if ($sourceItems.Count -eq 0) {
    throw "No Unreal assets exist under mod/Content yet. Complete the inventory research/Blueprint pass first."
}

New-Item -ItemType Directory -Force -Path $destination | Out-Null

# Remove only project-owned destinations so deleted assets do not linger.
$ownedPaths = @(
    (Join-Path $destination "Mods\MinecraftDungeonsQoL"),
    (Join-Path $destination "BPLoader\Lobby\MCDQoL_Lobby.umap"),
    (Join-Path $destination "BPLoader\Ingame\MCDQoL_Ingame.umap")
)
foreach ($path in $ownedPaths) {
    if (Test-Path $path) { Remove-Item -Recurse -Force $path }
}

foreach ($item in $sourceItems) {
    Copy-Item -Path $item.FullName -Destination $destination -Recurse -Force
}

Write-Host "[OK] Synced project assets into Mod Kit."
