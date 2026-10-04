# Research Log

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
