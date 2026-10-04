# Favorites and batch salvage implementation

## Current private test increment (2026-10-05)

The seven-package source ZIP has been received and all fourteen manifest hashes verified (ZIP SHA-256 `1e7b7a4595afb4d3e2ba8869f81ab3b07d68ad32990c90c36c2d2c3aa0ea9d82`). The user confirms keyboard/mouse only and a hosted successful read-only test, wants joining players supported, and intends personal use.

`CookedInventoryFeatures` now patches the original HUD and inspector locally. The HUD's own Tick drives the controls using its owning local player; it has no GameMode/Blueprint Loader dependency. F5 toggles favorites on the normally selected item, F8 toggles queue membership, F9 selects all eligible stash items, F10 reviews, and Escape clears/cancels. Favoriting removes pending selection. Favorites are physical object references held by the inspector, distinct even for identical-looking items. Synchronous guards precede vanilla `CanSalavage` and `SalvageSlot`; the salvage button follows guarded eligibility. Closing inventory clears the batch and confirmation through the real `OpenCloseInventory` function, even if hidden Slate widgets no longer tick.

The optional native batch path rechecks slot/item identity, current inventory membership, favorites, `InventoryItem.CanSalvage`, and all six equipment widgets before each item. Missing equipment state excludes the candidate. Its inspector helper also checks the owning local player, membership and exact expected item, invokes the original mission/item eligibility check, then calls `SalvageItemInSlot` with a typed undo-info local and a separate bool out parameter. Only successful calls broadcast the original `OnItemSalvaged` delegate. That delegate already sets the undo info, refreshes slots/inspector, purges the salvaged item and clears selection, so filtered-out items do not need visible slot widgets. Confirmed batches process one item per UI Tick and Escape stops remaining items. Vanilla undo restores the last successful item only, not the whole batch.

**Default output is a selection/guard test with native batch deletion disabled.** The source implements native execution behind `--enable-salvage` / `-EnableNativeSalvage`, but no game run has verified the new UI patch or native batch. First verify the default guard test on expendable items; do not present parser/build success as game compatibility. Favorites currently persist only while that inspector widget remains alive: inventory reopen may retain them, but restart/travel/rejoin persistence is not implemented. This does not yet meet the full persistent-lock requirement. Loadout management also remains future work. Joining-client execution is structurally independent of the host loader, but still needs an actual joining-player test.

Both default and native modes compiled, generated and reopened successfully on the supplied packages. Validators check root jump boundaries, favorite early-return guards, reference argument types and native bool-out/undo notification contracts. Preservation checks compare all 11,152 original exports, properties and widget data, and every original function body. Only HUD Tick/OpenCloseInventory and inspector CanSalavage/SalvageSlot gain prefixes; their original bodies remain byte-identical after jump/switch relocation. Other 232 functions are unchanged. Original dependency graphs are retained with only new references/children appended, rather than forcing every widget function to preload every import.

Build locally from your extracted collection (requires Python and .NET 8, or the collector's SDK):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-InventoryFeatures.ps1 -SourcesDirectory '.research\inventory-patch-sources-20261004-231945\Metadata\PatchSources\Dungeons\Content'
```

This creates a fresh private output directory and a `zzz_MinecraftDungeonsQoL-InventoryFeaturesTest_P.pak`. It does not install the pak, modify saves, or commit game assets. The output manifest records source commit, original hashes, mode and pak hash. Generated original-derived widgets stay out of public GitHub artifacts. Installation/test instructions accompany the private test download.

## Required behavior

The user requires individual favorites to block every salvage entry point until explicitly unfavorited. Favoriting an item must remove it from any pending batch. Individual selection and Select All must exclude favorites and equipped items. A confirmed batch must recheck every slot and item before each native salvage call; stale or changed entries must never be salvaged. Selection is by physical item reference, not display name or power, so two identical-looking items remain independent.

## Evidence and implementation boundary

The user confirmed that the current inventory probe opens without crashing, displays item names and power, and responds to F6/F7. Those keys move our independent cursor; they do not yet follow the highlighted vanilla inventory item. No favorites or destructive salvage feature is enabled by this pass.

Before the source upload, the supplied metadata contained `Dungeons/Content/UI/Inventory/Inspector2/UMG_InventoryItemInspector.uasset`, but no original cooked package bytes. Its `SalvageSlot` function calls `CanSalavage` at bytecode offset 35, checks the combined slot/salvage predicate at 96, obtains the item at 216, and calls native `ItemStashComponent.SalvageItemInSlot` at 303. The native return is an undo-info struct with a separate success out parameter. On success the vanilla graph emits `OnItemSalvaged` and updates feedback. Its `CanSalavage` predicate includes valid inspected item, mission salvage permission, and `InventoryItem.CanSalvage()`.

These observations identified the patch boundary used by the current generator. The guard must cover the actual slot item immediately before mutation as well as the UI eligibility predicate. Merely disabling the button on Tick cannot satisfy the protection requirement. Original cooked bytes are required to preserve widget exports, field contracts, function serialization, jump targets, delegates and undo behavior when generating a local patch. The decoded JSON cannot faithfully reconstruct the widget package.

Game-owned originals and generated replacement widgets must stay private and out of this repository and public release artifacts. The intended distribution is project-owned patching code applied to the player's own packages. Persistent per-item favorites also require a proven stable identity; item type IDs and name/power fingerprints are insufficient. Online host and joining-client execution must both be verified before declaring this feature supported.

## Collect the missing packages

After updating the repository, run this in its PowerShell terminal. The AES key is the public game-archive decryption key already documented in GAME_EVIDENCE.md and verified by the previous collection on this installation. It is not a password you need to create or look up.

```powershell
git pull
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Collect-LegacyGameEvidence.ps1 -PaksPath 'C:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks' -AesKey '0x7D5F892ECEBFA53CC22001DF48B871D51C0DF7C54CE41933BFB285219829B3A8' -CollectInventoryPatchSources
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

The next steps are to runtime-verify the default guard/selection patch, verify the native batch path, establish persistent item identity and test online host/join execution. Runtime verification must include duplicate items, favoriting a queued item, ordinary salvage refusal, stale selections, equipped items, undo/notifications, inventory reopen, travel and online host/join sessions.
