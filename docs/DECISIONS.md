# Decisions

## D-001: Target Microsoft Store / Xbox app first

Reason: this is the primary player's edition. Steam compatibility can be tested later without shaping the initial install workflow around it.

## D-002: Separate QoL persistence

Lock state and loadouts live in a project-owned SaveGame slot instead of modifying the Minecraft Dungeons hero save.

Reason: lower corruption risk, easier uninstall/recovery, cleaner version migration.

## D-003: Native salvage only

Mass salvage must call the same native salvage behavior used by the game.

Reason: preserves emerald/enchantment-point logic and reduces risk from duplicated economy rules.

## D-004: Fail closed on protection ambiguity

If the project cannot prove that an item is safe to salvage, it is treated as protected.

## D-005: Overlay-first UI

Prefer new overlay widgets and small hooks to replacing the full vanilla inventory asset.

Reason: lower update fragility and fewer mod conflicts.

## D-006: Loadout membership implies protection

Items referenced by a loadout are automatically protected from mass salvage.

## D-007: No automatic salvage in MVP

The first release requires explicit selection, review, and confirmation.

Reason: prove identity/protection/destructive-action correctness before introducing automation.

## D-008: Diagnostic builds contain no destructive inventory calls

The baseline fingerprint identity and equipment guards are insufficient for the project's fail-closed policy. Generate a preview graph without `SalvageItemInSlot`, `RemoveItem`, `SalvageItemUndo` or `Swap`. Enforce that with asset validation and negative tests. Reintroduce native salvage only after exact ABI, identity, equipment/loadout guards and disposable-item tests are proven.

## D-009: Isolate and pin reusable templates

Use the MIT LetMeMove 1.1.0 cooked shell with a recorded SHA-256 and retained license. Relocate all manager/loader references into our namespace to coexist with the original mod. Pin the Mod Kit commit. Diagnostic CI uploads the exact-source artifact and cannot write a rebased manifest to main.

## 2026-10-05: Shared inventory core and optional feature builds

User requested clickable controls and asked to split the features if better. Favorites and multi-salvage both modify original inventory HUD/inspector assets; independent co-installed paks would silently replace each other's whole assets. Keep the protection core shared and provide mutually exclusive favorites-only and combined builds, with the same native eligibility/mutation guards. Other modules may be separate when they own non-conflicting assets. This provides smaller runtime tests without allowing batch salvage to bypass favorites. Private default downloads remain batch-disabled until retail UI/guard verification.
