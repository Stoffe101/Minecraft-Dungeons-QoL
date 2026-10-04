# Research Log

## 2026-10-04 — Project bootstrap

### Goal

Establish a safe development foundation for a Minecraft Dungeons 1 QoL mod containing:

- gear locking
- gear manager/loadouts
- multi-select mass salvage

### Findings

- Dokucraft Dungeons Mod Kit uses UE4.22 and is MIT licensed.
- Blueprint Loader injects content at Menu, Lobby, and Ingame triggers.
- Current Dungeons 1 community work still uses the established modding pipeline.
- Modern Xbox app / Microsoft Store installs expose an accessible Paks directory.
- Vanilla salvage returns currency and invested enchantment points.
- Mass salvage should invoke the native salvage path rather than reimplementing the economy.

### Decisions

- Use a separate project-owned SaveGame slot for lock/loadout metadata.
- Do not mutate hero save files.
- Prefer overlay UI and native game APIs over wholesale vanilla asset replacement.
- Native vanilla salvage must be the destructive backend.

### Implementation completed

- repository documentation structure
- Mod Kit bootstrap tooling
- environment/path detection
- asset sync/build/install tooling
- initial feature/architecture/test specifications
- offline inventory asset research helper

### Not yet tested

PowerShell tooling still needs verification on a real Windows Dungeons development machine.

## 2026-10-04 — Save identity investigation

### Source

CutFlame/MCDSaveEdit:
https://github.com/CutFlame/MCDSaveEdit

### Findings

The save model contains:

- profile `playerId`
- profile `uniqueSaveId`
- item `inventoryIndex`
- item `equipmentSlot`
- type, rarity, power, enchantments, gilded/netherite data

The editor's item-list logic sorts by inventory index and assigns new items `max(existing index) + 1`.

Storage transfer can assign a new target-collection index, so `inventoryIndex` is not a permanent cross-storage identity.

### Result

`uniqueSaveId + inventoryIndex` is a provisional locator, not a guaranteed permanent identity. Prefer a native runtime GUID if one exists. Otherwise combine the locator with sanity/fingerprint data and fail closed on disagreement.

## 2026-10-04 — Microsoft Store ownership through Minecraft Launcher

### Finding

Microsoft-account ownership and game-file location are separate concerns.

For mod installation, the authoritative location is the `Dungeons\Content\Paks` folder belonging to the copy actually launched.

Common layouts:

- Minecraft Launcher: `%LOCALAPPDATA%\Mojang\products\dungeons\dungeons\Dungeons\Content\Paks`
- Xbox app: `C:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks`

### Project impact

The project supports both path families and permits explicit override when multiple copies are installed.

## 2026-10-04 — Native inventory/salvage API research

### Source

Minecraforever/MCD-PE:
https://github.com/Minecraforever/MCD-PE

### Findings

Final-build-verified class architecture includes:

- `UItemStashComponent`
- `UInventoryItemSlot`
- equipment-slot enumeration
- inventory-slot enumeration
- slot swap functions
- native `SalvageItemInSlot`
- native salvage undo/info functions

### Result

The QoL mod does not need custom salvage reward calculations.

Bulk salvage can validate each chosen slot and call the game's own native salvage transaction.

## 2026-10-04 — Proven Dungeons modding workflow research

### Findings

Research across the Dungeons Mod Kit, Blueprint Loader documentation, current Dungeons 1 mods, and known Blueprint Loader examples established the preferred production workflow:

1. UE4.22.x
2. Dungeons Mod Kit
3. Blueprint Loader
4. small Lobby/Ingame loader levels
5. manager actor
6. actor-created UI widgets
7. cook with UE4.22
8. package with the Mod Kit/u4pak pipeline
9. install the pak under the active game's `Paks\~mods`

Camera Coordinates Overlay is explicitly published as a Blueprint Loader example and demonstrates the actor-to-widget pattern we need.

A Dungeons 1 mod updated in 2026 explicitly states it was made using the Dungeons Mod Kit, confirming the toolchain is still relevant to the final Dungeons 1 release.

### Binary tooling result

UAssetAPI 1.1.0 successfully parses the known-working LetMeMove Dungeons Blueprint:

- object version 517
- 228 names
- 67 imports
- 83 exports

KismetKompiler's old UAssetAPI cannot parse the same asset. Replacing the dependency with modern UAssetAPI causes source-API incompatibilities, so KismetKompiler remains an experimental automation path rather than the main implementation strategy.

### Decision

Standard UE4.22 authoring is now the primary path.

Permitted existing mod assets are allowed as templates where they materially reduce risk, especially Camera Coordinates Overlay for UI bootstrapping and LetMeMove for a known-working actor/loader structure.

Modern UAssetAPI remains the preferred inspection/validation/precooked-patching tool.

Full analysis is documented in `MODDING_RESEARCH.md`.
