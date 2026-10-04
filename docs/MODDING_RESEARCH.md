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
