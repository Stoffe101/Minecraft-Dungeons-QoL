# Favorites and batch salvage implementation

## Required behavior

The user requires individual favorites to block every salvage entry point until explicitly unfavorited. Favoriting an item must remove it from any pending batch. Individual selection and Select All must exclude favorites and equipped items. A confirmed batch must recheck every slot and item before each native salvage call; stale or changed entries must never be salvaged. Selection is by physical item reference, not display name or power, so two identical-looking items remain independent.

## Evidence and implementation boundary

The user confirmed that the current inventory probe opens without crashing, displays item names and power, and responds to F6/F7. Those keys move our independent cursor; they do not yet follow the highlighted vanilla inventory item. No favorites or destructive salvage feature is enabled by this pass.

The supplied metadata contains `Dungeons/Content/UI/Inventory/Inspector2/UMG_InventoryItemInspector.uasset`, but no original cooked package bytes. Its `SalvageSlot` function calls `CanSalavage` at bytecode offset 35, checks the combined slot/salvage predicate at 96, obtains the item at 216, and calls native `ItemStashComponent.SalvageItemInSlot` at 303. The native return is an undo-info struct with a separate success out parameter. On success the vanilla graph emits `OnItemSalvaged` and updates feedback. Its `CanSalavage` predicate includes valid inspected item, mission salvage permission, and `InventoryItem.CanSalvage()`.

These observations identify a patch boundary, not a completed guard. The guard must cover the actual slot item immediately before mutation as well as the UI eligibility predicate. Merely disabling the button on Tick cannot satisfy the protection requirement. Original cooked bytes are required to preserve widget exports, field contracts, function serialization, jump targets, delegates and undo behavior when generating a local patch. The decoded JSON cannot faithfully reconstruct the widget package.

Game-owned originals and generated replacement widgets must stay private and out of this repository and public release artifacts. The intended distribution is project-owned patching code applied to the player's own packages. Persistent per-item favorites also require a proven stable identity; item type IDs and name/power fingerprints are insufficient. Online host and joining-client execution must both be verified before declaring this feature supported.

## Collect the missing packages

After updating the repository, run this in its PowerShell terminal. Supply the same AES key used for the previous successful legacy evidence collection when prompted; the key is not written into the report.

```powershell
git pull
$inventoryEvidenceKey = Read-Host 'AES key used for the previous evidence collection'
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Collect-LegacyGameEvidence.ps1 -PaksPath 'C:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks' -AesKey $inventoryEvidenceKey -CollectInventoryPatchSources
```

Upload the resulting `.research/inventory-patch-sources-<timestamp>.zip`, whose full path the script prints. This collection reads game archives without modifying them. It includes metadata and precisely these cooked UI packages, plus available `.uexp` companions:

- `UI/Inventory/UMG_InventoryHUD`
- `UI/Inventory/UMG_InventorySlotBase`
- `UI/Inventory/Inspector2/UMG_InventoryItemInspector`
- `UI/Inventory/Inspector2/UMG_InventoryItemInspectInfo`
- `UI/Inventory/Salvage/UMG_SalvageButtonConfirm`
- `UI/Inventory/Salvage/UMG_SalvageButtonToggle`
- `UI/Inventory/Salvage/UMG_SalvageUndoButton`

No saves, executables, whole archives, bulk data, or unrelated UI packages are collected. The manifest records file paths, sizes and SHA-256 hashes. Missing required packages produce an unsuccessful result with retained diagnostic logs. The existing collector remains metadata-only unless this switch is supplied; arbitrary `-AssetMatch` is forbidden in patch-source mode.

## Validation for this pass

Direct compilation against the pinned dependencies passed. A local fixture pak containing seven allowlisted paths and one unrelated path exported precisely seven packages and fourteen companions; all bytes, sizes and SHA-256 hashes matched the source fixture. The fixture used the licensed LetMeMove actor under synthetic UI paths, so this proves extraction behavior, not Minecraft inventory UI compatibility. A negative fixture lacking the UI packages produced no cooked sources and identified all seven missing packages. Repository validation and PowerShell syntax checks passed. Windows CI additionally checks that default collection stays metadata-only, patch-source mode rejects unrelated assets, and source archives remain unchanged.

The next implementation steps are to inspect the actual packages, implement a shared item-identity protection predicate, patch both vanilla eligibility and mutation guards, then add favorites, guarded selection/Select All, preview and confirmed batch execution through the audited vanilla/native path. Runtime verification must include duplicate items, favoriting a queued item, ordinary salvage refusal, stale selections, equipped items, undo/notifications, inventory reopen, travel and online host/join sessions.
