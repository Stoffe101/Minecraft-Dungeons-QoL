# UE4.22 Dungeons Editor SDK Overlay

This directory contains the **canonical editor-only reflection mirror** used to expose the Minecraft Dungeons runtime inventory APIs inside the Dungeons Mod Kit's Unreal Engine 4.22 project.

These files are not replacement gameplay implementations. Minecraft Dungeons supplies the real `/Script/Dungeons` classes/functions at runtime.

## Canonical source

```text
sdk/modkit/Source/Dungeons/
  MCDQoLInventoryStubs.h
  MCDQoLInventoryStubs.cpp
```

`scripts/Sync-GameApi.ps1` copies those files into the pinned Dungeons Mod Kit project.

## Why this exists

Blueprint nodes can only reference native types/functions the editor knows about.

Minecraft Dungeons contains classes such as:

- `UItemStashComponent`
- `UInventoryItemSlot`

but the public Mod Kit exposes only a small subset of Dungeons source declarations. The mirror provides the verified reflection surface required by this mod.

The editor C++ bodies are intentionally harmless. Their purpose is to let Unreal Header Tool and UE4.22 build reflection metadata while authoring/cooking.

## Reflected API

The current mirror includes the non-destructive inventory API plus the exact reflected salvage result structures needed by the native salvage function.

Highlights:

- `GetInventorySlots()`
- `GetEquipmentSlots()`
- `GetChangeIndex()`
- `CanSwapWith(...)`
- `Swap(...)`
- `IsLocked()`
- `SalvageItemInSlot(...)`
- `SalvageItemUndo(...)`
- salvage currency/enchantment-point result data

The first playable build still **must not invoke salvage** until inventory enumeration/selection/protection has been verified in-game.

## Sync the mirror

```powershell
./scripts/Sync-GameApi.ps1
```

## Rebuild the editor module

Adding/changing reflected C++ declarations requires rebuilding the Mod Kit's Dungeons editor module:

```powershell
./scripts/Build-ModKit-Editor.ps1
```

UE4.22 supports Visual Studio 2017 or 2019. Install the C++ workload and a compatible Windows SDK.

## Build the mod

`scripts/Build.ps1` syncs the mirror, rebuilds the editor module, syncs project assets, cooks them, and packages the final pak.

## Licensing

The Dungeons declarations are adapted from:

https://github.com/Minecraforever/MCD-PE

MCD-PE is Apache-2.0 licensed. See `docs/THIRD_PARTY.md`.
