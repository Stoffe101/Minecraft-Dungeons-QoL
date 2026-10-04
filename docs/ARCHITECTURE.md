# Architecture

## Guiding principle

Add behavior around the vanilla inventory rather than replacing it.

Replacing a large vanilla UI asset is more fragile across game updates and creates conflicts with other mods. The preferred architecture is a Blueprint Loader actor plus lightweight overlay widgets.

## Planned runtime components

### BP_MCDQoL_Manager

Loaded through Blueprint Loader in both Lobby and Ingame.

Responsibilities:

- locate the local player/controller
- detect when the inventory UI exists
- discover/track the currently selected inventory item
- own service references
- create/remove QoL overlay widgets
- route input only while relevant UI is open

### BP_MCDQoL_LockService

Responsibilities:

- build or read a stable item identity
- query/set lock state
- persist lock state
- expose `IsProtected(Item)`
- later include loadout-assigned protection

### BP_MCDQoL_SalvageService

Responsibilities:

- maintain the current multi-select set
- reject protected/equipped/invalid items
- calculate a preflight summary where safely possible
- present confirmation
- revalidate every item immediately before salvage
- call the native salvage path sequentially
- abort safely if an item disappears or the native call fails

It must not directly grant emeralds or enchantment points.

### BP_MCDQoL_GearSetService

Later phase.

Responsibilities:

- named gear sets/loadouts
- assign melee / armor / ranged / artifacts
- automatically protect referenced items
- validate all references before equipping
- call native equip functions sequentially
- report partial failures rather than silently substituting gear

### WBP_MCDQoL_Overlay

Initial UI surface.

Planned elements:

- lock/unlock button next to selected-item context
- obvious lock state indicator
- multi-select mode toggle
- selected-count indicator
- `Review Salvage` button
- gear-manager button in later phase

### WBP_MCDQoL_SalvageReview

Shows:

- selected item count
- protected/skipped count
- item list
- reward preview only if sourced from trustworthy game data/functions
- final confirmation

## Persistence

Preferred mechanism: a project-owned Unreal `SaveGame` slot named:

`MinecraftDungeonsQoL_v1`

Do not modify the hero save structure.

### Versioned data concept

```text
SaveVersion: 1
Profiles:
  <uniqueSaveId or verified runtime hero key>:
    LockedItems:
      - NativeId? / InventoryIndex?
        TypeSanity
        FingerprintVersion
    Loadouts:
      - Name
        MeleeItemId
        ArmorItemId
        RangedItemId
        ArtifactItemIds[3]
Settings:
  ConfirmMassSalvage: true
```

## Item identity

Preferred order:

1. native stable item GUID / unique ID exposed at runtime
2. another stable native item-instance identifier
3. verified runtime `inventoryIndex` scoped by hero `uniqueSaveId`
4. fingerprint fallback

Open-source save-format code confirms that hero profiles contain `uniqueSaveId` and items contain `inventoryIndex`. This is promising but not sufficient by itself: storage transfers can change the index, and the highest deleted index can later be reused.

If `inventoryIndex` becomes the fallback key, store sanity data such as item type and reconcile stale entries. Any ambiguous identity must fail closed.

See `INVENTORY_IDENTITY.md`.

## Mass salvage transaction

Phase-1 design:

1. Enter selection mode.
2. Select items.
3. Locked/equipped/loadout items cannot be selected.
4. Open review screen.
5. Confirm once.
6. Freeze the selected identities.
7. For each identity:
   - resolve current item
   - revalidate identity and protection
   - invoke native salvage
   - record success/failure
8. Show summary.
9. Clear selection.

No custom multi-item undo is promised until we prove it can be made safe.

## Multiplayer

The mod should only manage the local player's inventory. It must never attempt to mutate remote players' items. Multiplayer testing is required before a public release.

## Implemented diagnostic architecture (2026-10-04)

The service/widget layout above is the production target. The current cooked diagnostic keeps its logic in one manager actor, relocated from a permitted template to `/Game/Mods/MinecraftDungeonsQoL/BP_MCDQoL_Manager`. Relocated Lobby/Ingame maps instantiate it through Blueprint Loader. A regenerated `/Game/Mods/MinecraftDungeonsQoL/SG_MCDQoL` provides the prototype sidecar.

The manager materializes native inventory slots in a reflected array local, keeps paired selected slot/item arrays and a selection-owner stash reference, and previews one candidate per tick after validation. It clears state on invalid/new stash and F5 cancellation. No native inventory mutation call is emitted.

This architecture still needs singleton lifecycle, actual inventory-open input gating, hero/item identity, explicit equipment-map exclusion, item labels/review UI and controller input. The raw cooked graph is a diagnostic bridge to the editor-authored production services, not their completed implementation.
