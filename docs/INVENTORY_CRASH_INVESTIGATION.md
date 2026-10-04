# Inventory-open crash investigation — 2026-10-05

## Result and limits

PR #13 still crashed after pressing I in the user's online session. Withdraw its inventory probe. We found and repaired a concrete VM reference-parameter defect in both executable generators. It is consistent with the new crash, but the minidump cannot identify the precise Blueprint call or establish that no further runtime defect remains. Do not label the replacement a working release.

## Dump evidence

The new dump's exception is 0xc0000005, read address 0xffffffffffffffff, Dungeons-relative RIP 0x10238ff. At the fault the instruction is an indirect virtual call through offset 0x48. The captured surrounding code operates on text/history-like interfaces; an FText processing path is a plausible interpretation without symbols, not a proven function name. This differs from PR #12's null UFunction dispatch at 0x1237080 reading 0x98. Heap objects and symbols are unavailable. Raw dumps and account identifiers remain private and uncommitted.

## Source investigation

Inspected Epic-authored UE4.22.3 source in the folgerwang/UnrealEngine mirror at commit 99a530d4ccbe6bea1e8f49df20acfeb294006962. Engine/Build/Build.version explicitly identifies 4.22.3. This is upstream engine source, not the user's patched retail binary. No engine implementation was copied or redistributed.

Relevant files and functions:

- CoreUObject/Public/UObject/Stack.h: FFrame::StepCompiledInRef resets MostRecentPropertyAddress, evaluates an expression into a temporary, then uses the most recent property address if one is present.
- CoreUObject/Private/UObject/ScriptCore.cpp: execLocalVariable sets that address; ProcessContextOpcode's valid path and native CallFunction do not clear it after executing a nested native call.
- Engine/Classes/Kismet/KismetTextLibrary.h: Conv_StringToText takes const FString&, while Conv_TextToString takes const FText&.

Before, the probe emitted SetText(Conv_StringToText(ProbePendingText)). Evaluating ProbePendingText leaves its FString address. The inner conversion returns FText into a temporary but leaves the FString address. The outer FText reference can therefore bind to FString storage. A second unsafe chain passes InventoryItem.GetDisplayNameText directly to Conv_TextToString: the local object context can leave an object-property address where FText is expected. These are concrete generated-code defects independent of which chain the dump reached. Not every nested expression is necessarily unsafe; the generators now follow compiler-style explicit locals rather than relying on undocumented combinations.

## Game and working-mod comparison

Supplied 008_UMG_InventoryItemInspectInfo metadata stores InventoryItem.GetDisplayNameText into a UTextProperty local with its matching context result property, then passes the local to SetText. Locally decoded Dungeons GUI X's GUIX widget (41 properties, eight functions, zero export errors) stores Conv_StringToText, Conv_TextToString and Concat_StrStr outputs in typed locals before subsequent calls. GUI X is AGPLv3 per its author page; its assets/code were inspected, not copied into this project. This validates the emitted pattern against actual cooked graphs, without claiming those graphs ran in this workspace.

## Implementation

Both generators materialize text conversions, native item display names and intermediate string building into typed function locals. Their EX_Context return property points at the assigned local. The full diagnostic generator also materializes slot text before PrintString and fingerprint components before name conversion. The probe passes a UTextProperty local to SetText. No item/save operations are added to the probe. Existing local-player, non-replication, visibility and paused-tick guards remain.

Shared validation rejects non-atomic or wrongly typed string/text reference parameters and mismatched scalar result properties. Five new negative cases cover nested SetText conversion, nested native name conversion, FString passed to SetText, nested concatenation and missing result property. All 19 diagnostic plus 28 probe checks pass locally. The new validator rejects the actual PR #13 packaged manager with `Reference argument must be a typed value: BuildString_Int[0]`; the earlier validator accepted it. This demonstrates the new gate catches previously shipped nesting, not runtime correctness.

## Online friends requirement

The target is network multiplayer, including hosting and joining friends, not splitscreen. Fixing text ABI behavior does not fix client bootstrap. The external Blueprint Loader's GameMode-dependent path remains a likely joining-client blocker. See COOP_COMPATIBILITY.md and MODDING_OPTIONS_REVIEW.md. Host, joining-client, travel/rejoin and friends without the mod all need retail validation.

## Sources

- https://github.com/folgerwang/UnrealEngine/tree/99a530d4ccbe6bea1e8f49df20acfeb294006962/Engine/Source/Runtime/CoreUObject
- https://github.com/folgerwang/UnrealEngine/blob/99a530d4ccbe6bea1e8f49df20acfeb294006962/Engine/Build/Build.version
- https://dev.epicgames.com/documentation/unreal-engine/API/Runtime/CoreUObject/FFrame/StepCompiledInRef (current API corroborates the reference stepping concept; it is not the version-specific proof).
- https://www.curseforge.com/minecraft-dungeons/mods/dungeons-gui-x
- Supplied retail inventory metadata and private crash files inspected locally.

## PR #14 packaged validation (2026-10-05)

Implementation head 102510aed032b4350da9a8ed1dc984d79a38b5c5 passed Windows Cooked QoL Diagnostic Build run 37240868277 and Project Validation run 37240868108. Downloaded both executable artifacts, integrity-unpacked their paks and reran the 19 diagnostic and 28 probe rejection checks against their actual managers. All 47 passed. The packaged probe has 88 statements and 99 exports; its CDO remains non-replicated with all three tick flags enabled and zero interval.

Inventory probe artifact 11317231607: https://github.com/Stoffe101/Minecraft-Dungeons-QoL/actions/runs/37240868277/artifacts/11317231607

Inventory pak SHA-256: `776295667ee229670b882c5c85dd3d07ad1fe969fb2375667d238063f691efa9`. Diagnostic pak SHA-256: `59315a06a8c529a840cd7bc8b6cdc990db7aadf41a74a0741b260d6fd8142e56`. BUILD_INFO records CI synthetic merge 62e799811c567f082aca7e3e591414a353082c94. The offered inventory probe must replace all older QoL paks; Blueprint Loader remains external.

Packaging/structural results do not establish a runtime crash fix. Online hosting, joining friends and the production feature set remain unverified or incomplete.
