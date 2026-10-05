# Inventory Identity Strategy

Last updated: 2026-10-04

## Reproduced mission-travel loss — 2026-10-06

User confirms otherwise-working v7 loses explicit favorites after mission travel. Runtime favorites live in UMG_InventoryItemInspector.MCDQoL_Favorites, an array of physical UInventoryItem references. Recreating the inspector loses the array; retaining UObject references alone would not establish identity after item reconstruction or restart. Until the player explicitly unfavorites is a hard requirement, including travel/restart.

Recovered private UI packages and 31-asset metadata after workspace cleanup. Re-inspected InventoryItem.Item/Meta/SerializableItemId, controller caching and save-state imports, plus MCD-PE inventory metadata/save reconstruction. No verified Blueprint-visible persistent hero/item unique ID emerged. ItemId is a type ID; MarkedNew/Cloned are gameplay state and must not be repurposed. GetCachedUIWidget caches by class and does not establish per-hero, restart-safe item identity. Do not guess GUID functions, alter gameplay flags, or silently group duplicate names/power.

Prepared Collect-FavoritesPersistenceEvidence.ps1 with the already verified public archive key. Its new metadata-only mode covers game-instance, character selection/profile, SaveGame, UserManager, Blacksmith and Storage paths, recording targets/candidates/errors. It never exports raw assets or reads/modifies character saves or executable memory. Inspect it for stable profile/item identifiers and reconstruction/transfer paths before implementing a versioned hero-scoped sidecar. Missing native contracts may require a runtime reflection probe; archive metadata is not proof of an API.

Persistence remains unfinished; v8 does not change favorite lifetime. Acceptance must cover independent duplicate items, explicit unfavorite, camp→mission→camp, quit/restart, hero switch, equipment, storage transfer, upgrade/reroll, and host/join. Only deliberately marked physical items may stay protected. Retain unresolved records and block destructive actions when identity cannot be reconciled; never clear records automatically.

A lock system is only safe if it can recognize the **same physical item instance** later. Item type alone is not enough because a player can own multiple copies of the same weapon or armor.

## Best-case target

Use a native runtime item GUID or other persistent unique identifier if Minecraft Dungeons exposes one to Blueprint.

This remains the preferred solution.

## Verified save-format fields

Current MCDSaveEdit source shows that each hero profile contains:

- `uniqueSaveId`
- `playerId`

and each item contains:

- `inventoryIndex`
- `equipmentSlot`
- `type`
- `power`
- `rarity`
- enchantments
- gilded/netherite enchant data
- other state useful for sanity checking

MCDSaveEdit also shows that unequipped inventory is ordered by `inventoryIndex` and that a newly-added item is assigned `max(existing inventoryIndex) + 1`.

## Provisional fallback key

If runtime Blueprint research finds no better native item ID:

```text
HeroKey = uniqueSaveId
ItemLocator = inventoryIndex
SanityFingerprint = selected immutable-or-mostly-stable item fields
```

A lock record might conceptually contain:

```text
HeroUniqueSaveId
InventoryIndex
Type
Rarity
FingerprintVersion
OptionalFingerprintFields
```

## Why inventoryIndex alone is not enough

It is a locator, not a proven permanent identity.

### Storage transfer

MCDSaveEdit's transfer flow adds an item to the target collection before removing it from the source collection. Adding it assigns the target collection's next index.

Therefore moving an item between inventory and storage can change `inventoryIndex`.

### Index reuse

New items use `max index + 1`. If the current highest-index item is removed, a later item can potentially receive the same number.

A stale lock keyed only by that number could then protect the wrong item.

## Reconciliation rule

If using the fallback locator:

1. Resolve the hero by `uniqueSaveId`.
2. Resolve the item by `inventoryIndex`.
3. Compare stored sanity data.
4. If the data agrees, treat the lock as valid.
5. If it disagrees, **do not silently transfer the lock to the new item**.
6. Mark the lock record stale/unresolved and fail closed for destructive operations until reconciliation is complete.

## Storage handling options

Once runtime behavior is known, choose one:

1. Detect inventory-to-storage transfer and migrate the lock record to the item's new locator.
2. Detect the same item through a stronger runtime object ID during the transfer and rewrite the stored locator.
3. If neither is reliable, clearly document that locks are inventory-local in the first prototype and automatically clear/migrate only when identity can be proven.

Option 1 or 2 is required before a polished release.

## Equipped items

Save-format research shows equipped gear remains in the same main Items collection and uses `equipmentSlot` to indicate its slot.

Equipped items should always receive implicit protection, independent of explicit lock state.

## Loadouts

A loadout reference must use the same identity service as locks. Do not create a separate, weaker lookup method for gear sets.

If a loadout reference no longer resolves confidently:

- show the slot as missing/unresolved
- do not substitute a similar-looking item automatically
- retain protection only when identity is sufficiently verified

## Runtime research checklist

We still need to confirm:

- whether the runtime item struct/object exposes `inventoryIndex`
- whether a native GUID/ID exists but is omitted from the save representation
- whether `uniqueSaveId` is available from the local hero/controller/profile at runtime
- whether equipping preserves the runtime identity object
- what happens to runtime identity during storage transfer
- what happens to identity after Blacksmith upgrades or enchant rerolls

Until those are tested, this document describes the safest provisional design rather than a final implementation contract.

## Audit of the implemented prototype (2026-10-04)

`CookedQoLPatcher` currently persists an `FName[]` of display-name/power/invested-enchantment fingerprints in `MinecraftDungeonsQoL_v1`. It does **not** implement the hero-scoped locator strategy above. The fingerprint is mutable, localized, collision-prone and shared across heroes. Unlocking one match removes group protection. This is a diagnostic prototype and cannot establish persistent physical-item locks.

Selection now snapshots both slot and item UObject and verifies object equality/current inventory membership before preview. This helps prevent selecting a replacement occupant in the same slot **within a session**; it is not restart identity. Clearing selection on stash changes also does not solve persisted multi-hero identity.

Production requires a new versioned sidecar schema, verified hero/item identifiers, migration of legacy fingerprint records as unresolved protection, equipment/loadout checks and reconciliation after storage/upgrades. Native salvage is absent from the diagnostic build until these gates are resolved.

## Installed-game metadata update (2026-10-04)

The 31 UI/controller assets expose InventoryItem.Item, Meta, SerializableItemId/ItemId and slot GetChangeIndex, but no verified persistent physical-item or hero identifier. ItemId is used in type filtering/comparison; do not promote it to instance identity. The revised diagnostic explicitly compares the current item against the six equipped objects before selection/preview and reads enchantment refund points through vanilla GetSalvageInfo. These improvements do not resolve restart identity, cross-hero persistence, or vanilla salvage protection.
