#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "MCDQoLGameAPI.generated.h"

/*
 * Editor-only reflection mirror for a minimal subset of Minecraft Dungeons.
 *
 * These declarations exist so UE4.22 can author/cook Blueprints that reference
 * /Script/Dungeons types present in the shipping game. The C++ implementations
 * are harmless editor stubs. Runtime behavior is provided by Minecraft Dungeons.
 *
 * Keep this file minimal. Add reflected members only after they are verified.
 */

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
struct DUNGEONS_API FItemSalvageInfo
{
    GENERATED_BODY()

    // Verified reflected property. Currency-map support is intentionally omitted
    // from the first mirror because the MVP does not inspect reward-map entries.
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
    bool AcceptsItem(const UInventoryItem* otherItem) const;

    UFUNCTION(BlueprintCallable, Category = "Dungeons")
    bool CanSwapWith(const UInventoryItemSlot* other) const;

    UFUNCTION(BlueprintCallable, Category = "Dungeons")
    virtual bool Swap(UInventoryItemSlot* other);

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

private:
    int32 DummyChangeIndex = 0;
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
    bool RemoveItem(UInventoryItemSlot* slot);

    UFUNCTION(BlueprintCallable, Category = "Dungeons")
    FItemSalvageUndoInfo SalvageItemInSlot(UInventoryItemSlot* slot, bool& success);

    UFUNCTION(BlueprintCallable, Category = "Dungeons")
    bool SalvageItemUndo(const FItemSalvageUndoInfo& undoInfo);

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

private:
    UPROPERTY()
    TArray<UInventoryItemSlot*> DummyInventorySlots;

    UPROPERTY()
    TMap<EEquipmentSlot, UInventoryItemSlot*> DummyEquipmentSlots;

    int32 DummyChangeIndex = 0;
};
