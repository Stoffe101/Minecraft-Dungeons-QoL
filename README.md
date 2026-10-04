# Minecraft Dungeons QoL

A quality-of-life mod project for **Minecraft Dungeons 1** focused on making inventory management much less painful.

## Primary goals

- **Gear Locking**: mark important items as protected and prevent destructive actions from touching them.
- **Gear Manager / Loadouts**: organize multiple builds and eventually equip a saved set quickly.
- **Mass Salvage**: enter a selection mode, mark multiple items, review the batch, then salvage them through the game's native salvage path.
- **Safety first**: equipped, locked, loadout-assigned, or otherwise protected items must never be bulk-salvaged accidentally.

## Target

- Game: Minecraft Dungeons 1
- Primary store edition: Microsoft Store / Xbox app
- Mod approach: Unreal Engine 4.22 + Dungeons Mod Kit + Blueprint Loader
- Runtime dependency: Blueprint Loader is installed separately by the player

## Current status

The project foundation is in place. Build/install/research tooling and the architecture are documented, but the first functional Blueprint assets are **not yet committed**.

The next implementation gate is identifying the current game's inventory item identity, salvage call, equip call, and inventory widget access points. We intentionally do not patch hero save files or fake currency changes.

## Quick start

1. Install **Unreal Engine 4.22.x**.
2. Install Blueprint Loader for Minecraft Dungeons.
3. From PowerShell, run:

```powershell
./scripts/Check-Environment.ps1
./scripts/Bootstrap-ModKit.ps1
```

4. When Blueprint assets exist under `mod/Content`, build with:

```powershell
./scripts/Build.ps1
```

5. Install the resulting pak:

```powershell
./scripts/Install.ps1
```

For Microsoft Store / Xbox app details, see [docs/MICROSOFT_STORE_SETUP.md](docs/MICROSOFT_STORE_SETUP.md).

## Documentation

Start at [docs/README.md](docs/README.md).

Key files:

- [Current state](docs/CURRENT_STATE.md)
- [Findings](docs/FINDINGS.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Feature specification](docs/FEATURE_SPEC.md)
- [Roadmap](docs/ROADMAP.md)
- [Ideas](docs/IDEAS.md)
- [Test plan](docs/TEST_PLAN.md)
- [Third-party / licensing notes](docs/THIRD_PARTY.md)
- [Research log](docs/RESEARCH_LOG.md)

## Project rule

Every meaningful implementation or research pass must update the docs with what changed, what was learned, decisions made, test results, and next work.
