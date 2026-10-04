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

The current prototype does not intercept vanilla salvage, so testing that action cannot establish lock safety. Controller, co-op, loadouts and inventory-open input gating remain required production work.
