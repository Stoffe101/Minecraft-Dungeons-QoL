# Current State

Last updated: 2026-10-04

## Repository state

The repository has been bootstrapped with:

- reproducible Dungeons Mod Kit setup
- environment detection for Unreal Engine 4.22 and Minecraft Dungeons
- asset sync, build, and install scripts
- Microsoft Store / Xbox app targeting
- initial architecture for gear locking, mass salvage, persistence, and loadouts
- research helper for current inventory assets
- documentation and test structure

## Functional status

No playable QoL pak has been produced yet.

There are not yet any project-owned `.uasset` / `.umap` Blueprint assets under `mod/Content`. This is intentional: the first Blueprint pass depends on identifying the current Dungeons 1 inventory classes/functions rather than guessing stale names from old builds.

## New identity findings

Public MCDSaveEdit source confirms:

- hero profiles contain `uniqueSaveId`
- items contain `inventoryIndex`
- equipped gear remains in the same item collection and is identified by `equipmentSlot`
- new items are assigned `max inventoryIndex + 1`

This gives us a strong fallback identity plan if there is no native runtime GUID.

Caveats: storage transfer can assign a new index, and a deleted highest index can later be reused. Therefore runtime GUID/unique-ID discovery is still preferred, and any index-based fallback needs sanity checks/reconciliation.

## Immediate technical target

Discover and verify, on the current Dungeons 1 runtime:

1. the inventory widget/class used in camp and missions
2. the selected item object / item instance structure
3. whether it exposes a native GUID/unique ID
4. whether it exposes the save-format `inventoryIndex`
5. whether hero state exposes `uniqueSaveId`
6. the native salvage function and its required parameters
7. the native equip/unequip function
8. whether the vanilla salvage button can be intercepted/guarded without replacing the whole inventory screen

Once those are known, implement the smallest vertical slice:

**select item -> toggle lock -> persist lock -> show lock overlay -> reject that item in mass-salvage selection**

## Known external requirements

- Windows
- Unreal Engine 4.22.x
- Dungeons Mod Kit
- Blueprint Loader
- Minecraft Dungeons 1 installation

## Safety policy

The project must not directly edit hero save files or manually add emeralds/enchantment points to simulate salvage. Bulk salvage should call the game's native salvage path for each validated item.
