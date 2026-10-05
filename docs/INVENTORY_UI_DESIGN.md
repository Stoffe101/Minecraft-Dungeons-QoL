# Inventory UI redesign — 2026-10-05

## v8: thicker red selection and equipped favorite stars — 2026-10-06

User confirms v7 visuals/placement work and requests thicker red selection and favorite icons on equipped gear. The four selection strips now use width/height 6 (previously 3), RGB 1/.12/.12, inset 1, with bottom/right offsets -7. Favorite stars and badge layout remain unchanged. Marker targets copy InventorySlotsInGrid, then append valid dynamically cast EquipSlots widgets with Array_AddUnique. All six observed equipment slots use the same base/native item contract. Native selection still excludes equipment; adding marker targets does not make equipped gear salvageable. Existing detached-widget pruning, physical item lookup and change-only visibility apply to both collections.

The user also reports favorite loss after a mission. Inspector-owned favorites cannot meet until-explicit-unfavorite persistence. v8 is a visual update ONLY while new evidence is collected. Stable hero/item runtime IDs remain unverified; names/power, ItemId/type, slot position and UObject paths are not safe replacements. See INVENTORY_IDENTITY.md and GAME_EVIDENCE.md for the blocker and prepared metadata-only collector.

## v7: recognizable favorite markers

User's supplied crops show an Elite Power Bow grid tile and the UNIQUE/GILDED/CUSTOM inspector row. Requested an icon on favorite tiles and a detail label. Replace the 8-unit square with a 25x25 dark-backed gold pixel star at the top-right, 5 units from the edges. The top-left enchantment count and bottom-right power remain clear. Build the star from native hit-test-invisible Border strips inside a CanvasPanel, avoiding font-dependent star glyphs, external textures or another replaced game asset. Five per-widget mark references remain; their common type is Widget so the favorite canvas and four selection borders can share cleanup/visibility logic. No new marks are allocated on unchanged ticks.

The FAVORITE label appends to the native tag HorizontalBox after rarity/gilded/custom. Private InspectInfo metadata provides UMG_ItemTagIconName and InspectedItem; its cooked tag widget provides UMG_ItemRarity. That rarity widget's reflected GetParent yields the existing row, checked with a HorizontalBox cast. Unsupported/missing rows hide the badge. A native Border with inset label joins through AddChildToHorizontalBox with 7-unit spacing and vertical centering. The label copies the actual ItemName pixel font at size 16, not a guessed font. Collapsing it removes its layout allocation. Resolve visibility against inspected item identity; null, different and unfavorited items cannot inherit it. If the inspector replaces the row, detach the previous badge before rebuilding. One badge is retained per HUD.

Pinned Epic UE4.22.3 headers (99a530d4ccbe6bea1e8f49df20acfeb294006962) verify reflected [Widget.GetParent](https://github.com/folgerwang/UnrealEngine/blob/99a530d4ccbe6bea1e8f49df20acfeb294006962/Engine/Source/Runtime/UMG/Public/Components/Widget.h), [HorizontalBox.AddChildToHorizontalBox](https://github.com/folgerwang/UnrealEngine/blob/99a530d4ccbe6bea1e8f49df20acfeb294006962/Engine/Source/Runtime/UMG/Public/Components/HorizontalBox.h), [HorizontalBoxSlot padding/alignment](https://github.com/folgerwang/UnrealEngine/blob/99a530d4ccbe6bea1e8f49df20acfeb294006962/Engine/Source/Runtime/UMG/Public/Components/HorizontalBoxSlot.h) and [Border padding](https://github.com/folgerwang/UnrealEngine/blob/99a530d4ccbe6bea1e8f49df20acfeb294006962/Engine/Source/Runtime/UMG/Public/Components/Border.h). Current Epic docs corroborate the APIs; pinned headers establish version-specific availability. No engine code copied.

39 generated action/layout mock cases per fixture include inspected favorite visibility, unchanged no-op, physical-item change, unfavorite, and null inspector. 27 declaring-owner cases include the new row/get-parent calls. All three modes compile/write/reopen with 11,152 original exports preserved; private pak still contains only four HUD/inspector files. These checks do not establish runtime layout/loading, GPU stability, persistence or joining-player support. Test tags with all existing badges present, item switching, scrolling/filtering, and unfavorite disappearance. Native salvage remains enabled only in explicit native packages.

## v6: relocate Favorite and avoid unchanged UI writes

The user confirms native multi-salvage in v5 works. The bottom-item screenshot shows Unlock favorite covering item details. Favorite now shares the left toolbar: combined x=510, width=156; count x=686. Favorites-only uses x=50 and an empty status at x=218. Bottom anchor, y=-72, height=36 and content/footer reservation remain. This supersedes the historical right-side Favorite location below; full-screen/scaling validation still needs the game.

Previously every tick collapsed each mark before showing selected/favorite marks again. All/Salvage/Clear and the modal did the same. They now choose one final visibility and read Widget.GetVisibility into a typed byte/ESlateVisibility local, invoking SetVisibility only on change. Visible marks remain HitTestInvisible. Mode/Favorite captions and the confirmation question cache their strings, avoiding unchanged SetText calls. Physical selection and native salvage behavior remain the v5 implementation.

34 generated action/layout checks include repeated visibility updates: unchanged hidden/selected marks perform zero writes, selection and deselection each perform one. 23 declaring-owner checks include GetVisibility/SetVisibility accepted on Widget and rejected on UserWidget. These mock checks and the three-mode write/reopen checks do not prove rendering, performance or a GPU crash fix.

## v5 follow-up: spacing and native salvage

User's new screenshot shows v4 selecting five items and improved appearance, but “Done selecting” and “Select All” spill outside their bounds. v5 uses Select items / Done, 14-point button text, widths 124/104/104/80 and 12-unit gaps at x=50/186/302/418. Count moves to x=518. Favorite labels also use 14 points. This supersedes the sizes/captions in the historical v4 table below.

The reported non-working salvage comes from the v4 download's deliberate nativeBatchEnabled=false setting. The new private CombinedNative v5 pak is built with --enable-salvage; the default source tool remains non-destructive unless explicitly enabled. Yes now starts the existing guarded one-item-per-tick worker; No still retains selection. Completion reports salvaged/skipped counts. Clear/Escape/close clear counters and stop remaining work.

Rechecked original retail SalvageSlot, CanSalavage and GetItemStash metadata: stash comes from the owning player, native mutation has slot + success-out parameters and a typed undo return, and success broadcasts the original OnItemSalvaged delegate. Native eligibility includes mission salvage availability. Added a declaring-owner regression for Dungeons.ItemStashComponent.SalvageItemInSlot. All original UI data/function bodies remain preserved except established prefixes. Native deletion, refunds, online clients and the new button spacing still require retail verification; source/mocks are not an in-game test.

For v5 install ONLY the CombinedNative pak, replacing the old QoL variant. First test No and then Yes with two expendable, unequipped items and a separate favorite: only the two approved items should disappear, rewards should match native salvage, the favorite must remain. Test Select All separately after that. Original undo restores only the last item; there is no full-batch undo. Favorites remain inspector-instance scoped, not restart/travel/rejoin persistent.

## Evidence and status

The user confirmed v3 favorites work well and the combined pak supports Select All. Their two cropped screenshots show default text spilling out of buttons and a three-line diagnostic wall covering gear. Batch deletion was disabled; this report does not establish deletion, restart persistence or joining-player compatibility. v4 is a private UI test candidate. Original-derived packages remain private; source, tests and docs go in GitHub.

## Implemented layout

| Element | Behavior |
| --- | --- |
| Favorite | Bottom-right, 156 × 36 Slate units; becomes “Unlock favorite” for a protected item. No diagnostic text in FavoritesOnly. |
| Multi salvage | Compact bottom-left toolbar; becomes “Done selecting”. Leaving mode retains the queue. |
| Select All | Appears in selection mode; excludes favorites and all six equipment slots. |
| Salvage / Clear | Appear when applicable. No standalone “Select item” button; click tiles in selection mode. |
| Count | One line: “15 items selected (preview)” in the shipped non-destructive variant. |
| Selected tile | Independent cyan edges, three units thick; preserves vanilla inspection and rarity frames. |
| Favorite tile | Small gold square near its upper-right corner; never also receives a selection frame. |
| Confirmation | Centered dark card, gold accent, count-specific question and separate No/Yes. Preview explicitly says no items will be salvaged. |

Font is copied from the retail HUD's InventorySpaceIndicator (observed NotoBold, size 16) into a constructed FSlateFontInfo local, then adjusted through reflected Size. Labels use 16, count/hints 14, question 18, title 24. Dark muted button tints and text shadows improve contrast. All dimensions follow native Slate/DPI scaling.

Original MainContentLayoutMargins reserves 64 bottom units for the game's footer. At runtime ContentMargins reserves 52 more units through its observed CanvasPanelSlot, preserving its other margins. New buttons sit 72 above the bottom with height 36, between the resized inventory content and original navigation hints. Left controls occupy x=50–450; count starts at 470. Favorite anchors 50 from the right. Full-screen alignment, narrow/ultrawide resolutions and user UI scaling still require retail checks; supplied screenshots are crops.

## Physical identity and lifetime

Original HUD archetypes show CanvasPanel roots for InventoryDelegate, six equipment widgets and embedded generic slots. Marks attach only to a valid runtime canvas reached through reflected UserWidget.WidgetTree / WidgetTree.RootWidget. Unsupported roots are skipped. No additional game asset is replaced.

Each grid widget gets four native Border strips and a gold square once, hit-test-invisible at z=200. Updates resolve the CURRENT native slot/item and require both queued item and matching queued slot. Empty/recycled widgets lose marks; favorites override selection. Detached grid widgets and their paired mark references are removed after rebuilds, avoiding retention of historical grids. Native inspection selection is untouched.

## Confirmation contract

Salvage filters the queue into paired physical slot/item snapshots before showing its count, removing stale, favorite, equipped or ineligible entries. While armed, custom editing controls are disabled and tile selection capture is refused. A full-viewport native Button behind the card consumes background clicks; No receives keyboard focus.

No disarms and clears pending Yes/No/Review without clearing selection. Yes requires an armed, nonempty, paired snapshot and cannot restart a running batch. Shipped CombinedPreview only dismisses; neither preview pak contains native batch mutation. Explicit native builds retain per-item identity, membership, favorites and equipment revalidation. Vanilla undo restores the LAST item only, not the whole batch. Escape and original open/close cancellation remain synchronous.

## Research

Epic UE4.22 mirror pinned at 99a530d4ccbe6bea1e8f49df20acfeb294006962:

- [TextBlock.h](https://github.com/folgerwang/UnrealEngine/blob/99a530d4ccbe6bea1e8f49df20acfeb294006962/Engine/Source/Runtime/UMG/Public/Components/TextBlock.h), [SlateFontInfo.h](https://github.com/folgerwang/UnrealEngine/blob/99a530d4ccbe6bea1e8f49df20acfeb294006962/Engine/Source/Runtime/SlateCore/Public/Fonts/SlateFontInfo.h): reflected font setter/Size; no guessed font layout.
- [CanvasPanelSlot.h](https://github.com/folgerwang/UnrealEngine/blob/99a530d4ccbe6bea1e8f49df20acfeb294006962/Engine/Source/Runtime/UMG/Public/Components/CanvasPanelSlot.h): stretched anchors use margin offsets; fixed anchors use position/size offsets.
- [UserWidget.h](https://github.com/folgerwang/UnrealEngine/blob/99a530d4ccbe6bea1e8f49df20acfeb294006962/Engine/Source/Runtime/UMG/Public/Blueprint/UserWidget.h), [WidgetTree.h](https://github.com/folgerwang/UnrealEngine/blob/99a530d4ccbe6bea1e8f49df20acfeb294006962/Engine/Source/Runtime/UMG/Public/Blueprint/WidgetTree.h): reflected root members. GetRootWidget is NOT reflected and is not imported.
- [Border.h](https://github.com/folgerwang/UnrealEngine/blob/99a530d4ccbe6bea1e8f49df20acfeb294006962/Engine/Source/Runtime/UMG/Public/Components/Border.h), [SlateBrush.h](https://github.com/folgerwang/UnrealEngine/blob/99a530d4ccbe6bea1e8f49df20acfeb294006962/Engine/Source/Runtime/SlateCore/Public/Styling/SlateBrush.h): native tint/default image brush. Thin strips avoid guessed FSlateBrush constants.

## Validation and retail checklist

All three modes compile/write/reopen and preserve 2,584 inspector + 8,568 HUD original exports. 30 generated action/layout mock cases and 17 reflected-owner cases pass per actor fixture, alongside 28 probe and 19 diagnostic rejection cases. New cases cover No retaining queues, stale requests, empty/mismatched/unarmed approval, repeat Yes and non-destructive preview approval. These do NOT execute Unreal/Slate, validate rendering/performance, prove loading or check the economy. Delivered pak unpacking must exactly match the four staged files.

1. Install one variant at a time. Check footer spacing and clicks at normal resolution. Supply a FULL inventory screenshot if alignment needs adjustment.
2. Favorite/unfavorite cheap gear; verify marker, caption, ordinary salvage refusal and exclusion from selection frames.
3. Toggle selections/Select All; check frames/count and unchanged vanilla inspection highlight. Scroll and change filters: marks must follow physical items, never reused widget positions.
4. Done selecting retains queue. Salvage opens the count-specific modal; No preserves it. Preview Yes closes without deleting. Clear, Escape and closing reset pending selection/confirmation.
5. Test UI scaling, narrow/wide resolutions and repeated grid rebuilds for clipping/stutter. Test online hosting and joining separately.

Native salvage/refunds, persistence and loadouts remain unfinished or unverified. The redesign must not be described as a finished retail-tested mod.
