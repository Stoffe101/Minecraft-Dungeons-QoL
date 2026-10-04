param()
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "Common.ps1")
Assert-Command git
Assert-Command python
Assert-Command dotnet
$root = Get-ProjectRoot
$config = Get-Content (Join-Path $root "config/cooked-template.json") -Raw | ConvertFrom-Json
if ($config.nativeSalvageEnabled -ne $false) { throw "This pipeline only builds non-destructive diagnostics." }
$work = Join-Path $root ".research/diagnostic-build"
$stage = Join-Path $work "stage"
$dist = Join-Path $root "dist/diagnostic"
New-Item -ItemType Directory -Force $work, $dist | Out-Null
$zip = Join-Path $work "template.zip"
Invoke-WebRequest -Uri $config.releaseUrl -OutFile $zip
if ((Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant() -ne $config.releaseSha256) {
    throw "Cooked template checksum differs from the reviewed release."
}
# Clear only this pipeline's staging folders to avoid including obsolete assets.
foreach ($name in @("zip", "unpacked", "stage", "patched")) {
    $path = Join-Path $work $name
    if (Test-Path $path) { Remove-Item $path -Recurse -Force }
    New-Item -ItemType Directory -Force $path | Out-Null
}
Expand-Archive $zip (Join-Path $work "zip") -Force
$pak = @(Get-ChildItem (Join-Path $work "zip") -Recurse -Filter "*.pak")
if ($pak.Count -ne 1) { throw "Expected exactly one cooked template pak." }
$modKit = Get-ModKitPath
if (-not (Test-Path $modKit)) {
    & git clone https://github.com/Dokucraft/Dungeons-Mod-Kit.git $modKit
    if ($LASTEXITCODE -ne 0) { throw "Mod Kit clone failed." }
    & git -C $modKit checkout $config.modKitCommit
    if ($LASTEXITCODE -ne 0) { throw "Mod Kit checkout failed." }
}
$head = (& git -C $modKit rev-parse HEAD).Trim()
if ($head -ne $config.modKitCommit) { throw "Existing Mod Kit is not the pinned commit; local changes were preserved." }
$u4pak = Join-Path $modKit "Tools/py/u4pak.py"
& python $u4pak unpack -C (Join-Path $work "unpacked") $pak[0].FullName
if ($LASTEXITCODE -ne 0) { throw "Unpack failed." }
$source = Join-Path $work "unpacked/Dungeons/Content"
$actor = Join-Path $source "Mods/LetMeMove/BP_WASD_Movement.uasset"
$patched = Join-Path $work "patched/BP_WASD_Movement.uasset"
& dotnet run --project (Join-Path $root "tools/CookedQoLPatcher") -- $actor $patched
if ($LASTEXITCODE -ne 0) { throw "Diagnostic runtime graph validation failed." }
$manager = Join-Path $stage "Dungeons/Content/Mods/MinecraftDungeonsQoL/BP_MCDQoL_Manager.uasset"
& dotnet run --project (Join-Path $root "tools/CookedAssetRelocator") -- $patched $manager
if ($LASTEXITCODE -ne 0) { throw "Manager relocation failed." }
foreach ($trigger in @("Lobby", "Ingame")) {
    $map = Join-Path $source "BPLoader/$trigger/LetMeMove.umap"
    $target = Join-Path $stage "Dungeons/Content/BPLoader/$trigger/MinecraftDungeonsQoL.umap"
    & dotnet run --project (Join-Path $root "tools/CookedAssetRelocator") -- $map $target
    if ($LASTEXITCODE -ne 0) { throw "$trigger loader relocation failed." }
}
& dotnet run --project (Join-Path $root "tools/CookedGraphTests") -- $manager
if ($LASTEXITCODE -ne 0) { throw "Cooked graph regression tests failed." }
$saveGenerated = Join-Path $work "patched/SG_MCDQoL.uasset"
$saveTarget = Join-Path $stage "Dungeons/Content/Mods/MinecraftDungeonsQoL/SG_MCDQoL.uasset"
# Regenerate the sidecar from the same reviewed template rather than trusting old binaries.
& dotnet run --project (Join-Path $root "tools/CookedSaveGameSynth") -- $actor $saveGenerated
if ($LASTEXITCODE -ne 0) { throw "Sidecar synthesis failed." }
& dotnet run --project (Join-Path $root "tools/CookedAssetRelocator") -- $saveGenerated $saveTarget
if ($LASTEXITCODE -ne 0) { throw "Sidecar namespace cleanup failed." }
$out = Join-Path $dist "MinecraftDungeonsQoL-diagnostic.pak"
Push-Location $stage
try {
    & python $u4pak pack $out Dungeons -p
    if ($LASTEXITCODE -ne 0) { throw "Diagnostic pak packing failed." }
} finally { Pop-Location }
$list = & python $u4pak list $out
if ($LASTEXITCODE -ne 0) { throw "Pak listing failed." }
$listText = $list -join "`n"
foreach ($required in @("BP_MCDQoL_Manager.uasset", "BP_MCDQoL_Manager.uexp", "SG_MCDQoL.uasset", "SG_MCDQoL.uexp", "BPLoader/Lobby", "BPLoader/Ingame")) {
    if (-not $listText.Replace('\','/').Contains($required)) { throw "Pak missing $required" }
}
if ($listText.Contains("LetMeMove") -or $listText.Contains("BP_WASD_Movement")) { throw "Template package collision remains." }
$sourceSha = (& git -C $root rev-parse HEAD).Trim()
$hash = (Get-FileHash $out -Algorithm SHA256).Hash.ToLowerInvariant()
@(
    "# Minecraft Dungeons QoL diagnostic build"
    ""
    "Build kind: diagnostic-no-destruction"
    "Source commit: $sourceSha"
    "SHA-256: $hash"
    "Native salvage: DISABLED (no call emitted)"
    "Runtime validation: NOT PERFORMED by this build"
    ""
    "Input only while the actual inventory HUD reports open; missing/closed UI clears selection."
    "Six equipped gear items are excluded from selection and preview; unresolved equipment UI blocks candidates."
    "F5 clears selection; F6/F7 browse; F8 toggles prototype fingerprint protection; F9 selects; F10 twice previews."
    "Protection groups matching name/power/enchantment points across heroes; it is not persistent item identity."
    "Vanilla salvage is not intercepted. Loadouts and controller UI are not implemented."
    "Blueprint Loader must be installed separately."
) | Set-Content (Join-Path $dist "BUILD_INFO.md")
Copy-Item (Join-Path $root "third_party/LetMeMove-LICENSE.txt") $dist -Force
Write-Host "[OK] Built non-destructive diagnostic: $out"
