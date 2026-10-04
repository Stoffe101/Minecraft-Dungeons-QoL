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
