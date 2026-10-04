# Third-Party Components and Licensing

## Dungeons Mod Kit

Repository:
https://github.com/Dokucraft/Dungeons-Mod-Kit

License: MIT.

Used as the canonical Dungeons 1 development/build dependency and pinned for reproducibility.

Pinned commit:
`c30e88ec5e99e401eadedddbe82af0265a056fe7`

Its normal UE4.22 cook + u4pak packaging workflow is the preferred release path.

## LetMeMove!

Repository:
https://github.com/StainlessStasis/LetMeMove

License: MIT.

Use:

- evidence that Blueprint Loader remains viable on current Dungeons 1
- known-working actor Blueprint package reference
- known-working BPLoader/Ingame level structure
- possible template reuse where useful

Any direct reuse must retain the MIT notice.

## Camera Coordinates Overlay

Repository:
https://github.com/EvenTorset/Camera-Coordinates-Overlay

Published mod page:
https://www.nexusmods.com/minecraftdungeons/mods/112

Use: reference and potential template for the proven Dungeons 1 pattern:

`Blueprint Loader level -> actor -> Create Widget -> Add To Viewport`

The Nexus permissions explicitly allow modification of the files and use of its assets in other mods. The GitHub repository itself does not expose an obvious standalone license, so any direct reuse should be documented as relying on the published Nexus asset permissions.

This is currently the preferred existing UI/template reference.

## Blueprint Loader for Dungeons 1

Nexus:
https://www.nexusmods.com/minecraftdungeons/mods/111

Blueprint Loader is a separate runtime dependency.

Its published permissions do not permit us to simply redistribute its assets as part of this project's release. Users install it separately from the original page.

## MCD-PE

Repository:
https://github.com/Minecraforever/MCD-PE

License: Apache-2.0.

Use:

- final-build Dungeons 1 class/function research
- `UItemStashComponent`
- `UInventoryItemSlot`
- equipment slot values
- native salvage signatures
- UI event wiring research

The repository states that the restored class architecture was checked against the final game binary.

If declarations/code are copied or adapted into editor stubs rather than merely referenced, Apache-2.0 attribution and notice requirements must be followed.

## UAssetAPI

Repository:
https://github.com/atenfyr/UAssetAPI

License: MIT.

Use:

- inspect known-working Dungeons `.uasset` files
- import/export/function-table research
- raw Kismet inspection
- regression validation
- possible permitted precooked Blueprint modification

CI has verified that UAssetAPI 1.1.0 can parse a working Dungeons 1 Blueprint package.

## KismetKompiler

Repository:
https://github.com/tge-was-taken/KismetKompiler

License: MIT.

Potential use: decompile/recompile existing UE4 Blueprint bytecode in automated build/research workflows.

Current status: experimental only. Its pinned UAssetAPI is too old for our Dungeons template and its source API does not directly compile against current UAssetAPI.

The release pipeline must not depend on it until compatibility is proven.

## UeBlueprintDumper

Repository:
https://github.com/CrystalFerrai/UeBluepr…2398 tokens truncated… "# Minecraft Dungeons QoL diagnostic build"
    ""
    "Build kind: diagnostic-no-destruction"
    "Source commit: $sourceSha"
    "SHA-256: $hash"
    "Native salvage: DISABLED (no call emitted)"
    "Runtime validation: NOT PERFORMED by this build"
    "Includes property archetype and creation preloads; earlier PR #7/#8 builds crashed. Test load probe first."
    ""
    "Input only while the actual inventory HUD reports open; missing/closed UI clears selection."
    "Six equipped gear items are excluded from selection and preview; unresolved equipment UI blocks candidates."
    "F5 clears selection; F6/F7 browse; F8 toggles prototype fingerprint protection; F9 selects; F10 twice previews."
    "Protection groups matching name/power/enchantment points across heroes; it is not persistent item identity."
    "Vanilla salvage is not intercepted. Loadouts and controller UI are not implemented."
    "Blueprint Loader must be installed separately."
) | Set-Content (Join-Path $dist "BUILD_INFO.md")
Copy-Item (Join-Path $root "third_party/LetMeMove-LICENSE.txt") $dist -Force
# Separate artifact: same reflected schema/dependencies, but no event code executes.
$probeStage = Join-Path $work "probe-stage"
if (Test-Path $probeStage) { Remove-Item $probeStage -Recurse -Force }
Copy-Item $stage $probeStage -Recurse
$probeManager = Join-Path $probeStage "Dungeons/Content/Mods/MinecraftDungeonsQoL/BP_MCDQoL_Manager.uasset"
& dotnet run --project (Join-Path $root "tools/CookedLoadProbe") -- $manager $probeManager
if ($LASTEXITCODE -ne 0) { throw "Load probe validation failed." }
$probeDist = Join-Path $root "dist/load-probe"
New-Item -ItemType Directory -Force $probeDist | Out-Null
$probePak = Join-Path $probeDist "MinecraftDungeonsQoL-load-probe.pak"
Push-Location $probeStage
try {
    & python $u4pak pack $probePak Dungeons -p
    if ($LASTEXITCODE -ne 0) { throw "Load probe packing failed." }
} finally { Pop-Location }
@(
    "# Minecraft Dungeons QoL loading isolation probe"
    "Source commit: $sourceSha"
    "SHA-256: $((Get-FileHash $probePak -Algorithm SHA256).Hash.ToLowerInvariant())"
    "Every manager event immediately returns. No hotkeys, inventory calls, saves or salvage run."
    "NOT a working mod or confirmed crash fix. Runtime test pending."
    "Remove every other MinecraftDungeonsQoL pak first; keep Blueprint-Loader.pak."
    "Install only this pak in Paks\~mods. Restart and select the character; reach camp."
    "Remove this probe before installing any later QoL diagnostic. They use the same package paths."
) | Set-Content (Join-Path $probeDist "BUILD_INFO.md")
Copy-Item (Join-Path $root "third_party/LetMeMove-LICENSE.txt") $probeDist -Force


# Visible read-only inventory gate: separate artifact, never install together with other QoL paks.
$readStage = Join-Path $work "inventory-stage"
if (Test-Path $readStage) { Remove-Item $readStage -Recurse -Force }
Copy-Item $stage $readStage -Recurse
$readManager = Join-Path $readStage "Dungeons/Content/Mods/MinecraftDungeonsQoL/BP_MCDQoL_Manager.uasset"
& dotnet run --project (Join-Path $root "tools/CookedInventoryProbe") -- $manager $readManager
if ($LASTEXITCODE -ne 0) { throw "Inventory-read probe generation failed." }
& dotnet run --project (Join-Path $root "tools/CookedGraphTests") -- $readManager
if ($LASTEXITCODE -ne 0) { throw "Inventory-read probe regression tests failed." }
$readDist = Join-Path $root "dist/inventory-probe"
New-Item -ItemType Directory -Force $readDist | Out-Null
$readPak = Join-Path $readDist "MinecraftDungeonsQoL-inventory-probe.pak"
Push-Location $readStage
try {
    & python $u4pak pack $readPak Dungeons -p
    if ($LASTEXITCODE -ne 0) { throw "Inventory-read probe packing failed." }
} finally { Pop-Location }
@(
    "# Minecraft Dungeons QoL inventory-read probe"
    "Source commit: $sourceSha"
    "SHA-256: $((Get-FileHash $readPak -Algorithm SHA256).Hash.ToLowerInvariant())"
    "PR #12/#13 crashed opening inventory online. Typed-reference ABI repair remains untested in retail."
    "Manager ticks while offline inventory pauses the world: enabled, pause-capable, zero interval."
    "String/text results use typed locals before native reference calls; open guard uses Widget.IsVisible."
    "No activation option is needed. Open inventory first; initial text should appear without pressing keys."
    "Requires a local player controller; manager is non-replicated. Only F6/F7 browse slots. No item/save changes."
    "While inventory is open, a text overlay should show slot count, current item name and power."
    "Closing inventory hides the overlay. Its widget does not capture mouse/controller input."
    "Remove ALL other MinecraftDungeonsQoL paks first; keep Blueprint-Loader.pak."
    "Install only this pak in Paks\~mods. Restart, enter camp and open inventory."
    "Report visible text, F6/F7 browsing, and whether closing inventory hides it."
    "Online host and joining friends remain unverified. Loader GameMode-dependent client startup is a known blocker."
) | Set-Content (Join-Path $readDist "BUILD_INFO.md")
Copy-Item (Join-Path $root "third_party/LetMeMove-LICENSE.txt") $readDist -Force
Write-Host "[OK] Built non-destructive diagnostic: $out"
