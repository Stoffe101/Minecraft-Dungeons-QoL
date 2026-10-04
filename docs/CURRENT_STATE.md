> Runtime status: PR #13 also crashed opening inventory online and is withdrawn. A UE4.22.3 source investigation identified unsafe nested calls into native reference parameters. The new generators store text/string results in typed locals before reuse, matching the supplied game graph. Retail crash repair remains unverified. Online host and joining-friend play are required; splitscreen is outside the current requirement. Production features remain unfinished.

# Current State

Last updated: 2026-10-04 (successful game metadata and inventory/equipment guards)

## Actual implementation

The repository has Windows Mod Kit/bootstrap/install tooling, editor reflection mirrors, cooked Blueprint inspection/patching tools and a generated SaveGame prototype. There are **no project-owned editable runtime/UI Blueprints under `mod/Content` yet**, so the standard `Build.ps1` authoring route is scaffolding, not a complete release build.

`Build-Diagnostic.ps1` is the implemented cooked-template route. It downloads the reviewed LetMeMove 1.1.0 release with checksum verification, uses a pinned Mod Kit packager, generates the runtime/sidecar, relocates both loader maps and the manager into our namespace and packages `dist/diagnostic/MinecraftDungeonsQoL-diagnostic.pak`.

## Diagnostic behavior emitted in bytecode

| Input | Prototype behavior |
| --- | --- |
| F5 | Clear selection and cancel confirmation/preview |
| F6 / F7 | Previous / next native inventory slot index |
| F8 | Toggle a persisted fingerprint protection group |
| F9 | Select/deselect paired inventory slot/item snapshots |
| F10 twice | Preview eligible candidates, one per tick; **no items salvaged** |

The graph checks local controller/pawn/stash validity, clears selection on stash replacement, validates snapshot bounds/current membership/object identity, checks native slot locking, gates input/reads on the real inventory-open field, rejects equipped items through the six HUD gear widgets, and stops on invalid sidecar data. It has structural write/re-open checks and negative validator tests.

These are **generated behaviors, not user-tested runtime guarantees**. `PrintString` visibility in the retail build, reflected item methods, wildcard array behavior and sidecar saving still need in-game verification.

## Explicit limitations

- No release-quality gear lock or gear manager/loadouts yet.
- No native salvage call in the diagnostic graph.
- Fingerprints use localized display name + power + invested enchantment points in one global sidecar. They are not physical item IDs or hero-scoped records. Matching items share protection; stat/localization changes can lose the match.
- No vanilla salvage interception.
- No actual inventory/review widget, controller support, modal/focus guards, singleton guard or runtime-tested co-op support. The generated open-inventory gate is not yet runtime-tested.
- Native `IsLocked()` is not an explicit user lock and does not replace verified equipped/loadout exclusion.
- Some selected native call shapes and UI fields are now observed in Store/Xbox game assets; persistent identity, native equip and unreferenced reflection remain unverified.

## Validation and next work

See `REPO_AUDIT.md` for findings/fixes and `RESEARCH_LOG.md` for actual checks. The diagnostic asset graph serializes, its package paths relocate, and the pak packages correctly. No in-game validation has been performed.

The audited changes were merged through [PR #1](https://github.com/Stoffe101/Minecraft-Dungeons-QoL/pull/1) after both Windows workflows passed: [Project Validation](https://github.com/Stoffe101/Minecraft-Dungeons-QoL/actions/runs/37205700653) and [Cooked QoL Diagnostic Build](https://github.com/Stoffe101/Minecraft-Dungeons-QoL/actions/runs/37205700636). That proves the Windows tooling/build path, not game execution.

`Collect-GameEvidence.ps1` gathers read-only asset lists/Blueprint metadata and optional executable version/hash evidence from the active installation. It pins its dumper/runtime downloads, refuses invalid/ambiguous installations and preserves failure diagnostics. See `GAME_EVIDENCE.md`. The latest user-supplied legacy export successfully recovered all 31 targeted assets. See `GAME_API_CONTRACTS.md` for the observed calls/fields and remaining gaps.

User confirmed the active Paks path as `C:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks`. Their first collection attempt encountered access denied reading `Binaries/Win64/Dungeons.exe`. Executable hashes are now optional: denied metadata is recorded as warnings while archive inspection continues.

The AES-configured retry successfully exposed 131,164 Dungeons paths (45,035 `.uasset` paths), confirming the supplied key works for this installation's catalog. The broad Inventory dump stopped at a cosmetic `GetButtonReference` parser error because native stderr was promoted to a terminating Windows PowerShell error. No class/function metadata was included in the uploaded archive. The collector now captures native stdout/stderr independently, continues per group and targets 31 inventory/controller assets identified in the catalog. See `FINDINGS.md` for exact targets. Real native signatures and item identity remain unverified.

The next blocker is runtime/reflection evidence from the actual Dungeons 1 executable, followed by stable hero/item identity, equipment guards and a proper review UI. Native salvage remains the intended production backend, gated behind that work. Standard UE4.22 Mod Kit Blueprint authoring remains the preferred route for the finished UI; KismetKompiler and UE4SS are optional research tools.

The targeted retry at `dcc5f5b` completed all groups but exported zero metadata files: UeBlueprintDumper 1.2.0 assumes `UStruct.ChildProperties` (new FProperty layout), which is null for legacy UE4.22 UProperty exports. The project now has `LegacyEvidenceExporter`: CUE4Parse mounts/reads packages, while pinned UAssetAPI 1.1.0 reads legacy imports/properties/functions/Kismet. Local actual UE4.22 actor and diagnostic-pak tests passed (34 properties/2 functions and 50 properties/2 functions respectively). `Collect-LegacyGameEvidence.ps1` builds and runs this route using an existing SDK or an automatic local checksum-pinned SDK. The subsequent legacy export succeeded; runtime feature completion remains pending.

The full legacy collector passed Windows PowerShell 5.1 CI against the real UE4.22 fixture (34 properties, 2 functions, Kismet present, unchanged input hash, metadata-only ZIP); both project validation and cooked diagnostic build passed on PR #6. That export has now been supplied and analyzed successfully; no repeat collection is needed.

## Applied installed-game evidence

The successful export contains 5,298 properties and 824 Blueprint functions. The generated manager now resolves controller SharedUI/InventoryHUD with the correct widget-class import kind and clears selection when inventory closes or cannot be resolved. It uses controller GetItemStashComponent, an imported GetInventorySlots call, and read-only GetSalvageInfo for fingerprint refund points. Selection/preview explicitly exclude all six equipped item objects and fail closed on incomplete equipment UI. Structural checks verify input/read control flow behind the open flag; negative tests cover bypasses and missing equipment guards. Runtime behavior still needs actual-game validation.

PR #7 head `11c64ed` passed both Windows workflows, including all 13 negative graph tests and full pak packaging. The downloaded CI pak re-read without errors. The next practical gate is an in-game diagnostic test with Blueprint Loader: verify inventory-only hotkeys, six-slot exclusion, closing-inventory cancellation and visible feedback. No further duplicate metadata export is needed. This is still a diagnostic build; physical-item locks, loadouts, vanilla interception and native bulk salvage remain unfinished.

## Character-selection crash response

The supplied minidump records a write access violation at address 0x28 while a reflected child-list reconstruction dereferences a null child. Its captured instruction sequence copies a serialized field array into linked Next pointers; without symbols/heap data we cannot identify the exact failed struct. The manager appended new class/function fields without adding child preload edges, and cleared new field outer/array-inner dependencies. The synthesized SaveGame retained actor exports and obsolete dependencies. These are independently demonstrated generation defects consistent with loading failure.

The new cooked dependency helper adds child, outer, array-inner and referenced-type ordering edges and rebuilds DependsMap. SaveGame now has only class/CDO/Records/Name inner exports with remapped indices and clean dependencies. Native bool metadata follows the donor. Dependency checks run before writing and after re-opening; 16 negative graph/dependency tests replace the previous 13-test gate. In-game retry remains pending.


## Second crash and next runtime gate

PR #8 also crashed. The new dump's 43-child count and failing loop position match the first appended ubergraph locals. Their zero native property archetypes were another independently verified generation defect. Shared repair now provides correctly typed property CDO imports plus class/archetype creation preloads; 19 negative tests cover the emitted graph. See RESEARCH_LOG for exact evidence and inference limits.

The next artifact to test is **MinecraftDungeonsQoL-load-probe**, whose manager event bodies immediately return. Remove all earlier QoL paks, keep Blueprint Loader, install only the probe, restart and select the character to enter camp. This isolates loading without executing feature code. A successful probe does not complete the mod or validate inventory behavior. The repaired full diagnostic remains unverified and must never be installed together with the probe.


## Confirmed loading and visible inventory probe

On 2026-10-04 the user confirmed PR #9's `MinecraftDungeonsQoL-load-probe` selected the character and reached camp without crashing. This confirms that repaired manager/sidecar schema can load through Blueprint Loader on that Store installation. It does not validate the full diagnostic functions, mission loading, save persistence or any promised feature.

`CookedInventoryProbe` is the next runtime gate. It reuses the repaired manager schema, resolves the observed controller/SharedUI/InventoryHUD path and only reads native inventory slots/item name/power while inventory is open. F6/F7 browse indices. It constructs its own native TextBlock in HUD WholeCanvas, anchors it above the action bar, avoids hit testing, hides it when inventory closes, caches unchanged text, and recreates its own widget on canvas replacement. A first-manager check prevents simultaneous loader instances from producing multiple overlays. None of those new runtime behaviors are confirmed yet.

The probe contains no item mutations, lock/select/preview hotkeys, fingerprint/refund reads, or sidecar load/save calls. The full diagnostic still exists as a separate unverified artifact. Never install multiple QoL variants together. Seven new negative tests complement the existing 19 tests; bytecode/metadata validation does not replace rendering/native execution tests.

## Silent inventory probe: event entry repair (2026-10-04)

The user reports camp/inventory opens without a crash, but no QoL text or visible F6/F7 response. The screenshot confirms no visible probe text; it cannot establish which runtime gate failed.

The reviewed LetMeMove actor starts ExecuteUbergraph with a 10-byte ComputedJump reading its EntryPoint parameter. ReceiveTick calls that graph with EntryPoint=10. Our executable replacement graphs had removed this dispatcher and placed the first controller lookup at offset 0 while preserving ReceiveTick. Both generators now restore the dispatcher, placing the first body statement at offset 10. The validator checks the parameter owner, dispatcher size, Tick target and argument; three new negative tests cover corrupt event wiring (10 probe tests, plus 19 original tests).

This repairs an independently observed cooked event entry mismatch. It is a candidate explanation for the silent probe, not a verified runtime fix. Native widget construction, HUD availability/open gating and manager ownership may still prevent feedback. F6/F7 browse the probe's own cursor and do not move the vanilla inventory selection. Keep the empty-event load probe separate from the executable inventory probe.


## Offline inventory pause repair (2026-10-04)

The user's second silent-probe report includes a full inventory screenshot with no overlay and Explorer showing Blueprint-Loader.pak plus one MinecraftDungeonsQoL-inventory-probe.pak under XboxGames/Minecraft Dungeons/Content/Dungeons/Content/Paks/~mods. This supports correct visible installation layout, not binary hash or event execution. No activation option exists.

The supplied BP_PlayerController.UIToggleInventory graph calls SetGamePaused(Self,true) after showing inventory when the session is offline and currently unpaused. The actual PR #11 packaged manager's PrimaryActorTick defaults only serialized bCanEverTick=true, inheriting the native paused-tick default. Its inventory-open-gated code therefore cannot reliably run while the offline UI is open.

Both executable generators now serialize bCanEverTick=true, bStartWithTickEnabled=true, bTickEvenWhenPaused=true and TickInterval=0 in their own class default object's existing ActorTickFunction struct. Neither generator unpauses the game nor changes the player's controller or game actors. Validation requires the emitted flags and zero interval; five new negative probe tests cover disabled flags, omitted pause ticking and positive interval (17 probe plus 19 diagnostic tests). An in-game retry must verify initial text without any hotkey, F6/F7 responses while paused and inventory-close hiding. This fixes an observed lifecycle omission, but rendering/input/native reads still require retail confirmation. All unimplemented production features remain open.


The same pass also replaces the field-only IsInventoryOpen check with native Widget.IsVisible on the resolved InventoryHUD, matching the controller's actual toggle/pausing logic. No assignment to that Boolean appears in the collected HUD/controller graphs, so its runtime semantics were not established. Two additional negative tests verify the visibility guard's widget receiver and native function owner. Pause behavior and visibility are independently observed contracts; the revised probe still needs retail confirmation.

## Inventory-open crash and online requirement (2026-10-04)

The user confirmed online play and reported an inventory-open crash with PR #12. Pause ticking does not explain this online failure. The new dump records a null read at 0x98 in Dungeons module offset 0x1237080. The instruction tests FUNC_Native on a null function pointer, consistent with unresolved script function dispatch. Without symbols or captured UObject/script heaps, the exact failing call remains unknown.

The replacement removes SetJustification and SetAutoWrapText imports previously assigned to TextLayoutWidget without a verified UE4.22 reflection contract. Native item name/power calls now use explicit FinalFunction imports supported by supplied game metadata. Validators require native UI owners and arities. Both generators require Controller.IsLocalPlayerController before input, inventory reads or UI construction and serialize bReplicates=false on their manager. Six new rejection tests raise coverage to 42 total. This is structural verification, not runtime success.

See COOP_COMPATIBILITY.md: the external Blueprint Loader branches on GetGameMode, which is normally null on joining clients. Client bootstrap needs a project-owned solution and retail tests. The dependency's permissions do not authorize modifying or redistributing it.

## PR #13 packaged validation (2026-10-04)

Implementation head: 845fece40161c1e3b371b2026d069663f4d907ac. Windows Cooked QoL Diagnostic Build run 37238191986 passed all 19 diagnostic and 23 probe regression checks; Project Validation run 37238191980 passed.

Downloaded artifact 11315778743, verified pak SHA-256 `db2e73dc8bf2cfb9634b1f0765a87c7a9c8d6563f56ab3e8a664ead05e715efb`, integrity-unpacked it and reran all 23 probe rejection tests against the packaged manager. Inspected its CDO: bReplicates=false; bCanEverTick, bStartWithTickEnabled and bTickEvenWhenPaused=true; TickInterval=0. BUILD_INFO records CI synthetic merge dc726c4c6d973cce03e5ba9909b4ff7e63202bcf.

Candidate download: https://github.com/Stoffe101/Minecraft-Dungeons-QoL/actions/runs/37238191986/artifacts/11315778743

This validates packaging and structure only. Inventory-open crash repair, visible overlay/input and online host/join compatibility require retail tests. PR #12 remains withdrawn.

## 2026-10-05: PR #13 inventory crash and VM source investigation

PR #13 still crashes opening inventory online and is withdrawn. See INVENTORY_CRASH_INVESTIGATION.md for the new dump, UE4.22.3 source trace and typed-local repair. Both generators had unsafe nested native reference arguments. Their new scalar locals and result properties match compiler-style game/mod output; the shared validator now rejects the actual old package. Local generation and 47 regression checks pass. Runtime confirmation is pending.

MODDING_OPTIONS_REVIEW.md records the expanded project/license review, existing MIT reuse and UE4SS as a potential live diagnostic/client bootstrap route with unverified Store compatibility. Required multiplayer is online hosting/joining friends, not splitscreen. No ready-made licensed complete implementation was established.

## PR #14 packaged validation (2026-10-05)

Implementation head 102510aed032b4350da9a8ed1dc984d79a38b5c5 passed Windows Cooked QoL Diagnostic Build run 37240868277 and Project Validation run 37240868108. Downloaded both executable artifacts, integrity-unpacked their paks and reran the 19 diagnostic and 28 probe rejection checks against their actual managers. All 47 passed. The packaged probe has 88 statements and 99 exports; its CDO remains non-replicated with all three tick flags enabled and zero interval.

Inventory probe artifact 11317231607: https://github.com/Stoffe101/Minecraft-Dungeons-QoL/actions/runs/37240868277/artifacts/11317231607

Inventory pak SHA-256: `776295667ee229670b882c5c85dd3d07ad1fe969fb2375667d238063f691efa9`. Diagnostic pak SHA-256: `59315a06a8c529a840cd7bc8b6cdc990db7aadf41a74a0741b260d6fd8142e56`. BUILD_INFO records CI synthetic merge 62e799811c567f082aca7e3e591414a353082c94. The offered inventory probe must replace all older QoL paks; Blueprint Loader remains external.

Packaging/structural results do not establish a runtime crash fix. Online hosting, joining friends and the production feature set remain unverified or incomplete.
