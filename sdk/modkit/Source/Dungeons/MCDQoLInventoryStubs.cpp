// Minecraft Dungeons QoL editor-only reflection mirror.
// These bodies are harmless editor stubs. Minecraft Dungeons supplies the
// real /Script/Dungeons implementations when the cooked Blueprint runs.

#include "MCDQoLInventoryStubs.h"

bool UInventoryItem::CanSalvage() const { return false; }
FText UInventoryItem::GetDisplayNameText() const { return FText::GetEmpty(); }
int32 UInventoryItem::GetDisplayItemPowerInt() const { return 0; }
FItemSalvageInfo UItemStashComponent::GetSalvageInfo(UInventoryItem* Item) const { return FItemSalvageInfo(); }

int32 UInventoryItemSlot::GetChangeIndex() const { return 0; }
bool UInventoryItemSlot::AcceptsItem(const UInventoryItem* OtherItem) const { return OtherItem != nullptr; }
bool UInventoryItemSlot::CanSwapWith(const UInventoryItemSlot* Other) const { return Other != nullptr && Other != this; }
bool UInventoryItemSlot::Swap(UInventoryItemSlot* Other) { return false; }
bool UInventoryItemSlot::IsLocked() const { return false; }
void UInventoryItemSlot::WasSelectedInUI() const {}
bool UInventoryItemSlot::HasSlotChanged() const { return false; }
void UInventoryItemSlot::FinishedSlotChanged() {}

int32 UItemStashComponent::GetMaxInventoryCount() { return MAX_INVENTORY_SIZE; }
bool UItemStashComponent::IsInventoryFull() const { return false; }
int32 UItemStashComponent::GetNumItemsInInventory() const { return 0; }
int32 UItemStashComponent::InventorySize() const { return 0; }
void UItemStashComponent::EnterInventoryUI() {}
void UItemStashComponent::ExitInventoryUI() {}
bool UItemStashComponent::RemoveItem(UInventoryItemSlot* Slot) { return false; }

FItemSalvageUndoInfo UItemStashComponent::SalvageItemInSlot(UInventoryItemSlot* Slot, bool& Success)
{
    Success = false;
    return FItemSalvageUndoInfo();
}

bool UItemStashComponent::SalvageItemUndo(const FItemSalvageUndoInfo& UndoInfo) { return false; }
int32 UItemStashComponent::GetChangeIndex() const { return 0; }

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

int32 UItemStashComponent::AvailableEnchantmentPoints() const { return 0; }
bool UItemStashComponent::InventoryUIRequiresRefresh() const { return false; }
