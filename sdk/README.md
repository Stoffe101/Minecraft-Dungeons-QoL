# UE4.22 Dungeons Editor SDK Overlay

This directory contains **editor-only reflection stubs** used to expose verified Minecraft Dungeons runtime inventory APIs inside the Dungeons Mod Kit UE4.22 project.

They are not replacement gameplay implementations.

## Why this exists

Blueprint can only create nodes for native classes/functions the editor knows about.

Minecraft Dungeons contains reflected types such as:

- `UItemStashComponent`
- `UInventoryItemSlot`
- `FItemSalvageInfo`
- `FItemSalvageUndoInfo`

but the public Dungeons Mod Kit does not ship their full declarations.

The mirror declares the subset needed by this project and provides harmless C++ bodies so the **editor module can link**.

A cooked Blueprint references paths under:

```text
/Script/Dungeons
```

When it runs inside Minecraft Dungeons, those references resolve against the game's real native module.

## Canonical source

```text
sdk/modkit/Source/Dungeons/MCDQoLInventoryStubs.h
sdk/modkit/Source/Dungeons/MCDQoLInventoryStubs.cpp
```

Do not create a second set of classes with the same reflected names.

## Current mirrored API

### UInventoryItemSlot

- `Item`
- `SlotType`
- `GetChangeIndex`
- `AcceptsItem`
- `CanSwapWith`
- `Swap`
- `IsLocked`
- `WasSelectedInUI`
- `HasSlotChanged`
- `FinishedSlotChanged`
- `OnSlotLockedChanged`

### UItemStashComponent

- `GetMaxInventoryCount`
- `IsInventoryFull`
- `GetNumItemsInInventory`
- `InventorySize`
- `EnterInventoryUI`
- `ExitInventoryUI`
- `RemoveItem`
- `SalvageItemInSlot`
- `SalvageItemUndo`
- `GetChangeIndex`
- `GetInventorySlots`
- `GetEquipmentSlots`
- `AvailableEnchantmentPoints`
- `InventoryUIRequiresRefresh`

### Supporting reflected types

- `ESlotType`
- `EEquipmentSlot`
- minimal `UInventoryItem` reflection shell
- `FSerializableItemId`
- `FItemSalvageInfo`
- `FItemSalvageUndoInfo`

## Important mirror rule

Only add a reflected type/member after its name/signature has evidence from the Dungeons runtime/source-restoration research.

The C++ bodies intentionally do not emulate the game. A call to the stub inside the editor returns harmless defaults.

## Sync into Mod Kit

```powershell
./scripts/Sync-GameApi.ps1
```

Bootstrap and Build invoke this automatically.

## Current validation state

The declarations are based on final-build research but still need to pass **UE4.22 UHT/editor compilation** on a machine with Unreal Engine 4.22.

Destructive runtime behavior is not considered validated until the cooked Blueprint has been tested in Minecraft Dungeons with disposable gear.

## Licensing

Declarations are adapted from:

https://github.com/Minecraforever/MCD-PE

MCD-PE is Apache-2.0 licensed. See `sdk/NOTICE.md` and `docs/THIRD_PARTY.md`.

## Installed-game call-shape updates (2026-10-04)

The editor mirror now includes InventoryItem CanSalvage/GetDisplayNameText/GetDisplayItemPowerInt and ItemStashComponent GetSalvageInfo(Item) -> ItemSalvageInfo, observed in the user’s UI assets. These are harmless project-authored editor bodies, not game implementations. Native flag/const/ABI certification and a real UE4.22 editor compile remain pending. See [GAME_API_CONTRACTS.md](../docs/GAME_API_CONTRACTS.md). No unverified physical-item ID was invented.
