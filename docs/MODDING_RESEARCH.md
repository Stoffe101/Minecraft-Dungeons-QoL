# Minecraft Dungeons 1 Modding Research

Last updated: 2026-10-04

This document records how Minecraft Dungeons 1 mods are actually built and loaded, which tools are reliable, which approaches are experimental, and why this project chooses its current workflow.

## Executive conclusion

For a gameplay/UI mod such as Minecraft Dungeons QoL, the primary route is:

1. Unreal Engine **4.22.x**
2. Dokucraft **Dungeons Mod Kit**
3. a small editor-only **mirror of verified Dungeons reflection APIs**
4. normal Blueprint actor/widget authoring
5. cook through UE4.22
6. package through the Mod Kit
7. load through **Blueprint Loader**

Raw cooked-Blueprint bytecode rewriting remains a fallback/automation route, not the primary way to author the project.

## 1. Why a mirror API is the right Unreal workflow

Mod authors do not have Mojang's complete Unreal project or headers.

The standard Unreal approach is to create editor-side dummy/mirror classes with the same reflected module/class/function names used by the shipping game.

The Dungeons Mod Kit already gives us the critical foundation: its UE module is named:

```text
Dungeons
```

If the editor project declares the verified class:

```text
UItemStashComponent
```

Blueprints reference:

```text
/Script/Dungeons.ItemStashComponent
```

At runtime, Minecraft Dungeons supplies the real `Dungeons` module and the Blueprint reference resolves to the real native class.

The mirror implementation itself is only compile/editor scaffolding.

### Mirror rules

- Mirror only reflection-visible APIs we can substantiate.
- Preserve names exactly.
- Preserve enum values/order exactly.
- Preserve function parameter/return types used by our Blueprints.
- Give editor dummy implementations harmless defaults.
- Do not recreate game logic in the mirror.
- Runtime behavior must come from the real game classes.

Primary final-build class research:

https://github.com/Minecraforever/MCD-PE

MCD-PE is Apache-2.0 and contains restored class/function architecture with final-binary verification work.

## 2. Dungeons Mod Kit remains the authoring/build foundation

Repository:

https://github.com/Dokucraft/Dungeons-Mod-Kit

License: MIT.

Requirements include Windows, Python 3.8+, and Unreal Engine 4.22.x.

The project:

- uses UE4.22
- uses a module named `Dungeons`
- cooks `WindowsNoEditor` assets
- stages a Dungeons-compatible asset tree
- supports project-provided precooked assets
- packages the staged tree into a `.pak`

Project implication:

Use its normal cook/package flow for releases instead of inventing a different mount layout.

## 3. Blueprint Loader is the proven Dungeons 1 runtime entry point

Blueprint Loader exposes trigger locations:

```text
/Game/BPLoader/Menu
/Game/BPLoader/Lobby
/Game/BPLoader/Ingame
```

A current 2026 Dungeons 1 mod, LetMeMove!, demonstrates that the Dungeons 1 loader still works with the current/final game.

Reference:

https://github.com/StainlessStasis/LetMeMove

Recommended project structure:

```text
BPLoader/Lobby/MCDQoL_Lobby.umap
  -> BP_MCDQoL_Manager

BPLoader/Ingame/MCDQoL_Ingame.umap
  -> BP_MCDQoL_Manager
```

Blueprint Loader stays a separate player-installed dependency because its published permissions do not allow us to simply redistribute it.

## 4. Proven Dungeons UI pattern: loader actor creates UMG widget

Camera Coordinates Overlay is a particularly useful Dungeons 1 example.

Source:

https://github.com/EvenTorset/Camera-Coordinates-Overlay

Its source project contains:

- a Blueprint Loader trigger map
- a `WidgetAdder` actor
- a working UMG widget
- viewport-add logic

Its published Nexus permissions allow modification/use of its assets.

This gives Minecraft Dungeons QoL a proven widget/viewport integration template.

Preferred UI architecture:

```text
BPLoader level
  -> BP_MCDQoL_Manager
      -> WBP_MCDQoL_Overlay
      -> Lock service
      -> Salvage selection service
      -> Loadout service
```

Replacing the complete vanilla inventory widget remains a last resort.

## 5. Verified native inventory API

Final-build Dungeons research identifies the native backend needed by this project.

### UInventoryItemSlot

Verified Blueprint-facing members include:

```text
Item
SlotType
GetChangeIndex()
AcceptsItem(...)
CanSwapWith(...)
Swap(...)
IsLocked()
WasSelectedInUI()
HasSlotChanged()
FinishedSlotChanged()
```

### UItemStashComponent

Verified Blueprint-facing operations include:

```text
GetInventorySlots()
GetEquipmentSlots()
GetChangeIndex()
EnterInventoryUI()
ExitInventoryUI()
RemoveItem(...)
SalvageItemInSlot(...)
SalvageItemUndo(...)
GetSalvageInfo(...)
CompareItemPowerWithEquipped(...)
AvailableEnchantmentPoints()
InventoryUIRequiresRefresh()
```

The core destructive operation is:

```cpp
FItemSalvageUndoInfo SalvageItemInSlot(
    UInventoryItemSlot* slot,
    bool& success
);
```

It is BlueprintCallable.

## 6. Correct mass-salvage implementation

Do not duplicate Dungeons' salvage reward rules.

Correct flow:

```text
selected native slot
  -> still valid?
  -> not equipped?
  -> not QoL-locked?
  -> not loadout-protected?
  -> identity still matches?
  -> SalvageItemInSlot(slot)
  -> inspect success
  -> retain result/undo metadata
```

This preserves the game's own:

- item destruction
- emerald/gold reward behavior
- enchantment-point return
- native state updates
- undo data where usable

Direct hero-save/currency mutation is unnecessary.

## 7. Gear-set switching should use native slot operations

`UInventoryItemSlot` exposes:

```text
CanSwapWith(...)
Swap(...)
```

The first loadout implementation should resolve the real inventory item slot plus the correct equipment slot, then use the native swap operation.

This is preferable to constructing replacement item data because it preserves the actual gear instance, enchantments, gilded state, upgrades, and other native metadata.

## 8. Item identity is a separate problem

Public save-format research confirms:

- hero `uniqueSaveId`
- hero `playerId`
- item `inventoryIndex`
- item `equipmentSlot`
- item type/power/rarity/enchant/gilded data

`uniqueSaveId + inventoryIndex` is useful as a fallback locator, not a permanent GUID.

Storage movement can change the index and removed high indexes can later be reused.

Preferred persistent identity:

1. native stable runtime instance ID/GUID, if available
2. other verified stable native identifier
3. hero ID + inventory index + sanity fingerprint
4. fail closed when ambiguous

See `INVENTORY_IDENTITY.md`.

## 9. Cooked-asset research tools

### FModel

Use for:

- browsing cooked Unreal packages
- locating asset paths
- inspecting/exporting supported asset data

Minecraft Dungeons is identified by Unreal tooling/community compatibility data as UE4.22.

### repak

Repository:

https://github.com/trumank/repak

Use for:

- listing pak contents
- extracting pak files
- repacking diagnostic/test paks

The Dungeons Mod Kit remains the project's normal release packager.

### UAssetAPI / UAssetGUI

Use for:

- cooked `.uasset` parsing
- import/export inspection
- property inspection
- targeted binary modification
- CI validation

Project CI verified that **UAssetAPI 1.1.0 successfully parses a known-working current Dungeons Blueprint**:

- object version 517
- 228 names
- 67 imports
- 83 exports

This makes UAssetAPI an excellent validation/research tool.

## 10. KismetKompiler research result

KismetKompiler is MIT licensed and can decompile/compile Kismet.

Our tests found:

- its bundled older UAssetAPI cannot parse the working Dungeons template
- modern UAssetAPI can parse the template
- modern UAssetAPI versions have breaking API changes relative to KismetKompiler
- forcing that toolchain adds complexity before we have authored any gameplay feature

Decision:

**Do not block the mod on KismetKompiler.**

Keep it as a fallback if a specific cooked Blueprint later needs bytecode-level modification.

## 11. UE4SS research result

UE4SS is powerful for many Unreal games and its general tooling/docs are valuable for understanding reflection dumps and mirror projects.

However, recent public Minecraft Dungeons reports describe retail-build startup/crash problems.

Decision:

- do not require UE4SS at runtime
- do not make the project depend on UE4SS header generation
- use it only experimentally if a known compatible Dungeons build/config is demonstrated

Blueprint Loader remains the runtime loader.

## 12. Existing-mod reuse policy

Reuse is encouraged when permissions are clear.

### Dungeons Mod Kit

MIT.

Safe to build on with normal license compliance.

### LetMeMove!

MIT.

Useful for:

- current Dungeons 1 loader compatibility
- loader map structure
- working actor Blueprint structure

### Camera Coordinates Overlay

Published permissions allow reuse/modification of its assets.

Useful for:

- widget-adder pattern
- UMG template
- viewport integration
- loader-map reference

Credit/reuse details must be recorded in `THIRD_PARTY.md`.

### No-license repositories

Public availability is not permission to copy.

For unclear/no-license source:

- research architecture
- do not copy code/assets into the project unless separate permission exists

## 13. Precooked assets are a legitimate fallback

The Dungeons Mod Kit supports a `Precooked` staging route.

If a permitted cooked template must be modified with UAssetAPI, a valid advanced pipeline is:

```text
permitted cooked template
  -> targeted UAssetAPI change
  -> parse/round-trip validation
  -> project Precooked path
  -> normal Mod Kit pak packaging
```

Use this only where normal UE4.22 authoring is less practical.

## 14. Installation research correction

The launcher button used to start Dungeons does not necessarily identify the installation type.

### Store/Xbox-managed copy launched from Minecraft Launcher

If the Minecraft Launcher Dungeons installation field shows only a drive such as:

```text
C:
D:
```

Dungeons modding guidance says to follow the Microsoft Store/Xbox installation workflow.

Typical modern path:

```text
<drive>:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks
```

This copy can normally still be launched from Minecraft Launcher/Xbox/Store/Start after mod installation.

### Older standalone Launcher copy

If Minecraft Launcher shows a real installation directory, the game lives below that directory, commonly:

```text
<installation>\dungeons\dungeons\Dungeons\Content\Paks
```

Older Launcher behavior may restore/remove modded files when the Launcher starts the game.

For that install type, test by launching its `Dungeons.exe` directly after placing the mod.

### Common rule

The actual target is always:

```text
<active Dungeons install>\Dungeons\Content\Paks\~mods
```

## 15. Destructive-feature testing

A bulk-salvage feature needs a stricter safety bar than a cosmetic mod.

Initial destructive testing should use:

- disposable/test hero
- cheap Common gear
- save backup

Progression:

1. mod loads in camp
2. mod loads in mission
3. overlay opens/closes
4. inventory slots enumerate
5. equipment slots enumerate
6. non-destructive select/deselect works
7. lock state works without item mutation
8. one cheap item salvages through native function
9. reward matches vanilla
10. multiple selected items salvage sequentially
11. cancel works
12. reordering/removing selected gear before confirmation is handled
13. locking after selection is revalidated
14. restart persistence
15. multiple heroes
16. storage transfer/reconciliation
17. controller
18. online co-op local-player-only behavior

Never use irreplaceable equipment for the first destructive test.

## 16. Release definition

A generated `.pak` alone is not a finished mod.

Minimum release gate:

- pak loads
- no launch/camp crash
- lock works
- mass selection works
- locked/equipped gear cannot batch salvage
- native salvage reward is correct
- persistent lock works across restart
- loadout save/switch works
- missing/stale item references fail safely
- keyboard/mouse works
- controller works
- Store/Xbox-managed install works
- Launcher behavior documented
- co-op sanity test passes

## 17. Stable project direction

Primary path:

```text
UE4.22
  -> Dungeons Mod Kit
  -> verified /Script/Dungeons mirror API
  -> project-owned Blueprint UI/logic
  -> cook
  -> pak
  -> Blueprint Loader
  -> Minecraft Dungeons
```

Supporting tools:

```text
FModel / UAssetAPI / repak
  -> inspect
  -> validate
  -> diagnose
  -> targeted precooked modification when necessary
```

The next engineering task is the minimal verified mirror API required for the first non-destructive inventory diagnostic Blueprint.

## 18. Fresh source review and applied workflow (2026-10-04)

This section supersedes earlier statements that the repository had no runtime graph. It also distinguishes upstream findings from independently verified game behavior. See `REPO_AUDIT.md`.

### How a Dungeons 1 mod works

Dungeons 1 loads Unreal cooked packages from pak archives. A pak is a container, not executable mod logic by itself. Replacing an asset requires its virtual package path to match the asset being replaced. New actor/widget logic additionally requires a runtime entry point: Blueprint Loader loads levels under its documented Menu/Lobby/Ingame triggers. Put our manager into small Lobby/Ingame levels; do not accidentally retain another mod's package paths.

Assets created in the Unreal editor are source assets; cook them for the matching engine/platform before packaging. A cooked `.uasset` may need its companion `.uexp`/`.ubulk`. Preserve those pairs and the `Dungeons/Content` package tree. Native game C++ does not become a new runtime mod merely by compiling the editor stub DLL. The shipping game supplies `/Script/Dungeons`; the mirror must match reflected signatures, including return/out parameter types.

For this gameplay/UI scope, editor-authored UE4.22 Blueprints are the maintainable target. Cooked patching is useful for diagnostic automation, but serialization success cannot certify Unreal VM behavior or native function signatures.

### Tool selection

| Tool / primary source | Task | Decision for this project |
| --- | --- | --- |
| [Dungeons Mod Kit](https://github.com/Dokucraft/Dungeons-Mod-Kit) | UE4.22 project, cooking, precooked staging, u4pak packaging | Primary authoring foundation; pin `c30e88ec5e99e401eadedddbe82af0265a056fe7` |
| [Blueprint Loader](https://www.nexusmods.com/minecraftdungeons/mods/111) | Spawn/load mod levels at Menu, Camp and mission triggers | Runtime dependency; separate download under its published restrictions |
| [LetMeMove](https://github.com/StainlessStasis/LetMeMove) | Working Dungeons 1 loader/actor example | MIT template; release 1.1.0 ZIP SHA-256 `9ed80bd696c5124861efe349c30d5e514412bd923364507514db4efb9299c675`; retain license and relocate assets |
| [Camera Coordinates Overlay](https://www.nexusmods.com/minecraftdungeons/mods/112) | Loader actor creates a viewport widget | Reference for future overlay; rechecked published asset permissions |
| [FModel](https://github.com/4sval/FModel), [installation](https://github.com/4sval/FModel/wiki/Installing-FModel) | Explore game archives/assets, paths and exported data | Inspect the installed game; current documented builds use Windows and .NET 10; choose UE4.22 settings appropriate to Dungeons 1 |
| [UAssetAPI](https://github.com/atenfyr/UAssetAPI), [basic guide](https://atenfyr.github.io/UAssetAPI/guide/basic.html) | Inspect/edit properties and Kismet; re-open validation | Pin project tools to NuGet 1.1.0 on .NET 8; current upstream docs target .NET 10 and must not be applied blindly to the pinned API |
| [UAssetGUI](https://github.com/atenfyr/UAssetGUI) | Interactive inspection of the same asset structures | Useful Windows companion; no automatic upgrade of project parser |
| [repak](https://github.com/trumank/repak) | List/extract/repack pak archives | Useful alternative inspector; not a cooker or Blueprint compiler |
| [KismetKompiler](https://github.com/tge-was-taken/KismetKompiler) | Decompile/recompile supported Blueprint graphs | Experimental; upstream states incomplete constructs and primarily UE4.23 testing; current project compatibility failures still matter |
| [UE4SS](https://github.com/UE4SS-RE/RE-UE4SS) | Reflection dump, SDK/mirror generation, hooks, Lua/C++ | Optional investigation only; generic UE4.22 support does not establish retail Dungeons compatibility |
| [UE4SS Dungeons issue 1219](https://github.com/UE4SS-RE/RE-UE4SS/issues/1219), [1211](https://github.com/UE4SS-RE/RE-UE4SS/issues/1211) | Dungeons 1 startup failure reports | Evidence of compatibility risk, not proof all configs fail; no UE4SS runtime dependency |
| [MCD-PE](https://github.com/Minecraforever/MCD-PE) | Restored Steam-final inventory architecture | Reference evidence only; independently verify reflected instance functions and Store ABI before runtime release |
| [Epic SaveGame documentation](https://dev.epicgames.com/documentation/en-us/unreal-engine/saving-and-loading-your-game?application_version=4.27) | Separate SaveGame object and SaveGameToSlot/LoadGameFromSlot | Design reference; check UE4.22 signatures and actual retail platform persistence |

Sources reviewed 2026-10-04. FModel's correct repository is `4sval/FModel`. Dungeons 2 toolchains/UE5 templates are outside this project's Dungeons 1 target. Fabric/Forge Java mod loaders do not provide this Unreal workflow.

### Practical development loop

1. Locate the active game's Paks folder; record edition, executable version/hash, mod dependencies and startup log.
2. Explore only the needed inventory assets/reflection metadata; keep local extraction under ignored `.research`.
3. Write a minimal non-destructive probe to verify controller/stash discovery and exact signatures before adding UI/destruction.
4. Author editor Blueprints against only substantiated mirror APIs. Use cooked templates where permissions and the exact patch scope are known.
5. Cook/stage, retain companions, namespace new assets, package, hash and list the pak. Include required third-party notices outside the pak in the release artifact.
6. Test loading and diagnostics in Camp and a mission on the actual Store/Xbox-managed installation.
7. Add persistent identity/guards/UI, then one native salvage test, then batching/loadouts.
8. Record source commit, actual results and unresolved checks; a generated archive is not a finished build.

### Applied to this repository

The new diagnostic builder pins inputs, regenerates the sidecar, relocates the manager/maps, checks bytecode and package content, includes the MIT notice and keeps the workflow read-only. F10 now previews rather than destroying items. Snapshot identity checks and stash-change cancellation provide a safer base for future batching.

No broad "all tooling researched forever" claim is made: the remaining reflection/identity/UI questions require game evidence, not more generic web searching.

## 19. Installed-build evidence route (2026-10-04)

[UeBlueprintDumper](https://github.com/CrystalFerrai/UeBlueprintDumper) exports Blueprint class/function metadata and disassembly through CUE4Parse. Reviewed commit `9726294772458eb6114946e967e204925f8b1b66`; pinned release 1.2.0, .NET 8 runtime. Its argument parser requires `game directory`, `engine version`, `asset match`, `output directory`, despite the shorter usage banner omitting the engine position. `--list --dump` supports both listing and inspection in one invocation. Dungeons is inspected initially with `UE4_22`; parser failure is evidence to investigate, not permission to assume compatibility.

Applied in `Collect-GameEvidence.ps1` with six inventory-related path terms, checksum verification, active-install disambiguation, manifest/diagnostic ZIP and no save access. Logs can include local paths and need review before sharing. Cooked Blueprint references can establish call patterns, but are not a replacement for native reflection signatures or in-game tests. See `GAME_EVIDENCE.md`.

Also inspected [zMCDungeons-SDK](https://github.com/zH4x-SDK/zMCDungeons-SDK), commit `ab9d8f0ab04b215577dd2eb067e65015b5a70521`. Its JSON identifies version 1.0.3; inspected inventory methods in C++ have void declarations, empty parameter structures and zero-size class metadata. Reject this dump as exact signature/identity evidence for the current release. No code was copied from it.

### Actual Store/Xbox collection result

The user's retry at source `820387e` enumerated 47 pak filenames but returned six empty asset lists. Dumper logs show `AES key No`, a localization warning and `Completed`, without native call metadata. Reviewed dumper source: without a key it submits an all-zero key to each unloaded archive, then lists the provider's visible files; successful program exit does not certify mounting. Encryption is the leading hypothesis, but unreadable archives/parser behavior remain alternatives until the explicit key retry produces paths.

Primary implementation evidence for a retry key: [Dungeons Arabic localization pak reader](https://github.com/Saad5400/minecraft-dungeons-arabic/blob/c1a8c20ea714ab63ea33b04025bc84c08747f12a/tools/pak.js) uses `7D5F892ECEBFA53CC22001DF48B871D51C0DF7C54CE41933BFB285219829B3A8`. The same value appears in a [Dungeons 1.17.0.0 extraction-tool comment](https://www.nexusmods.com/minecraftdungeons/mods/67?tab=posts). This is a sourced candidate, not verified compatibility with this installation. No implementation code copied. Added a list-only archive catalog, visible path count and explicit key-provided flag; key values are not stored in the report.
