// Minecraft Dungeons QoL editor-only reflection stubs.
// Class/function signatures are adapted from Minecraforever/MCD-PE (Apache-2.0)
// and are used only so UE4.22 can author Blueprints that reference Dungeons runtime APIs.
// The real implementations are supplied by Minecraft Dungeons at runtime.

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
class DUNGEONS_API UInventoryItemSlot : public UObject
{
    GENERATED_BODY()

public:
    UFUNCTION(BlueprintCallable, Category = "Dungeons")
    int32 GetChangeIndex() const;

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
    int32 InventorySize() const;

    UFUNCTION(BlueprintCallable, Category = "Dungeons")
    void EnterInventoryUI();

    UFUNCTION(BlueprintCallable, Category = "Dungeons")
    void ExitInventoryUI();

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
