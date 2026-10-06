# External native reflection review — 2026-10-06

Follow-up implementation: the project-authored experimental LegacyNativeEvidence reader now implements bounded legacy declaration collection with seven call-shape gates, without bundling these reviewed tools. See NATIVE_FAVORITES_EVIDENCE.md. Local fixtures pass; Store process access/discovery still needs a private runtime report, and persistence remains unfinished. The reviewed stock tools below remain inspection references only.

The injected UE4SS probe is withdrawn after the user's startup crash. A replacement should obtain allowlisted native declarations without injecting a DLL or invoking gameplay/save functions. External process reads are a research option, not a proven implementation for this Store build. All three projects below were inspected privately at pinned source commits; their root MIT licenses were checked. No code is copied into this repository, and none was built, executed, installed or bundled.

| Project / inspected commit | Relevant behavior | Decision |
|---|---|---|
| [Unreal-eXternalrEsolve](https://github.com/zushinzackery2-ship-it/Unreal-eXternalrEsolve/tree/1416c889a68f80becb7caf8201977be8bd252d90), `1416c889a68f80becb7caf8201977be8bd252d90` | Windows x64 C++20 library; WinAPI attach uses PROCESS_QUERY_INFORMATION and PROCESS_VM_READ. Separate write and shared-memory/driver facilities exist. Automatic SDK path has legacy parameter/layout gaps. | Potential reader components only after correction and bounded verification. Not ready to send as a Dungeons collector. |
| [McDaived/UE-Dumper](https://github.com/McDaived/UE-Dumper/tree/f3515c4479c22dee25a937c5406243a4f3db44ef), `f3515c4479c22dee25a937c5406243a4f3db44ef` | External reads, but stock memory attachment requests PROCESS_ALL_ACCESS; optional driver path exists. No Dungeons Store configuration was established. | Do not run stock build as a read-only replacement. Compatibility unverified. |
| [Spuckwaffel/UEDumper](https://github.com/Spuckwaffel/UEDumper/tree/5b2b5264a66aa9edb28619c5ff654b16d3b9e038), `5b2b5264a66aa9edb28619c5ff654b16d3b9e038` | Versioned UE4.19–5.3 SDK/live-editor source; requests PROCESS_ALL_ACCESS. GNames/GObjects defaults are 0xDEADBEEF placeholders. Useful UE4.22 class layout reference, not verified installation offsets. | Inspection reference only. Live writes are unnecessary; no guessed-offset build is deployed. |

## Source findings that block automatic reuse

At the Unreal-eXternalrEsolve pin:

- `include/unreal/core/process.hpp` OpenProcessForRead uses query/read rights; `init/lifecycle/attach.hpp` selects that path. This narrows the intended access but does not prove successful access to the protected Store process. Separate write helpers exist and would need to be excluded from a purpose-built collector.
- `include/unreal/helpers/dump/dump_collect.hpp` CollectFuncParams only traverses when bUseFProperty is true. It has no legacy UProperty branch. In legacy mode it returns an empty parameter list, which cannot certify the known profile/salvage contracts.
- `include/unreal/core/context.hpp` initializes bUseFProperty/bUseNamePool to true. `resolve/uobject/scan_struct_offsets.hpp` reports fallback to UField when ChildProperties validation fails, but does not set bUseFProperty to false in that branch. The log therefore cannot establish a successful legacy-mode switch.
- Auto-init's `init/phases/validation.hpp` requires GWorld, DebugCanvas, ProcessEvent and AppendString in addition to names/objects. Those requirements are unnecessary for declaration-only collection and can prevent completion even if sufficient reflected metadata is readable. A cancellation callback exists, but a top-level callback does not establish a bound on every nested scan.

At the UEDumper pin, `Engine/UEClasses/UnrealClasses.h` and `Engine/Userdefined/StructDefinitions.h` model the UE4.22 FStructBaseChain contribution before UStruct's SuperStruct/Children members. Applying newer FProperty offsets to this version is unsafe. Source layouts are candidates requiring runtime validation, not certified Store offsets.

The earlier [guttir14/UnrealDumper-4.25](https://github.com/guttir14/UnrealDumper-4.25) explicitly targets UE4.23–4.27; that claim does not cover this game's established UE4.22.3. Android UE4Dumper and injected Dumper-7 are not direct replacements for this Windows read-only task.

## Requirements before a replacement can be offered

A project-owned collector would need a strictly query/read process handle, bounded scans and reads, correct legacy names/UObject/UField/UProperty traversal, no injection/driver/write APIs, explicit failure reporting, and an allowlist covering only needed native declarations. Do not export executable bytes, complete object memory, save values or whole generated SDKs by default. Private raw metadata remains outside git/public CI.

Verify reflected class chains and the six independently observed profile calls before accepting any item metadata: Guid struct result; the three int32 getters; index → CharacterSaveData; index/bool → PlayerCharacterSaveSlot. Also cross-check the established SalvageItemInSlot object/out-bool/undo-struct shape. Missing or inconsistent fields must stop collection, not produce guessed declarations.

A complete native declaration dump still might expose no permanent physical-item ID. Favorite persistence then requires a version-checked serialization/transfer/lifecycle bridge, with reconciliation covering duplicate items, storage, upgrades, hero clones and host/join sessions. No persistent favorite release is established by this review.
