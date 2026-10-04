# Current State

Last updated: 2026-10-04

## Repository state

The repository has:

- reproducible Dungeons Mod Kit setup
- environment detection for Unreal Engine 4.22 and Minecraft Dungeons
- build/install scripts
- support for both Minecraft Launcher and Xbox app / Microsoft Store install paths
- player-facing installation instructions for `.pak` placement
- architecture for gear locking, mass salvage, persistence, and loadouts
- item identity research
- final-build native inventory/salvage API research
- documentation and test structure

## Functional status

No user-tested release pak exists yet.

The project is now past the earlier "unknown salvage API" blocker. A final-build reverse-engineering/source-restoration project confirms the relevant Dungeons 1 native class architecture and Blueprint-callable methods still exist.

## Verified native inventory surface

`UInventoryItemSlot` is Blueprint-visible and exposes the current item.

`UItemStashComponent` is a Blueprint-spawnable actor component and exposes the critical operations we need:

```text
GetInventorySlots()
GetEquipmentSlots()
GetChangeIndex()
EnterInventoryUI()
ExitInventoryUI()
SalvageItemInSlot(slot, success)
SalvageItemUndo(undoInfo)
GetSalvageInfo(item)
CompareItemPowerWithEquipped(item)
AvailableEnchantmentPoints()
```

This changes the implementation plan substantially: mass salvage does **not** need custom destruction/reward math. Each validated selected slot can be passed through `SalvageItemInSlot`, preserving Dungeons' own salvage result handling and undo metadata.

## Selection finding

Dungeons internally has:

`UItemStashComponent::OnInventoryItemSlotSelected`

and the vanilla hint system subscribes to it for gear-selection hints.

However, the restored declaration is a native multicast delegate and is not marked `BlueprintAssignable`. A pure Blueprint mod cannot assume it can bind directly to that delegate.

Therefore the first playable build must either:

1. hook/observe the vanilla inventory widget/selection through another Blueprint-accessible path, or
2. provide its own selection overlay backed by `GetInventorySlots()`.

Option 2 is the safe fallback and still allows a complete lock + batch-salvage workflow without replacing the game's save/economy behavior.

## Identity state

Public save-format research confirms:

- hero profiles contain `uniqueSaveId`
- items contain `inventoryIndex`
- equipped gear remains in the same item collection and is identified by `equipmentSlot`

This remains the fallback persistence locator if runtime item objects expose no stronger stable instance identity.

## Immediate implementation target

Produce a first playable Blueprint Loader build that:

1. finds the local player's `UItemStashComponent`
2. enumerates `GetInventorySlots()`
3. excludes all `GetEquipmentSlots()`
4. lets the player mark/unmark inventory slots in a batch
5. keeps an in-session protected/locked set
6. revalidates every slot immediately before destruction
7. calls `SalvageItemInSlot` for each accepted slot
8. records success/failure and undo information
9. never directly mutates currency or the hero save

Persistent locks/loadouts follow once the runtime identity/persistence bridge is verified.

## Known external requirements

- Windows
- Minecraft Dungeons 1
- Blueprint Loader
- Dungeons Mod Kit / UE4.22 for normal authoring, unless the bytecode-tooling route proves reliable

## Safety policy

The project must not directly edit hero save files or manually add emeralds/enchantment points to simulate salvage.

Every destructive operation must be revalidated and sent through Dungeons' native salvage path.
