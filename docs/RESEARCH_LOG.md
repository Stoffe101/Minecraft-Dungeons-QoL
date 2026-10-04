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
