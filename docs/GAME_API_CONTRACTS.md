# Installed-game API contracts

## Completed serializer declaration scope — 2026-10-06

CharacterSerializeComponent directly inherits Engine.ActorComponent. Its two properties are mDefaultSaveData/mSaveData, both CharacterSaveData objects. Ten functions: SetSkin(Name), SetName(String), SetLegendaryStatus(Enum), SetIsTowerLevel(), ReadSkin()->Name, ReadName()->String, HasProfile()->Bool, GetLegendaryStatus()->Enum, GetCloudPlayerId()->Guid, AssignCharacter(CharacterSaveData). GetCloudPlayerId return property flags are `0018001040000782`; this does not establish lifetime/clone semantics. There is no reflected record-returning serializer in this declaration. BaseCharacter also adds no reflected save identity. EquipmentComponent's EquipmentSlots/GetSlotsOfType use ItemSlot, distinct from InventoryItemSlot; do not substitute these object types.

The optional native code collector inspects native flags/count/parameter sizes and source-derived function-pointer layouts, accepting a unique variant only when seven known anchors agree and pointers fall in read-only executable main-image sections. This is evidence collection, not a callable native bridge. Raw snippets/RVAs remain private and are never embedded as guessed production offsets.


## Native declaration confirmation — 2026-10-06

The completed private v5 report matches all six profile caller shapes below plus ItemStashComponent.SalvageItemInSlot(slot, out-success) returning ItemSalvageUndoInfo. GetCharacterSlotByIndex's bool parameter is named `forceRefreshSlot`; that name does not establish refresh/lifetime semantics. SerializeSaveState has no parameters or return. GetInventorySlots/GetStorageChestSlots return arrays of InventoryItemSlot; return property flags are `0010000008000782`. GetEquipmentSlots returns a map whose inner types this reader does not export. Save slot/controller reference CharacterSerializeComponent; save slot inherits BaseCharacter.

This collector does not export FunctionFlags, property offsets, structure sizes or complete enum/map typing. Parameter flags and matching call shapes do not certify complete C++ ABI, const/static/BlueprintPure specifiers, GUID lifetime, clone identity or physical-item identity. Editor stubs remain provisional for these details. Expanded serialization declarations are the next gate.


## Profile call sites — 2026-10-06

Private exact profile evidence SHA-256 `056d6178ddd194d421f05f926dacf01201416af9d2c467b86dec635d12d7264b`: 26/26 packages completed, zero reported errors. All owners below are `/Script/Dungeons` classes. These are caller layouts, not complete native declarations/flags or runtime lifetime guarantees.

| Reflected declaring owner / function | Observed inputs | Typed result |
|---|---|---|
| PlayerCharacterSaveSlot.GetCloudPlayerId | none | FGuid (`/Script/CoreUObject.Guid`) |
| PlayerControllerBase.GetRecentSaveDataIndex | none | int32 |
| PlayerControllerBase.GetNumProfiles | none | int32 |
| PlayerControllerBase.GetSaveLocalUserNum | none | int32 |
| PlayerControllerBase.GetAvailableSaveDataByIndex | int32 index | CharacterSaveData object |
| PlayerControllerBase.GetCharacterSlotByIndex | int32 index, bool | PlayerCharacterSaveSlot object |

Legacy UAssetAPI `UIntProperty` is IntProperty/int32 here; its class name does not mean uint32. Cloud picker TrySelectCurrentSave statements 53/103/155/205/480/552 establish local-index → slot(index, False) → GUID → cloud row GUID comparison. The bool's native meaning is unresolved. CharacterPicker call anchors and return properties are recorded in FAVORITES_PERSISTENCE_INVESTIGATION.md.

HeroProfileCallContracts rejects wrong argument count, non-int index, non-bool second argument, wrong/missing typed result and bare final-function calls. FunctionImportContracts rejects wrong declaring class/script package. No new getter call is emitted by this pass. Cloud GUID is not yet a certified durable hero key, and no physical-item ID was found. CloneCharacter/CreateNewCharacter/DeleteCharacterByIndex/AssignSaveData/SaveGlobalState are mutation APIs, not getters.

Persistence evidence (2026-10-05): storage chest ExecuteUbergraph statement 1370 calls /Script/Dungeons.ItemStashComponent.SerializeSaveState with zero parameters and no assigned return. It is a save writer, not an FString/JSON getter. The new 88-package collection imports user-manager/controller APIs but establishes no stable hero/item-instance identifier. See FAVORITES_PERSISTENCE_INVESTIGATION.md.

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
4. Call native `/Script/UMG.Widget.IsVisible` on the resolved InventoryHUD, matching the game's own UIToggleInventory check. The older `IsInventoryOpen` bool/`IsInventoryOpenCall` getter exists in metadata, but the collected HUD graph shows no writer; field presence alone does not establish its runtime open-state semantics.

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

The collected SetAnchors bytecode imports `Anchors` from **`/Script/Slate`**, not `/Script/UMG`; Vector2D comes from `/Script/CoreUObject`. The probe follows those observed owners, and a negative test rejects an Anchors import under the wrong module. Native struct module names must be verified independently of the UMG class consuming them.


## Offline inventory lifecycle

The user's collected BP_PlayerController.UIToggleInventory bytecode first toggles SharedUI.InventoryHUD, reads the resulting IsVisible state, checks IsGamePaused and OnlineUtil.IsOnlineSession, then calls GameplayStatics.SetGamePaused(Self,true) at statement 909 when inventory is visible, the game is not already paused and the session is offline. This makes pause-capable ticking a required lifecycle contract for an actor that only reads inventory while its UI is open.

The manager's own PrimaryActorTick must explicitly enable bCanEverTick, bStartWithTickEnabled and bTickEvenWhenPaused, with TickInterval=0. This does not require enabling or changing game input settings, changing the player controller's tick flags, or unpausing the world. Actor tick validation is necessary but does not prove that the retail controller updates WasInputKeyJustPressed while inventory is open; confirm text first, then F6/F7 responses in-game.


The executable gate now uses native Widget.IsVisible with zero parameters on the resolved MCDQoL_InventoryHUD local. This matches the collected controller's before/after ToggleWidget checks; it does not assume the otherwise unwritten IsInventoryOpen field is updated by the game. Shared validation verifies the receiver plus the function's Widget owner and /Script/UMG module. Wrong receivers/owners are rejected alongside missing/bypassed guards.

## Local inventory probe contracts after the online crash

Require /Script/Engine.Controller.IsLocalPlayerController on the resolved player controller before reads/input/widget creation; a server may have remote controllers. The manager CDO must serialize bReplicates=false. Do not infer local ownership from player index alone.

Native UI calls must be imported from /Script/UMG with these reflected owners: AddChildToCanvas → CanvasPanel; SetText → TextBlock; SetVisibility/RemoveFromParent → Widget; SetAnchors/SetAlignment/SetPosition/SetSize/SetZOrder → CanvasPanelSlot. Each setter takes one argument; RemoveFromParent takes none. Optional SetJustification/SetAutoWrapText are forbidden in the probe until a retail reflected contract is established.

Native InventoryItem.GetDisplayNameText is present in supplied inventory-inspect metadata; GetDisplayItemPowerInt is present in HUD metadata. Emit explicit FinalFunction imports. Metadata supports ownership/signature; it does not prove that every runtime slot is valid.

## UE4.22 native reference argument ABI

Do not nest native text/string-producing calls in const-reference parameters. Materialize results as UTextProperty/UStrProperty function locals, set the EX_Context RValuePointer to the assigned property, and pass the local afterward. SetText and Conv_TextToString require FText; Conv_StringToText and string concatenation/building require FString. StepCompiledInRef may reuse an inner call's MostRecentPropertyAddress, which can point to the wrong type. See INVENTORY_CRASH_INVESTIGATION.md for version-pinned source evidence. This applies to both generators, not just the overlay.
