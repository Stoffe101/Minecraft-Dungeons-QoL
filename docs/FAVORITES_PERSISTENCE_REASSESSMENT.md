# Favorites persistence reassessment — 2026-10-06

## Result and user action

Keep the working v8 pak. This review does not release a new gameplay build or fix persistence. No additional collector run is requested. Earlier instructions to capture native serialization code are paused after the user's report that repeated commands were not producing a working feature.

The declaration investigation is complete for its intended scope. More declarations of the same classes will not establish the missing runtime-to-save mapping. A future evidence request must explain which specific unresolved relationship it establishes and what implementation it unlocks; a successful capture alone must not be described as progress in gameplay functionality.

## Existing evidence reused

- QoL v5: eleven declarations, 132 properties, 262 functions; completed without issues.
- QoL v6: fourteen declarations, 172 properties, 333 functions; completed without issues.
- Previously supplied Rebalance native-contracts capture: completed, control contracts passed, no missing allowlisted classes or capture issues. Archive SHA-256: `23f8c58f7c46b7df653281ee9942fb445fe40de71fee3a4a17ba965fffba8d99`. Its field offsets supplement the QoL declarations; no new capture was required. Keep the report private.
- Working generator: `tools/CookedInventoryFeatures/Program.cs` owns the favorites object array in the inspector, reads that array for markers, and guards vanilla and batch salvage. It stores live object references, not durable item identities.

## What this establishes

| Candidate | Evidence | Persistence consequence |
| --- | --- | --- |
| Inspector favorites array | Array of InventoryItem object references | Lost with widget lifetime; references do not identify reconstructed items after restart. |
| SerializableItemId.SerializedId | Native item-type name | Multiple physical items can share it. |
| InventoryItem.Item | Native 120-byte InventoryItemData at offset 40 | Item state is available, but no permanent instance ID is reflected. |
| InventoryItemData fields | Reflected fields cover bytes 0–63, 96, and 100–115 | Bytes 64–95 are unaccounted for by reflection; other gaps may be padding. Do not infer a UUID, overwrite gaps, or serialize raw pointer-bearing records. |
| SubItemID in reference source | Assigned Eye-of-Ender type enum; consumed as that enum | It is used for subtype semantics, not a general physical-item ID. Store-build offsets/lifetime remain unverified. |
| InventoryItemSlot | Item at offset 48, OldItem at 56; transient change counter | Slot position and counter do not establish durable identity. |
| SerializeSaveState | Void method with no parameters | Writer, not a Blueprint-accessible saved-record getter. |
| CharacterSerializeComponent | Profile metadata and CharacterSaveData references | No reflected item-record getter or custom-save extension found. |
| Cloud profile GUID | Observed profile matching | Does not establish independent physical-item identity or clone behavior. |

The gap is specific: how the native serializer associates a live physical item with a saved item record, and how the loader recreates that relationship. The received reports describe declarations, not those method bodies. The available reference headers do not supply the Store-build serializer/load implementation. No corresponding code capture or executable was present in the reviewed project evidence.

## Implementation decision

Retain v8's existing item-reference protection until a verified persistence bridge can replace it. Moving references into GameInstance would extend their lifetime but would not solve restart/reconstruction. Saving a name/power fingerprint, a full reflected state snapshot, or an inventory index would not meet the agreed independent-item requirement: identical items and replacement items can collide, while modifications can change a favorite's state.

The intended implementation remains a hero-scoped sidecar plus a verified native serialization/load/transfer bridge. A read-only external companion is a candidate architecture, not a completed or certified runtime. Before implementing its binding, establish the live-item → saved-record mapping, record lifetime/reuse, hero identity/clone behavior, and reconstruction/transfer behavior. Then wire the clicked-item favorite state into that binding and keep all salvage guards using the restored state. No guessed injected loader is enabled.

Acceptance requires a favorite to survive mission travel and full restart, independent identical duplicates, explicit unfavorite, hero switching, equipment/storage moves, upgrades, and host/join play. Existing asset/collector tests cannot substitute for these gameplay checks.

## Source review and verification

Read the actual generator, both completed QoL reports, the existing Rebalance report and MCD-PE reference source at `be646dcd82a689e24709b7abd4cff30fb60b7c9f`. In that Apache-2.0 reference, `_re/systems/item/source/game/item/ItemConsoleCommands.cpp` assigns SubItemID from EEyeOfEnderType, and `_re/systems/item/source/game/item/instance/EyeOfEnderInstance.cpp` reads it as EEyeOfEnderType. These are reference-build semantics, not proof of the Store ABI. No source implementation or game data was copied into the mod.

This pass changes documentation only. It does not alter gameplay assets, game files, or saves.
