# Save and favorites journal contracts

Source-only .NET 8 parser and validator for the proposed native persistence bridge. This tool does not decrypt or write game saves, access the game process, assign live item identities, install a loader, or change the UI pak. Passing these checks does not establish gameplay persistence.

Run synthetic checks:

```powershell
dotnet run --project tools/FavoritesPersistenceCore/FavoritesPersistenceCore.csproj -c Release -- --self-test
```

Optional private analysis of an already decoded, closed-game save copy:

```powershell
dotnet run --project tools/FavoritesPersistenceCore/FavoritesPersistenceCore.csproj -c Release -- --inspect-decoded-json <private-copy.json> <original-dat-SHA256>
```

Only location counts are printed. No user-supplied save is needed in CI. Neither this command nor a successful parse is a favorite restoration operation.

The internal `uniqueSaveId` scopes the proposed journal; the filename and `playerId` are not substitutes. Native cloud-ID equivalence and clone semantics remain unresolved. Inventory and storage indices occupy separate domains; equipment uses the six explicit gear labels. A locator is valid within one exact save-file generation, never a permanent item identity. The current parser supports indices 0–299, at most 300 records per indexed domain, six equipment locations, an 8 MiB decoded document and JSON depth 64. These are conservative parser bounds, not a claim about every retail inventory configuration. Unsupported input is refused.

`FavoriteJournal` records project-owned GUIDs, favorites, locators and canonical item-state digests. `RestorePlan.Verify` requires a complete, one-to-one mapping to the same hero and exact original-file SHA-256. Digests retain unknown item fields and array order; object key order and whitespace are ignored. Matching state is only a validation check, never an identity search. Identical physical items must have independent GUIDs supplied by a verified adapter.

`CanRestoreSavedBindings` approves only the journal-to-saved-record relationship. It cannot authorize runtime salvage, prove a clicked live item belongs to a saved record, or manufacture missing mappings. The native adapter must establish those separately and block destructive operations while identity is unresolved. There is no native adapter or durable journal writer in this project yet.
