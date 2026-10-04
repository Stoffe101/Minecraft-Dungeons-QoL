#include "MCDQoLGameAPI.h"

int32 UInventoryItemSlot::GetChangeIndex() const
{
    return DummyChangeIndex;
}

bool UInventoryItemSlot::AcceptsItem(const UInventoryItem* otherItem) const
{
    return otherItem != nullptr;
}

bool UInventoryItemSlot::CanSwapWith(const UInventoryItemSlot* other) const
{
    return other != nullptr && other != this;
}

bool UInventoryItemSlot::Swap(UInventoryItemSlot* other)
{
    return other != nullptr && other != this;
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
    return 300;
}

bool UItemStashComponent::IsInventoryFull() const
{
    return GetNumItemsInInventory() >= GetMaxInventoryCount();
}

int32 UItemStashComponent::GetNumItemsInInventory() const
{
    return DummyInventorySlots.Num() + DummyEquipmentSlots.Num();
}

int32 UItemStashComponent::InventorySize() const
{
    return DummyInventorySlots.Num();
}

void UItemStashComponent::EnterInventoryUI()
{
}

void UItemStashComponent::ExitInventoryUI()
{
}

bool UItemStashComponent::RemoveItem(UInventoryItemSlot* slot)
{
    return false;
}

FItemSalvageUndoInfo UItemStashComponent::SalvageItemInSlot(UInventoryItemSlot* slot, bool& success)
{
    success = false;
    return FItemSalvageUndoInfo();
}

bool UItemStashComponent::SalvageItemUndo(const FItemSalvageUndoInfo& undoInfo)
{
    return false;
}

int32 UItemStashComponent::GetChangeIndex() const
{
    return DummyChangeIndex;
}

const TArray<UInventoryItemSlot*>& UItemStashComponent::GetInventorySlots() const
{
    return DummyInventorySlots;
}

const TMap<EEquipmentSlot, UInventoryItemSlot*>& UItemStashComponent::GetEquipmentSlots() const
{
    return DummyEquipmentSlots;
}

int32 UItemStashComponent::AvailableEnchantmentPoints() const
{
    return 0;
}

bool UItemStashComponent::InventoryUIRequiresRefresh() const
{
    return true;
}
