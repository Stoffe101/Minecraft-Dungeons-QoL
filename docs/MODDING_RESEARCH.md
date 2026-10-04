# Minecraft Dungeons 1 Modding Research

Last updated: 2026-10-04

This document records the project-level research into how Minecraft Dungeons 1 mods are actually built and loaded, which tools are proven, which approaches are experimental, and which approach this project should prefer.

The goal is to avoid designing the project around guesses or stale assumptions.

## Executive conclusion

For a gameplay/UI mod such as Minecraft Dungeons QoL, the best-supported Dungeons 1 development path remains:

1. Unreal Engine 4.22.x
2. Dokucraft Dungeons Mod Kit
3. Blueprint Loader
4. Project-owned Blueprint actor/widget assets
5. Small loader levels under the Blueprint Loader trigger folders
6. Cook through UE4.22
7. Package the cooked files into a normal Dungeons .pak
8. Install the .pak in the active game's Dungeons/Content/Paks/~mods folder

This is not merely a historical 2021 workflow. Dungeons 1 mods targeting 1.10.3.0 were still being released or updated in 2026 and explicitly describe themselves as made using the Dungeons Mod Kit.

Primary references:

- Dungeons Mod Kit: https://github.com/Dokucraft/Dungeons-Mod-Kit
- Blueprint Loader: https://www.nexusmods.com/minecraftdungeons/mods/111
- Camera Coordinates Overlay example: https://github.com/EvenTorset/Camera-Coordinates-Overlay
- Camera Coordinates Overlay permissions: https://www.nexusmods.com/minecraftdungeons/mods/112
- LetMeMove: https://github.com/StainlessStasis/LetMeMove
- Current Dungeons 1 Mod Kit example: https://www.nexusmods.com/minecraftdungeons/mods/186
- UAssetAPI: https://github.com/atenfyr/UAssetAPI

## 1. Dungeons Mod Kit is the canonical authoring environment

The Dungeons Mod Kit requires Windows, Python 3.8+, and Unreal Engine 4.22.x.

The Mod Kit explicitly warns that using a different Unreal version can cause strange problems.

The project contains a Dungeons UE4 project with editor binaries and stubs that allow mod authors to create and cook assets compatible with Minecraft Dungeons.

### What its build scripts actually do

The Mod Kit cook script:

1. reads the configured UE4 editor path
2. removes old cooked Unreal assets from the staging directory
3. runs UE4Editor-Cmd.exe with -run=cook -targetplatform=WindowsNoEditor
4. copies cooked assets into the Dungeons staging tree
5. copies project-provided precooked files into that same tree

The package script then calls u4pak.py to pack the staged Dungeons tree into the configured output .pak.

Project implication:

Our normal release pipeline should produce assets the same way unless we have a specific reason not to. We should not invent a custom pak layout while the standard Mod Kit already matches the game's expected mount structure.

## 2. Blueprint Loader is the correct runtime entry point

Blueprint Loader exists specifically to execute mod-created Blueprints inside existing Dungeons levels without replacing every level.

It exposes three trigger locations:

    /Game/BPLoader/Menu
    /Game/BPLoader/Lobby
    /Game/BPLoader/Ingame

A level placed in one of these paths is loaded when its corresponding game state is entered.

Recommended project structure:

    BPLoader/Lobby/MCDQoL_Lobby.umap
        -> contains BP_MCDQoL_Manager

    BPLoader/Ingame/MCDQoL_Ingame.umap
        -> contains BP_MCDQoL_Manager

The manager should then create and manage widgets and inventory services.

This keeps loader integration tiny, gameplay logic in our own namespace, UI creation under our control, and Camp/missions supported independently.

## 3. Proven UI pattern: actor creates a widget

Camera Coordinates Overlay was created specifically as a Blueprint Loader example.

Its architecture is:

    Blueprint Loader level
        -> WidgetAdder actor
            -> creates CameraCoordsOverlay widget
                -> Add To Viewport

It also checks whether the widget is already present before creating another copy.

This is a strong template for Minecraft Dungeons QoL.

Our equivalent:

    MCDQoL loader level
        -> BP_MCDQoL_Manager
            -> create WBP_MCDQoL_Overlay
            -> add to viewport
            -> bind/update inventory state

The lock and mass-salvage interface therefore does not need to replace the entire vanilla inventory screen just to display controls.

Replacing a full vanilla UI asset should remain a last resort because it is more likely to conflict with game updates, other UI mods, and internal widget changes.

## 4. Use Dungeons' native inventory and salvage functions

Research against final-build Dungeons class reconstruction identified the real inventory backend.

Important classes:

    UItemStashComponent
    UInventoryItemSlot

Important Blueprint-facing methods include:

    GetInventorySlots()
    GetEquipmentSlots()
    GetChangeIndex()
    EnterInventoryUI()
    ExitInventoryUI()
    SalvageItemInSlot(...)
    SalvageItemUndo(...)
    GetSalvageInfo(...)
    CompareItemPowerWithEquipped(...)
    AvailableEnchantmentPoints()

Mass salvage should not implement salvage itself.

Correct flow:

    Selected UInventoryItemSlot
        -> verify it still exists
        -> verify not equipped
        -> verify not user-locked
        -> verify not loadout-protected
        -> call native SalvageItemInSlot
        -> inspect success/result

This preserves the game's own emerald/gold reward calculation, returned enchantment points, item destruction flow, salvage state, and undo information where available.

Direct hero-save editing and custom currency changes are unnecessary and less safe.

## 5. Item identity is separate from item access

Save-format research shows hero fields such as uniqueSaveId and playerId, and item fields such as inventoryIndex, equipmentSlot, type, power, rarity, enchantments, and gilded/netherite data.

uniqueSaveId + inventoryIndex is a useful provisional locator, but it is not a proven permanent GUID.

Storage movement can assign another inventory index and removed high indexes can eventually be reused.

Persistent locks/loadouts should prefer:

1. native stable runtime item ID/GUID if available
2. otherwise hero ID + inventory index + sanity fingerprint
3. fail closed if identity becomes ambiguous

See INVENTORY_IDENTITY.md.

## 6. UAssetAPI is valuable, but not the primary authoring tool

Modern UAssetAPI supports cooked and uncooked Unreal assets, UE4-era assets, raw Kismet Blueprint bytecode, import/export inspection, and property inspection.

Our CI has already verified that UAssetAPI 1.1.0 successfully parses a known working Minecraft Dungeons 1 Blueprint asset.

It correctly reads Unreal object version 517, name tables, imports, exports, and Blueprint function exports.

Best project uses:

- inspect working Dungeons mods
- verify package/import/function assumptions
- automated regression checks
- modify precooked template assets where legally permitted
- check that generated assets remain parseable
- automate narrow Blueprint changes

Why it is not the first-choice authoring environment:

Raw Blueprint packages contain more than runtime bytecode. They include import tables, generated-class metadata, exported functions/properties, graph/editor data, references, and package metadata.

Creating a complex widget or actor by manually manufacturing all of that is more fragile than creating it in the matching Unreal Editor.

Therefore UE4.22 editor authoring is primary. UAssetAPI patching is the automation/fallback route.

## 7. KismetKompiler is experimental for this project

KismetKompiler can decompile and compile Unreal Kismet scripts and is MIT licensed.

However:

- its pinned UAssetAPI dependency is old
- that old dependency does not parse our working Dungeons template correctly
- modern UAssetAPI does parse it
- KismetKompiler expects APIs removed or refactored in newer UAssetAPI versions

We are researching a compatible intermediate UAssetAPI version.

Until that is proven, the release pipeline must not depend on KismetKompiler.

## 8. Dungeons II tooling is architectural evidence, not drop-in code

Minecraft Dungeons II has newer tooling such as Blueprint Mod Template and NeoRune.

NeoRune demonstrates an important architecture:

    game reflection data
        -> generated API bindings
        -> C# source
        -> compiled into Unreal Kismet bytecode
        -> cooked Blueprint package
        -> Blueprint Loader

This confirms programmatic Kismet generation is viable in principle.

However Dungeons II uses another Unreal generation and toolchain. We can learn from the architecture but must not assume its packages or APIs work in Dungeons 1.

## 9. Existing mod reuse policy

Reusing proven implementation is encouraged when permission is clear.

### Dungeons Mod Kit

MIT licensed.

Allowed for project use with license compliance.

### LetMeMove

MIT licensed.

Useful for current Blueprint Loader compatibility evidence, working actor package structure, and working loader-level structure.

### Camera Coordinates Overlay

Its published Nexus permissions explicitly allow modification and asset reuse.

It is especially valuable as a working UI-widget + WidgetAdder template.

Even though the GitHub repository itself does not expose an obvious license file, the published mod permissions are explicit. Any reused asset should still be documented in THIRD_PARTY.md.

### Sources with no clear permission

Do not copy source/assets from a GitHub repository merely because it is public.

If a repo has no license and no external permissions granting reuse:

- inspect it for research
- learn architecture/function names
- do not copy its code/assets into our release

## 10. Precooked assets are a legitimate path

The Dungeons Mod Kit build process supports a Precooked directory.

If we modify an already cooked Blueprint package with UAssetAPI or another compatible tool, it does not necessarily need to pass through UE4 cooking again.

A viable advanced pipeline is:

    permitted working cooked template
        -> UAssetAPI modification
        -> binary-equality/parse validation
        -> place under Precooked with correct package path
        -> package with normal Dungeons Mod Kit pak pipeline

This is likely the best fallback when a particular asset cannot conveniently be recreated in the editor.

## 11. Package path matters

Unreal asset references are package-path based.

Moving a binary .uasset to another folder without updating internal references can break it.

Final project-owned assets should preferably be created directly under:

    /Game/Mods/MinecraftDungeonsQoL/

rather than permanently shipping another mod's package names.

Existing binary templates may be used for experiments, but final release assets should ultimately use the project namespace to avoid collisions.

## 12. Installation model

A player's license source is not the important part. The important part is the installation that actually launches.

Common Dungeons 1 Paks locations include:

Minecraft Launcher:

    %LOCALAPPDATA%\Mojang\products\dungeons\dungeons\Dungeons\Content\Paks

Xbox app / Microsoft Store style:

    <drive>:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks

Mods are placed under:

    Paks\~mods

Blueprint Loader must be installed into the same active game installation.

## 13. Blueprint Loader must remain a separate dependency

Blueprint Loader's published permissions prohibit redistributing its assets without permission.

Therefore our release should contain MinecraftDungeonsQoL.pak and project documentation, but not silently bundle Blueprint Loader.

Players install Blueprint Loader from its original page.

## 14. Testing strategy for destructive inventory code

A bulk-salvage mod has a higher safety requirement than a cosmetic mod.

Initial destructive tests should use a disposable/test hero, cheap/common gear, and backed-up save data.

Required progression:

1. Mod loads in Camp.
2. Mod loads in missions.
3. Inventory slots enumerate correctly.
4. Equipment slots are excluded.
5. Select/deselect does not mutate items.
6. Lock/protection does not mutate items.
7. One-item native salvage works.
8. Reward matches vanilla.
9. Multi-item sequential salvage works.
10. Cancel flow works.
11. Item removed/reordered between selection and confirmation is handled.
12. Lock changed after selection is handled.
13. Controller input works.
14. Restart persistence works.
15. Multiple heroes work.
16. Storage transfer works.
17. Online co-op remains local-player-only.

Never test the first destructive implementation against irreplaceable gear.

## 15. Recommended architecture

Runtime:

    BPLoader Lobby/Ingame level
        |
        v
    BP_MCDQoL_Manager
        |
        +-- locate local player
        +-- find UItemStashComponent
        +-- create WBP_MCDQoL_Overlay
        |
        +-- BP_MCDQoL_LockService
        +-- BP_MCDQoL_SalvageService
        +-- BP_MCDQoL_LoadoutService

Salvage service:

    selection set
        -> preflight
        -> protection check
        -> equipment exclusion
        -> slot re-resolution
        -> SalvageItemInSlot
        -> result logging

Lock service:

    runtime item identity
        -> stable key if available
        -> fallback identity reconciliation
        -> separate QoL persistence

Loadout service:

Resolve real inventory/equipment slots and use native slot swap/equip APIs instead of rebuilding item data.

## 16. Chosen development strategy

### Path A: standard UE4.22 authoring

Preferred.

- extend the Dungeons Mod Kit editor-facing type stubs with only the Dungeons classes/functions needed by this mod
- author project-owned actor/widget assets in UE4.22
- cook normally
- package normally

Advantages:

- robust Blueprint metadata
- proper widget creation
- easy UI iteration
- project-owned package paths
- least binary-format guesswork

### Path B: permitted working-template reuse

Use existing working Blueprint Loader mods where permissions allow it.

Best current UI reference/template:

- Camera Coordinates Overlay

Best MIT actor/runtime reference:

- LetMeMove

Advantages:

- starts from packages known to load in Dungeons
- useful for validating automated tooling
- can bootstrap before every editor-facing Dungeons type has been stubbed

### Path C: UAssetAPI precooked modification

Use modern UAssetAPI to modify allowed cooked templates and feed them through Precooked.

Advantages:

- CI friendly
- avoids UE4 editor dependency for narrow changes

Disadvantages:

- package metadata/import management is more complex
- structural widget edits can be painful

### Path D: KismetKompiler automation

Only after a compatible toolchain is proven.

Do not block the mod on this path.

## 17. Things we deliberately avoid

- guessing function names when final-build information exists
- direct modification of the player's hero save for locks
- custom emerald/gold/enchantment-point reward code
- treating inventoryIndex as an eternal unique ID
- replacing the entire vanilla inventory screen unless necessary
- copying unlicensed community assets
- bundling Blueprint Loader against its permissions
- relying on a different Unreal Engine version
- claiming a build works before it has been launched in the real game

## 18. Current direction after research

The technology choice is now stable:

Minecraft Dungeons QoL is a UE4.22 Blueprint Loader mod built and packaged using the established Dungeons Mod Kit workflow.

UAssetAPI and Kismet tooling are supporting tools for inspection, validation, automation, and precooked modifications.

Next implementation work:

1. expose the verified Dungeons inventory classes/functions in the Mod Kit editor project
2. create the project-owned manager actor
3. create the project-owned UI overlay
4. get a harmless diagnostic build loading in Camp/Ingame
5. enumerate inventory/equipment slots
6. implement non-destructive selection/lock state
7. only then enable the native salvage call
