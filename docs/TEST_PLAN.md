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


## Loading probe after the second crash

1. Close the game. Remove every earlier `MinecraftDungeonsQoL*.pak` from the active `Paks\~mods`; keep `Blueprint-Loader.pak`.
2. Install only `MinecraftDungeonsQoL-load-probe.pak` from the separate load-probe artifact.
3. Restart, select the character and enter camp. No inventory hotkeys are active in this build.
4. Record whether camp loads. If it crashes, retain the newly generated CrashContext and minidump; include a contemporaneous Dungeons gameplay log if available. `debug.log` from the launcher is not a substitute.
5. Remove the probe before testing any full diagnostic. Do not install multiple QoL paks together, since their package paths collide.

Automated archetype gates reject zero TemplateIndex, the wrong native property CDO, and missing class/archetype creation preload edges. Probe re-opening verifies that both manager functions contain only an empty return and EndOfScript and that reflected child indices/export count are preserved. These checks do not emulate the engine's loader.


## Visible inventory-read probe (after confirmed camp loading)

Close the game, remove every other MinecraftDungeonsQoL pak (including load-probe), keep Blueprint-Loader.pak and install only MinecraftDungeonsQoL-inventory-probe.pak.

1. Select character and enter camp. Confirm no new crash.
2. Open inventory. Expect a bottom-center `MCD QoL READ-ONLY` text block with inventory slot count, current item name and power. This cursor is separate from vanilla selection.
3. Press F7/F6 to browse next/previous, including wrap and empty slots. Compare the shown item name/power with the actual inventory.
4. Close inventory. Confirm the text hides and F6/F7 no longer browse. Reopen and confirm one overlay appears.
5. If stable, enter a mission and repeat; record whether overlay ownership/lifecycle survives the transition.

No F8/F9/F10 actions are active. No items or sidecar saves are changed. If text is missing, report that alongside crash/no-crash and inventory responsiveness; a missing overlay is not proof that the reads ran successfully. If it crashes, preserve fresh CrashContext/minidump. Layout at common resolutions/controller behavior require screenshots and actual testing later.


## Pause-capable inventory probe retry

The PR #11 event entry repair still produced no text or visible F6/F7 response for the user. The next probe adds explicit paused ticking to the manager class defaults. Use the replacement inventory-probe artifact alone with Blueprint Loader; there is no activation setting.

1. Enter camp offline and open inventory. Wait briefly without pressing keys; record whether the initial read-only overlay appears. This separates initial rendering from input polling.
2. If text appears, press F7 once and F6 once. Record text changes, separately from the vanilla selected item's highlight.
3. Close/reopen inventory; confirm hiding/reappearance and only one overlay. Retest a mission transition if stable.
4. If text still does not appear, preserve the installed pak's SHA-256 alongside the report. The screenshot's filename/location does not establish the installed build or which guard failed.

Automatic tests reject all three disabled tick flags, omitted bTickEvenWhenPaused and a positive TickInterval. Packaging verification reopens the actual pak's manager to check those defaults. The original offline game pause is preserved; this probe performs no item/save mutations.
