# Findings

Last updated: 2026-10-04

## Verified

### Dungeons Mod Kit

The Dokucraft Dungeons Mod Kit is still the standard practical foundation for Dungeons 1 Blueprint/pak mod work.

- Repository: https://github.com/Dokucraft/Dungeons-Mod-Kit
- License: MIT
- Requires Windows, Python 3.8+, and Unreal Engine 4.22.x.
- It cooks Unreal assets and packages them into a `.pak`.
- Current project tooling pins commit `c30e88ec5e99e401eadedddbe82af0265a056fe7` for reproducibility.

### Blueprint Loader

Minecraft Dungeons 1 Blueprint Loader exposes three trigger folders:

- `/Game/BPLoader/Menu`
- `/Game/BPLoader/Lobby`
- `/Game/BPLoader/Ingame`

Our first implementation should load in **Lobby** and **Ingame**, because the inventory can be used in camp and during missions.

The original Blueprint Loader is a runtime dependency and should **not** be bundled into this repository.

### Current Dungeons 1 compatibility

The 2026 mod **LetMeMove!** reports that the original Dungeons 1 Blueprint Loader still works on the latest Dungeons 1 version despite the loader being old.

Reference:
https://github.com/StainlessStasis/LetMeMove

The LetMeMove repository is MIT licensed and demonstrates the current expected content layout:

- `Content/BPLoader/Ingame/<map>.umap`
- `Content/Mods/<mod>/<blueprint>.uasset`

We may use its structure and MIT-licensed implementation as a reference. We do not currently copy its assets because they solve movement rather than inventory management.

### Microsoft Store / Xbox app install

Modern Microsoft Store / PC Game Pass installs can be modded from the normal Xbox app installation directory. The common default is:

`C:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks`

The correct path should be discovered through Xbox app -> Minecraft Dungeons -> Manage -> Files when installed elsewhere.

Old guides that require dumping/re-registering the UWP package predate the modern Xbox installation model and are not the preferred route for this project.

### Save/profile identity

The current open-source MCDSaveEdit model exposes both:

- `playerId`
- `uniqueSaveId`

on a hero profile.

`uniqueSaveId` is the better candidate for a per-hero QoL namespace. We still need to prove the corresponding value is available from runtime Blueprints.

Source:
https://github.com/CutFlame/MCDSaveEdit

### Item identity lead: inventoryIndex

The save model exposes `inventoryIndex` on each item. MCDSaveEdit:

- orders unequipped inventory by `InventoryIndex`
- assigns a newly-added item `max(existing index) + 1`
- keeps equipped gear in the same Items collection and distinguishes it with `equipmentSlot`

This makes `inventoryIndex` a strong **provisional** per-hero item key if the same value is exposed at runtime.

Important limitations:

- moving an item between main inventory and storage assigns an index in the target collection
- deleting the highest-index item can allow that number to be reused later
- therefore `uniqueSaveId + inventoryIndex` alone must not be treated as a permanent globally unique ID

For the first prototype, if no native GUID exists, use `uniqueSaveId + inventoryIndex` plus a sanity fingerprint and fail closed on mismatches.

See `INVENTORY_IDENTITY.md`.

### Encrypted game data

The DungeonsModding/Useful-things repository contains the community extraction tooling/AES-key reference used for Dungeons data files.

Research tooling should accept an AES key as a parameter rather than hardcoding or vendoring extraction data.

Reference:
https://github.com/DungeonsModding/Useful-things

### Salvage behavior

Vanilla salvage:

- destroys the selected item
- awards emeralds
- returns invested enchantment points
- provides a limited vanilla undo opportunity

Therefore bulk salvage should invoke the game's own salvage operation instead of reproducing reward math ourselves.

## Strong design conclusions

1. **Separate persistence** is safer than altering the hero save.
2. **Native salvage calls** are safer than direct currency/save mutation.
3. A non-destructive overlay is preferable to replacing the entire vanilla inventory widget.
4. Lock protection is not complete until it guards both our mass-salvage flow and the vanilla salvage action.
5. Mass salvage needs a review/confirmation screen because true multi-item undo may not be safely available.
6. `uniqueSaveId` is a promising hero namespace and `inventoryIndex` is a promising fallback item locator, but runtime verification is still required.

## Unknowns to resolve

- exact runtime inventory widget class names on the current build
- exact item instance class/struct
- whether runtime items expose a native stable GUID or stronger ID
- whether runtime items expose the saved `inventoryIndex`
- whether runtime profile data exposes `uniqueSaveId`
- salvage function/event name and ownership
- equip/unequip function/event name and ownership
- how vanilla undo stores its one-item state
- best hook for blocking vanilla salvage of a locked item
- whether the same inventory APIs are used in camp and missions
