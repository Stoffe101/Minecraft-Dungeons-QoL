# Current State

Last updated: 2026-10-04

## Repository state

The repository now has:

- reproducible Dungeons Mod Kit setup
- environment detection for Unreal Engine 4.22 and Minecraft Dungeons
- build/install scripts
- Minecraft Launcher and Xbox app / Microsoft Store install support
- player-facing pak installation instructions
- architecture for locking, mass salvage, persistence, and loadouts
- item identity research
- final-build native inventory/salvage API research
- comprehensive Dungeons 1 modding/toolchain research
- UAssetAPI compatibility probes against real working Dungeons Blueprints
- documentation and test structure

## Functional status

No user-tested release pak exists yet.

The project is no longer blocked on understanding how Dungeons inventory/salvage works.

The remaining work is primarily **authoring and packaging the actual runtime Blueprints/UI**, then validating them in the real game.

## Chosen production approach

The canonical implementation route is now:

1. Unreal Engine 4.22.x
2. Dokucraft Dungeons Mod Kit
3. editor-facing Dungeons class/function stubs
4. project-owned manager actor and widget Blueprints
5. Blueprint Loader levels under Lobby and Ingame
6. normal UE4 cook
7. normal Mod Kit/u4pak packaging
8. install to the active game's `Paks\~mods`

UAssetAPI is a supporting inspection/validation/precooked-modification tool.

KismetKompiler is experimental and is no longer allowed to block the release path.

See `MODDING_RESEARCH.md`.

## Proven reusable patterns

Camera Coordinates Overlay demonstrates the exact UI bootstrap pattern needed:

`Blueprint Loader level -> actor -> create widget -> add to viewport`

Its published Nexus permissions allow modification and asset reuse.

LetMeMove is MIT licensed and provides a modern known-working Dungeons 1 actor/loader reference.

## Verified native inventory surface

`UInventoryItemSlot` and `UItemStashComponent` expose the core operations needed, including:

```text
GetInventorySlots()
GetEquipmentSlots()
GetChangeIndex()
CanSwapWith(...)
Swap(...)
IsLocked()
EnterInventoryUI()
ExitInventoryUI()
SalvageItemInSlot(...)
SalvageItemUndo(...)
GetSalvageInfo(...)
CompareItemPowerWithEquipped(...)
AvailableEnchantmentPoints()
```

Mass salvage therefore uses the native Dungeons transaction rather than recreating reward math.

## Immediate implementation target

The next build milestone is deliberately non-destructive:

1. add minimal verified Dungeons inventory class/function stubs to the Mod Kit editor overlay
2. create the project-owned manager actor
3. create the project-owned overlay widget
4. load it in Camp and missions
5. find the local player's `UItemStashComponent`
6. enumerate inventory/equipment slots
7. display diagnostic counts/state
8. implement selection and in-session lock state without destroying anything

Only after that works in-game do we enable `SalvageItemInSlot`.

## Identity state

Persistent item identity still needs runtime verification.

Fallback research supports:

`hero uniqueSaveId + inventoryIndex + sanity fingerprint`

but storage transfers and index reuse mean ambiguity must fail closed.

## Known external requirements

- Windows
- Minecraft Dungeons 1
- Blueprint Loader
- Unreal Engine 4.22.x for the standard authoring route
- Dungeons Mod Kit

## Safety policy

The project must not directly edit hero saves or manually grant salvage rewards.

Every destructive operation must be revalidated and sent through the native Dungeons salvage path.
