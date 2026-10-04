# Installed-game API contracts

Evidence: user `game-evidence-legacy.zip`, SHA-256 `20b5f51b085b3c8b80f285e14896d26e7c677e4b43a82ca917f431e35de31545`, collected 2026-10-04. All 31 targeted packages completed, containing 5,298 legacy properties and 824 Blueprint functions, with zero reported export errors. Raw metadata/Kismet stays under ignored research; this document records only necessary technical findings.

These are observed vanilla Blueprint call shapes, not a complete native SDK, native memory layout, or an in-game test of our emitted graph. Native function flags and unreferenced fields cannot be recovered from imported function names alone. Null/zero RValue pointers in void contexts may display as `#Pointer Error#`; zero export errors does not certify every printed pointer as a native ABI.

## Native call shapes

| Owner / function | Observed inputs | Observed result | Evidence |
| --- | --- | --- | --- |
| BasePlayerController.GetItemStashComponent | none | ItemStashComponent object | Inspector.GetItemStash / player controller |
| ItemStashComponent.GetInventorySlots | none | array of InventoryItemSlot objects | InventoryHUD.DisplayInventory |
| ItemStashComponent.GetEquipmentSlots | none | map from EEquipmentSlot to InventoryItemSlot | HUD map key/value property exports |
| ItemStashComponent.GetSalvageInfo | InventoryItem object | ItemSalvageInfo struct | Inspector.SetSalvageDialogVisibility |
| ItemStashComponent.SalvageItemInSlot | InventoryItemSlot object, separate output success bool | ItemSalvageUndoInfo struct | Inspector.SalvageSlot, statement 303 |
| InventoryItem.CanSalvage | none | bool | Inspector.CanSalavage (vanilla spelling) |
| InventoryItemSlot.CanSwapWith | other InventoryItemSlot | bool | HUD inventory slot flows |
| InventoryItemSlot.GetChangeIndex | none | int | InventoryGenericSlot item-change flows |

The `SalvageItemInSlot` return value is **not success**. The vanilla graph checks the separate success local at statement 393 and then fires `OnItemSalvaged`, preserving undo information. A future batch adapter must preserve that contract and refresh/notify UI; calling the native operation alone is not the whole vanilla transaction. It remains excluded from the diagnostic build.

`GetEquipmentSlots` is a **map**, not an array. Our diagnostic uses the separately verified six-widget HUD array described below; it does not reinterpret a native map as an array.

## Inventory UI access

1. Dynamic-cast the local controller to `/Game/Actors/Characters/Player/BP_PlayerController.BP_PlayerController_C`.
2. Resolve its `SharedUI` object (`BP_PlayerControllerSharedUI_C`).
3. Resolve `SharedUI.InventoryHUD`, declared as UserWidget, and dynamic-cast it to `/Game/UI/Inventory/UMG_InventoryHUD.UMG_InventoryHUD_C`.
4. Read `IsInventoryOpen` (bool). `IsInventoryOpenCall` simply writes that field into its output parameter `Open`; it is not a bool return function.

Widget classes must be imported as `WidgetBlueprintGeneratedClass` from `/Script/UMG`, rather than an actor `BlueprintGeneratedClass` from `/Script/Engine`.

HUD `ExecuteUbergraph` statements 5662/5727 initialize `EquipSlots` with armor, melee, ranged, and the three hotbar/artifact widgets. Each inherits `UMG_InventorySlotBase_C.InventoryItemSlot`; the native slot exposes `InventoryItemSlot.Item`. The diagnostic compares item object identity across all six slots before selection and again before preview. A missing array/widget/native slot is unresolved equipment state and blocks the candidate. This is not a verified persistent identity or loadout resolver.

The open flag alone does not implement modal/controller focus or singleton lifecycle. Those remain production work.

## Corrected diagnostic calls

The previous diagnostic attempted `InventoryItem.GetTotalInvestedEnchantmentPoints` as an instance virtual call. That is not established by the recovered game UI. It now obtains `ItemStashComponent.GetSalvageInfo(Item)` and reads the verified `ItemSalvageInfo.enchantmentPoints` field (used by InventoryItemSalvaged). The fingerprint still groups localized name, power and enchantment refund points; it is a diagnostic group, not a physical-item lock.

Stash lookup now uses the observed controller function instead of assuming a pawn component lookup. Inventory slot retrieval is an imported final native call using its observed object-array result.

## Identity and equip gaps

- `SerializableItemId` / `InventoryItemData.ItemId` participates in item-type comparisons/filtering (`ItemFunctionLibrary.MakeItemId`, `BreakItemId`, `EqualEqual_ItemTypeID`). Its name does not establish unique physical-item identity.
- `InventoryItem.Meta` and `InventoryItemMetaData` exist, but these 31 assets do not expose a persistent item GUID/index or hero `uniqueSaveId` contract.
- `GetChangeIndex` is an item/slot change counter, not proven restart identity.
- `IsLocked` remains a restoration-mirror assumption; it is not observed in this selected UI metadata and cannot substitute for our lock policy.
- The target export does not establish a native equip/swap transaction usable for loadouts.

Required next work: validate the revised diagnostic in the actual game, recover a supported persistent hero/item identity path, author the review/lock/loadout UI, and verify native equip plus vanilla salvage interception. Repeating the same targeted metadata export is unnecessary.

## Alternative tooling check

UE4SS is an optional research route, not an established retail dependency. As of 2026-10-04, its upstream [issue #1219](https://github.com/UE4SS-RE/RE-UE4SS/issues/1219) reports startup access violations on normal Dungeons UE4.22.3 builds and remains open. [Issue #1211](https://github.com/UE4SS-RE/RE-UE4SS/issues/1211) concerns a separately compiled/debug build; its closed status does not validate the user's Microsoft Store executable. Do not substitute generic UE-version support for game-specific compatibility evidence. The existing Blueprint Loader route remains the implemented diagnostic path.


## Feedback attachment field

The same collected InventoryHUD package declares `WholeCanvas` as a native `/Script/UMG.CanvasPanel` and calls `AddChildToCanvas` in its vanilla graph. The inventory-read probe uses that field to parent its own native TextBlock, with native CanvasPanelSlot layout and TextBlock setters. This is an observed attachment point plus engine-documented API shape; the project's widget creation/rendering still requires an in-game test. It does not replace or overwrite any vanilla TextBlock content.
