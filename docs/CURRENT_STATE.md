# Current State

Last updated: 2026-10-04

## Repository state

The repository has been bootstrapped with:

- reproducible Dungeons Mod Kit setup
- environment detection for Unreal Engine 4.22 and Minecraft Dungeons
- asset sync, build, and install scripts
- Microsoft Store / Xbox app targeting
- initial architecture for gear locking, mass salvage, persistence, and loadouts
- research plan for discovering the current runtime inventory APIs
- documentation and test structure

## Functional status

No playable QoL pak has been produced yet.

There are not yet any project-owned `.uasset` / `.umap` Blueprint assets under `mod/Content`. This is intentional: the first Blueprint pass depends on identifying the current Dungeons 1 inventory classes/functions rather than guessing stale names from old builds.

## Immediate technical target

Discover and verify, on the current Dungeons 1 build:

1. the inventory widget/class used in camp and missions
2. the selected item object / item instance structure
3. a stable per-item identifier, ideally a GUID or equivalent
4. the native salvage function and its required parameters
5. the native equip/unequip function
6. whether the vanilla salvage button can be intercepted/guarded without replacing the whole inventory screen

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
