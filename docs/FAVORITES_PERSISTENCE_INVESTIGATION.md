# Favorites persistence investigation

## 2026-10-06: bounded capture exhausted; traversal and failure diagnostics improved

Inspected private native-favorites-20261006-132748-c19746.zip, SHA-256 `c8171da5ed80eb1cc728e3314d792fe89e8b3191771e485772aea6f0a5fd0679`. REPORT.json identifies `legacy-objects-capacity-v3`, Completed=false, no declarations and `Read/time budget exceeded; capture is incomplete.` The old message contains no stage/counter/limit cause. This report therefore does not prove object discovery succeeded, which limit was reached, or any native identity/layout contract. Do not request another unchanged v3 run or increase the limits to hide the failure.

Revision `legacy-bounded-traversal-v4` reduces repeated reads: adjacent name-table counters are read together; UObject name index/number are read together; traversal reads index/class together from a bounded 32-byte header; class-name lookups and used object-chunk pointers are cached for one collection. Both caches reset between object candidates and before accepted traversal. Class names are refreshed for the final seven-contract check, and every used chunk pointer is reread and compared before declarations are accepted. No bulk process/object dump or permanent instance values are exported. The discovery loop no longer allocates tiny candidate arrays for every image word. Existing source-defined capacities, unique matches, object-index/owner/chain gates and seven retail contract checks remain.

Reports now include only bounded diagnostic scalars: constant stage label, attempted read-call count, requested bytes and elapsed milliseconds. Budget exceptions distinguish calls/bytes/time. The two-million-call, 256 MiB requested-byte, 90-second and one-MiB single-read limits are unchanged. These counters are diagnostics, not evidence of native API or physical-item identity.

Added a 2,048-instance fixture that retains exactly eleven allowlisted declarations and adds at most three reads per instance, a cache-change rejection before final acceptance, stage/call-budget diagnostics and separate byte/time rejection checks. All 102 local reader checks pass; Windows adds an own-process read check. Fixtures do not prove retail performance or completion. No gameplay pak or persistence release is produced. Next gate is one capture from v4; keep the working v8 pak and UE4SS disabled. Durable favorites still require validated identity/lifecycle access and mission/restart/duplicate/hero/storage/online acceptance.


## 2026-10-06: name table validated; object capacity filter corrected

Inspected private native-favorites-20261006-132111-71d697.zip, SHA-256 `4fe92a78974593228c4a978013ce49653002ccb99aa7a57e6593218a6ad0079e`. REPORT.json identifies revision `legacy-names-256-v2`, Completed=false, zero declarations and `Legacy object array not uniquely validated (0 matches from 0 candidates); no declarations accepted.` Reaching this error means exactly one name table passed the three anchor-name checks. It does not establish the selected table capacity/string offset, native declaration layout, actual object capacity or permanent item identity. No access denial was reported.

Pinned Epic-authored Unreal 4.22 UObjectArray.h defines TUObjectArray as FChunkedFixedUObjectArray. Its PreAllocate calculates MaxChunks = InMaxElements / 65536 + 1 and MaxElements = MaxChunks * 65536; optionally it allocates all chunks before live objects fill them. UObjectBase.cpp defaults MaxUObjects to 2 * 1024 * 1024. That default rounds to 33 chunks and 2,162,688 capacity, which our <=2,000,000 reserved-capacity and <=32 chunk filters rejected. Exact NumChunks == ceil(live count / 65536) also rejected fully preallocated arrays. These are source-verified collector defects; the runtime report alone does not prove which one rejected this game's table.

Revision `legacy-objects-capacity-v3` separates reserved capacity from live traversal: capacity up to 4 Mi elements / 64 chunks, live count still <=2,000,000, sufficient allocated chunks bounded by reserved chunks, and capacity exactly consistent with the chunk table. It retains index/class validation, unique candidate selection, seven independent native call contracts, existing read/time budgets and declaration-only output. No native calls/game writes/assets or injected loader are added. Seven new tests cover default 33-chunk reservation, preallocated chunks, insufficient/excess chunks, inconsistent capacity, over-bound reservation and unchanged live-count limit. All 96 local checks pass; Windows adds its own-process read check. Retail object discovery still needs one report from the changed reader. Keep the working v8 pak; persistent favorites remain unfinished.

Sources: [UObjectArray.h](https://github.com/folgerwang/UnrealEngine/blob/99a530d4ccbe6bea1e8f49df20acfeb294006962/Engine/Source/Runtime/CoreUObject/Public/UObject/UObjectArray.h), [UObjectBase.cpp](https://github.com/folgerwang/UnrealEngine/blob/99a530d4ccbe6bea1e8f49df20acfeb294006962/Engine/Source/Runtime/CoreUObject/Private/UObject/UObjectBase.cpp). No engine implementation is copied.


## 2026-10-06: visual feedback and first external native capture

User reports the updated appearance looks better and is acceptable. This confirms general visual feedback, not a six-slot equipment matrix, travel/restart persistence or host/join acceptance. Keep the working v8 pak.

Inspected private native-favorites-20261006-130720-9fdf11.zip (SHA-256 `cee7785026223cb5c3c1f6d7a509f987ba4958e4664a1a2b99b83873f4b77e34`): REPORT.json only, Completed=false, zero declarations, `Legacy name array not uniquely validated (0 matches); no declarations accepted.` The collector reached the name scan; no access-denial error is reported. This does not establish the game's actual table layout or absence of reflected item identity.

Primary engine-source review at folgerwang/UnrealEngine commit `99a530d4ccbe6bea1e8f49df20acfeb294006962`, Engine/Source/Runtime/Core/Public/UObject/NameTypes.h, found a concrete discovery defect: TNameEntryArray uses 4 * 1024 * 1024 elements / 16384 per chunk = 256 inline chunk pointers. The reader only checked 128 (counts at +1024/+1028), omitting the source-defined +2048/+2052 layout. Reserve can allocate more chunks than the live element count requires, so exact chunk-count equality was also too strict. FNameEntry HashNext-before-Index and encoded index >> 1 agree with the existing reader; UnrealNames.inl confirms indices 0/1/2 are None/ByteProperty/IntProperty. No engine implementation is copied.

Reader revision `legacy-names-256-v2` checks both 128 and 256 candidates, permits bounded reserved chunks, and increases image block overlap to 4096 bytes to cover the larger header at scan seams. Existing unique-name and seven retail call-shape acceptance gates, read budgets and metadata-only output remain. Tests cover 24 layout combinations, inline discovery without a global pointer, reserved capacity, corrupt entry indices, image scan seam coverage and previous rejection cases: 89 local checks pass (Windows adds its own-process read check). These fixtures do not prove retail discovery; a new capture from this changed reader is required. Persistent favorites remain unfinished; no gameplay pak change or runtime success is claimed.

Sources: [NameTypes.h](https://github.com/folgerwang/UnrealEngine/blob/99a530d4ccbe6bea1e8f49df20acfeb294006962/Engine/Source/Runtime/Core/Public/UObject/NameTypes.h), [UnrealNames.inl](https://github.com/folgerwang/UnrealEngine/blob/99a530d4ccbe6bea1e8f49df20acfeb294006962/Engine/Source/Runtime/Core/Public/UObject/UnrealNames.inl).


## 2026-10-06: external native reader implemented

Added LegacyNativeEvidence and Collect-NativeFavoritesEvidence.ps1 for eleven allowlisted native declarations from the running Dungeons process. The reader uses query/read rights only, bounded legacy name/object/UField/UProperty traversal and seven profile/salvage shape gates. Failures report incomplete; no injection, executable copying, protection changes or loader retry. Metadata only, no instance/account/save values or memory dumps. Type coverage is partial; it is not a complete SDK or persistence fix. See NATIVE_FAVORITES_EVIDENCE.md for the one-run camp command and limits.

Equipped-marker collection is shared with generated-graph tests covering all six widgets, deduplication, unchanged native arrays and missing widgets. Wider red border/star generation is retained. Next inspect the private native report for item identity/serialization access before connecting a durable favorite store. Runtime visuals and persistent identity remain unconfirmed.

## 2026-10-06: profile evidence inspected; cloud GUID candidate

The uploaded private profile-evidence-20261006-013555.zip has SHA-256 `056d6178ddd194d421f05f926dacf01201416af9d2c467b86dec635d12d7264b`. All 26 targets completed with zero reported issues/export errors; metadata only, no raw patch sources. The exact collector succeeded on the user's installation. No repeat profile collection is needed.

`UMG_CloudSavePicker.TrySelectCurrentSave` obtains the recent local save index (statement 53), calls `PlayerControllerBase.GetCharacterSlotByIndex(index, False)` (103), obtains `PlayerCharacterSaveSlot.GetCloudPlayerId()` into an FGuid local (155), copies it to CurrentGUID (205), and compares a cloud row's GUID (480) with CurrentGUID using KismetGuidLibrary.EqualEqual_GuidGuid (552). Return property exports 2793/2794 reference `/Script/CoreUObject.Guid` through import -391. This establishes a cloud matching identity candidate, **not** equivalence to the save file's uniqueSaveId, independent clone IDs, or permanent item identity. Do not convert it to FString or assign unobserved semantics to the second bool argument.

`UMG_CharacterPicker` uses GetAvailableSaveDataByIndex(OriginalSaveIndex) at ExecuteUbergraph statement 5202, GetRecentSaveDataIndex at SetIsCurrentlyPicking statement 161, GetCharacterSlotByIndex(recentIndex, False) at OnRecentSaveDataIndexChanged statement 121, and GetSaveLocalUserNum at Confirm statement 148. BP_3DPlayerCharacterSlot also bounds profile access with GetNumProfiles. All six declaring owners and observed call layouts are now documented in GAME_API_CONTRACTS.md and guarded by the patcher's FunctionImportContracts/HeroProfileCallContracts. AssignSaveData, CloneCharacter, CreateNewCharacter, DeleteCharacterByIndex and SaveGlobalState are writers; none is used as a read-only getter.

No permanent physical-item ID or usable record-returning serialization getter was found in these 26 packages. ItemStashComponent references and CharacterLazySaveComponent failure delegates do not establish serialization data access. The favorites array remains transient. Hero/cloud GUID evidence alone cannot safely fix travel/restart persistence or distinguish duplicate items.

Local direct Roslyn compilation and 142 graph checks pass, including 18 added owner checks and 38 typed profile-call checks. Regenerated native combined UI reopens with 11,152 original exports preserved; all four generated asset files are byte-identical to the prior v8 output. No profile getter is newly emitted and no new gameplay pak/persistence fix is released. Retail v8 visuals, clone behavior, persistence and online host/join acceptance remain unverified.

Read-only external reflection tools were inspected at pinned MIT sources, but none is validated for this Store installation. One auto-resolver's parameter collector omits legacy UProperty functions; its fallback does not reset the default FProperty mode. Two other tools request PROCESS_ALL_ACCESS, and one requires game-specific offsets that are placeholders. See EXTERNAL_REFLECTION_REVIEW.md. No external probe has been executed, deployed or bundled. UE4SS Install remains disabled.

## 2026-10-06: recovered full catalog and exact profile targets

Recovered the earlier private game-evidence-4.zip (SHA-256 89fad1870529cd4688f5a6253e1a4a5104dad1bebe99e25cd2b7b8d27d7b5bc0), which contains the 131,164-path AssetList previously recorded in CURRENT_STATE. It identifies UI/Character/UMG_CharacterPicker, UMG_CharacterOptions, UMG_PlayerCharacterPickers, UICharacterDataBind, counters/cloud picker, Menu/UMG_SwitchProfile, GameModes/Menu/BP_3DPlayerCharacterSlot variants and BP_PlayerCharacter. Previous CharacterSelection/CharacterSelect/CharacterProfile filename filters missed these actual names. Catalog presence proves filenames, not native member signatures or persistent IDs.

Drive raw fetch explicitly rejects pakchunk0 (1,191,543,250 bytes) with HTTP 413 because its download limit is 268,435,456 bytes. Chunk4 (1,324,896,477 bytes) also exceeds that stated limit; no request/download of chunk4 is claimed. Existing catalog recovery avoids another catalog request. Do not bypass authenticated access or reinstall the withdrawn native loader.

Added Collect-FavoritesProfileEvidence.ps1 and --profile-evidence to the existing pinned legacy exporter. It reads exactly 26 catalog-observed packages, records every missing package, and outputs legacy properties/imports/Kismet only. No raw patch sources, loader, executable memory or character saves are collected or changed. Public archive key is supplied automatically. Collection can run with the game closed and produces a private .research/profile-evidence-TIMESTAMP.zip. Partial failures retain diagnostic metadata; missing packages do not certify an API. Native signatures/identity remain unresolved until these call sites are inspected.

Local direct Roslyn build against the pinned inspector libraries passed. Negative actual UE4.22 LetMeMove pak test selects zero and records all 26 missing targets. Positive temporary fixture places the same reference actor at one allowlisted path and a similarly named Backup path: exactly one selected/completed, 34 properties/2 functions/zero export errors, 25 missing targets. This tests exact selection and legacy parsing, not retail character UI or persistence. PowerShell/JSON/repository validation passes. Windows workflow adds negative selection, missing-target completeness, input hash preservation and metadata-only ZIP checks. No new gameplay pak or completed persistence fix is released by this pass.

Next: inspect profile call sites for a supported local hero identifier and item serialization/locator access, then implement the versioned sidecar/reconciliation with explicit-unfavorite semantics. If those members are native-only, archive call sites alone cannot establish a safe bridge. Runtime travel/restart/hero-switch/duplicates/storage/upgrades and online host/join remain required.

## 2026-10-06: recovery confirmed and startup evidence inspected

User confirms normal launch is restored after Collect/Remove. Private reflection-evidence-20261006-011355-8ef156.zip SHA-256 is e9afa27ac62c30f45d1aa98778e929085288b4c6d39ee7408e9e2db96ea4c51d. It contains REPORT.json and a 180-byte UE4SS.log only. captureCompleted=false; all five target classes and all four generated headers are absent. Log contains just console creation, v3.0.1/d935b5b identification and Game__Shipping__Win64 build banner. There is no script-ready marker, scan diagnostic, stack trace or SDK/object dump.

Pinned UE4SSProgram.cpp writes that banner before installing LoadLibrary IAT hooks and initializing Unreal modules/mods. The last log line supports an early loader-startup failure, not an identified faulting instruction; buffering or an intervening failure prevents more precise attribution. Recovery strengthens the conclusion that adding this probe triggered startup failure on this installation. It does not establish an item-persistence fix. Keep Install disabled; no further loader setting guesses or forced native retries are requested.

Began direct static archive investigation through the authorized Drive copy. Downloaded pakchunk99-WindowsNoEditor.pak (3,880,208 bytes, SHA-256 ea22ae76a4e6a437d32b30d1057ed06126c4618fa33d64001db720690832d5fb). Parsed its version-8 encrypted index with the already verified key, checked the index SHA-1 and all 118 record boundaries (15 padding bytes). This proves access and this index's integrity, not payload integrity or completeness of the entire installation. No profile/save/hero/character path matches occur in this chunk; it includes DLC gold-chest assets. Game bytes and file lists remain private. Continue static catalog/profile-UI investigation against relevant base chunks; stable physical-item identity remains unresolved.

## 2026-10-06: probe withdrawn after startup crash

User reports immediate crash after Install and pressing Play, before camp/Ctrl+H. The supplied screenshot shows launcher error 0xc0000005. It contains no stack trace or native function/signature evidence. Restoration after removal is now user-confirmed; the failing instruction remains unidentified. Do not reinstall or propose guessed signatures/engine settings. Install is blocked in production config. Collect/Remove remain operational; tests enable historical installation only in a copied temporary repository against a fake executable.

Upstream research now found [UE4SS #1219](https://github.com/UE4SS-RE/RE-UE4SS/issues/1219), opened 2026-03-18 and open when read on 2026-10-06. The reporter describes retail Dungeons UE4.22.3 failing at startup with empty Mods and no UE4SS log; their Event Viewer names UE4SS.dll with 0xc0000005. They distinguish a working self-compiled debug build from failing retail builds. This is a related failure report, not proof of our user's module/offset or the exact same UE4SS binary. [#1211](https://github.com/UE4SS-RE/RE-UE4SS/issues/1211) concerns an earlier startup failure in that reporter's self-compiled build and is closed; its closure cannot establish retail compatibility. The earlier generic engine-range/config review missed these game-specific reports. Do not offer repeated loader/settings tests without evidence.

Close the game and launcher, then from the repo:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Invoke-FavoritesReflectionProbe.ps1 -Action Collect
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Invoke-FavoritesReflectionProbe.ps1 -Action Remove
```

Run Remove even if collection fails, then verify the game launches normally. Upload the printed private reflection-evidence ZIP; if none is produced, provide UE4SS.log from the game's Binaries/Win64 folder or the terminal error. Removal leaves game saves and QoL paks untouched. Do not delete the game's own DLLs or alter executable protections. If removal fails, use its concrete error/ownership manifest to decide the next step. Favorites persistence remains unresolved.

## 2026-10-06: Drive installation copy available

Authenticated Drive listing confirmed the user-supplied MCD-ModdingCopy folder is accessible, including Dungeons/Content/Paks: 47 game pak files totaling 4,836,730,663 bytes and a separate mods folder. This is a useful source for direct asset/catalog extraction without repeatedly asking the user to export individual packages. Listing is verified; archive contents/integrity/completeness have not yet been downloaded or checked. The listed Dungeons/Binaries/Win64 folder contains XGamingRuntimeThunks.dll and small ancillary files, with no Dungeons executable or runtime reflection output. Static archives do not replace the missing native-class/lifecycle evidence for durable favorites. No protected executable upload is required for asset research. Keep the Drive URL and game files private; only necessary findings belong in git.

## Supplied archive evidence — 2026-10-05

Inspected the private `persistence-evidence-20261005-234040.zip` (SHA-256 `3e45b55d81dcfb58bd0c73c31c916d404b92111e51757cafb230f427ada03187`). Its exporter completed 88/88 UE4.22 packages with no errors and no raw patch sources. Useful metadata includes BP_GameInstance, its interface, storage chest content, transfer-slot UI and blacksmith UI; most other matches are unrelated audio/animation/material packages.

No CharacterSelection, CharacterSelect, CharacterProfile, SaveGame or UserManager package matched. These path filters do not enumerate native `/Script/Dungeons` class definitions. Imported references are not complete native reflection metadata.

Observed contracts:

- BP_GameInstance imports DungeonsGameInstance.GetUserManager, DungeonsUserManager.GetInitialUser and controller/login APIs. It does not establish persistent hero/item-instance IDs.
- Storage chest content ExecuteUbergraph statement 1370 calls ItemStashComponent.SerializeSaveState() with **zero parameters**, without assigning a result. MCD-PE's reconstructed declaration is `void SerializeSaveState()`. It writes game state, rather than providing JSON/item records. Do not call it as an FString getter or invoke it for this read-only investigation.
- Storage transfer UI uses StorageUtil.SortItems, TowerFunctionLibrary.CreateInventoryItemSlot, InventoryItemSlot.Item and grid caches. No permanent physical-item identifier was found.
- InventoryItem.Item.ItemId remains a type ID. Change counters, UObject names/addresses and MarkedNew/Cloned flags cannot substitute for persistent identity.

The inspector-owned MCDQoL_Favorites array remains session scoped. This pass releases no persistence fix or new gameplay pak. v8's wider red border and equipped markers remain unconfirmed in retail.

## Withdrawn runtime reflection probe design

Invoke-FavoritesReflectionProbe.ps1 installs temporary MIT UE4SS **3.0.1**, pinned to commit `d935b5b23bac03b65c14ae38382b02007204cc2e` and official release ZIP SHA-256 `4b47d4bceddd2f561a4e395bfa00924ccfc945af576a2d0c613e6537846c57ec`. The ZIP was independently downloaded, hashed and inspected. Upstream targets UE4.12–5.3, which includes 4.22, but Store compatibility is **unverified** and may require custom signatures. A failed startup/log is useful evidence.

Installation refuses existing proxies/loaders/configs, checks the release hash, records ownership before installing the proxy last, and copies only six owned files. No upstream cheat/console/Blueprint loader/splitscreen mods are installed. Our Lua script registers Ctrl+H using the upstream RegisterKeyBindAsync pattern and calls only GenerateSDK() and DumpAllObjects(). It requests no item setters, save calls, gameplay hooks or forced asset loading. **UE4SS itself hooks the engine during startup**; Lua pcall cannot catch a native access violation.

GUI/external consoles, hot reload, UObject cache, crash dumps and forced asset loading are disabled. Engine override is the established UE4.22. No executable bytes, ownership or ACLs are changed. Headers describe reflected members, not complete non-reflected save data or portable runtime offsets.

The earlier Install/camp/Ctrl+H test is withdrawn. Follow collection/removal above instead. Any future native probe requires investigation of this startup failure first.

Upload the printed reflection-evidence ZIP. Collection copies fresh UE4SS.log, four allowlisted CXX headers and only `/Script/Dungeons` lines from the object dump. Missing data/classes/completion are reported explicitly, including failed startup. No saves, executables, crash memory or item values are copied. Keep generated game metadata private. Removal deletes only unchanged owned files and preserves edited files/evidence; it warns if an edited loader DLL remains. QoL paks are untouched.

Default Win64 path is `C:\XboxGames\Minecraft Dungeons\Content\Dungeons\Binaries\Win64`; override with -Win64Path if needed. If Store permissions deny writes, report the error. Do not change ownership to force installation.

## Implementation gate and validation

Inspect native InventoryItem, InventoryItemSlot, ItemStashComponent, DungeonsGameInstance, DungeonsUserManager and related profile/serialization declarations. If supported hero/physical-item IDs exist, implement a versioned hero-scoped sidecar. Otherwise investigate a version-checked native serialization/transfer bridge. Widget caching or name/power matching cannot establish persistence.

Acceptance requires independent duplicates, explicit unfavorite, travel, restart, hero switch, equipment, storage, upgrades/rerolls and online host/join. Unresolved protection must not be silently discarded or transferred to similar items. Reflection alone cannot prove these behaviors.

Local PowerShell 7.4.6 tests use the actual pinned release against a fake game directory: checksum rejection, collision refusal, only our diagnostic enabled, missing capture reported incomplete, allowlisted ZIP roundtrip, native object-line filtering, edited config retained, unchanged loader removed, original executable/unrelated files preserved. UTF-16 without a BOM is tested because pinned UE4SS object output uses wchar_t; UTF-8/BOM variants are also accepted. Lua mock tests verify the key binding, reentrancy guard, call order and recovery after a capture error. Repository parser/JSON checks and diff checks pass. Windows PowerShell 5.1 runs the fixture in Project Validation. None proves game compatibility/persistence.

## Primary references

- [UE4SS release](https://github.com/UE4SS-RE/RE-UE4SS/releases/tag/v3.0.1), [pinned README](https://github.com/UE4SS-RE/RE-UE4SS/blob/v3.0.1/README.md), [MIT license](https://github.com/UE4SS-RE/RE-UE4SS/blob/v3.0.1/LICENSE).
- [Pinned keybinds](https://github.com/UE4SS-RE/RE-UE4SS/blob/v3.0.1/assets/Mods/Keybinds/Scripts/main.lua), [LuaMod.cpp](https://github.com/UE4SS-RE/RE-UE4SS/blob/v3.0.1/UE4SS/src/Mod/LuaMod.cpp): SDK output is CXXHeaderDump, not a guessed UE4SS_SDK directory.
- [Installation](https://docs.ue4ss.com/release/installation-guide.html), [dumpers](https://docs.ue4ss.com/release/feature-overview/dumpers.html).
- [MCD-PE inventory reconstruction](https://github.com/Minecraforever/MCD-PE/blob/main/_re/systems/item/Restored_ItemStashComponent.h), Apache-2.0; reference declaration, not proof of the Store ABI.
