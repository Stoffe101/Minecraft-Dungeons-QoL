# Read-only native favorites evidence

> 2026-10-07 update: requested serialization capture and private character save received and inspected. No further collector run is requested. Source-only saved-record/journal validation is implemented; native runtime persistence remains unfinished. See [SERIALIZATION_CODE_FINDINGS.md](SERIALIZATION_CODE_FINDINGS.md).

Favorites persistence remains unfinished. Archive exports establish six profile call shapes and the salvage shape, but no permanent physical-item identifier. This project-authored experimental reader checks loaded native declarations without installing an injected loader. Keep UE4SS disabled.

## 2026-10-07: targeted code input under renewed user request

The user asked what additional input can help and authorized native tools/code reuse. The useful next input is one `-CollectSerializationCode` report from idle camp, not another default/expanded declaration scan. That existing tool records bounded native instruction snippets; it does not install a DLL or invoke/write the game. See [PERSISTENCE_NATIVE_BRIDGE_RESEARCH.md](PERSISTENCE_NATIVE_BRIDGE_RESEARCH.md) for the online source review, selected bridge route, precise missing mapping, optional closed-game save copy, and collection limitations. Earlier requests remain historical; do not repeat declaration collection.

## 2026-10-06: collection pause (historical)

No further command is requested. Completed v5/v6 declarations and the existing related-project field-layout capture have been reviewed. Repeating declaration collection will not resolve the live-item-to-save-record mapping. The serialization code collector remains an optional research tool; its previous user-run recommendation is paused. See [FAVORITES_PERSISTENCE_REASSESSMENT.md](FAVORITES_PERSISTENCE_REASSESSMENT.md).

## Available code research tool (not a current action)

The supplied v6 report completed all fourteen declarations (172 properties, 333 functions) with no issues. It confirms that CharacterSerializeComponent provides profile metadata and save-object assignment, not a reflected item-record getter/custom save-data API. BaseCharacter and EquipmentComponent do not expose durable physical-item identity either. Neither default nor expanded declaration collection needs repeating.

The optional code collector uses the same external query/read process rights; no injected loader, native calls or writes. It corroborates source-derived UFunction metadata/native-pointer layout against seven anchors before sampling nine exact functions: ItemStashComponent.SerializeSaveState/GetInventorySlots/GetStorageChestSlots, PlayerControllerBase.SaveCharacterData/GetCharacterSlotByIndex/GetAvailableSaveDataByIndex, CharacterSerializeComponent.AssignCharacter/GetCloudPlayerId and InventoryItemSlot.Swap. Named mutation methods are inspected as code only, never invoked.

Iced 1.21.0 decodes reachable x64 instructions and direct targets. Limits: 4 KiB per routine, 48 routines, roots plus only one level of direct targets, existing 2-million-call / 256 MiB / 90-second overall bounds. Code windows are reread and named roots refreshed. Only reachable instruction bytes/RVAs and necessary declaration/layout metadata are exported; no whole-image dump, instance values or account/save contents. Encoded code can contain address constants; this archive must stay private. Indirect/external calls, invalid/truncated instructions and bounded paths are explicitly reported. Completed collection is not full behavior/ABI/identity proof. Local core fixtures pass; Windows source validation passes 112 core-reader and 28 decoder checks.

## Latest result: budget exhaustion; efficient reader and diagnostic stages

The third capture (`legacy-objects-capacity-v3`) exhausted a budget and returned no declarations. Its old report does not reveal the failing stage or whether calls, bytes or time ran out. Revision `legacy-bounded-traversal-v4` reduces repeated reads and loop allocations while retaining the existing limits. Cached class/chunk metadata is scoped to a collection; chunks and seven retail contracts are refreshed before acceptance. Reports now carry constant stage names, read calls/requested bytes/elapsed milliseconds and the exact exhausted limit. No raw addresses or instance values are exported. All 102 local checks pass, including 2,048-instance traversal and changed-chunk rejection; Windows adds an own-process check. Retail performance/completion and persistence remain unverified. Update to v4 before running the camp command; another unchanged v3 run is not needed.

## Current result and object capacity correction

The second capture (`legacy-names-256-v2`) validated a unique name table, then found zero object-array candidates. Revision `legacy-objects-capacity-v3` corrects source-verified reserved-capacity filters: Unreal 4.22's default object reservation rounds to 33 chunks / 2,162,688 slots, and preallocation can allocate more chunks than the live object count needs. Previously our reader rejected both. Live traversal/read/time limits remain; reserved capacity is bounded separately and must agree with its chunk table. 96 local checks pass; Windows adds an own-process read check. Retail discovery and persistence are still unverified. Update before repeating the camp command below; an unchanged v2 capture is not needed. Detailed source evidence and the supplied report hash are in RESEARCH_LOG.md.

## First result and corrected reader

The first user capture reached discovery but found zero name-array matches; no declarations were accepted. Revision `legacy-names-256-v2` adds the 256-pointer layout defined by pinned Unreal 4.22 source, permits reserved capacity, and covers the larger header across scan boundaries. The previous collector omitted this source-defined layout. A repeat is useful only after updating to this revision; keep the working v8 gameplay pak. See the latest RESEARCH_LOG entry for evidence and source links. Retail discovery and permanent item identity remain unverified.

## Earlier camp capture request — paused

Do not run another collector on the strength of earlier instructions in this file. The code tool remains available in source, but no further run is requested during this reassessment. Keep UE4SS disabled and retain the working v8 gameplay pak. Nothing needs installing or removing for this review.

## Declaration-only implementation and limits (default / -CollectSerializationContracts)

- Attachment rights are PROCESS_QUERY_INFORMATION and PROCESS_VM_READ (0x410). APIs are OpenProcess, ReadProcessMemory and safe handle disposal. No write, injection, suspend, remote thread, driver or privilege APIs.
- Scan writable, non-executable image data only. No on-disk protected executable access. Limit image data to 64 MiB, individual reads to 1 MiB, total reads to 256 MiB, two million requests and 90 seconds. Unreadable or changing regions produce an incomplete report.
- Discover legacy names/chunked objects from bounded candidates. Verify None/ByteProperty/IntProperty, name indices, object indices/classes, owners and field chains. Traverse legacy UProperty parameters, not just modern FProperty. Ambiguous globals/layouts, cycles, partial reads and mismatches stop acceptance.
- Accept a reflection layout only if seven observed call shapes match: Guid return; three int32 getters; index → CharacterSaveData; index/bool → PlayerCharacterSaveSlot; salvage slot/out-bool/undo-struct. This establishes consistency, not a complete native ABI.
- Export eleven allowlisted `/Script/Dungeons` class/struct declarations, or fourteen with the explicit serialization option. No executable buffers, addresses, item/save values, account IDs, full object dump or complete SDK. Container/enum/subclass typing is partial; non-reflected C++ members are unavailable. A completed report does not certify GUID lifetime, clone behavior, permanent item identity or persistence.

Fixtures test twenty-four legacy layout combinations, inline 256-chunk discovery, reserved capacity and corrupt entry indices, Guid typing/export privacy, mismatch/cycle/partial read/index/count/range/budget failures. Windows CI also reads eight bytes from this test process's own allocation through the actual query/read handle. No tests attach to a game or require game assets. 111 local checks pass; Windows adds an own-process read check. The supplied v5 retail capture completed the core scope; the supplied v6 retail capture also completed the expanded scope. Native code capture completion remains unverified.

## Next gate

Inspect the report for reflected item identity/serialization access before connecting durable hero-scoped favorite save/load code. If reflection lacks item IDs, a version-checked serialization/transfer bridge is required. Name/power grouping or a save index alone must not silently substitute for physical identity. Acceptance remains missions, return to camp, restart, independent duplicates, hero switching, equipment, storage, upgrades and host/join.

Primary references: [ReadProcessMemory](https://learn.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-readprocessmemory), [access rights](https://learn.microsoft.com/en-us/windows/win32/procthread/process-security-and-access-rights), [PE sections](https://learn.microsoft.com/en-us/windows/win32/debug/pe-format). Pinned legacy source reviews are in EXTERNAL_REFLECTION_REVIEW.md. No third-party implementation is bundled.
