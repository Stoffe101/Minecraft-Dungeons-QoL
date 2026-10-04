// Minecraft Dungeons QoL editor-only reflection stubs.
// These bodies are never intended to provide gameplay behavior.
// They exist so the UE4.22 editor module links while authoring/cooking Blueprints.
// At runtime Minecraft Dungeons supplies the real /Script/Dungeons implementations.

#include "MCDQoLInventoryStubs.h"

int32 UInventoryItemSlot::GetChangeIndex() const
{
    return 0;
}

bool UInventoryItemSlot::CanSwapWith(const UInventoryItemSlot* Other) const
{
    return false;
}

bool UInventoryItemSlot::Swap(UInventoryItemSlot* Other)
{
    return false;
}

bool UInventoryItemSlot::IsLocked() const
{
    return false;
}

void UInventoryItemSlot::WasSelectedInUI() const
{
}

bool UInventoryItemSlot::HasSlotChanged() const
{
    return false;
}

void UInventoryItemSlot::FinishedSlotChanged()
{
}

int32 UItemStashComponent::GetMaxInventoryCount()
{
    return MAX_INVENTORY_SIZE;
}

int32 UItemStashComponent::InventorySize() const
{
    return 0;
}

void UItemStashComponent::EnterInventoryUI()
{
}

void UItemStashComponent::ExitInventoryUI()
{
}

int32 UItemStashComponent::GetChangeIndex() const
{
    return 0;
}

const TArray<UInventoryItemSlot*>& UItemStashComponent::GetInventorySlots() const
{
    static const TArray<UInventoryItemSlot*> Empty;
    return Empty;
}

const TMap<EEquipmentSlot, UInventoryItemSlot*>& UItemStashComponent::GetEquipmentSlots() const
{
    static const TMap<EEquipmentSlot, UInventoryItemSlot*> Empty;
    return Empty;
}

int32 UItemStashComponent::AvailableEnchantmentPoints() const
{
    return 0;
}

bool UItemStashComponent::InventoryUIRequiresRefresh() const
{
    return false;
}
