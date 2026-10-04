# Repository audit and applied research — 2026-10-04

Audited baseline: `b35d841` on `main`. This audit covers README, all canonical docs, PowerShell tooling, workflows, editor mirrors, generated SaveGame assets, research reports and the cooked runtime patcher.

## Result

The repository contains a real asset-generation prototype, not a finished gear manager. `CookedQoLPatcher` already assembled navigation, fingerprint protection, selection and a native salvage call, while README/current-state/roadmap still described these as future work. Earlier structural parsing did not prove runtime safety.

The current pass preserves that progress, fixes selection/build gaps, and produces a **non-destructive diagnostic pak**. No item removal/salvage/swap call is emitted in its runtime graph. The native salvage backend remains the intended production design after the release gates are met.

## Findings and action

| Finding at baseline | Consequence | Applied action / remaining work |
| --- | --- | --- |
| Selection stored slot pointers without the selected item objects | Another item placed into a selected slot could become a salvage target | Keep paired slot/item snapshots; preview revalidates object equality and current inventory membership |
| Batch ran before checking the current stash | Pawn changes or missing local inventory could leave stale queued work | Validate controller/pawn/stash before all inventory work; clear queue and confirmation when stash changes or disappears |
| Batch index checked only against zero | Changed/unequal arrays could be accessed out of range | Check both snapshot array bounds before access |
| `ArrayGetByRef` read a nested `GetInventorySlots` result | Temporary array behavior was unproven | Materialize inventory slots in a reflected function local before indexing |
| No explicit native slot lock test | Vanilla slot lock semantics were ignored | Check `IsLocked` during selection and preview; equipment exclusion still needs separate proof |
| Persistence was one global `FName[]` of display name/power/enchantment points | Duplicate items/heroes share keys; upgrades/rerolls/localization can lose a match | Label it fingerprint group protection, not item identity; destructive release remains blocked |
| Loaded SaveGame object was not class checked; save success ignored | Bad sidecar data or failed writes could be presented as reliable locks | Cast loaded sidecar to `SG_MCDQoL_C`, stop preview on invalid save and report write failure |
| No clear-selection/cancel key | Queue remained armed until another action | F5 clears queue and confirmation, including during preview |
| Repacking reused LetMeMove paths | Overrides/conflicts with installed LetMeMove | Relocate manager and both loader maps to MinecraftDungeonsQoL paths; validate no template paths remain |
| Live dependency download without checksum; Mod Kit cloned at HEAD | Build input can silently change | Pin release ZIP SHA-256 and Mod Kit commit |
| MIT template reuse notice absent from build package | Attribution missing from derivative artifacts | Retain the full LetMeMove MIT license and include it in diagnostic artifact |
| CI builds did not track patcher changes | Runtime code could bypass compilation/build checks | Trigger diagnostic pipeline on runtime/tool/config edits; build all .NET tools in project validation |
| Build workflow committed manifests to main with rebase | A manifest could describe different code after concurrent changes | Use read-only workflow permissions; keep exact source SHA in uploaded artifact; no bot commit |
| Native calls checked only by strings surviving serialization | A parseable graph may still be unsafe at runtime | Check jump/context offsets, forbid diagnostic mutations, add negative validator tests; retain in-game gates |

## Evidence levels

1. Primary tool documentation establishes the UE4.22 Mod Kit, cooking, precooked staging and Blueprint Loader triggers.
2. LetMeMove source/release proves a working loader/actor template is available; it does not prove our changes run in Dungeons.
3. MCD-PE provides restored API architecture for the final **Steam** build. Its claims are upstream research, not our independent verification of Store/Xbox reflection signatures.
4. UAssetAPI write/re-open verifies serialization. It does not execute Unreal bytecode or validate native parameter layout.
5. Only a real Dungeons run can establish loading, local-player routing, sidecar serialization and safe inventory behavior.

The prototype calls `CanSalvage`, `GetDisplayNameText`, `GetDisplayItemPowerInt` and `GetTotalInvestedEnchantmentPoints` on `InventoryItem`. The inspected MCD-PE snapshot does not establish all those as Blueprint-reflected instance functions; some names appear in utility/native source. These calls need a reflection/asset check on the actual shipping game before claiming even the diagnostic fingerprint behavior works. Do not add guessed declarations to the editor mirror.

## Verification this pass

- Compiled the changed C# patcher, validator, relocator, SaveGame synth and graph regression runner against pinned UAssetAPI 1.1.0.
- Wrote/reopened diagnostic graph: 7,180 iCode units, 136 statements; jump boundaries and context skips validated.
- Relocated/reopened manager and Lobby/Ingame maps; synthesized/reopened the sidecar class.
- Packed and listed a diagnostic pak with eight required asset/companion files under the project namespace.
- Negative regression tests exercise malformed jumps/context skips and four forbidden mutation calls (results recorded in `RESEARCH_LOG.md`).
- PowerShell and GitHub CI results are recorded separately when actually run. No Windows/UE editor/Dungeons runtime verification has been performed here.

## Next implementation sequence

1. Run the diagnostic on the Store/Xbox-managed Dungeons 1 installation with Blueprint Loader and collect startup/log/reflection evidence.
2. Prove the exact reflected signatures; replace unsupported fingerprint calls rather than inventing headers.
3. Verify hero-scoped stable identity and equipment map semantics; use a versioned sidecar with migration and unresolved-record handling.
4. Author a proper inventory/review overlay in UE4.22 with named items/counts, inventory-open input gating, controller support and singleton lifecycle.
5. Enable one disposable-item native salvage transaction only after guards are verified; inspect success/undo metadata and rewards; then extend to sequential batching.
6. Implement loadout assignment/protection and native `CanSwapWith`/`Swap`, followed by the full test matrix.
