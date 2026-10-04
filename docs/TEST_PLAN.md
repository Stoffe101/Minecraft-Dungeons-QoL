# Test Plan

## Principle

A destructive inventory mod is only useful if it is boringly safe.

No destructive feature is considered complete from visual testing alone.

## Environment matrix

- Microsoft Store / Xbox app: required
- Camp/lobby: required
- Mission: required
- Keyboard/mouse: required
- Controller: required before 1.0
- Online multiplayer: required before 1.0
- Multiple heroes: required before 1.0

## Gear Lock tests

- lock common item
- lock rare item
- lock unique item
- lock gilded item if applicable
- change inventory filter
- reorder inventory
- close/reopen inventory
- return to camp
- enter mission
- restart game
- switch hero and back
- attempt vanilla salvage of locked item
- attempt bulk-select of locked item
- unlock and verify normal salvage becomes available

## Mass Salvage tests

- select 1 item
- select 20+ items
- mix rarities
- mix enchanted/unenhanced
- include equipped item and verify exclusion
- include locked item and verify exclusion
- lock an item after selection and verify final preflight skips it
- remove/change inventory between selection and confirmation
- cancel confirmation
- force one item in the queue to fail and verify safe behavior
- compare emerald/enchantment-point results with equivalent vanilla salvages

## Persistence tests

- clean first run
- normal save/load
- corrupted QoL save
- version mismatch
- missing item referenced by lock/loadout
- same-looking duplicate items
- upgraded/rerolled item identity

## Gear Set tests

- full valid set
- missing weapon
- missing artifact
- item locked
- same item referenced by multiple sets
- equip from camp
- attempt in unsupported context
- multiplayer local-player-only behavior

## Regression tests after game updates

- Blueprint Loader trigger fires
- inventory widget still resolves
- item ID still resolves
- salvage signature still matches
- equip signature still matches
- lock guard still intercepts vanilla salvage


## Diagnostic gate and regression additions (2026-10-04)

Build gate: re-open the emitted graph; validate all absolute jump targets and null-context skip sizes; reject native item removal/salvage/undo/swap calls. Test rejection of corrupted jump/skip offsets and each prohibited function, then verify the restored graph. Verify relocated manager/Lobby/Ingame packages, companions, sidecar and absence of LetMeMove package paths.

Runtime diagnostic checks (pending):

- Record Store/Xbox executable identity and Blueprint Loader version; test Camp and mission entry.
- Verify reflected instance methods resolve; report missing functions instead of assuming successful bytecode parsing proves them.
- Check whether retail PrintString is visible; if not, implement an actual diagnostic widget.
- Select gear, replace the item occupying its slot, then preview; the replacement must not be processed.
- Select, change hero/pawn/stash, then preview; stale queue must clear.
- F5 cancels armed confirmation and an active preview.
- Native slot-locked items are excluded; explicit equipped-item testing remains a separate release gate.
- Corrupt/unreadable sidecar stops preview; failed save writes report failure.
- Demonstrate the known same-fingerprint/multi-hero collision and changed-stat/localization behavior. Do not treat fingerprint groups as persistent item IDs.
- F10 preview leaves all items and currencies unchanged. No diagnostic code path may call native salvage.

The current prototype does not intercept vanilla salvage, so testing that action cannot establish lock safety. Controller, co-op, loadouts and runtime validation of inventory-open gating remain required production work.

## Installed-game contract regression tests

The validator requires an IsInventoryOpen guard that dominates all hotkey/GetInventorySlots/GetSalvageInfo instructions and proves its closed branch cannot reach those operations. It requires distinct current-item/equipment comparisons in selection and preview. Negative tests preserve instruction sizes while renaming the open property, redirecting its false branch to fallthrough, and substituting the comparison operand in each equipment guard. The unsupported InventoryItem instance enchantment-points call is also rejected. Total: 13 negative tests.

Additional runtime checks: F6–F10 do nothing with inventory closed; closing inventory clears armed preview/selection; each of armor/melee/ranged/three artifacts is excluded; an unavailable equipment widget/native slot blocks selection; an item equipped after selection is skipped at preview; fingerprint enchantment points resolve without a missing instance function. These are pending actual-game tests.

## Cooked load dependency regressions

Validate every reflected child index/owner and preload edge, every field outer creation edge, array-inner serialization and dependency index ranges. Three additional negative tests remove each required edge while preserving the bytecode, bringing the suite to 16. Sidecar must re-open with exactly four exports and no retained actor functions/components. These static checks address discovered generation faults; they do not prove retail loading. Runtime retry starts at profile selection before testing inventory inputs.
