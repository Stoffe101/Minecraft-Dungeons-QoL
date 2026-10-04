# Findings

Last updated: 2026-10-04

## Dungeons Mod Kit

Dokucraft's Dungeons Mod Kit remains the practical UE4.22 foundation.

- Repository: https://github.com/Dokucraft/Dungeons-Mod-Kit
- License: MIT
- Requires Windows, Python 3.8+, Unreal Engine 4.22.x
- Current project tooling pins commit `c30e88ec5e99e401eadedddbe82af0265a056fe7`

## Blueprint Loader

Minecraft Dungeons 1 Blueprint Loader loads trigger levels under:

- `/Game/BPLoader/Menu`
- `/Game/BPLoader/Lobby`
- `/Game/BPLoader/Ingame`

It remains a separate runtime dependency and is not redistributed here.

A 2026 Dungeons 1 mod, LetMeMove!, reports that the old Dungeons 1 Blueprint Loader still works on the latest game build.

Reference:
https://github.com/StainlessStasis/LetMeMove

## Supported Microsoft-account install layouts

Ownership source and install source are separate.

Common Minecraft Launcher path:

```text
%LOCALAPPDATA%\Mojang\products\dungeons\dungeons\Dungeons\Content\Paks
```

Common Xbox app path:

```text
C:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks
```

The mod belongs in the active installation's `Paks\~mods` folder.

## Final-build Dungeons source restoration

Repository:
https://github.com/Minecraforever/MCD-PE

License: Apache-2.0.

This project restores class/function architecture from Dungeons 1 and explicitly verifies the relevant UCLASS names against the final Steam build binary.

For our purposes, the major result is that the classic `UItemStashComponent` / `UInventoryItemSlot` architecture is still present in the final build.

### UInventoryItemSlot

Verified Blueprint-facing properties/methods include:

- `SlotType`
- `Item`
- `GetChangeIndex()`
- `IsLocked()`
- `WasSelectedInUI()`
- `HasSlotChanged()`
- `FinishedSlotChanged()`

Note: native `IsLocked()` is vanilla slot/equipment locking and is **not** our persistent user lock feature.

### UItemStashComponent

Critical Blueprint-callable methods:

```text
GetInventorySlots()
GetEquipmentSlots()
GetChangeIndex()
EnterInventoryUI()
ExitInventoryUI()
GetLowestPoweredItem()
GetEquippedItemsOfSlotType(...)
RemoveItem(...)
SalvageItemInSlot(...)
SalvageItemUndo(...)
GetSalvageInfo(...)
CompareItemPowerWithEquipped(...)
AvailableEnchantmentPoints()
```

The exact native destructive call we want is:

```cpp
FItemSalvageUndoInfo SalvageItemInSlot(
    UInventoryItemSlot* slot,
    bool& success
);
```

This is marked `BlueprintCallable`.

### Why this matters

Mass salvage can be implemented as repeated native salvage transactions:

```text
selected slot
 -> protection validation
 -> slot still exists?
 -> not equipped?
 -> not locked/loadout protected?
 -> SalvageItemInSlot(slot)
 -> inspect success
 -> retain undo/result metadata
```

We do not need to recreate emerald/gold/enchantment-point calculations.

### Native salvage data

The restored salvage utility shows Dungeons computes salvage value from item power and rarity, gives gold for Netherite items and emeralds otherwise, and returns invested enchantment points.

Using `SalvageItemInSlot` therefore remains the safest implementation.

## Vanilla selection event

Dungeons internally exposes:

`FOnInventoryItemSlotSelected OnInventoryItemSlotSelected`

The vanilla hint system subscribes to it.

The delegate is native rather than `BlueprintAssignable`, so we cannot yet rely on a pure Blueprint binding.

This is the remaining vanilla-UI integration question, not the salvage backend.

## Save/profile identity

MCDSaveEdit confirms profile fields:

- `playerId`
- `uniqueSaveId`

and item fields such as:

- `inventoryIndex`
- `equipmentSlot`
- type
- power
- rarity
- enchantments
- gilded/netherite data

`uniqueSaveId + inventoryIndex` remains a useful fallback locator but not a globally permanent item identity. Storage transfers can reassign indexes and the highest deleted index may later be reused.

See `INVENTORY_IDENTITY.md`.

## Blueprint bytecode tooling lead

KismetKompiler:
https://github.com/tge-was-taken/KismetKompiler

License: MIT.

It can decompile/recompile UE4 Blueprint bytecode and supports editing existing Blueprint assets. It was primarily tested against UE4.23, while Dungeons uses UE4.22, so compatibility must be proven before it becomes part of the release build pipeline.

Its major limitation is that editing an existing Blueprint is substantially better supported than creating a new class from scratch.

## Strong conclusions

1. Native salvage is available to Blueprint and should be used.
2. Inventory and equipped slots are enumerable from Blueprint.
3. The big technical unknown has narrowed from "how do we salvage?" to "how do we integrate cleanly with vanilla selection/UI?"
4. A custom QoL selection overlay is a safe fallback if vanilla selection cannot be bound from Blueprint.
5. Persistent identity still needs runtime validation before permanent locks/loadouts are considered release-safe.


## Audit correction: evidence vs runtime validation (2026-10-04)

Upstream MCD-PE restoration describes final Steam class architecture; this repository has not independently verified Store/Xbox reflection metadata or complete parameter layouts. In particular, the cooked prototype's InventoryItem display/enchantment/CanSalvage instance calls are not all established as reflected functions by the inspected source. Native IsLocked describes vanilla slot state, not the user's persistent gear lock.

The implemented diagnostic graph now snapshots items as well as slots, cancels on stash changes, uses a reflected inventory array local, checks sidecar class/write success and emits no destructive call. Reusing the exact LetMeMove paths would override that mod; relocating manager and loader maps resolves that packaging collision structurally. See REPO_AUDIT.md for the complete findings and runtime gaps.

## Actual Store/Xbox archive catalog (2026-10-04)

User evidence at source `3b86620` confirms the explicit Dungeons 1 key mounts this installation's catalog: 131,164 visible Dungeons paths, including 45,035 `.uasset` paths. This validates archive enumeration, not function parameter signatures or in-game execution. The broad Inventory export was interrupted by Windows PowerShell promoting the inspector's stderr to a terminating error at cosmetic `UMG_CosmeticButtonEquip.GetButtonReference`. Uploaded folders contain no class/function output; do not infer native API layouts from them.

The catalog establishes these concrete inspection targets. Counts refer to matching asset paths, not independently verified Blueprint classes.

| Collector group | Catalog path match under `Dungeons/Content/` | Asset count | Intended evidence |
| --- | --- | --- | --- |
| Inventory | `UI/Inventory/UMG_Inventory` | 9 | HUD, slot widgets, gear slots and inventory visibility |
| Salvage | `UI/Inventory/Salvage/` | 7 | Confirm/cancel/toggle, undo and resource updates |
| ItemWidgets | `UI/Inventory/UMG_Item` | 5 | Item power, usage and enchantment info |
| ItemInspector | `UI/Inventory/Inspector2/UMG_InventoryItem` | 2 | Item inspector bindings |
| SlotGrid | `UI/Grid/` | 5 | Cached grid and slot navigation |
| PlayerController | `Actors/Characters/Player/BP_PlayerController` | 3 | Controller/interface/shared UI integration |

Important names include `UMG_InventoryHUD`, `UMG_InventorySlotWidget`, `UMG_InventoryGearSlotWidget`, `UMG_SalvageButtonConfirm`, `UMG_SalvageButtonToggle`, `UMG_SalvageUndoButton`, `UMG_InventoryItemInspector`, `UMG_SlotGridWidget` and `BP_PlayerControllerSharedUI`. No ItemStash-named cooked asset is required for its native `/Script/Dungeons` class to exist. The next evidence is these assets' actual exported references/properties/functions, followed by a non-destructive in-game probe.
