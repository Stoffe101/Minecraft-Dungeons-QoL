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
