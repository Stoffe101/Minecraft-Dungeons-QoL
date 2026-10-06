# Third-Party Components and Licensing

Native reflection evidence probe: UE4SS 3.0.1 (MIT, copyright 2022 Narknon), pinned commit d935b5b23bac03b65c14ae38382b02007204cc2e. Installer downloads the official release with checksum verification, copies only UE4SS.dll/dwmapi.dll and our diagnostic config/script, and includes third_party/UE4SS-LICENSE.txt. No upstream gameplay mods enabled. Store compatibility is not verified. See FAVORITES_PERSISTENCE_INVESTIGATION.md.

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
https://github.com/CrystalFerrai/UeBlueprintDumper

License: Apache-2.0.

Potential use: offline research against locally installed game assets.

## MCDSaveEdit

Repository:
https://github.com/CutFlame/MCDSaveEdit

Use: save-format research only, including `uniqueSaveId`, `inventoryIndex`, `equipmentSlot`, and item fingerprint fields.

Before copying implementation code, its applicable license must be checked for the exact version/repository content being reused.

## Sources without reuse permission

A public GitHub repository is not automatically reusable.

If a project has no license and no separately published permission:

- research/inspection is allowed for understanding behavior
- do not copy its implementation or binary assets into this project

## Minecraft / Minecraft Dungeons assets

Game assets belong to their respective rights holders.

Do not commit extracted proprietary game assets merely because they are technically useful. Raw extraction/research output belongs under ignored local research folders. Commit only project-owned assets, permitted third-party assets with correct attribution, and documentation of necessary technical findings.


## Actual template derivative build (2026-10-04)

`Build-Diagnostic.ps1` directly adapts the LetMeMove 1.1.0 release actor and Lobby/Ingame maps. Its full MIT text is retained at `third_party/LetMeMove-LICENSE.txt` and copied alongside the diagnostic pak. Template release checksum and packager commit live in `config/cooked-template.json`. The derived packages use MinecraftDungeonsQoL paths, so they do not deliberately replace the original mod. Blueprint Loader is still downloaded/installed separately.

The editor mirror also retains the complete Apache-2.0 license at `sdk/Apache-2.0-LICENSE.txt`, alongside the existing SDK attribution notice.

## Installed-game evidence tooling

[UeBlueprintDumper](https://github.com/CrystalFerrai/UeBlueprintDumper) (Apache-2.0) is downloaded from its official 1.2.0 release into ignored `.tools`, preserving the release contents/notices. The collector does not bundle it in mod paks. A local .NET runtime is downloaded from Microsoft's official distribution with its checksum from official release metadata. Versions/hashes are recorded in `config/evidence-tool.json`. No code/assets from the old unlicensed zMCDungeons-SDK were copied.

`LegacyEvidenceExporter` uses CUE4Parse's archive-reader DLL from that same pinned distribution and UAssetAPI 1.1.0 (MIT) through NuGet. Its code is project-authored; it does not vendor the incompatible BlueprintDumper implementation. The original release/notices are preserved locally. A local SDK, if needed, is from Microsoft's official checksum-pinned distribution. CI's legacy regression downloads the MIT LetMeMove fixture with its existing checksum; no game assets are committed or put in mod releases by this collector.


## Dungeons GUI X (inspection reference only)

Published project: https://www.curseforge.com/minecraft-dungeons/mods/dungeons-gui-x

Published file: https://www.curseforge.com/minecraft-dungeons/mods/dungeons-gui-x/files/6857197

The author listing specifies AGPLv3. Its cooked WidgetAdder was inspected locally to understand actor-started UMG creation. No implementation or binary assets were copied, adapted or redistributed; it introduces no bundled dependency. Our inventory feedback implementation is project-authored, using native engine widgets and references to game fields. Do not copy this mod's implementation under assumed MIT terms.

## Blueprint Loader inspection and permissions

Official CurseForge file 3385182 was inspected locally to understand online startup. Its widget uses GetGameMode-dependent triggers. The author permissions on https://www.nexusmods.com/minecraftdungeons/mods/111?tab=description require permission for modifications/asset use and prohibit uploads to other sites. No loader assets or code are copied, modified or bundled by this project. It remains an external installation dependency. A joining-client replacement must use project-owned implementation or separately obtained author permission.

## Expanded runtime/tool review (2026-10-05)

See MODDING_OPTIONS_REVIEW.md for pinned inspected sources and reuse decisions. UE4SS currently publishes MIT licensing and is a potential runtime diagnostic/client loader option; it is not yet a verified or bundled dependency. GUI X AGPLv3 assets were inspected for compiled value flow, with no implementation/assets copied. Epic UE4.22.3 source was inspected via a version-pinned mirror to understand VM references; no engine code is redistributed. DungeonsLevelLoader, Custom-Skins-Loader and MCD-SMF had no root reuse license observed and remain inspection only. Existing MIT LetMeMove/UAssetAPI/Mod Kit reuse retains its notices.

## External reflection source review — 2026-10-06

See [EXTERNAL_REFLECTION_REVIEW.md](EXTERNAL_REFLECTION_REVIEW.md) for pinned MIT source/license checks on Unreal-eXternalrEsolve, McDaived/UE-Dumper and Spuckwaffel/UEDumper. These are inspection references only: no code, binary or runtime dependency is copied, executed or bundled. Legacy UProperty parameter/layout gaps, overbroad process access and unconfigured game offsets prevent declaring a ready Dungeons Store collector. The withdrawn UE4SS Install remains disabled.
