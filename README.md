# Minecraft Dungeons QoL

A quality-of-life mod for **Minecraft Dungeons 1** focused on safer, faster inventory management.

## Core features

- **Gear Locking**: protect important items from salvage.
- **Gear Manager / Loadouts**: save multiple melee, armor, ranged, and artifact setups.
- **Mass Salvage**: select several eligible items, review the batch, then salvage them together.
- **Safety first**: equipped, locked, loadout-assigned, or unresolved items must never be bulk-salvaged.

## Supported PC installs

Minecraft Dungeons ownership and the location of the installed game are separate concerns.

The project is being built to support:

- **Microsoft Store / Xbox app installs**, including the common case where the game is owned through Microsoft Store but launched from the normal Minecraft Launcher.
- the older standalone **Minecraft Launcher installation layout**.
- Steam validation later in the hardening phase.

### Important: Microsoft Store ownership launched from Minecraft Launcher

If Minecraft Launcher shows only a drive such as `C:` or `D:` for the Dungeons installation, the Launcher is using the Microsoft Store/Xbox-managed installation. Use the **Microsoft Store / Xbox app path** below.

Do **not** assume that launching from Minecraft Launcher means the files are under `%LOCALAPPDATA%\Mojang\products`.

## Installing a release

### 1. Find the active `Paks` folder

#### Microsoft Store / Xbox app install

Open the Xbox app:

1. Minecraft Dungeons
2. **Manage**
3. **Files**
4. Note the installation folder.

A common default is:

```text
C:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks
```

It may instead be on another drive, for example:

```text
D:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks
```

This is also the path family to use when Minecraft Launcher is merely launching the Store/Xbox-managed copy.

#### Older standalone Minecraft Launcher install

If Minecraft Launcher shows a real folder path for its Dungeons installation, use that folder and locate:

```text
<launcher-install>\dungeons\dungeons\Dungeons\Content\Paks
```

Older Launcher installs have an important caveat: launching Dungeons through Minecraft Launcher can restore/remove modified files. For that layout, launch `Dungeons.exe` directly after installing mods.

### 2. Create the mods folder

Inside the active `Paks` folder, create:

```text
~mods
```

Example:

```text
C:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks\~mods
```

### 3. Install Blueprint Loader

Minecraft Dungeons QoL uses **Blueprint Loader for Minecraft Dungeons 1** as a separate runtime dependency:

https://www.nexusmods.com/minecraftdungeons/mods/111

Blueprint Loader is not bundled with this repository because its redistribution permissions require it to remain a separate download.

Install Blueprint Loader into the **same Dungeons installation that you actually run**.

### 4. Install Minecraft Dungeons QoL

Put:

```text
MinecraftDungeonsQoL.pak
```

inside:

```text
Dungeons\Content\Paks\~mods
```

Example:

```text
C:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks\~mods\MinecraftDungeonsQoL.pak
```

For a modern Store/Xbox install, launch normally through Minecraft Launcher, Xbox app, Microsoft Store, or Start.

For the older standalone Launcher layout, launch its `Dungeons.exe` directly if Minecraft Launcher removes the mod files.

Updating the mod is simply replacing the old `MinecraftDungeonsQoL.pak` while the game is closed.

## Development approach

The primary development route is now:

1. **Unreal Engine 4.22.x**
2. **Dungeons Mod Kit**
3. a small project-owned **mirror of verified Dungeons reflection APIs**
4. normal Blueprint authoring against those mirror classes
5. cook/package through the Mod Kit
6. load the resulting assets through **Blueprint Loader**

This is preferable to raw cooked-Blueprint bytecode patching. The mirror module is also named `Dungeons`, so Blueprint references compile against paths such as `/Script/Dungeons.ItemStashComponent`, which the shipping game can resolve to its real classes at runtime.

Only reflection-visible classes, properties, enums, and functions that we can substantiate are mirrored.

## Building from source

Development requirements:

- Windows
- Unreal Engine **4.22.x**
- Python 3.8+
- Dungeons Mod Kit
- Minecraft Dungeons 1

Check the environment:

```powershell
./scripts/Check-Environment.ps1
```

Bootstrap the pinned Mod Kit:

```powershell
./scripts/Bootstrap-ModKit.ps1
```

Build:

```powershell
./scripts/Build.ps1
```

Install the resulting pak:

```powershell
./scripts/Install.ps1
```

If automatic game detection chooses the wrong copy:

```powershell
./scripts/Install.ps1 -PaksPath "D:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks"
```

## Current development status

The final-build Dungeons inventory architecture has been researched far enough to identify the native Blueprint-facing operations needed for the core mod:

- inventory slot enumeration
- equipped slot enumeration
- native slot swapping
- native salvage
- native salvage undo metadata

Mass salvage will therefore use Dungeons' own `SalvageItemInSlot` transaction rather than recreating emerald/gold/enchantment-point calculations.

The next build milestone is compiling the mirror API into the UE4.22 Mod Kit project, then authoring the first playable lock + batch-salvage Blueprint slice.

See [docs/CURRENT_STATE.md](docs/CURRENT_STATE.md).

## Documentation

Start at [docs/README.md](docs/README.md).

- [Current state](docs/CURRENT_STATE.md)
- [Findings](docs/FINDINGS.md)
- [Modding research](docs/MODDING_RESEARCH.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Feature specification](docs/FEATURE_SPEC.md)
- [Roadmap](docs/ROADMAP.md)
- [Ideas](docs/IDEAS.md)
- [Inventory identity](docs/INVENTORY_IDENTITY.md)
- [Test plan](docs/TEST_PLAN.md)
- [Installation notes](docs/MICROSOFT_STORE_SETUP.md)
- [Third-party / licensing notes](docs/THIRD_PARTY.md)
- [Research log](docs/RESEARCH_LOG.md)

## Project rule

Every meaningful implementation or research pass must update the canonical docs with what changed, what was learned, decisions made, tests/results, and next work.
