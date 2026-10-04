# UE4.22 Dungeons Editor SDK Overlay

This directory contains **editor-only reflection stubs** used to expose the Minecraft Dungeons runtime inventory APIs inside the Dungeons Mod Kit's Unreal Engine 4.22 project.

They are not replacement implementations of Minecraft Dungeons gameplay code.

## Why this exists

A Blueprint can only create nodes for native classes/functions the editor knows about.

Minecraft Dungeons itself contains classes such as:

- `UItemStashComponent`
- `UInventoryItemSlot`

but the public Dungeons Mod Kit does not expose all of those declarations in its small source tree.

The overlay declares the verified class/function signatures and provides harmless dummy C++ bodies so the **editor module can link**.

When the cooked Blueprint runs inside Minecraft Dungeons, the imported `/Script/Dungeons` class/function names resolve against the game's real runtime module.

## Current exposed API

The first safe/non-destructive slice exposes:

- `UItemStashComponent::GetInventorySlots`
- `UItemStashComponent::GetEquipmentSlots`
- `UItemStashComponent::GetChangeIndex`
- `UItemStashComponent::InventorySize`
- `UItemStashComponent::AvailableEnchantmentPoints`
- `UItemStashComponent::EnterInventoryUI`
- `UItemStashComponent::ExitInventoryUI`
- `UInventoryItemSlot::GetChangeIndex`
- `UInventoryItemSlot::CanSwapWith`
- `UInventoryItemSlot::Swap`
- `UInventoryItemSlot::IsLocked`
- slot UI/change helpers

Native salvage is intentionally **not exposed in this first stub pass** because its return structs contain additional native types. We will add the exact reflected struct declarations before enabling destructive behavior.

## Apply

```powershell
./scripts/Apply-ModKit-SDK.ps1
```

## Licensing

The class/function declarations are adapted from:

https://github.com/Minecraforever/MCD-PE

MCD-PE is Apache-2.0 licensed. See `docs/THIRD_PARTY.md`.
