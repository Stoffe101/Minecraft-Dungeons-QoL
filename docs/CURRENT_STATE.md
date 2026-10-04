# Current State

Last updated: 2026-10-04 (Windows CI and installed-game evidence tooling)

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

The graph checks local controller/pawn/stash validity, clears selection on stash replacement, validates snapshot bounds/current membership/object identity, checks native slot locking and stops on invalid sidecar data. It has structural write/re-open checks and negative validator tests.

These are **generated behaviors, not user-tested runtime guarantees**. `PrintString` visibility in the retail build, reflected item methods, wildcard array behavior and sidecar saving still need in-game verification.

## Explicit limitations

- No release-quality gear lock or gear manager/loadouts yet.
- No native salvage call in the diagnostic graph.
- Fingerprints use localized display name + power + invested enchantment points in one global sidecar. They are not physical item IDs or hero-scoped records. Matching items share protection; stat/localization changes can lose the match.
- No vanilla salvage interception.
- No actual inventory/review widget, controller support, input gating to an open inventory, singleton guard or runtime-tested co-op support.
- Native `IsLocked()` is not an explicit user lock and does not replace verified equipped/loadout exclusion.
- API findings from upstream restoration are not independently verified Store/Xbox reflection signatures.

## Validation and next work

See `REPO_AUDIT.md` for findings/fixes and `RESEARCH_LOG.md` for actual checks. The diagnostic asset graph serializes, its package paths relocate, and the pak packages correctly. No in-game validation has been performed.

The audited changes were merged through [PR #1](https://github.com/Stoffe101/Minecraft-Dungeons-QoL/pull/1) after both Windows workflows passed: [Project Validation](https://github.com/Stoffe101/Minecraft-Dungeons-QoL/actions/runs/37205700653) and [Cooked QoL Diagnostic Build](https://github.com/Stoffe101/Minecraft-Dungeons-QoL/actions/runs/37205700636). That proves the Windows tooling/build path, not game execution.

`Collect-GameEvidence.ps1` now gathers read-only asset lists/Blueprint metadata and executable version/hash evidence from the active installation. It pins its dumper/runtime downloads, refuses invalid/ambiguous installations and preserves failure diagnostics. See `GAME_EVIDENCE.md`. No installed-game evidence has been collected here.

The next blocker is runtime/reflection evidence from the actual Dungeons 1 executable, followed by stable hero/item identity, equipment guards and a proper review UI. Native salvage remains the intended production backend, gated behind that work. Standard UE4.22 Mod Kit Blueprint authoring remains the preferred route for the finished UI; KismetKompiler and UE4SS are optional research tools.
