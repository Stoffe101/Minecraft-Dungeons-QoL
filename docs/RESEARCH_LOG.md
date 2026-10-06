# Research Log

## 2026-10-06: exact call-budget failure in object traversal; batched slots

Inspected private native-favorites-20261006-133827-9942ba.zip, SHA-256 `f2217f4fe22d43e5b7b6b6f2abf8cee1baea5bdf88d438d7d6e43c8c5d16d8c8`. Revision `legacy-bounded-traversal-v4` reports Completed=false, zero declarations, budget cause `calls`, stage `object-traversal`, 2,000,001 attempted reads, 33,054,711 requested bytes and 3,505 elapsed milliseconds. This establishes that unique name/basic object-table validation passed and traversal began. It does not establish seven native call contracts, exact native declaration layout or stable item identity. The failure is the read-call ceiling, not the 90-second timeout or an access-denial error. No repeat of unchanged v4 is needed.

Revision `legacy-batched-slots-v5` reads FUObjectItem slots in blocks of at most 2,048 entries / 48 KiB, constrained to the live-count tail within one 65,536-entry chunk. Only the current block is retained; UObject index/class checks still read each non-null object's header directly. Before accepting declarations, each selected object's current index, direct slot pointer, class kind and full declaring path are reread and checked, followed by fresh seven-contract and used-chunk/header checks. The two-million-call / 256 MiB / 90-second limits and read-only handle remain unchanged. Batched slot flags/serials/buffers are never exported; REPORT.json remains declaration-only plus safe scalar diagnostics. No executable bytes, account/item/save contents or raw dump are written.

Added empty-slot read-count coverage, declaration-slot replacement rejection/direct reread verification, a 65,537-slot/two-chunk fixture with a one-entry final block, bounded call-count coverage and partial-block rejection. Fixture slot buffers are separated from synthetic UObject allocations. The initial new replacement test accidentally targeted the unselected CoreUObject class slot; corrected it to the selected Dungeons.InventoryItem slot and confirmed rejection. A previously zero-timeout test exposed timer-resolution nondeterminism; it now uses an already expired test deadline, leaving the production timeout unchanged. All 108 local checks pass; Windows adds its own-process read check. These fixtures do not establish retail completion or persistence. Keep the working v8 pak; next gate is one v5 report from idle camp.


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


## 2026-10-06: equipped marker coverage and external reader

Implemented MarkerWidgetCollection shared by production generator and tests, with four checks for equipped widgets, owner deduplication, array copy semantics and missing widgets. Added project-authored LegacyNativeEvidence and existing-SDK PowerShell wrapper: query/read only; bounded legacy names/objects/UProperty traversal; seven observed call-shape gates; eleven native declaration targets; incomplete reports on failure; private metadata-only ZIP. No injected loader or item/save values. See NATIVE_FAVORITES_EVIDENCE.md. Native identity/save-load integration remains unresolved, so no persistent release. User's screenshot shows a work timer without a visible handoff, not a game test result.

Local direct Roslyn builds pass. Graph tests: 146 passes; native-reader fixtures: 49 passes. Regenerated UI preserves 2,584 inspector + 8,568 HUD exports (11,152 total); all four files match v8 byte-for-byte. Repository/PowerShell/JSON and diff validation pass. Existing v8 ZIP verified as SHA-256 3a00de133bf75495a6f16794d9ddbde3c005f95005c0ecbf4029d7de7dda261e. Private inputs/output remain outside git/public CI. Windows CI additionally checks the actual own-process query/read handle; no retail outcome is inferred.

## 2026-10-06: verified profile call contracts and external tool review

The uploaded private profile-evidence-20261006-013555.zip has SHA-256 `056d6178ddd194d421f05f926dacf01201416af9d2c467b86dec635d12d7264b`. All 26 targets completed with zero reported issues/export errors; metadata only, no raw patch sources. The exact collector succeeded on the user's installation. No repeat profile collection is needed.

`UMG_CloudSavePicker.TrySelectCurrentSave` obtains the recent local save index (statement 53), calls `PlayerControllerBase.GetCharacterSlotByIndex(index, False)` (103), obtains `PlayerCharacterSaveSlot.GetCloudPlayerId()` into an FGuid local (155), copies it to CurrentGUID (205), and compares a cloud row's GUID (480) with CurrentGUID using KismetGuidLibrary.EqualEqual_GuidGuid (552). Return property exports 2793/2794 reference `/Script/CoreUObject.Guid` through import -391. This establishes a cloud matching identity candidate, **not** equivalence to the save file's uniqueSaveId, independent clone IDs, or permanent item identity. Do not convert it to FString or assign unobserved semantics to the second bool argument.

`UMG_CharacterPicker` uses GetAvailableSaveDataByIndex(OriginalSaveIndex) at ExecuteUbergraph statement 5202, GetRecentSaveDataIndex at SetIsCurrentlyPicking statement 161, GetCharacterSlotByIndex(recentIndex, False) at OnRecentSaveDataIndexChanged statement 121, and GetSaveLocalUserNum at Confirm statement 148. BP_3DPlayerCharacterSlot also bounds profile access with GetNumProfiles. All six declaring owners and observed call layouts are now documented in GAME_API_CONTRACTS.md and guarded by the patcher's FunctionImportContracts/HeroProfileCallContracts. AssignSaveData, CloneCharacter, CreateNewCharacter, DeleteCharacterByIndex and SaveGlobalState are writers; none is used as a read-only getter.

No permanent physical-item ID or usable record-returning serialization getter was found in these 26 packages. ItemStashComponent references and CharacterLazySaveComponent failure delegates do not establish serialization data access. The favorites array remains transient. Hero/cloud GUID evidence alone cannot safely fix travel/restart persistence or distinguish duplicate items.

Local direct Roslyn compilation and 142 graph checks pass, including 18 added owner checks and 38 typed profile-call checks. Regenerated native combined UI reopens with 11,152 original exports preserved; all four generated asset files are byte-identical to the prior v8 output. No profile getter is newly emitted and no new gameplay pak/persistence fix is released. Retail v8 visuals, clone behavior, persistence and online host/join acceptance remain unverified.

Read-only external reflection tools were inspected at pinned MIT sources, but none is validated for this Store installation. One auto-resolver's parameter collector omits legacy UProperty functions; its fallback does not reset the default FProperty mode. Two other tools request PROCESS_ALL_ACCESS, and one requires game-specific offsets that are placeholders. See EXTERNAL_REFLECTION_REVIEW.md. No external probe has been executed, deployed or bundled. UE4SS Install remains disabled.

## 2026-10-06: launch restored; three-line startup log; direct archive access

User confirms normal launch after removing UE4SS. Inspected private reflection-evidence-20261006-011355-8ef156.zip (e9afa27a…ea4c51d): REPORT.json + UE4SS.log only, log 180 bytes/three initial banner lines; no ready/capture markers or headers/object dump. Pinned UE4SSProgram.cpp emits the last line before LoadLibrary IAT hooks/Unreal initialization, but a truncated log cannot pinpoint a faulting instruction. Retain withdrawal; no settings guesses justified. Recovery does not complete durable favorites.

Verified direct authenticated Drive download for game chunk99 (3,880,208 bytes, ea22ae76…32d5fb), parsed version-8 AES index using existing verified key and validated index SHA-1 plus 118 record boundaries/15-byte padding. No profile/save/hero/character target paths in this chunk. This validates this index, not payloads/all archives; private downloaded bytes/index not committed. Next investigate relevant base archive profile UI/native references without reinstating the crashing loader.

## 2026-10-06: native probe startup failure and withdrawal

Primary-source follow-up found UE4SS upstream #1219 (opened 2026-03-18; open when retrieved): retail Dungeons UE4.22.3 startup failure even with empty Mods, 0xc0000005 attributed by the reporter's Event Viewer to UE4SS.dll, sometimes no log. #1211 is a closed report involving their self-compiled build; do not infer retail compatibility from that fix. These game-specific reports were missed by the earlier generic engine/version review. Recorded the correction and retained withdrawal. User's faulting module/offset still requires their evidence. Sources: https://github.com/UE4SS-RE/RE-UE4SS/issues/1219 and https://github.com/UE4SS-RE/RE-UE4SS/issues/1211.

Inspected user screenshot: immediate crash after installation/Play, launcher reports 0xc0000005, before manual reflection capture. No native stack/function is available. Newly installed UE4SS loader is the leading suspect; removal/restored launch still needs retail confirmation. Blocked production Install, preserved Collect/Remove, and replaced recommended installation steps with rollback. Test exercises the block in the real configuration, then historical checksum/ownership mechanics only in a copied temporary fixture repo/fake game. No guessed loader settings/signatures, save changes, executable protection changes or gameplay pak changes. Need private startup evidence before further native work.

## 2026-10-06: assess user-supplied Drive installation copy

Read the supplied folder using authenticated Drive metadata/listing. Verified MCD-ModdingCopy structure and 47 game paks (4,836,730,663 bytes), plus a mods directory. Listed Win64 contains no game executable or native reflection output. Useful for independent static asset research; not proof of archive integrity/completeness or a substitute for native persistence/lifecycle evidence. No game binaries downloaded/published and no sharing changed. Detailed scope recorded in FAVORITES_PERSISTENCE_INVESTIGATION.md.

## 2026-10-05: supplied persistence evidence and native reflection gate

Follow-up before release: inspected v3.0.1 wchar_t object-dump output and added BOM/UTF-16 detection plus streaming native-line filtering. Tested headers/object dump encoded UTF-16 without a BOM, avoiding Windows PowerShell's ANSI fallback. Lua mock checks pass binding, reentrancy, call order and error recovery.

Inspected private persistence-evidence-20261005-234040.zip: 88/88 completed, no export errors/raw sources. Game-instance/storage/blacksmith metadata establishes no permanent hero/physical-item identifier. Character/profile/UserManager/SaveGame filters matched no packages; they do not enumerate native classes. Storage SerializeSaveState is a zero-parameter writer with no assigned result, consistent with the reconstructed void declaration.

Rechecked MIT UE4SS 3.0.1, independently hashed official release (4b47d4bc…46c57ec), inspected pinned d935b5b source/config/keybinds. Prepared temporary reflection probe with only our diagnostic, no forced loading or save/item mutation calls, hash-owned removal and failed-capture reporting. Fixed PowerShell 7 auto-DateTime freshness conversion by preserving UTC kind; 5.1 strings parse with RoundtripKind. Actual-release fixture, parser/JSON and diff checks pass locally; Windows 5.1 CI test added. Game compatibility/persistent favorites remain unfinished. Details/commands/primary sources: FAVORITES_PERSISTENCE_INVESTIGATION.md.

## 2026-10-06: confirmed UI, travel-loss investigation and v8 visual follow-up

User confirms v7 features/layout work, requests thicker red selection and equipped stars, and reproduces lost favorites after a mission. Workspace maintenance removed scratch inputs; recovered original source/evidence ZIPs from their saved identities and re-cloned source. Inspector-owned UObject favorites explain the lifecycle limitation. Existing game metadata and fresh MCD-PE item/save/caching inspection still expose no verified stable per-item/hero ID. Item type, localized fingerprints, transient object names and serialized gameplay flags are unsuitable. No guessed persistence path implemented.

v8 uses 6-unit red strips and deduplicates the six equipment widgets into mark targets; native equipment exclusion remains. Added metadata-only persistence collector, a key-supplying wrapper and target manifest. A real legacy-pak negative test checks unrelated assets, missing-target failure, no raw output and unchanged input hash. All three UI modes write/reopen preserving 11,152 original exports. Existing 39 action/layout and 27 owner cases plus probe/diagnostic checks remain required. Windows checks are the remaining validation gate. Private assets stay excluded.

Persistence remains blocked on new game-instance/profile/save/storage evidence; do not call v8 a persistence repair. New equipped-star placement needs retail testing. Next: inspect uploaded persistence metadata, obtain runtime reflection if native identifiers are unavailable, then implement hero-scoped sidecar plus travel/restart/transfer/reconciliation tests. Record requirements and acceptance in INVENTORY_IDENTITY.md.

## 2026-10-05: v7 favorite icon and inspector tag

Inspected the user's two local crops and private InspectInfo/tag widget layout. The observed native rarity/gilded/custom tags share a HorizontalBox; UMG_ItemTagIconName and inspected-item fields are reflected. Checked GetParent/AddChildToHorizontalBox/padding/alignment in pinned Epic UE4.22 headers, with current official docs corroboration. Replaced tiny gold square with a native pixel star and appended a FAVORITE badge to the native row, resolving the physical inspected item. Dynamic row replacement detaches the old badge; unsupported rows hide it. Native selection/salvage safeguards, session-scoped favorites and v6 redraw suppression remain.

39 action/layout checks and 27 declaring-owner checks pass on each actor fixture, with existing 28 probe and 19 diagnostic rejection cases. All three modes write/reopen preserving original exports; package round-trip must compare the four staged files before delivery. Private assets remain excluded from git/CI. Retail marker placement, badge flow under UI scaling and host/join still need user tests; the GPU-hang trigger remains unknown. See INVENTORY_UI_DESIGN.md for geometry, native contracts and limits.

## 2026-10-05: v5 multi-salvage confirmation and v6 GPU-hang mitigation

User confirms multi-salvage works; Select All followed by salvage and refund totals remain unverified. Screenshot shows bottom item's details obscured by Favorite. Relocated Favorite to the left toolbar, shifted count after it. Private crash XML/minidump identify a D3D11 DEVICE_HUNG GPU assertion after alt-tab, rather than the historical Blueprint access violation. Its trigger remains unknown.

Inspected pinned Epic UE4.22 Widget/TextBlock implementations and reflected GetVisibility/byte comparison declarations, and Microsoft's DXGI error reference (links in INVENTORY_CRASH_INVESTIGATION.md). Removed repeated collapse/show cycles for unchanged marks/buttons/modal and cached dynamic captions. GetVisibility uses a typed enum byte local; setter declaring-owner contracts cover Widget. No copied engine code or private assets committed.

Local generated graph tests pass on both fixtures: 34 action/layout checks and 23 declaring-owner cases each, plus 28 probe and 19 diagnostic rejection cases. Three build modes serialize/reopen with all 11,152 original exports preserved; native mode retains the guarded one-item-per-tick salvage worker. Private pak packing must round-trip all four files before delivery. Runtime layout, alt-tab stability and Select All batch execution remain user checks; no claim that GPU hang is fixed.

## 2026-10-05 — v5 overlap correction and enabled native batch

Inspected the user's cropped v4 toolbar screenshot: selection count reads five; Done selecting/Select All overflow. Reduced captions/14-point button font, widened controls, increased gaps, and moved count. Actual salvage was absent because the previous download deliberately disabled it, not because the native worker had been tested and failed. New private CombinedNative v5 explicitly enables the worker after Yes, with existing identity/favorite/equipment/membership/mission eligibility guards and original native undo/delegate behavior. Completion shows salvaged/skipped counts.

Rechecked original CanSalavage/GetItemStash/SalvageSlot metadata, including owning-player stash, native slot + success-out ABI and typed undo return. Added reflected mutation-owner regression cases (19 owner checks per fixture; existing 30 action/layout cases, 28 probe and 19 diagnostic rejection tests). All three source modes must compile/write/reopen and preserve 11,152 original exports; delivered native pak must unpack to exactly four matching staged files. Runtime deletion/refunds, joining-player compatibility and persistent favorites remain unverified. See INVENTORY_UI_DESIGN.md for native retail checks and distinction from the old preview.

## 2026-10-05 — Working v3 and UI v4

User confirms favorites work well and combined selection/Select All works. Actual batch deletion remains disabled and untested. Inspected both local screenshots, original HUD font/content margins and embedded slot CanvasPanel roots. Checked UE4.22 Font/WidgetTree/Border/CanvasPanelSlot contracts against the pinned Epic mirror. Implemented a compact footer, retail font sizing, separate favorite button, cyan physical-item selection frames, gold markers and a real Yes/No modal. No guessed offsets/native font or brush struct layouts.

Three private modes pass compile/write/reopen/original-export preservation. 30 action/layout mock cases and 17 owner cases pass per fixture, alongside 28 probe/19 diagnostic rejection tests. New cases cover retained selection on No, stale requests and malformed/repeated/preview approvals. Rendering, performance, native deletion and joining-player runtime behavior remain unverified. See [INVENTORY_UI_DESIGN.md](INVENTORY_UI_DESIGN.md) for exact layout, researched contracts and retail checks. Private game-derived output stays out of GitHub/CI artifacts.

## 2026-10-04 — Project bootstrap

### Goal

Establish a safe development foundation for a Minecraft Dungeons 1 QoL mod containing:

- gear locking
- gear manager/loadouts
- multi-select mass salvage

### Findings

- Dokucraft Dungeons Mod Kit uses UE4.22 and is MIT licensed.
- Blueprint Loader injects content at Menu, Lobby, and Ingame triggers.
- Current Dungeons 1 community work still uses the established modding pipeline.
- Modern Xbox app / Microsoft Store installs expose an accessible Paks directory.
- Vanilla salvage returns currency and invested enchantment points.
- Mass salvage should invoke the native salvage path rather than reimplementing the economy.

### Decisions

- Use a separate project-owned SaveGame slot for lock/loadout metadata.
- Do not mutate hero save files.
- Prefer overlay UI and native game APIs over wholesale vanilla asset replacement.
- Native vanilla salvage must be the destructive backend.

### Implementation completed

- repository documentation structure
- Mod Kit bootstrap tooling
- environment/path detection
- asset sync/build/install tooling
- initial feature/architecture/test specifications
- offline inventory asset research helper

### Not yet tested

PowerShell tooling still needs verification on a real Windows Dungeons development machine.

## 2026-10-04 — Save identity investigation

### Source

CutFlame/MCDSaveEdit:
https://github.com/CutFlame/MCDSaveEdit

### Findings

The save model contains:

- profile `playerId`
- profile `uniqueSaveId`
- item `inventoryIndex`
- item `equipmentSlot`
- type, rarity, power, enchantments, gilded/netherite data

The editor's item-list logic sorts by inventory index and assigns new items `max(existing index) + 1`.

Storage transfer can assign a new target-collection index, so `inventoryIndex` is not a permanent cross-storage identity.

### Result

`uniqueSaveId + inventoryIndex` is a provisional locator, not a guaranteed permanent identity. Prefer a native runtime GUID if one exists. Otherwise combine the locator with sanity/fingerprint data and fail closed on disagreement.

## 2026-10-04 — Microsoft Store ownership through Minecraft Launcher

### Finding

Microsoft-account ownership and game-file location are separate concerns.

For mod installation, the authoritative location is the `Dungeons\Content\Paks` folder belonging to the copy actually launched.

Common layouts:

- Minecraft Launcher: `%LOCALAPPDATA%\Mojang\products\dungeons\dungeons\Dungeons\Content\Paks`
- Xbox app: `C:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks`

### Project impact

The project supports both path families and permits explicit override when multiple copies are installed.

## 2026-10-04 — Native inventory/salvage API research

### Source

Minecraforever/MCD-PE:
https://github.com/Minecraforever/MCD-PE

### Findings

Final-build-verified class architecture includes:

- `UItemStashComponent`
- `UInventoryItemSlot`
- equipment-slot enumeration
- inventory-slot enumeration
- slot swap functions
- native `SalvageItemInSlot`
- native salvage undo/info functions

### Result

The QoL mod does not need custom salvage reward calculations.

Bulk salvage can validate each chosen slot and call the game's own native salvage transaction.

## 2026-10-04 — Proven Dungeons modding workflow research

### Findings

Research across the Dungeons Mod Kit, Blueprint Loader documentation, current Dungeons 1 mods, and known Blueprint Loader examples established the preferred production workflow:

1. UE4.22.x
2. Dungeons Mod Kit
3. Blueprint Loader
4. small Lobby/Ingame loader levels
5. manager actor
6. actor-created UI widgets
7. cook with UE4.22
8. package with the Mod Kit/u4pak pipeline
9. install the pak under the active game's `Paks\~mods`

Camera Coordinates Overlay is explicitly published as a Blueprint Loader example and demonstrates the actor-to-widget pattern we need.

A Dungeons 1 mod updated in 2026 explicitly states it was made using the Dungeons Mod Kit, confirming the toolchain is still relevant to the final Dungeons 1 release.

### Binary tooling result

UAssetAPI 1.1.0 successfully parses the known-working LetMeMove Dungeons Blueprint:

- object version 517
- 228 names
- 67 imports
- 83 exports

KismetKompiler's old UAssetAPI cannot parse the same asset. Replacing the dependency with modern UAssetAPI causes source-API incompatibilities, so KismetKompiler remains an experimental automation path rather than the main implementation strategy.

### Decision

Standard UE4.22 authoring is now the primary path.

Permitted existing mod assets are allowed as templates where they materially reduce risk, especially Camera Coordinates Overlay for UI bootstrapping and LetMeMove for a known-working actor/loader structure.

Modern UAssetAPI remains the preferred inspection/validation/precooked-patching tool.

Full analysis is documented in `MODDING_RESEARCH.md`.

## 2026-10-04 — Repository audit, fresh primary-source research and diagnostic safety pass

### Baseline and findings

Audited `main` at `b35d841`, including all canonical docs, scripts, workflows, mirrors, asset research and runtime tools. Code already contained a cooked navigation/protection/native-salvage prototype; current-state docs were stale. Found slot-only queue identity, missing stash continuity/bounds guards, weak global fingerprint persistence, unverified reflected item calls, package collisions with LetMeMove, unpinned build inputs and incomplete validation coverage. Full findings and source links: `REPO_AUDIT.md` and `MODDING_RESEARCH.md` section 18.

### Research / decisions

Rechecked Mod Kit, Blueprint Loader, UAssetAPI, FModel, repak, KismetKompiler, UE4SS and current Dungeons 1 examples. Keep UE4.22 editor-authored Blueprints/Mod Kit/Blueprint Loader as the production target; use the permitted cooked shell for non-destructive diagnostics. UE4SS retail crash reports and KismetKompiler limitations prevent making them mandatory. Upstream restored Steam APIs do not establish Store reflection/ABI compatibility. Missing exact function evidence remains a runtime gate.

The current fingerprint cannot identify physical items safely across duplicate items, heroes or stat/localization changes. Removed native salvage from the emitted diagnostic graph rather than declaring those gaps solved.

### Applied implementation

- Paired selected slot/item snapshots, inventory membership/object equality checks, bounds validation and stash-change/invalid-context queue clearing.
- Reflected local inventory array before indexing, native slot-lock checks, F5 cancel/clear.
- Sidecar class check, write-result handling, explicit prototype fingerprint messages and F10 preview with no destruction.
- Namespace relocator for manager, Lobby/Ingame maps and remaining sidecar template names.
- Reproducible diagnostic builder with LetMeMove 1.1.0 checksum, pinned Mod Kit commit, generated sidecar and required package-content checks.
- Full MIT template notice bundled alongside diagnostics; full Apache license retained with editor mirror notices.
- Bytecode validator and eight negative regression cases; CI compiles all runtime tools and uploads read-only exact-source diagnostic artifacts.
- Corrected README, current state, architecture, identity, roadmap, decisions, research, test plan and attribution docs.

### Tests / results

- Changed C# tools compiled against UAssetAPI 1.1.0 with the .NET 8 compiler. Local SDK CLI/MSBuild encountered process-information failures in this execution environment; direct Roslyn compilation was used successfully. This is not a claimed Windows MSBuild run.
- Diagnostic graph write/re-open passed: 7,180 iCode units, 136 statements, context skips and statement-boundary jump targets validated; no destructive inventory call allowed.
- Manager, both maps and sidecar relocation/re-open passed. SaveGame synth confirms `SG_MCDQoL_C`, parent `SaveGame`, `Records` array and `CPF_SaveGame` flag.
- Eight negative tests passed: invalid/out-of-range jumps, conditional payload jump, bad context skip, and forbidden SalvageItemInSlot/RemoveItem/SalvageItemUndo/Swap. Original graph validates after restoration.
- Pak packed/listed successfully with exactly eight expected files. Binary scan found no LetMeMove/BP_WASD_Movement names after sidecar cleanup.
- Local diagnostic pak SHA-256: `0bdb85381f482aa8de281c755da2287c27273a97fda12a3b0b10b97816690cfc` (pre-commit local build, not a runtime-certified release).
- `Validate-Repository.ps1` passed in PowerShell 7.4.6 on Linux: all PowerShell syntax, JSON, mirror/doc/notice requirements passed. Workflow YAML parsed successfully; `git diff --check` passed.
- No UE4.22 UHT/editor build, Store/Xbox/Steam game boot, controller, co-op, sidecar persistence, item identity or destructive transaction test was performed.

### Next work

Run the diagnostic/reflection check on actual Store/Xbox Dungeons 1; establish exact reflected method signatures and stable hero/item identity; implement equipped/loadout guards and proper review/input UI. Then test a native salvage transaction with disposable gear and continue the documented loadout/release roadmap. Do not present the diagnostic archive as a finished mod.

## 2026-10-04 — Windows build verification and installed-game evidence collection

### Findings and decisions

Published the audited diagnostic through PR #1 and merged after exact-head Windows Project Validation and Cooked QoL Diagnostic Build passed (runs 37205700653 / 37205700636). This resolves the previous local MSBuild uncertainty. The diagnostic artifact is available from the build run; it remains non-destructive and untested in game.

Reviewed UeBlueprintDumper source/argument parser and the old zMCDungeons-SDK inventory declarations. The latter's empty parameter metadata cannot establish current signatures. Choose read-only installed-game Blueprint evidence next; do not inject UE4SS or guess native ABI. Pin dumper 1.2.0 and Windows x64 .NET 8.0.31 runtime with official checksums.

### Applied changes / checks

- Added `Collect-GameEvidence.ps1`, manifest/metadata ZIP and `GAME_EVIDENCE.md`; no hero-save access, no game-folder output, no input archive modifications.
- Explicit nonexistent game paths now fail immediately; automatic selection refuses multiple installations.
- Added collector regression checks for paths with spaces, six correctly ordered inspector invocations, preserved input hashes, metadata-only ZIP, overwrite/game-folder rejection and failed/empty inspection diagnostics. Local PowerShell 7.4.6 checks passed.
- Initial Windows run proved real bootstrap/startup but exposed inherited exit code 1 from the expected-failure fixture. Fixed the test runner to return success only after its assertions.
- Windows CI additionally exercises pinned downloads/checksums and actual dumper startup against an intentionally invalid fixture archive. This checks bootstrap/failure handling; it does not inspect a real game.
- [Windows validation run 37206639517](https://github.com/Stoffe101/Minecraft-Dungeons-QoL/actions/runs/37206639517) passed both collector test steps (PowerShell 7 and Windows PowerShell 5.1), real downloads/dumper startup, repository validation and all five .NET tool builds on code commit `29b13f9c55da18e7ab18e260020f8946e6c95f06`.
- Actual Store/Xbox archives, native reflection, UE4.22 editor, item identity and in-game tests remain unavailable in this workspace. No agreed release feature is marked complete by this pass.

### Next work

Obtain the collector ZIP from the active installed Dungeons 1 copy, analyze real call metadata and author the runtime/identity probe. Continue stable locks, gear manager/loadouts, inventory/review/controller UI and native salvage with the test matrix. Windows/game access remains required for runtime completion.

## 2026-10-04 — Xbox executable access-denied collector fix

User confirmed `C:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks` and supplied a Windows PowerShell failure reading the adjacent Dungeons.exe hash. Executable collection ran before the protected dumper block, so optional metadata aborted the whole run. No game asset inspection occurred in that attempt.

Made version/hash reads individually best-effort and protected executable enumeration. Unavailable values remain null, errors are recorded per executable and in manifest warnings, and archive inspection continues. Required archive/tool failures still produce nonzero status. No elevation, ownership or ACL changes are needed for an optional hash. Documented using a fresh output path because the failed run already created its default folder.

Local PowerShell regression passed with a synthetic UnauthorizedAccessException on Dungeons.exe: six dumps, valid ZIP, warning/error metadata and successful status, alongside the existing path/input/failure checks. Repository syntax/JSON validation passed. The same regression is included in Windows CI under PowerShell 7 and 5.1. Actual game inspection remains pending the user's retry.

## 2026-10-04 — Analyze uploaded Store evidence and prepare AES retry

Read the uploaded `.research.zip` and terminal screenshot. ZIP SHA-256: `02c3b159114eb6c6cda198ba1bfcca9c19ef0cb31f4f78ea6e247635ec9a0fde`. The user's collector at commit `820387e` listed 47 pak files. Optional Dungeons.exe access denial became a warning as intended, but all six term lists were empty. Logs show no AES key, localization failure and Completed; no Blueprint metadata was exported. Retained the supplied logs/manifest only under ignored local research.

Reviewed UeBlueprintDumper's key submission/listing implementation and a public Dungeons localization mod's pak reader. An explicit documented Dungeons 1 AES candidate is the next check; neither encryption nor candidate compatibility is declared proven on this installation. Added list-only ArchiveCatalog with visible path count and a key-provided flag, so future logs can distinguish no visible game paths from search misses. Validate key formatting and forward it to all seven inspector calls without persisting its value. No save edits, permission changes or raw asset dumps. See GAME_EVIDENCE.md for the exact retry command and source URLs.

Local collector regressions passed, including catalog mode/count and AES forwarding with six Blueprint dumps, alongside prior access/path/failure tests. PowerShell syntax/JSON repository validation passed. Windows CI includes these checks on PowerShell 7 and 5.1. Runtime feature development still needs a successful real archive export; no native signatures or item identity can be inferred from empty lists.

## 2026-10-04 — Verified Store catalog and native stderr isolation

Uploaded retry ZIP SHA-256 `3249f641a4f9678fac2ab8a5f00031d26bcfc5f933c424278fccad5e932430ad`, collector source `3b86620`. The AES-configured archive catalog lists 131,164 Dungeons paths and 45,035 .uasset paths. This proves catalog/key compatibility on the active installation. The Inventory pass exposed names but was terminated by native stderr at cosmetic UMG_CosmeticButtonEquip.GetButtonReference. There are no exported class/function files in the uploaded ZIP; remaining groups never ran. Key success does not verify native reflection.

Applied catalog evidence to six exact UI/controller path matches (31 asset candidates total). Removed broad texture/cosmetic and absent ItemStash-name searches. Added redirected native process stdout/stderr capture with Windows argument quoting compatible with .NET Framework, per-group failure isolation and explicit partial-export error reporting. No extracted game assets or disassembly are committed. Documented known HUD/slot/salvage/grid/controller names in FINDINGS.md and a fresh output-directory retry in GAME_EVIDENCE.md.

Local collector regressions and PowerShell syntax/JSON checks passed. Windows CI now additionally compiles a synthetic native inspector that writes stderr with exit zero; asserts all seven calls finish, six metadata files and ZIP survive, paths with spaces remain valid and errors are recorded. Both PowerShell 7 and 5.1 run this regression. Actual targeted class/function export and in-game tests remain the next gate.

## 2026-10-04 — Resolve legacy UE4.22 metadata parser incompatibility

Analyzed targeted ZIP `89fad1870529cd4688f5a6253e1a4a5104dad1bebe99e25cd2b7b8d27d7b5bc0`, collector `dcc5f5b`. All groups ran, but no class/function JSON/text files exist. Logs consistently show class ArgumentNullException (source) and function NullReferenceException. Source review of UeBlueprintDumper commit 9726294 and pinned CUE4Parse bc36dc7 identifies unconditional ChildProperties enumeration against UE4.22's legacy property exports. Ignoring errors or declaring API availability would not solve missing signatures.

Added project-authored LegacyEvidenceExporter: reuse checksum-pinned CUE4Parse archive libraries, parse temporary companions with UAssetAPI 1.1.0, output indexed imports/property metadata/class/function children and Kismet only, remove raw staging in finally blocks. Added Collect-LegacyGameEvidence.ps1 with SDK selection/local bootstrap and metadata-only ZIP. Shared native stdout/stderr process capture moved into Common.ps1. The old inspector remains for catalog/history; the new route is documented as preferred for Dungeons 1.

Actual local direct-Roslyn compilation and UE4.22 tests passed: known LetMeMove actor 34 properties, 2 functions, zero metadata errors; generated diagnostic pak manager 50 properties/2 functions and sidecar 35 properties/2 functions, zero errors. Parser handles legacy property types/flags and serializes Kismet. Existing collector regressions, syntax/JSON validation and diff checks passed. Windows CI now builds/runs the complete new collector on the actual checksum-pinned LetMeMove pak, asserts property/function/Kismet output, unchanged input hash and absence of raw assets in the ZIP.

This resolves the demonstrated parser gap, not game-runtime behavior. Next input is a real legacy collector export of the 31 game UI/controller assets; then confirm native call layouts/identity and implement the documented runtime features.

Windows end-to-end verification passed on PR #6 head `e09c4d9`: Project Validation run [37212879318](https://github.com/Stoffe101/Minecraft-Dungeons-QoL/actions/runs/37212879318), including the actual legacy pak collector under Windows PowerShell 5.1. Cooked QoL Diagnostic Build [37212879310](https://github.com/Stoffe101/Minecraft-Dungeons-QoL/actions/runs/37212879310) also passed. No actual Dungeons runtime or game package metadata is available here yet.

## 2026-10-04 — Apply successful installed-game metadata

Analyzed user legacy ZIP SHA-256 `20b5f51b085b3c8b80f285e14896d26e7c677e4b43a82ca917f431e35de31545`. All 31 targeted packages completed without errors: 5,298 properties, 824 functions. Recorded necessary call shapes/fields in GAME_API_CONTRACTS.md; no raw game assets or full disassembly committed. Salvage returns ItemSalvageUndoInfo and writes a separate success bool; GetEquipmentSlots returns a map. ItemId/type comparison and GetChangeIndex do not prove persistent identity.

Applied the real controller SharedUI/InventoryHUD chain, WidgetBlueprintGeneratedClass import kind, open-inventory gating with transient state cancellation, controller GetItemStashComponent and final native GetInventorySlots. Added explicit six-slot equipped-object exclusion to selection and preview, with unresolved UI/native slots rejected. Replaced the unestablished InventoryItem.GetTotalInvestedEnchantmentPoints instance call with vanilla GetSalvageInfo(Item).enchantmentPoints. Fingerprint grouping remains diagnostic, not a physical-item lock.

Expanded graph validation with control-flow reachability/dominance of the inventory-open gate and two explicit equipment comparisons. Negative tests preserve bytecode sizes for missing/open-bypass/equipment cases and reject the unsupported instance call, alongside existing jump/context/mutation tests. Local direct-Roslyn compile and UAssetAPI write/re-open passed; complete Windows packaging/checks run through the PR workflows before merge. No in-game execution is available in this environment.

Checked UE4SS primary upstream issues #1211/#1219: the retail Dungeons UE4.22.3 startup-crash report remains open; a resolved custom debug-build report is insufficient compatibility evidence for Store/Xbox. Retain Blueprint Loader as the implemented route. Next: validate the revised diagnostic in game, resolve persistent hero/item identity and supported equip, then implement UI/controller/loadouts/vanilla salvage guard and native batch. The supplied metadata is sufficient for this pass; no duplicate targeted export requested.

Updated the canonical editor mirror with the observed item display/power/CanSalvage and stash GetSalvageInfo call shapes, using harmless project-authored stub bodies. A real UE4.22 editor compile and native function flag/const certification remain unavailable; this is documented rather than presented as tested game code.

Windows verification passed on PR #7 head `11c64ed`: [Project Validation 37214308252](https://github.com/Stoffe101/Minecraft-Dungeons-QoL/actions/runs/37214308252) and [Cooked QoL Diagnostic Build 37214308346](https://github.com/Stoffe101/Minecraft-Dungeons-QoL/actions/runs/37214308346), including all 13 graph rejection tests and namespace/sidecar packaging. Downloaded and independently re-read the actual CI pak (SHA-256 `2426799e71c5eaaae675f7aa3e667a22e4b6c730ab8ebd47d1b2db9d4c5d466d`): manager 58 properties/2 functions and sidecar 35 properties/2 functions, zero metadata errors. Its BUILD_INFO identifies the Actions synthetic merge checkout `00f9453`, not a completed in-game test.

## 2026-10-04 — Character-selection crash / cooked preload repair

User installed Blueprint-Loader.pak and the 51 KB diagnostic, then reported a crash at profile selection. Inspected both attached screenshots, crash XML and minidump locally; raw dumps remain private/ignored and are not committed. Retail executable reports UE4.22.3 and Store Lovika 1.17.0.0. Exception 0xc0000005 writes 0x28, captured instruction `mov [rax+0x28], rdi` with rax/rdi zero. Nearby captured instructions reconstruct a linked child list from a serialized array; the observed list length is one. This supports a reflected-field load failure, but no PDB/heap payload exists to name the exact struct or prove the sole root cause.

Independent asset audit demonstrated stale preload graphs: manager Children includes newly added fields absent from its/function's serialization dependencies; new properties have cleared outer/type/inner dependencies. SaveGame conversion retained 38 actor exports and actor preload/CDO edges. Fixed generation: repair child/outer/inner/type ordering and DependsMap; compact sidecar to four exports with remapped indices, cleared actor data/registry tags and regenerated dependencies. Copy native-bool metadata from the reviewed donor.

Local manager and four-export sidecar write/re-open passed. All 16 negative graph/dependency tests passed, including missing child preload, field outer creation and array-inner serialization edges. Complete Windows build/packaging must pass before the candidate is offered. No runtime crash-resolution claim is made. User should remove the old QoL pak; no save modification or executable-permission change is required. Next test is profile selection with only Blueprint Loader and the replacement candidate, followed by inventory input if login succeeds.

Candidate head `860afe6` passed [Project Validation 37215913101](https://github.com/Stoffe101/Minecraft-Dungeons-QoL/actions/runs/37215913101) and [Cooked QoL Diagnostic Build 37215913121](https://github.com/Stoffe101/Minecraft-Dungeons-QoL/actions/runs/37215913121). Downloaded the actual CI pak and verified SHA-256 `8bb54780143f00712b8f1ce97bc3aa42a3fa3392299cd160b2cd3e40793b9bed`; metadata re-read: manager 58 properties/2 functions, compact sidecar 2 properties/0 functions, no errors. All 16 regression tests pass. This remains a crash-fix candidate awaiting the user’s profile-selection test.


## 2026-10-04 — Second character-selection crash / archetype repair

The user retried the PR #8 replacement and crashed at character selection again. It is withdrawn alongside PR #7. The new dump has a write access violation (`0xc0000005`, address `0x28`) at module offset `0x11d836e`, 30 bytes before the first failure in the same child-list reconstruction sequence. Captured registers: `r10=43` (child count), `r8=33` (loop counter after increment), `rcx=0` (previous child), `rax=0` (next child). In that loop, counter 33 corresponds to linking children at zero-based indices 31 and 32. Our manager ubergraph has exactly 43 children: 31 original locals plus 12 additions, with `MCDQoL_CurrentSlot` at index 31 and `MCDQoL_CurrentItem` at index 32. This is strong structural evidence for the added manager fields failing creation. It is still an inference: the minidump has no captured object heap or symbols to identify the struct directly.

Every added PropertyExport had `TemplateIndex=0`; all original template property exports use the native property's CDO. The package reader accepted this invalid creation metadata. UAssetAPI's own Export docs describe zero TemplateIndex as a problem. Epic's [FObjectExport.TemplateIndex documentation](https://dev.epicgames.com/documentation/unreal-engine/API/Runtime/CoreUObject/UObject/FObjectExport/TemplateIndex?application_version=5.5) describes it as the cooked loader archetype reference and says zero is an invalid cook-time condition. That is corroborating documentation from a newer engine, not a UE4.22 loader simulation. The actual UE4.22 template supplies the version-specific comparison.

The shared dependency repair now resolves a correctly typed `/Script/CoreUObject` default property import for every property export, including donors converted from ObjectProperty to ArrayProperty/NameProperty. It adds native property class and archetype serialization-before-create edges. Validation rejects missing/wrong archetypes and missing creation preloads before write and after re-open. Three negative tests extend the gate from 16 to 19. Existing class/outer/inner/child preloads and SaveGame cleanup remain.

A separate CookedLoadProbe strips both manager function bodies to immediate empty returns, retains the reflected children and imports, validates the written result, and packages a separate `MinecraftDungeonsQoL-load-probe` artifact. It performs no hotkey processing, inventory calls, save creation/writes or salvage. This provides the next runtime gate: character selection and entering camp with the repaired schema loaded. The full diagnostic must not be installed alongside it, since both use the same package paths. A passing probe would validate this loading path only; actual inventory behavior would still need a separate diagnostic test.

The supplied `debug.log` is 210 bytes of Chromium GPU initialization errors dated `0511`, not a Dungeons gameplay log. The screenshot gives no symbolized call stack. The supplied CrashReportClient.pak could not be listed by the legacy Mod Kit packager (unsupported magic). A subsequent read-only UE4 v8 index parse verified the index SHA-1 and all 2,959 entries; the catalog includes CrashReportClient configuration and no `.log` files. It is not the missing gameplay log. Raw dumps, account identifiers and uploaded binaries remain private and uncommitted.


Validation outcome: PR #9 implementation head `880822288ae30780a1c94a3e9439cc10e7b02953` passed Windows [Project Validation](https://github.com/Stoffe101/Minecraft-Dungeons-QoL/actions/runs/37218105686) and [Cooked QoL Diagnostic Build](https://github.com/Stoffe101/Minecraft-Dungeons-QoL/actions/runs/37218105709). The build log confirms all 19 rejection tests, SaveGame regeneration, namespace relocation and load-probe write/re-open validation. The downloaded [load-probe artifact](https://github.com/Stoffe101/Minecraft-Dungeons-QoL/actions/runs/37218105709/artifacts/11308194650) was unpacked and its actual manager/sidecar reopened: 65 manager exports, both functions exactly empty return + EndOfScript, four sidecar exports, valid property archetypes and preload edges. Pak SHA-256: `b4f8939497edb72866761cadc4218eddcc1fb1024eef10562171c827284d48de`; BUILD_INFO records Actions synthetic merge commit `2c84c5a57b3bd77ec5b7be87f53fb41f64706b2e`. No game retry yet; the crash remains unresolved from the user's perspective.


## 2026-10-04 — Loading probe succeeds; existing-mod UI research applied

User confirmed the PR #9 loading probe reaches camp with no crash. Record this as a narrow success: repaired cooked schema/entry maps load on this Store game, with both event bodies disabled. The prior diagnosis is now supported by a real retry, but full feature code remains untested.

Reviewed [Camera Coordinates Overlay's source README](https://github.com/EvenTorset/Camera-Coordinates-Overlay): a Blueprint Loader level starts WidgetAdder, which adds a UMG widget unless already present. Its font assets are references excluded from packaging. Reviewed [Dungeons GUI X](https://www.curseforge.com/minecraft-dungeons/mods/dungeons-gui-x) and downloaded its published file 6857197 from the associated ForgeCDN distribution for local inspection only. ZIP SHA-256 `31facb0e33446ae0f6c73d213d27784f1e2166d033c396ca244cf0f556a6d116`. Its cooked WidgetAdder parses under UE4.22 with six properties/two functions/zero errors and imports GetPlayerController, Delay, Create and AddToViewport. The listing marks it AGPLv3/alpha, targeting 1.10.3.0; it does not establish support on this user's current game. No GUI X binary, copied graph or code is included in our mod or repository.

Applied the general actor/UMG pattern with project-authored code, using native TextBlock creation and the installed game's already verified InventoryHUD.WholeCanvas field rather than requiring another cooked widget template. The collected HUD metadata declares WholeCanvas as CanvasPanel and uses AddChildToCanvas; item name/power and inventory retrieval are the previously observed contracts. Epic's [SpawnObject](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Runtime/Engine/Kismet/UGameplayStatics/SpawnObject?application_version=4.27), [AddChildToCanvas](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Runtime/UMG/Components/UCanvasPanel/AddChildToCanvas?application_version=4.27), [SetText](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Runtime/UMG/Components/UTextBlock/SetText?application_version=4.27) and [SetAnchors](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Runtime/UMG/Components/UCanvasPanelSlot/SetAnchors?application_version=4.27) document the native types; these newer UE4.27 docs corroborate shapes, not actual UE4.22 game execution.

CookedInventoryProbe emits only inventory-open-gated native reads and F6/F7 browsing plus feedback in its own TextBlock. It checks manager ownership before UI creation, retains its own widget/canvas references, recreates on canvas replacement, hides when closed, and updates text only when it changes. The widget has HitTestInvisible visibility and bottom-center anchors; it should not capture mouse/controller input. No packaged game fonts/images/assets are copied. Raw research stays ignored.

The build produces a separate inventory-probe artifact. Seven rejection tests cover save-call insertion, feature-hotkey insertion, missing/bypassed inventory-open gate, invalid jump targets and invalid context skips. The shared validator also gates SpawnObject/AddChildToCanvas behind inventory open. This gives visible evidence before enabling lock/select/preview logic, since PrintString output is not a reliable retail UI contract. Layout, native widget construction, actual slot browsing, transitions and co-op remain in-game test gates. All promised production features remain on the roadmap.

A final installed-game import cross-check before distributing the probe found that its Anchors struct must resolve under `/Script/Slate`. Corrected that owner and added a seventh negative test, since a successful offline package parse cannot prove native import resolution. The first PR #10 implementation build (`8b267b2`) passed Windows build/25 tests but is superseded and must not be offered for game testing. The corrected build will be validated before delivery.

Corrected PR #10 implementation head `50c93b9572758ba44e352cb1b3e5f9112e298e86` passed Windows [Project Validation](https://github.com/Stoffe101/Minecraft-Dungeons-QoL/actions/runs/37224203993) and [Cooked QoL Diagnostic Build](https://github.com/Stoffe101/Minecraft-Dungeons-QoL/actions/runs/37224204144). Logs confirm all 19 original tests plus seven inventory-probe tests, full generation and packaging. Downloaded the corrected [inventory-probe artifact](https://github.com/Stoffe101/Minecraft-Dungeons-QoL/actions/runs/37224204144/artifacts/11311178657), unpacked its actual pak, and reran all seven tests against that packaged manager successfully. Pak SHA-256 `de14e92d1e2aba554dd7545a02bc308b92345a8d4bae12567966fd81230199e0`; BUILD_INFO records Actions synthetic merge commit `1ce5d2ea6cb977be8efcc5d74140dedc271c2ee9`. The artifact is ready for the inventory UI/native-read test; runtime rendering, browsing and transitions remain unverified.


## 2026-10-04: Silent inventory probe and cooked event entry contract

The user installed the corrected PR #10 probe and reached camp, but reports no inventory text and no visible F6/F7 response. The provided screenshot shows vanilla inventory without the probe overlay. Package loading success does not establish actor event execution or widget rendering.

Re-exported the pinned MIT LetMeMove source actor. ExecuteUbergraph starts with ComputedJump(LocalVariable EntryPoint), serialized bytecode size 10; controller lookup starts at offset 10. ReceiveTick first copies DeltaSeconds into the persistent frame, then calls ExecuteUbergraph with IntConst 10. Both our executable generators instead started the controller lookup at 0 while leaving the tick stub unchanged. This is an observed event entry mismatch, irrespective of whether the game takes the ordinary stub or event-graph fast-call path. Epic's current [UFunction API](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Runtime/CoreUObject/UFunction) documents EventGraphFunction and EventGraphCallOffset; this newer API corroborates the concept, not UE4.22 binary behavior. UAssetAPI FunctionExport reads function flags then retains unparsed trailing bytes, so preserving the original entry position also avoids assuming that changing the visible stub alone updates every event dispatch representation.

Restored the original 10-byte EntryPoint dispatch prefix in both CookedQoLPatcher and CookedInventoryProbe before computing explicit jump offsets. The existing ReceiveTick and original function trailing bytes remain intact. Shared validation now rejects a missing/wrong dispatcher, incorrectly owned EntryPoint, wrong prefix size, wrong tick graph target or non-10 tick argument. Three negative probe tests exercise missing dispatcher, wrong argument and wrong target. Local write/reopen generation and all 29 existing/new rejection cases pass. The empty-event loading probe still strips both event bodies, so it intentionally has no executable entry contract.

This candidate repair still needs retail-game confirmation. No production feature completion is claimed; physical item locks, salvage interception, loadouts and reviewed salvage remain incomplete. No private game assets or screenshots are committed.


PR #11 implementation head `702fe877102f92cc2b8c0b41af5b2d4cbcbc600e` passed Windows [Project Validation](https://github.com/Stoffe101/Minecraft-Dungeons-QoL/actions/runs/37227010202) and [Cooked QoL Diagnostic Build](https://github.com/Stoffe101/Minecraft-Dungeons-QoL/actions/runs/37227010213). Build logs confirm 19 diagnostic plus 10 probe rejection tests. Downloaded the [replacement inventory-probe artifact](https://github.com/Stoffe101/Minecraft-Dungeons-QoL/actions/runs/37227010213/artifacts/11311864686), unpacked its actual pak, and reran all 10 probe tests successfully. Pak SHA-256 `238f511a4b9edd1e734e5f86459eeb56805905d8af77e90323976216011bb593`; BUILD_INFO records synthetic merge commit `50f23146fa299aa54f0122ee6e914724ae460fda`. Runtime retry remains pending.


## 2026-10-04: Second silent probe; offline inventory pauses the actor

The user reports the PR #11 replacement still produces no text or visible F6/F7 response and asks whether activation is needed. Successfully viewed the two attached screenshots: full inventory without the overlay; Explorer shows the expected ~mods directory, Blueprint Loader and exactly one visible QoL inventory probe (plus Unlock-All-Cosmetics). No activation setting is required. These images do not prove installed content hash or actor event execution. The previous entry-layout repair remains a structural fix but did not resolve the user-visible symptom.

Inspected the actual PR #11 pak's manager class default object: PrimaryActorTick contains only bCanEverTick=true, no paused-tick override. Cross-checked the supplied installed-game BP_PlayerController metadata. UIToggleInventory toggles SharedUI.InventoryHUD, reads IsVisible, IsGamePaused and OnlineUtil.IsOnlineSession, computes visible AND not-online AND not-paused, and calls GameplayStatics.SetGamePaused(Self,true) at statement 909. This is direct local evidence that the normal offline inventory path pauses the game. Our only feedback/input/read path was gated on inventory open, while the donor movement actor's tick lifecycle was intended for unpaused gameplay.

Epic's [FTickFunction API](https://dev.epicgames.com/documentation/unreal-engine/API/Runtime/Engine/FTickFunction) identifies bTickEvenWhenPaused as the pause execution flag and TickInterval as the execution interval. [Actor Ticking](https://dev.epicgames.com/documentation/unreal-engine/actor-ticking-in-unreal-engine) shows configuring these through PrimaryActorTick before registration. These current engine docs corroborate general semantics; the game-specific pause call and donor defaults come from local UE4.22 cooked assets. Reviewed [Blueprint Loader's author description](https://www.curseforge.com/minecraft-dungeons/mods/blueprint-loader): trigger folders remain /Game/BPLoader/Menu, Lobby and Ingame; no extra user activation step is documented. The source loader maps contain no PrimaryActorTick instance override.

Both executable generators now configure their own existing ActorTickFunction class-default struct with bCanEverTick, bStartWithTickEnabled and bTickEvenWhenPaused true, TickInterval zero. This avoids depending on a first unpaused tick to call SetTickableWhenPaused. No world unpause, player-controller mutation, item mutation or new game asset is included. Shared validation requires these defaults after reopening; five negative tests reject false tick flags, missing pause override and positive interval. Local compile/generation/write/reopen, 19 diagnostic and 15 probe rejection tests pass. The emitted CDO was independently inspected: all three flags true, interval zero. Full production feature work remains pending the runtime gate.


Before distribution, cross-checked the open-state field beyond its declaration: the collected HUD graph contains only the IsInventoryOpenCall getter, with no assignment to IsInventoryOpen. The game controller's actual UIToggleInventory logic instead calls native Widget.IsVisible on SharedUI.InventoryHUD before/after ToggleWidget. Switched both executable generators to that observed visibility contract. Field presence/getter shape alone was insufficient to establish current open-state behavior. The visibility import is a Function owned by native /Script/UMG.Widget in both controller/HUD metadata. Shared validation checks this exact owner/module and the resolved HUD-local receiver. Two additional same-size mutation tests reject a wrong receiver and wrong function owner; all 36 tests pass (19 diagnostic, 17 probe). The intermediate pause-only head `18497fe7861c9b2a182044ecf680d03f92511a38` passed Windows CI but is superseded before delivery by this combined pause/visibility repair.


Combined PR #12 implementation head `dba3341c103e575a0da186051cb5d3a36618c648` passed Windows [Project Validation](https://github.com/Stoffe101/Minecraft-Dungeons-QoL/actions/runs/37236710991) and [Cooked QoL Diagnostic Build](https://github.com/Stoffe101/Minecraft-Dungeons-QoL/actions/runs/37236710957). Logs confirm all 36 tests. Downloaded the [paused-UI inventory probe](https://github.com/Stoffe101/Minecraft-Dungeons-QoL/actions/runs/37236710957/artifacts/11316036639), unpacked the actual pak and reran all 17 probe rejection tests successfully. Independently inspected the packaged CDO: all three tick flags true, TickInterval zero. Pak SHA-256 `ff170d3e5c9e7c2e189be8a02cd878643e6804e940cc5379320888fe51f796b9`; BUILD_INFO records synthetic merge commit `4188077fe3c873cdeca122b7a272f2e84ffecd54`. The probe needs an in-game retry; no native rendering/input/production feature success is claimed.

## 2026-10-04: online inventory crash, native calls and client bootstrap

Inspected the supplied screenshot, CrashContext and minidump locally. Private account data, dumps and game assets remain outside git. debug(1).log contains Chromium GPU initialization messages, not the gameplay call stack. The exception is 0xc0000005 reading 0x98; Dungeons-relative instruction offset 0x1237080 executes `testl $0x400,0x98(%r9)` with r9=0. This differs from the earlier class initialization crashes and strongly suggests a null UFunction at script dispatch. The dump lacks the UObject/function/script heap needed to identify which import failed.

Removed the optional TextLayoutWidget.SetJustification/SetAutoWrapText calls rather than replacing them with another unproven owner. Current Unreal documentation is not proof of a reflected function in retail UE4.22. Supplied 008_UMG_InventoryItemInspectInfo metadata verifies native InventoryItem.GetDisplayNameText; HUD metadata verifies GetDisplayItemPowerInt. Explicit native FinalFunction calls now preserve those owners. UI validation checks owner, module and argument count.

Downloaded the official Blueprint Loader archive (CurseForge file 3385182), SHA-256 5f6dc432674b951c917a0cfb718793188bdcd223f698cccb4bfca52e7052bcd6, for inspection only. Its widget/tent exports decoded with zero parser errors. The widget selects Menu/Lobby/Ingame GameModes via GameplayStatics.GetGameMode before loading the matching trigger folders. All failed casts end the execution path. Unreal's documented contract returns null on clients; thus joining a friend likely prevents loader startup. This is an architectural inference, not a tested Dungeons network result. Host and joining client require separate tests. No dependency assets or implementation were copied.

Both manager generators now require native Controller.IsLocalPlayerController and set bReplicates=false. Graph reachability checks reject inventory/input/UI creation before the guard or on its false branch. These checks constrain local execution, but cannot make the existing loader start on a joining client.

Primary references:
- https://dev.epicgames.com/documentation/unreal-engine/API/Runtime/Engine/UGameplayStatics (GetGameMode client behavior; GetPlayerController network distinctions).
- https://dev.epicgames.com/documentation/unreal-engine/API/Runtime/Engine/AController/IsLocalPlayerController
- https://www.curseforge.com/minecraft-dungeons/mods/blueprint-loader/files/3385182
- https://www.nexusmods.com/minecraftdungeons/mods/111?tab=description (modification and asset use require author permission; uploads to other sites forbidden).

Local verification: 19 diagnostic plus 23 inventory-probe rejection tests passed after generating both cooked graphs. Runtime crash repair, visible text, native item reads and all online/co-op paths remain unverified.

## PR #13 packaged validation (2026-10-04)

Implementation head: 845fece40161c1e3b371b2026d069663f4d907ac. Windows Cooked QoL Diagnostic Build run 37238191986 passed all 19 diagnostic and 23 probe regression checks; Project Validation run 37238191980 passed.

Downloaded artifact 11315778743, verified pak SHA-256 `db2e73dc8bf2cfb9634b1f0765a87c7a9c8d6563f56ab3e8a664ead05e715efb`, integrity-unpacked it and reran all 23 probe rejection tests against the packaged manager. Inspected its CDO: bReplicates=false; bCanEverTick, bStartWithTickEnabled and bTickEvenWhenPaused=true; TickInterval=0. BUILD_INFO records CI synthetic merge dc726c4c6d973cce03e5ba9909b4ff7e63202bcf.

Candidate download: https://github.com/Stoffe101/Minecraft-Dungeons-QoL/actions/runs/37238191986/artifacts/11315778743

This validates packaging and structure only. Inventory-open crash repair, visible overlay/input and online host/join compatibility require retail tests. PR #12 remains withdrawn.

## 2026-10-05: PR #13 inventory crash and VM source investigation

PR #13 still crashes opening inventory online and is withdrawn. See INVENTORY_CRASH_INVESTIGATION.md for the new dump, UE4.22.3 source trace and typed-local repair. Both generators had unsafe nested native reference arguments. Their new scalar locals and result properties match compiler-style game/mod output; the shared validator now rejects the actual old package. Local generation and 47 regression checks pass. Runtime confirmation is pending.

MODDING_OPTIONS_REVIEW.md records the expanded project/license review, existing MIT reuse and UE4SS as a potential live diagnostic/client bootstrap route with unverified Store compatibility. Required multiplayer is online hosting/joining friends, not splitscreen. No ready-made licensed complete implementation was established.

## 2026-10-05: PR #14 retail read-only milestone

The user confirmed visible text and working F6/F7 browsing with correct armor/weapon names. Viewed the supplied screenshot successfully: 216 slots, slot 0 Ghostly Armor, power 163. The typed-local repair now has user-reported runtime success for the inventory-read path. This strengthens the VM defect diagnosis but does not prove the precise earlier crash site.

Documented the next feature increment: individual session locks/favorites, multi-selection and review preview, followed by stable persistent identity and audited native salvage. Duplicate name/power fingerprints must not become per-item persistent keys. No runtime code changed or tests rerun for this documentation-only pass. Host/join-friend role and travel/close behavior remain unverified.

## 2026-10-04: Collect original UI sources for favorites and salvage guards

The user approved favorites that forbid all salvage until unfavorited, multiselect and Select All excluding protected items, and offered any required files. Re-read the supplied inspector metadata: CanSalavage gates ordinary salvage; SalvageSlot calls native SalvageItemInSlot with undo-info return and separate success out parameter, then preserves the vanilla OnItemSalvaged delegate. Only metadata exists locally, so a faithful vanilla-widget patch cannot yet be built. Added an explicit seven-package private cooked-source mode to the existing pinned legacy collector, with companion hashes, missing-package reporting and no arbitrary match override. Default collection remains metadata-only. Documented the exact command, guard requirements and missing implementation in FAVORITES_SALVAGE_IMPLEMENTATION.md. No destructive runtime feature was enabled.

Direct C# compilation and PowerShell/repository validation passed. A synthetic licensed actor fixture under seven required UI paths plus an unrelated path yielded precisely 14 original uasset/uexp files with matching hashes; the unrelated package was excluded. A fixture without UI sources failed and reported all seven missing packages. Windows CI extends the existing real legacy-pak test with source-mode exclusion, missing-source and unchanged-input checks. These checks do not establish runtime UI compatibility or online support.

## 2026-10-05: Correct patch-source command to include the documented archive key

The user left the AES prompt empty and PowerShell rejected the missing -AesKey argument before collection ran. The previous response incorrectly assumed the user knew the key; GAME_EVIDENCE.md already contains the public Dungeons 1 archive key confirmed by the earlier successful catalog collection. Replaced the prompt-based command with the complete explicit-key command and explained that this is an archive decryption key, not an account password. Documentation-only correction; no runtime or collector code changed, so no new tests are required. Next: receive the seven-package source ZIP and implement the audited vanilla guard.

## 2026-10-05: Original UI source integration and private favorites/selection test

Received source ZIP SHA-256 1e7b7a4595afb4d3e2ba8869f81ab3b07d68ad32990c90c36c2d2c3aa0ea9d82; verified all fourteen uasset/uexp hashes and retained game assets under ignored private research only. User confirms keyboard/mouse, hosted read-probe success, desired joining-player support, personal use. Audited HUD Tick and OnItemSalvaged: the existing delegate carries native undo info, refreshes/purges by item and clears selection. This supports guarded native-slot batch salvage without requiring a visible widget for every filtered item.

Added CookedInventoryFeatures and Build-InventoryFeatures.ps1. Patch original HUD Tick (owning local player, visible UI; no GameMode/loader) and open/close cancellation; normal highlighted item; session per-object favorites; queue/remove/Select All; two-step review/confirmation; revalidate expected physical item, native inventory membership, favorites, CanSalvage and six equipment widgets. Guards precede both vanilla eligibility and mutation. Optional native inspector helper preserves mission eligibility, typed undo struct, bool out success and successful OnItemSalvaged notifications. Default test build disables batch deletion; native implementation remains unverified in-game. Session-only references are explicitly not persistent item identity.

Both generator modes compiled and reopened against actual supplied assets. Validation checks branch targets, typed text/reference arguments, favorite early-return guards and native out/delegate contracts. All 11,152 original exports' identifying metadata, properties, opaque buffers and widget data are preserved. Only four existing functions gain guard/event prefixes; after relocation their original bodies remain identical and other 232 function bodies are unchanged. Replaced broad all-import dependency repair with append-only new-field/reference dependencies for original widgets. PowerShell/repository validation passed. Windows CI builds the new source tool; game-derived packages are not uploaded to GitHub CI. Next: default patch runtime test, native batch test, persistent identity, host/join verification and loadouts.

## Inventory feature crash response and mouse controls (2026-10-05)

The private `InventoryFeaturesTest-v1` crashed when opening inventory; it is withdrawn as a usable feature build. Both supplied crash reports have the same leading stack and both dumps fault at `Dungeons.exe+0x1237080`, reading address `0x98`. Captured machine code executes `testl $0x400,0x98(%r9)` with `r9=0`: a null function pointer at Blueprint dispatch, not the earlier FText ABI crash.

A concrete invalid import was found in both new helpers: `/Script/UMG.UserWidget.GetOwningPlayer`. In the pinned UE 4.22.3 headers the reflected declaration belongs to **Widget**; the original game HUD calls the function by name rather than importing it from UserWidget. The patch now uses `/Script/UMG.Widget.GetOwningPlayer`. Inspector Blueprint calls use the game's observed local virtual dispatch pattern. This corrects a demonstrated import error consistent with the crash; the dump does not contain enough heap memory to identify the exact failing script expression. Runtime repair remains unconfirmed.

The new UI uses clickable **Favorite / Lock**, **Multi salvage**, **Select item**, **Select All**, **Review / Confirm**, and **Cancel / Clear** buttons. Multi salvage toggles selection mode; in that mode the original HUD `SlotClicked(Source)` event records the physical clicked slot, and the next HUD Tick checks eligibility and toggles its queue membership. Click an item again to deselect. Select item toggles the normally highlighted item without enabling the mode. Favorite toggles protection; favoriting removes the item from selection. The status area distinguishes the highlighted favorite/selected item and shows counts and mode. Buttons bind their native `Button.OnClicked` delegates to zero-parameter functions on the same HUD. Favorite/edit controls are disabled during a running batch; Cancel remains enabled. Closing inventory or Escape clears all pending requests, selection, snapshots and confirmation.

### Packaging decision

Do not install independent favorites and salvage paks that both replace the same HUD/inspector: the later-mounted asset replaces the entire earlier asset, including its guard. Use a **shared protected inventory core with selectable features** instead. The build supports `-FavoritesOnly` (one visible button and no slot-click interception) and the combined selection build. They are alternative packages: install exactly one. Both guard vanilla salvage using the same physical-item favorites, and combined/native salvage shares that protection. Future unrelated features can be separate paks if their assets do not conflict. A standalone salvage module without favorites protection is deliberately not provided.

The two downloadable test alternatives disable batch deletion. The native implementation is built/validated separately with `-EnableNativeSalvage`; it is not certified by these checks and is not included in the next test download. First verify inventory opening, mouse interaction, normal salvage refusal for favorites, duplicate-item independence, reopen and Select All. Then verify native salvage and online host/join behavior. Favorites remain inspector-lifetime state; restart/travel/rejoin persistence and full loadouts are unfinished.

### Verification

Compiled all three variants against pinned UAssetAPI 1.1.0 and UE 4.22 parsing. Each was written and reopened, all 11,152 original exports preserved, with original function changes limited to guard/callback prefixes and relocated jumps. Favorites-only leaves `SlotClicked` unchanged. Added declaring-owner regression coverage (seven cases, including the rejected UserWidget import and inherited Button.SetContent import). Existing 28 inventory-probe and 19 diagnostic negative tests pass. Button delegate handlers must exist in the owner function map and take zero parameters; native vs preview salvage call/delegate counts are checked. Repository/PowerShell syntax validation passes. These are structural/source checks; no game or Unreal Editor is available here.

### Primary source contracts checked

Research used the engine version from the crash reports, not current UE5 API owner names. Epic-authored UE 4.22.3 headers were read privately from the existing pinned mirror `folgerwang/UnrealEngine` at `99a530d4ccbe6bea1e8f49df20acfeb294006962`:

- [Widget.h](https://github.com/folgerwang/UnrealEngine/blob/99a530d4ccbe6bea1e8f49df20acfeb294006962/Engine/Source/Runtime/UMG/Public/Components/Widget.h): reflected GetOwningPlayer is declared by Widget.
- [Button.h](https://github.com/folgerwang/UnrealEngine/blob/99a530d4ccbe6bea1e8f49df20acfeb294006962/Engine/Source/Runtime/UMG/Public/Components/Button.h): BlueprintAssignable OnClicked; header explicitly says IsPressed is not a click detector.
- [ContentWidget.h](https://github.com/folgerwang/UnrealEngine/blob/99a530d4ccbe6bea1e8f49df20acfeb294006962/Engine/Source/Runtime/UMG/Public/Components/ContentWidget.h): reflected SetContent is declared by ContentWidget, not Button.

Original HUD BindDelegate/AddMulticastDelegate expressions provided the delegate binding pattern. Engine implementation/header files are research inputs only and are not copied into this repository or private release.

## 2026-10-05: Confidence audit after the user's request for certainty

No retail confirmation exists for the mouse-control candidate. Continued auditing identified two logic defects: mode exit cleared the queue, and requests stored only a slot (or read the later highlight) rather than the item identity at the click. Added shared generated action capture/resolver functions; both mouse-button and original slot-click requests now hold slot/item pairs and refuse replacements before applying the action. Exiting mode retains selected items, cancels armed review, and cannot run during salvage. Cancel still clears all pending captures and snapshots.

Examined Epic-authored [UE 4.22.3 Class.cpp](https://github.com/folgerwang/UnrealEngine/blob/99a530d4ccbe6bea1e8f49df20acfeb294006962/Engine/Source/Runtime/CoreUObject/Private/UObject/Class.cpp) through the GitHub connector at the same pinned commit. `UFunction::InitializeDerivedMembers` stops its parameter scan at locals and uses `FUNC_HasDefaults` for non-zero-constructible local initialization; `GetReturnProperty` also requires a parameter prefix. The generator lacked the initialization flag. New functions with locals now set it, and return/parameter fields must precede locals. A resolver introduced during this pass initially placed its return after a local; corrected that before shipping and added a regression rejection. Existing original function flags/layouts remain untouched.

Added 17 generated-action execution checks and 3 function-layout rejection checks. Their restricted interpreter uses synthetic object fields and mocked native IsValid/object-equality/bool operations, and evaluates function arguments in the caller frame as the reviewed ScriptCore.cpp does. It explicitly fails on unsupported expressions. A temporary interpreter context bug was found by its first test, corrected, and rerun; no runtime result is inferred from it. All 20 checks pass on both licensed actor fixtures; the existing 7 owner checks, 28 probe rejection cases and 19 diagnostic rejection cases also pass. All three private UI variants compile/write/reopen with 11,152 original exports preserved; PowerShell/repository checks pass. Game-owned binaries remain private. Retail startup, mouse click/guard tests and online host/join are still required.

## 2026-10-06: recovered full catalog and exact profile targets

Recovered the earlier private game-evidence-4.zip (SHA-256 89fad1870529cd4688f5a6253e1a4a5104dad1bebe99e25cd2b7b8d27d7b5bc0), which contains the 131,164-path AssetList previously recorded in CURRENT_STATE. It identifies UI/Character/UMG_CharacterPicker, UMG_CharacterOptions, UMG_PlayerCharacterPickers, UICharacterDataBind, counters/cloud picker, Menu/UMG_SwitchProfile, GameModes/Menu/BP_3DPlayerCharacterSlot variants and BP_PlayerCharacter. Previous CharacterSelection/CharacterSelect/CharacterProfile filename filters missed these actual names. Catalog presence proves filenames, not native member signatures or persistent IDs.

Drive raw fetch explicitly rejects pakchunk0 (1,191,543,250 bytes) with HTTP 413 because its download limit is 268,435,456 bytes. Chunk4 (1,324,896,477 bytes) also exceeds that stated limit; no request/download of chunk4 is claimed. Existing catalog recovery avoids another catalog request. Do not bypass authenticated access or reinstall the withdrawn native loader.

Added Collect-FavoritesProfileEvidence.ps1 and --profile-evidence to the existing pinned legacy exporter. It reads exactly 26 catalog-observed packages, records every missing package, and outputs legacy properties/imports/Kismet only. No raw patch sources, loader, executable memory or character saves are collected or changed. Public archive key is supplied automatically. Collection can run with the game closed and produces a private .research/profile-evidence-TIMESTAMP.zip. Partial failures retain diagnostic metadata; missing packages do not certify an API. Native signatures/identity remain unresolved until these call sites are inspected.

Local direct Roslyn build against the pinned inspector libraries passed. Negative actual UE4.22 LetMeMove pak test selects zero and records all 26 missing targets. Positive temporary fixture places the same reference actor at one allowlisted path and a similarly named Backup path: exactly one selected/completed, 34 properties/2 functions/zero export errors, 25 missing targets. This tests exact selection and legacy parsing, not retail character UI or persistence. PowerShell/JSON/repository validation passes. Windows workflow adds negative selection, missing-target completeness, input hash preservation and metadata-only ZIP checks. No new gameplay pak or completed persistence fix is released by this pass.

Next: inspect profile call sites for a supported local hero identifier and item serialization/locator access, then implement the versioned sidecar/reconciliation with explicit-unfavorite semantics. If those members are native-only, archive call sites alone cannot establish a safe bridge. Runtime travel/restart/hero-switch/duplicates/storage/upgrades and online host/join remain required.
