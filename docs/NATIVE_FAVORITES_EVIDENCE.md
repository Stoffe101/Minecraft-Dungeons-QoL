# Read-only native favorites evidence

Favorites persistence remains unfinished. Archive exports establish six profile call shapes and the salvage shape, but no permanent physical-item identifier. This project-authored experimental reader checks loaded native declarations without installing an injected loader. Keep UE4SS disabled.

## Run once from camp

Update the repository, launch normally with the working QoL pak, and enter camp. Leave the game idle while running this from the repository's PowerShell terminal:

```powershell
git pull
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Collect-NativeFavoritesEvidence.ps1
```

Upload the printed `.research/native-favorites-TIMESTAMP-SUFFIX.zip` privately, including an incomplete report. It contains REPORT.json only. If the build fails before a report, provide the terminal message. Do not copy the protected executable, change ACL/ownership, elevate to bypass denial, or reinstall UE4SS. There is nothing to remove afterward: no files are installed into the game folder. The wrapper reuses the SDK installed by the prior archive collector or an installed dotnet SDK; `-DotNetPath` can select that existing SDK. For multiple instances, `-GameProcessId` selects one Dungeons process.

## Implementation and limits

- Attachment rights are PROCESS_QUERY_INFORMATION and PROCESS_VM_READ (0x410). APIs are OpenProcess, ReadProcessMemory and safe handle disposal. No write, injection, suspend, remote thread, driver or privilege APIs.
- Scan writable, non-executable image data only. No on-disk protected executable access. Limit image data to 64 MiB, individual reads to 1 MiB, total reads to 256 MiB, two million requests and 90 seconds. Unreadable or changing regions produce an incomplete report.
- Discover legacy names/chunked objects from bounded candidates. Verify None/ByteProperty/IntProperty, name indices, object indices/classes, owners and field chains. Traverse legacy UProperty parameters, not just modern FProperty. Ambiguous globals/layouts, cycles, partial reads and mismatches stop acceptance.
- Accept a reflection layout only if seven observed call shapes match: Guid return; three int32 getters; index → CharacterSaveData; index/bool → PlayerCharacterSaveSlot; salvage slot/out-bool/undo-struct. This establishes consistency, not a complete native ABI.
- Export eleven allowlisted `/Script/Dungeons` class/struct declarations only. No executable buffers, addresses, item/save values, account IDs, full object dump or complete SDK. Container/enum/subclass typing is partial; non-reflected C++ members are unavailable. A completed report does not certify GUID lifetime, clone behavior, permanent item identity or persistence.

Fixtures test twelve legacy layout combinations, Guid typing/export privacy, mismatch/cycle/partial read/index/count/range/budget failures. Windows CI also reads eight bytes from this test process's own allocation through the actual query/read handle. No tests attach to a game or require game assets. Runtime Store discovery remains unverified.

## Next gate

Inspect the report for reflected item identity/serialization access before connecting durable hero-scoped favorite save/load code. If reflection lacks item IDs, a version-checked serialization/transfer bridge is required. Name/power grouping or a save index alone must not silently substitute for physical identity. Acceptance remains missions, return to camp, restart, independent duplicates, hero switching, equipment, storage, upgrades and host/join.

Primary references: [ReadProcessMemory](https://learn.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-readprocessmemory), [access rights](https://learn.microsoft.com/en-us/windows/win32/procthread/process-security-and-access-rights), [PE sections](https://learn.microsoft.com/en-us/windows/win32/debug/pe-format). Pinned legacy source reviews are in EXTERNAL_REFLECTION_REVIEW.md. No third-party implementation is bundled.
