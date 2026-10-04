> Runtime status: PR #13 also crashed opening inventory online and is withdrawn. A UE4.22.3 source investigation identified unsafe nested calls into native reference parameters. The new generators store text/string results in typed locals before reuse, matching the supplied game graph. Retail crash repair remains unverified. Online host and joining-friend play are required; splitscreen is outside the current requirement. Production features remain unfinished.

# Minecraft Dungeons QoL

A quality-of-life mod for **Minecraft Dungeons 1** focused on safer, faster inventory management.

**Status: development prototype.** A non-destructive diagnostic pak can be generated. Gear locking, mass salvage and loadouts are not a finished, in-game-validated release yet.

## Planned release features

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

## Installation location and dependencies

These paths apply to a future release and the diagnostic prototype. No finished release is currently available.

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

## Building the current diagnostic

Requirements: Git, Python 3.8+, .NET 8 SDK and PowerShell. No Unreal installation is needed for this cooked-template diagnostic.

```powershell
./scripts/Build-Diagnostic.ps1
```

Output: `dist/diagnostic/MinecraftDungeonsQoL-diagnostic.pak`, `BUILD_INFO.md` and the template MIT license. The build verifies the template checksum, regenerates the sidecar, relocates loader/actor packages and checks the graph. These checks do not execute the mod inside Dungeons.

To install that diagnostic into your active copy:

```powershell
./scripts/Install.ps1 -PakPath "./dist/diagnostic/MinecraftDungeonsQoL-diagnostic.pak" -PaksPath "D:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks"
```

The prototype uses F5 to clear selection, F6/F7 to browse slot indexes, F8 to toggle fingerprint group protection, F9 to select/deselect and F10 twice to preview. **It does not salvage any items.** Fingerprint protection is not stable per-item locking; matching items across heroes share it, and vanilla salvage is not intercepted. See [the audit](docs/REPO_AUDIT.md) before runtime testing.

## Building editor-authored source (production target)

This route is scaffolding until runtime/UI assets are authored under `mod/Content`. `Build.ps1` currently rejects the missing source assets rather than producing an empty release.


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

To supply the installed-game evidence required for the next runtime work, run the read-only collector described in [docs/GAME_EVIDENCE.md](docs/GAME_EVIDENCE.md). It produces `.research/game-evidence.zip` without reading hero saves or modifying the game.

The existing cooked runtime prototype has been audited and hardened into a diagnostic with no destructive calls. Its graph, loader maps and sidecar serialize and package successfully, but the actual reflected item methods, retail loading, persistence and input behavior still need in-game verification.

Stable hero/item identity, equipment/loadout protection, a real review UI, controller support, native salvage testing and the gear manager remain outstanding. API findings from upstream final-Steam-build research do not establish Store/Xbox compatibility by themselves.

See [docs/CURRENT_STATE.md](docs/CURRENT_STATE.md) and [docs/REPO_AUDIT.md](docs/REPO_AUDIT.md).

## Documentation

Start at [docs/README.md](docs/README.md).

- [Repository audit](docs/REPO_AUDIT.md)
- [Current state](docs/CURRENT_STATE.md)
- [Installed-game evidence](docs/GAME_EVIDENCE.md)
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
