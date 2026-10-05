# Test Plan

Native identity evidence gate: Test-FavoritesReflectionProbe.ps1 verifies the pinned release, collision/checksum rejection, metadata allowlist, incomplete capture reporting and hash-owned removal against a fake installation. Retail: install closed, enter camp/open inventory, Ctrl+H once, quit, collect and remove. Upload the private printed ZIP even if startup fails. Inspect native identity before implementing persistence; do not repeat the exhausted broad archive export or treat SDK generation as feature validation. See FAVORITES_PERSISTENCE_INVESTIGATION.md.

## v8 and persistent-favorite acceptance

v8 visual check: selected tiles have 6-unit red borders without covering power/count; favorited melee/ranged/armor/artifacts show stars in equipped slots. Equip/unequip, scroll/filter/rebuild, unfavorite and reopen: markers must follow current physical items, deduplicate widgets and stay hit-test invisible. Equipped items remain excluded from Select All/native salvage.

Persistence is NOT repaired in v8. Collect the new evidence through GAME_EVIDENCE.md. Future persistence acceptance must cover mission return, restart, hero switching, independent identical items, explicit unfavorite, storage, upgrades/rerolls and online host/join. An unresolved identity must never silently clear protection or transfer a favorite to the wrong item.

## v7 favorite marker acceptance

1. Favorite an unequipped item: a gold star appears top-right on its grid tile, and FAVORITE appears after the existing rarity/gilded/custom tags when inspecting it. Check an item with all three native badges present; the label must not cover the item name or power.
2. Switch to another physically distinct item, including one with identical name/power. Neither marker may transfer. Unfavorite the original: star and badge disappear; normal salvage becomes available only if vanilla eligibility permits it.
3. Scroll/filter/rebuild the grid, reopen inventory and change inspection rapidly. Marks must follow current physical items and remain non-interactive; no duplicate badges or accumulated historical widgets. Check normal/narrow/wide UI scaling.
4. Recheck selecting/favoriting, native multi-salvage and Select All exclusions. These presentation changes must not bypass favorites protection. Repeat alt-tab and host/join checks; no GPU crash fix or persistence claim is established.

Automated favorite-state graph checks cover inspected favorite, unchanged no-op, changed physical item, unfavorite, and null inspector. Existing visibility/action/layout/owner checks remain. Mock/parser/package checks are not retail execution.

## v6 retail follow-up

User-confirmed v5: actual multi-item salvage works. Still open: Select All then salvage, reward/refund totals and online joining-player tests.

1. Replace the older QoL pak with the single v6 CombinedNative pak. Hover/select the last item and bottom row; Favorite/Unlock favorite must remain beside the left controls, without covering the right item name or power. Check count spacing at normal UI scaling.
2. Select two expendable unequipped items and keep a third favorited. Review count; No preserves selection, Yes removes only reviewed eligible items. Select All must exclude favorites/equipment before a confirmed batch. Vanilla undo restores only the last item.
3. Repeat the inventory-open alt-tab/return sequence. Watch for a GPU crash or visible flicker; unchanged selection borders must remain stable. If a crash recurs, preserve the new XML/minidump and compare a run without the QoL pak. Current evidence does not establish the v6 mitigation fixes device hangs.
4. Repeat scrolling/filtering/clear/reopen and host/join sessions. Marker identity must follow physical items across widget reuse.

Automated v6 coverage executes generated visibility branches with mock widgets: hidden no-op, visible transition, repeated visible no-op, and collapsed transition. Owner contracts reject GetVisibility/SetVisibility on the wrong declaring class. These tests do not execute Slate or a GPU.

## v5 native batch acceptance

The private CombinedNative v5 replaces v4's preview. First select two cheap unequipped items and keep another item favorited. Check shorter labels and 12-unit button gaps. Salvage must open the correct count; No retains selections and removes nothing. Reopen and click Yes: exactly the two items should disappear, native currency/enchantment refunds should apply, the favorite and equipment must remain, and completion must report the correct totals. A stale/replaced or newly ineligible item must be skipped. Escape/close/Clear stops remaining work. Test Select All only after the small batch, then host/join separately. Vanilla undo restores the last item only. Native runtime success is not established by source/mocks/CI.

## UI v4 acceptance (2026-10-05)

v3 favorites and selection/Select All are user-confirmed. Check the v4 reserved toolbar against the existing navigation footer, font sizing and all button clicks; four cyan edges must follow each selected physical slot/item across scrolling, filtering and grid rebuilds. Favorite gold markers and Unlock caption must agree with synchronous vanilla salvage refusal. Selected favorites/equipment must be excluded. Salvage opens a count-specific modal, No retains selection, preview Yes deletes nothing, and Escape/close/Clear reset pending confirmation. Test host and joining-player sessions independently. See [INVENTORY_UI_DESIGN.md](INVENTORY_UI_DESIGN.md) for detailed checks and unverified limits.

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

The revised open guard calls Widget.IsVisible on InventoryHUD as the controller does. Additional negative tests reject checking visibility on the wrong local or importing IsVisible under the wrong native class. Inventory reads and UI construction remain unreachable before or after the closed path of that guard.

## Required online/co-op gate after the inventory crash

PR #12 inventory probe is withdrawn after an online inventory-open crash. Test the replacement alone after a full restart, with Blueprint Loader present. No activation setting is needed. Record the pak hash, session role, visible initial text, F6/F7 response and inventory-close hiding. A passed camp test does not pass this gate.

| Session | Required checks | Status |
|---|---|---|
| Online alone | Camp and mission inventory, initial text, F6/F7, repeated opens | Pending |
| Host with friends | Own inventory only, friends unaffected, transitions and disconnect | Pending |
| Join a friend | Client bootstrap, own inventory only, transitions/rejoin | Blocked/unverified |
| Friends without mod | Joining/hosting continues; no mod actor or UI replicated | Pending |
| Offline | Inventory remains paused; overlay and keys still work | Pending |

Run mission travel, return to camp, character change, friend join/leave, disconnect/reconnect and simultaneous inventory opens. Verify no remote item reads/writes and no sidecar writes in this probe. Production lock/salvage/loadout tests remain separate and incomplete.

## Typed-reference repair gate (PR #13 withdrawn)

PR #13 also crashes when pressing I online. The replacement materializes string/text values before native reference calls. Verify initial text before F6/F7, repeated inventory opens, item names/power and close hiding. Use online host and join-friend cases; splitscreen does not substitute for these. If it still crashes, retain the new dump and record whether any overlay appeared before the failure. Do not claim all planned features work because this read-only probe passes.

## 2026-10-05 user-confirmed PR #14 result

Passed by user report: inventory opens, visible text, F6/F7 browsing, correct armor/weapon names. Screenshot confirms 216 slots and Ghostly Armor power 163. Not recorded: hosting versus joining a friend, simultaneous friends, inventory-close hiding, repeated use/mission travel. Do not generalize this to persistent locks, full feature graph or network client bootstrap.

## Mouse-control feature crash follow-up

Remove v1/older inventory QoL paks; install exactly one new variant. Test favorites-only first: inventory open/close, mouse button, lock/unlock, ordinary salvage refusal while locked, two identical items, reopen. Then replace it with the combined preview: mode on/off, repeated clicks toggling selection, Select item, Select All exclusions, favorite a selected item, review/cancel, closing while armed, filtering, and no action from old F5/F8/F9/F10 controls. Preview confirmation must not delete anything. Check readability and click targets at the user's resolution. Test hosted and joined online sessions separately; the patch has no GameMode/loader bootstrap but that alone does not prove client support. Native deletion, undo, stale-item revalidation and travel/identity persistence are later runtime gates.

Follow-up audit regression: exiting mode must retain the queue for Review, while explicit Cancel must clear it. Favorite/Select must target the item captured at click time, not a later highlight or replacement in the same slot. The 20 generated action/layout tests cover these logical and function-signature invariants with mocks; they are not a substitute for the manual tests above.
