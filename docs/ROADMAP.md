# Roadmap

## Phase 0 — Foundation [IN PROGRESS]

- [x] Repository structure
- [x] Docs system
- [x] Dungeons Mod Kit bootstrap script
- [x] Microsoft Store/Xbox app path detection
- [x] Build/install scripts
- [x] Initial architecture and safety rules
- [ ] Verify scripts on a real Windows development machine

## Phase 1 — Runtime Inventory Research [NEXT]

- [ ] Enumerate current inventory-related assets/classes
- [ ] Identify inventory widget
- [ ] Identify selected item reference
- [ ] Identify stable item ID/GUID
- [ ] Identify native salvage function
- [ ] Identify native equip/unequip function
- [ ] Identify vanilla salvage button/event chain
- [ ] Document exact paths/functions from the current build

Exit criterion: we can inspect a selected item and safely invoke or block the same native action vanilla uses.

## Phase 2 — Gear Lock Vertical Slice

- [ ] Blueprint Loader Lobby map
- [ ] Blueprint Loader Ingame map
- [ ] Manager actor
- [ ] Lock service
- [ ] separate SaveGame persistence
- [ ] lock toggle UI
- [ ] lock icon/overlay
- [ ] persistence across restart
- [ ] protect locked item from vanilla salvage

Exit criterion: a locked item cannot be accidentally salvaged through normal gameplay.

## Phase 3 — Mass Salvage MVP

- [ ] selection mode
- [ ] multi-item selection state
- [ ] protected/equipped filtering
- [ ] review screen
- [ ] single confirmation
- [ ] native sequential salvage
- [ ] completion/failure summary
- [ ] keyboard/mouse test
- [ ] controller test

Exit criterion: 20+ mixed items can be safely reviewed and salvaged without touching protected gear.

## Phase 4 — Gear Manager / Loadouts

- [ ] loadout data model
- [ ] create/rename/delete
- [ ] assign gear/artifacts
- [ ] loadout items auto-lock/protect
- [ ] one-click native equip flow
- [ ] missing-item handling
- [ ] multiple heroes/profiles

## Phase 5 — Inventory QoL Expansion

Candidates:

- filters/sorting
- select all eligible
- rarity filters
- power threshold selection
- duplicate detection
- enchanted/gilded protection rules
- compare to equipped
- tags/favorites
- salvage presets

## Phase 6 — Hardening / Release

- [ ] Microsoft Store/Xbox app
- [ ] Steam if a tester is available
- [ ] camp and missions
- [ ] offline and online multiplayer
- [ ] multiple heroes
- [ ] restart persistence
- [ ] large inventories
- [ ] coexistence with common Blueprint Loader mods
- [ ] packaging/release documentation
