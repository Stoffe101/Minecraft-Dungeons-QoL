# Inventory UI redesign — 2026-10-05

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
