# Read-only native favorites evidence

Favorites persistence remains unfinished. Archive exports establish six profile call shapes and the salvage shape, but no permanent physical-item identifier. This project-authored experimental reader checks loaded native declarations without installing an injected loader. Keep UE4SS disabled.

## Current gate: focused serialization contracts

The private v5 capture completed all eleven requested declarations: 132 properties and 262 functions, with all seven native call-shape gates matching. It required 1,667,667 read calls / 49,019,979 requested bytes / 3,345 ms; there were no reported issues. Default collection does not need repeating. This validates reflection consistency, not native ABI, item identity or persistence.

`InventoryItemData` exposes eleven type/stat fields but no reflected physical-item identifier. `SerializableItemId.SerializedId` is a name identifying item type. `ItemStashComponent.SerializeSaveState` has no parameters or return data. Both the save slot and controller reference `CharacterSerializeComponent`; the slot inherits `BaseCharacter`. Revision `legacy-serialization-contracts-v6` adds an optional focused scope containing those two classes and `EquipmentComponent`, alongside the existing eleven contract anchors. A missing additional declaration makes the focused report incomplete while retaining the validated declarations. The default scope remains eleven. Local fixtures: 111 checks pass; Windows adds its own-process check. No gameplay assets or save values are changed.

## Latest result: budget exhaustion; efficient reader and diagnostic stages

The third capture (`legacy-objects-capacity-v3`) exhausted a budget and returned no declarations. Its old report does not reveal the failing stage or whether calls, bytes or time ran out. Revision `legacy-bounded-traversal-v4` reduces repeated reads and loop allocations while retaining the existing limits. Cached class/chunk metadata is scoped to a collection; chunks and seven retail contracts are refreshed before acceptance. Reports now carry constant stage names, read calls/requested bytes/elapsed milliseconds and the exact exhausted limit. No raw addresses or instance values are exported. All 102 local checks pass, including 2,048-instance traversal and changed-chunk rejection; Windows adds an own-process check. Retail performance/completion and persistence remain unverified. Update to v4 before running the camp command; another unchanged v3 run is not needed.

## Current result and object capacity correction

The second capture (`legacy-names-256-v2`) validated a unique name table, then found zero object-array candidates. Revision `legacy-objects-capacity-v3` corrects source-verified reserved-capacity filters: Unreal 4.22's default object reservation rounds to 33 chunks / 2,162,688 slots, and preallocation can allocate more chunks than the live object count needs. Previously our reader rejected both. Live traversal/read/time limits remain; reserved capacity is bounded separately and must agree with its chunk table. 96 local checks pass; Windows adds an own-process read check. Retail discovery and persistence are still unverified. Update before repeating the camp command below; an unchanged v2 capture is not needed. Detailed source evidence and the supplied report hash are in RESEARCH_LOG.md.

## First result and corrected reader

The first user capture reached discovery but found zero name-array matches; no declarations were accepted. Revision `legacy-names-256-v2` adds the 256-pointer layout defined by pinned Unreal 4.22 source, permits reserved capacity, and covers the larger header across scan boundaries. The previous collector omitted this source-defined layout. A repeat is useful only after updating to this revision; keep the working v8 gameplay pak. See the latest RESEARCH_LOG entry for evidence and source links. Retail discovery and permanent item identity remain unverified.

## Collect the additional serialization declarations once from camp

Update the repository, launch normally with the working QoL pak, and enter camp. Leave the game idle while running this from the repository's PowerShell terminal:

```powershell
git pull
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Collect-NativeFavoritesEvidence.ps1 -CollectSerializationContracts
```

Upload the printed `.research/native-favorites-TIMESTAMP-SUFFIX.zip` privately, including an incomplete report. It contains REPORT.json only. If the build fails before a report, provide the terminal message. Do not copy the protected executable, change ACL/ownership, elevate to bypass denial, or reinstall UE4SS. There is nothing to remove afterward: no files are installed into the game folder. The wrapper reuses the SDK installed by the prior archive collector or an installed dotnet SDK; `-DotNetPath` can select that existing SDK. For multiple instances, `-GameProcessId` selects one Dungeons process.

## Implementation and limits

- Attachment rights are PROCESS_QUERY_INFORMATION and PROCESS_VM_READ (0x410). APIs are OpenProcess, ReadProcessMemory and safe handle disposal. No write, injection, suspend, remote thread, driver or privilege APIs.
- Scan writable, non-executable image data only. No on-disk protected executable access. Limit image data to 64 MiB, individual reads to 1 MiB, total reads to 256 MiB, two million requests and 90 seconds. Unreadable or changing regions produce an incomplete report.
- Discover legacy names/chunked objects from bounded candidates. Verify None/ByteProperty/IntProperty, name indices, object indices/classes, owners and field chains. Traverse legacy UProperty parameters, not just modern FProperty. Ambiguous globals/layouts, cycles, partial reads and mismatches stop acceptance.
- Accept a reflection layout only if seven observed call shapes match: Guid return; three int32 getters; index → CharacterSaveData; index/bool → PlayerCharacterSaveSlot; salvage slot/out-bool/undo-struct. This establishes consistency, not a complete native ABI.
- Export eleven allowlisted `/Script/Dungeons` class/struct declarations, or fourteen with the explicit serialization option. No executable buffers, addresses, item/save values, account IDs, full object dump or complete SDK. Container/enum/subclass typing is partial; non-reflected C++ members are unavailable. A completed report does not certify GUID lifetime, clone behavior, permanent item identity or persistence.

Fixtures test twenty-four legacy layout combinations, inline 256-chunk discovery, reserved capacity and corrupt entry indices, Guid typing/export privacy, mismatch/cycle/partial read/index/count/range/budget failures. Windows CI also reads eight bytes from this test process's own allocation through the actual query/read handle. No tests attach to a game or require game assets. 111 local checks pass; Windows adds an own-process read check. The supplied v5 retail capture completed the core scope; expanded-scope completion remains unverified.

## Next gate

Inspect the report for reflected item identity/serialization access before connecting durable hero-scoped favorite save/load code. If reflection lacks item IDs, a version-checked serialization/transfer bridge is required. Name/power grouping or a save index alone must not silently substitute for physical identity. Acceptance remains missions, return to camp, restart, independent duplicates, hero switching, equipment, storage, upgrades and host/join.

Primary references: [ReadProcessMemory](https://learn.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-readprocessmemory), [access rights](https://learn.microsoft.com/en-us/windows/win32/procthread/process-security-and-access-rights), [PE sections](https://learn.microsoft.com/en-us/windows/win32/debug/pe-format). Pinned legacy source reviews are in EXTERNAL_REFLECTION_REVIEW.md. No third-party implementation is bundled.
