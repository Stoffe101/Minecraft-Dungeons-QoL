# Serialization capture and save contract — 2026-10-07

## Result

The supplied private serialization capture completed with no reported issues: fourteen declarations, nine root entry points and twenty reachable-code samples. The supplied closed-game character save was decoded as a private analysis copy; its original was not changed. No raw save, decoded values, native instructions, AES key or whole executable is committed or sent to CI.

These inputs resolve useful parts of the save boundary, but do not yet establish a deployable native hook or persistent favorites. Keep the working v8 gameplay pak. No additional collector run is requested by this pass.

## What the evidence establishes

| Evidence | Finding | Limit |
| --- | --- | --- |
| Inventory/storage getter bodies | Each returns the corresponding stash array; offsets agree with the previously supplied reflected fields. | Does not identify a physical item across object reconstruction. |
| `SerializeSaveState` reachable body | Inventory and storage loops pass each live slot and its loop index to the same native conversion routine, with separate destinations. Equipped slots use an equipment key and a distinct flag. | The conversion routine itself lies beyond the capture's one direct-call level. Final emitted index semantics are not yet certified. |
| `SaveCharacterData` wrapper | Reads the controller serializer member and calls deeper native save routines. | Durable write completion, failure behavior and ordering remain unresolved. |
| `AssignCharacter` body | Chooses the supplied/default save object, updates the serializer's save reference and invokes an indirect virtual function. | The reconstruction body is not captured; no verified saved-record-to-live-object map. |
| `GetCloudPlayerId` body | Copies a 16-byte value from the native save object. | Which JSON identity it represents, and its clone lifetime, are not established. |
| `InventoryItemSlot.Swap` wrapper | Dispatches to an indirect virtual target. | Does not establish transfer/replacement lifecycle coverage. |
| Supplied character JSON | Thirty indexed inventory records, thirty-two indexed storage records, six equipped records; distinct inventory/storage index domains and the six gear labels. | One save is not proof of every schema or configuration. No general item UUID was observed in these records. |
| Hero identities | Both internal hero and player identifiers exist; the filename differs from the internal hero identifier in this copy. | Filename, cloud ID, player ID and internal hero ID must not be conflated. Clone semantics need verification. |

The native serializer also traverses deferred native item records. Those are not ordinary live inventory objects and cannot automatically inherit favorites through item-state matching. Pending reward records are outside the current parser's binding scope.

The capture is bounded reachable code, not a full decompilation. Indirect calls and deeper direct calls remain explicit gaps. Analysis padding used to display sparse instruction blocks is synthetic, not observed executable bytes. No native function was invoked and no memory was written.

## Implemented source layer

`tools/FavoritesPersistenceCore` parses the supported saved-record domains and validates a proposed hero-scoped favorite journal against an exact file generation. Project-owned GUIDs distinguish physical duplicates; locators and state digests validate a binding already supplied by the future native adapter. They never recover identity by similar gear, power or a prior slot index.

Thirty-one synthetic checks cover separate domains, independent duplicate gear, journal round trips, explicit unfavorite, stale generations, wrong heroes, duplicate IDs/locators, incomplete bindings, changed/unknown fields and malformed location data. The supplied private decoded copy passes schema validation. These are source checks; no game persistence or salvage permission is proved.

## Remaining integration work

1. Establish the converter's emitted locator and the reconstruction relationship, including deferred records, transfers and replacements. Reuse already available reference source, but do not certify current Store ABI from an older or Steam build.
2. Establish save completion and a verified game-thread execution point before installing hooks. Use module identity, target bytes/contracts and lifetime checks; basename-selected addresses are insufficient.
3. Maintain project-owned GUIDs across verified native lifecycle events; commit the favorite journal only against a completed matching save generation, with recovery for interrupted writes and explicit hero/clone policy.
4. Connect the journal-backed live lock set to markers and every synchronous native/Blueprint salvage path. Unresolved bindings must not silently drop protection.
5. Validate mission travel, full restart, identical duplicates, equipment/storage movement, upgrades/rerolls, explicit unfavorite and online host/join in the real game.

There is no new gameplay release, bridge DLL, save modification or loader installation in this pass.
