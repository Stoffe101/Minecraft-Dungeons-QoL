// Minecraft Dungeons QoL editor-only reflection mirror.
// Signatures are adapted from Minecraforever/MCD-PE (Apache-2.0).
// The real gameplay implementations are supplied by Minecraft Dungeons at runtime.

#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "MCDQoLInventoryStubs.generated.h"

UENUM(BlueprintType)
enum class ESlotType : uint8
{
    HealthPotion,
    Arrow,
    BurningArrow,
    FireworksArrow,
    TormentProjectile,
    HeavyHarpoon,
    ThunderingArrow,
    VoidArrow,
    PoisonArrow,
    TNT,
    Trident,
    Conduit,
    ActivePermanent,
    Consumable,
    MeleeWeapon,
    RangedWeapon,
    Armor,
    Any,
    None,
    Last
};

UENUM(BlueprintType)
enum class EEquipmentSlot : uint8
{
    HotbarSlot1,
    HotbarSlot2,
    HotbarSlot3,
    MeleeGear,
    RangedGear,
    ArmorGear,
    Invalid
};

UCLASS(BlueprintType)
class DUNGEONS_API UInventoryItem : public UObject
{
    GENERATED_BODY()
};

USTRUCT(BlueprintType)
struct DUNGEONS_API FSerializableItemId
{
    GENERATED_BODY()

    UPROPERTY(EditDefaultsOnly, Category = "Dungeons")
    FName SerializedId;

    bool operator==(const FSerializableItemId& Other) const
    {
        return SerializedId == Other.SerializedId;
    }

    friend uint32 GetTypeHash(const FSerializableItemId& Value)
    {
        return GetTypeHash(Value.SerializedId);
    }
};

USTRUCT(BlueprintType)
struct DUNGEONS_API FItemSalvageInfo
{
    GENERATED_BODY()

    UPROPERTY(BlueprintReadOnly, Category = "Dungeons")
    TMap<FSerializableItemId, int32> currencies;

    UPROPERTY(BlueprintReadOnly, Category = "Dungeons")
    int32 enchantmentPoints = 0;
};

class UInventoryItemSlot;

USTRUCT(BlueprintType)
struct DUNGEONS_API FItemSalvageUndoInfo
{
    GENERATED_BODY()

    UPROPERTY(BlueprintReadOnly, Category = "Dungeons")
    UInventoryItemSlot* slot = nullptr;

    UPROPERTY(BlueprintReadOnly, Category = "Dungeons")
    UInventoryItem* item = nullptr;

    UPROPERTY(BlueprintReadOnly, Category = "Dungeons")
    FItemSalvageInfo salvageInfo;
};

DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FOnSlotLockedChanged, bool, locked);

UCLASS(BlueprintType)
class DUNGEONS_API UInventoryItemSlot : public UObject
{
    GENERATED_BODY()

public:
    UPROPERTY(BlueprintReadOnly, Category = "Dungeons")
    ESlotType SlotType = ESlotType::Any;

    UPROPERTY(BlueprintReadOnly, Category = "Dungeons")
    UInventoryItem* Item = nullptr;

    UFUNCTION(BlueprintCallable, Category = "Dungeons")
    int32 GetChangeIndex() const;

    UFUNCTION(BlueprintCallable, Category = "Dungeons")
    bool AcceptsItem(const UInventoryItem* OtherItem) const;

    UFUNCTION(BlueprintCallable, Category = "Dungeons")
    bool CanSwapWith(const UInventoryItemSlot* Other) const;

    UFUNCTION(BlueprintCallable, Category = "Dungeons")
    virtual bool Swap(UInventoryItemSlot* Other);

    UFUNCTION(BlueprintCallable, Category = "Dungeons")
    virtual bool IsLocked() const;

    UFUNCTION(BlueprintCallable, Category = "Dungeons")
    virtual void WasSelectedInUI() const;

    UFUNCTION(BlueprintCallable, Category = "Dungeons")
    virtual bool HasSlotChanged() const;

    UFUNCTION(BlueprintCallable, Category = "Dungeons")
    virtual void FinishedSlotChanged();

    UPROPERTY(BlueprintAssignable, Category = "Dungeons")
    FOnSlotLockedChanged OnSlotLockedChanged;
};

UCLASS()
class DUNGEONS_API UInventoryEquipmentItemSlot : public UInventoryItemSlot
{
    GENERATED_BODY()
};

UCLASS(ClassGroup = (Custom), meta = (BlueprintSpawnableComponent))
class DUNGEONS_API UItemStashComponent : public UActorComponent
{
    GENERATED_BODY()

public:
    static constexpr int32 MAX_INVENTORY_SIZE = 300;

    UFUNCTION(BlueprintCallable, Category = "Dungeons")
    static int32 GetMaxInventoryCount();

    UFUNCTION(BlueprintCallable, Category = "Dungeons")
    bool IsInventoryFull() const;

    UFUNCTION(BlueprintCallable, Category = "Dungeons")
    int32 GetNumItemsInInventory() const;

    UFUNCTION(BlueprintCallable, Category = "Dungeons")
    int32 InventorySize() const;

    UFUNCTION(BlueprintCallable, Category = "Dungeons")
    void EnterInventoryUI();

    UFUNCTION(BlueprintCallable, Category = "Dungeons")
    void ExitInventoryUI();

    UFUNCTION(BlueprintCallable, Category = "Dungeons")
    bool RemoveItem(UInventoryItemSlot* Slot);

    UFUNCTION(BlueprintCallable, Category = "Dungeons")
    FItemSalvageUndoInfo SalvageItemInSlot(UInventoryItemSlot* Slot, bool& Success);

    UFUNCTION(BlueprintCallable, Category = "Dungeons")
    bool SalvageItemUndo(const FItemSalvageUndoInfo& UndoInfo);

    UFUNCTION(BlueprintCallable, Category = "Dungeons")
    int32 GetChangeIndex() const;

    UFUNCTION(BlueprintCallable, Category = "Dungeons")
    const TArray<UInventoryItemSlot*>& GetInventorySlots() const;

    UFUNCTION(BlueprintCallable, Category = "Dungeons")
    const TMap<EEquipmentSlot, UInventoryItemSlot*>& GetEquipmentSlots() const;

    UFUNCTION(BlueprintCallable, Category = "Dungeons")
    int32 AvailableEnchantmentPoints() const;

    UFUNCTION(BlueprintCallable, Category = "Dungeons")
    bool InventoryUIRequiresRefresh() const;
};
