param(
    [Parameter(Mandatory=$true)][string]$SourcesDirectory,
    [string]$OutputDirectory,
    [string]$DotNetPath,
    [switch]$EnableNativeSalvage,
    [switch]$FavoritesOnly
)
if ($FavoritesOnly -and $EnableNativeSalvage) { throw 'FavoritesOnly cannot enable native batch salvage.' }
. (Join-Path $PSScriptRoot 'Common.ps1')
Assert-Command git
Assert-Command python
$root = Get-ProjectRoot
$sources = [System.IO.Path]::GetFullPath($SourcesDirectory)
if (-not (Test-Path (Join-Path $sources 'UI/Inventory/UMG_InventoryHUD.uasset'))) { throw 'SourcesDirectory must point at extracted Metadata/PatchSources/Dungeons/Content.' }
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $root ('.research/private-inventory-features-' + [Guid]::NewGuid().ToString('N')) }
$out = [System.IO.Path]::GetFullPath($OutputDirectory)
if (($out + [IO.Path]::DirectorySeparatorChar).StartsWith(($sources + [IO.Path]::DirectorySeparatorChar), [StringComparison]::OrdinalIgnoreCase)) { throw 'Output must be outside the original source directory.' }
if (Test-Path $out) { throw 'Choose a fresh output directory.' }
New-Item -ItemType Directory -Force $out | Out-Null
$config = Get-Content (Join-Path $root 'config/cooked-template.json') -Raw | ConvertFrom-Json
if (-not $DotNetPath) {
    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($dotnet) { $DotNetPath = $dotnet.Source }
    else {
        $cfg = Get-Content (Join-Path $root 'config/evidence-tool.json') -Raw | ConvertFrom-Json
        $DotNetPath = Join-Path $root ".tools/dotnet-evidence-sdk-$($cfg.sdkVersion)/dotnet.exe"
        if (-not (Test-Path $DotNetPath)) { throw 'Install .NET 8 or supply -DotNetPath pointing to the SDK used by the evidence collector.' }
    }
}
$modKit = Get-ModKitPath
if (-not (Test-Path $modKit)) {
    & git clone https://github.com/Dokucraft/Dungeons-Mod-Kit.git $modKit
    if ($LASTEXITCODE -ne 0) { throw 'Mod Kit clone failed.' }
    & git -C $modKit checkout $config.modKitCommit
    if ($LASTEXITCODE -ne 0) { throw 'Mod Kit checkout failed.' }
}
if ((& git -C $modKit rev-parse HEAD).Trim() -ne $config.modKitCommit) { throw 'Existing Mod Kit is not the pinned commit; local changes were preserved.' }
$build = Join-Path $out 'tool'
& $DotNetPath build (Join-Path $root 'tools/CookedInventoryFeatures/CookedInventoryFeatures.csproj') -c Release -o $build
if ($LASTEXITCODE -ne 0) { throw 'Feature patcher build failed.' }
$stage = Join-Path $out 'stage'
$arguments = @((Join-Path $build 'CookedInventoryFeatures.dll'), $sources, (Join-Path $stage 'Dungeons/Content'))
if ($EnableNativeSalvage) { $arguments += '--enable-salvage' }
if ($FavoritesOnly) { $arguments += '--favorites-only' }
& $DotNetPath @arguments
if ($LASTEXITCODE -ne 0) { throw 'Feature patch/graph validation failed.' }
$name = if ($FavoritesOnly) { 'zzz_MinecraftDungeonsQoL-FavoritesOnlyTest_P.pak' } elseif ($EnableNativeSalvage) { 'zzz_MinecraftDungeonsQoL-InventoryFeaturesNative_P.pak' } else { 'zzz_MinecraftDungeonsQoL-InventoryFeaturesTest_P.pak' }
$pak = Join-Path $out $name
Push-Location $stage
try {
    & python (Join-Path $modKit 'Tools/py/u4pak.py') pack $pak Dungeons -p
    if ($LASTEXITCODE -ne 0) { throw 'Feature pak packaging failed.' }
} finally { Pop-Location }
[ordered]@{
    schemaVersion = 1
    sourceCommit = (& git -C $root rev-parse HEAD).Trim()
    nativeBatchEnabled = [bool]$EnableNativeSalvage
    favoritesOnly = [bool]$FavoritesOnly
    controls = 'Mouse buttons; multi-select intercepts original SlotClicked only in combined build.'
    installation = 'Install one inventory QoL variant only; these packages override the same game assets.'
    favoritesPersistence = 'Inspector UI lifetime only; no persistent item identity certified.'
    pakSha256 = (Get-FileHash $pak -Algorithm SHA256).Hash.ToLowerInvariant()
    originals = @(Get-ChildItem $sources -Recurse -File | Where-Object { $_.Name -in @('UMG_InventoryHUD.uasset', 'UMG_InventoryHUD.uexp', 'UMG_InventoryItemInspector.uasset', 'UMG_InventoryItemInspector.uexp') } | ForEach-Object { @{ name = $_.Name; sha256 = (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() } })
    note = 'Private build from your game-owned UI assets. Do not commit or publish the cooked output. No game or save files modified by this build command.'
} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $out 'BUILDINFO.json')
Write-Host "[OK] Private feature pak: $pak"
