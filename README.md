# Minecraft Dungeons QoL

A quality-of-life mod for **Minecraft Dungeons 1** focused on fixing the inventory chores that become painful once you start keeping several builds.

## Planned core features

- **Gear Locking**: protect important items from salvage.
- **Gear Manager / Loadouts**: save and switch between multiple gear sets.
- **Mass Salvage**: select several items, review the batch, then salvage them together.
- **Safety first**: equipped, locked, loadout-assigned, or unresolved items are protected from destructive actions.

## Supported PC installs

The mod targets the same Windows Dungeons 1 game regardless of where the Microsoft account entitlement came from.

Supported install layouts:

- **Minecraft Launcher**, including players who bought/own Dungeons through Microsoft Store but install and launch it from the normal Minecraft Launcher.
- **Xbox app / Microsoft Store installation**.
- Steam is planned for validation as well, but the first testing target is the Microsoft-account builds above.

The important part is **which Dungeons installation is actually being launched**, not where the license was purchased.

## Installing a release

### 1. Find the active Dungeons `Paks` folder

If you use the **normal Minecraft Launcher**, a common path is:

```text
%LOCALAPPDATA%\Mojang\products\dungeons\dungeons\Dungeons\Content\Paks
```

If you installed Dungeons through the **Xbox app / Microsoft Store**, a common path is:

```text
C:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks
```

The Xbox app may place `XboxGames` on another drive, for example:

```text
D:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks
```

If you have more than one Dungeons installation, use the `Paks` folder belonging to the copy you actually launch.

### 2. Create the Blueprint Loader mods folder

Inside `Paks`, create a folder named exactly:

```text
~mods
```

The final path should look like one of these:

```text
%LOCALAPPDATA%\Mojang\products\dungeons\dungeons\Dungeons\Content\Paks\~mods
```

or:

```text
C:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks\~mods
```

### 3. Install Blueprint Loader

This mod uses **Blueprint Loader for Minecraft Dungeons 1** as a separate runtime dependency.

Download/install Blueprint Loader from its original Nexus Mods page:

https://www.nexusmods.com/minecraftdungeons/mods/111

Follow Blueprint Loader's installation instructions and place its required files in the same Dungeons installation you actually launch.

Blueprint Loader is **not bundled** with this project because its redistribution permissions do not allow us to simply repackage it.

### 4. Install Minecraft Dungeons QoL

Put the release file:

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

Then launch Minecraft Dungeons normally through the Minecraft Launcher or Xbox app.

Updating the mod is simply replacing the old `MinecraftDungeonsQoL.pak` with the newer one while the game is closed.

## Building from source

Development currently targets:

- Windows
- Unreal Engine **4.22.x**
- Python 3.8+
- Dungeons Mod Kit
- Blueprint Loader
- Minecraft Dungeons 1

Check the local environment:

```powershell
./scripts/Check-Environment.ps1
```

Bootstrap the pinned Dungeons Mod Kit:

```powershell
./scripts/Bootstrap-ModKit.ps1
```

Build once project Blueprint assets are available:

```powershell
./scripts/Build.ps1
```

Install the built pak automatically:

```powershell
./scripts/Install.ps1
```

The installer searches both the normal Minecraft Launcher path and Xbox app paths.

If more than one copy exists, specify the active one:

```powershell
./scripts/Install.ps1 -PaksPath "$env:LOCALAPPDATA\Mojang\products\dungeons\dungeons\Dungeons\Content\Paks"
```

or:

```powershell
./scripts/Install.ps1 -PaksPath "D:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks"
```

## Current development status

The repository now has the build/install foundation and the exact native Dungeons inventory/salvage API has been identified from final-build reverse-engineering work.

Most importantly, Dungeons exposes Blueprint-callable inventory functions including:

- inventory slot enumeration
- equipped slot enumeration
- the native salvage transaction
- native salvage undo information

That means the bulk-salvage implementation can use the **same game operation as normal salvage**, rather than recreating reward calculations.

The next milestone is producing and validating the first playable Blueprint Loader build.

See [docs/CURRENT_STATE.md](docs/CURRENT_STATE.md) for the exact implementation state.

## Documentation

Start at [docs/README.md](docs/README.md).

Key project records:

- [Current state](docs/CURRENT_STATE.md)
- [Findings](docs/FINDINGS.md)
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
