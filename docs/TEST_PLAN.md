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
