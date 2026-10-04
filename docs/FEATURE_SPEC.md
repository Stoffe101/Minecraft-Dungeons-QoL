# Feature Specification

## 1. Gear Lock

### Required behavior

- Player can toggle a selected item between locked/unlocked.
- Locked state is visually obvious.
- Lock survives game restart.
- Lock follows the same item when inventory order/filter changes.
- Locked items cannot be selected by our mass-salvage system.
- Locked items cannot be salvaged through the vanilla salvage action once the native guard hook is implemented.
- Equipped items are implicitly protected even if not explicitly locked.

### Safety

If item identity is ambiguous, fail closed: treat the item as protected until resolved.

## 2. Mass Salvage

### Interaction

- Toggle `Salvage Select` mode.
- Clicking/toggling an eligible item adds/removes it from the batch.
- Selected items receive a clear visual state.
- UI displays selected count.
- Player opens a review screen before destruction.
- One final confirmation starts the batch.

### Eligibility rules

Never allow:

- equipped items
- locked items
- items assigned to a gear set
- unresolved/invalid item references

Optional later protections:

- enchanted items
- gilded items
- uniques
- purchased items
- items above a configurable power threshold

### Execution

Use the native salvage operation for each item. Revalidate immediately before every destructive call.

## 3. Gear Manager / Loadouts

### Planned first version

- named loadouts
- melee weapon slot
- armor slot
- ranged weapon slot
- three artifact slots
- item assignment from inventory
- assigned items automatically protected
- missing items shown clearly
- one action to equip a valid set

### Later ideas

- duplicate/loadout copy
- icons/colors
- notes/tags
- import/export of loadout definitions without exporting actual gear
- hotkeys/controller shortcuts

## 4. UI principles

- fit the visual language of Dungeons rather than adding a desktop-looking panel
- avoid covering vanilla item details
- obvious destructive-action coloring and confirmation
- controller support is a requirement before 1.0
- UI should remain usable at common resolutions/UI scales

## 5. Non-goals for the first release

- save editor functionality
- generating items
- changing item stats
- granting currency
- automatic salvage on pickup
- cloud synchronization
