# Favorites persistence investigation

## 2026-10-06: probe withdrawn after startup crash

User reports immediate crash after Install and pressing Play, before camp/Ctrl+H. The supplied screenshot shows launcher error 0xc0000005. It contains no stack trace or native function/signature evidence. The newly added loader is the leading suspect; the exact failure and restoration after removal are not yet verified. Do not reinstall or propose guessed signatures/engine settings. Install is blocked in production config. Collect/Remove remain operational; tests enable historical installation only in a copied temporary repository against a fake executable.

Close the game and launcher, then from the repo:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Invoke-FavoritesReflectionProbe.ps1 -Action Collect
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Invoke-FavoritesReflectionProbe.ps1 -Action Remove
```

Run Remove even if collection fails, then verify the game launches normally. Upload the printed private reflection-evidence ZIP; if none is produced, provide UE4SS.log from the game's Binaries/Win64 folder or the terminal error. Removal leaves game saves and QoL paks untouched. Do not delete the game's own DLLs or alter executable protections. If removal fails, use its concrete error/ownership manifest to decide the next step. Favorites persistence remains unresolved.

## 2026-10-06: Drive installation copy available

Authenticated Drive listing confirmed the user-supplied MCD-ModdingCopy folder is accessible, including Dungeons/Content/Paks: 47 game pak files totaling 4,836,730,663 bytes and a separate mods folder. This is a useful source for direct asset/catalog extraction without repeatedly asking the user to export individual packages. Listing is verified; archive contents/integrity/completeness have not yet been downloaded or checked. The listed Dungeons/Binaries/Win64 folder contains XGamingRuntimeThunks.dll and small ancillary files, with no Dungeons executable or runtime reflection output. Static archives do not replace the missing native-class/lifecycle evidence for durable favorites. No protected executable upload is required for asset research. Keep the Drive URL and game files private; only necessary findings belong in git.

## Supplied archive evidence — 2026-10-05

Inspected the private `persistence-evidence-20261005-234040.zip` (SHA-256 `3e45b55d81dcfb58bd0c73c31c916d404b92111e51757cafb230f427ada03187`). Its exporter completed 88/88 UE4.22 packages with no errors and no raw patch sources. Useful metadata includes BP_GameInstance, its interface, storage chest content, transfer-slot UI and blacksmith UI; most other matches are unrelated audio/animation/material packages.

No CharacterSelection, CharacterSelect, CharacterProfile, SaveGame or UserManager package matched. These path filters do not enumerate native `/Script/Dungeons` class definitions. Imported references are not complete native reflection metadata.

Observed contracts:

- BP_GameInstance imports DungeonsGameInstance.GetUserManager, DungeonsUserManager.GetInitialUser and controller/login APIs. It does not establish persistent hero/item-instance IDs.
- Storage chest content ExecuteUbergraph statement 1370 calls ItemStashComponent.SerializeSaveState() with **zero parameters**, without assigning a result. MCD-PE's reconstructed declaration is `void SerializeSaveState()`. It writes game state, rather than providing JSON/item records. Do not call it as an FString getter or invoke it for this read-only investigation.
- Storage transfer UI uses StorageUtil.SortItems, TowerFunctionLibrary.CreateInventoryItemSlot, InventoryItemSlot.Item and grid caches. No permanent physical-item identifier was found.
- InventoryItem.Item.ItemId remains a type ID. Change counters, UObject names/addresses and MarkedNew/Cloned flags cannot substitute for persistent identity.

The inspector-owned MCDQoL_Favorites array remains session scoped. This pass releases no persistence fix or new gameplay pak. v8's wider red border and equipped markers remain unconfirmed in retail.

## Withdrawn runtime reflection probe design

Invoke-FavoritesReflectionProbe.ps1 installs temporary MIT UE4SS **3.0.1**, pinned to commit `d935b5b23bac03b65c14ae38382b02007204cc2e` and official release ZIP SHA-256 `4b47d4bceddd2f561a4e395bfa00924ccfc945af576a2d0c613e6537846c57ec`. The ZIP was independently downloaded, hashed and inspected. Upstream targets UE4.12–5.3, which includes 4.22, but Store compatibility is **unverified** and may require custom signatures. A failed startup/log is useful evidence.

Installation refuses existing proxies/loaders/configs, checks the release hash, records ownership before installing the proxy last, and copies only six owned files. No upstream cheat/console/Blueprint loader/splitscreen mods are installed. Our Lua script registers Ctrl+H using the upstream RegisterKeyBindAsync pattern and calls only GenerateSDK() and DumpAllObjects(). It requests no item setters, save calls, gameplay hooks or forced asset loading. **UE4SS itself hooks the engine during startup**; Lua pcall cannot catch a native access violation.

GUI/external consoles, hot reload, UObject cache, crash dumps and forced asset loading are disabled. Engine override is the established UE4.22. No executable bytes, ownership or ACLs are changed. Headers describe reflected members, not complete non-reflected save data or portable runtime offsets.

The earlier Install/camp/Ctrl+H test is withdrawn. Follow collection/removal above instead. Any future native probe requires investigation of this startup failure first.

Upload the printed reflection-evidence ZIP. Collection copies fresh UE4SS.log, four allowlisted CXX headers and only `/Script/Dungeons` lines from the object dump. Missing data/classes/completion are reported explicitly, including failed startup. No saves, executables, crash memory or item values are copied. Keep generated game metadata private. Removal deletes only unchanged owned files and preserves edited files/evidence; it warns if an edited loader DLL remains. QoL paks are untouched.

Default Win64 path is `C:\XboxGames\Minecraft Dungeons\Content\Dungeons\Binaries\Win64`; override with -Win64Path if needed. If Store permissions deny writes, report the error. Do not change ownership to force installation.

## Implementation gate and validation

Inspect native InventoryItem, InventoryItemSlot, ItemStashComponent, DungeonsGameInstance, DungeonsUserManager and related profile/serialization declarations. If supported hero/physical-item IDs exist, implement a versioned hero-scoped sidecar. Otherwise investigate a version-checked native serialization/transfer bridge. Widget caching or name/power matching cannot establish persistence.

Acceptance requires independent duplicates, explicit unfavorite, travel, restart, hero switch, equipment, storage, upgrades/rerolls and online host/join. Unresolved protection must not be silently discarded or transferred to similar items. Reflection alone cannot prove these behaviors.

Local PowerShell 7.4.6 tests use the actual pinned release against a fake game directory: checksum rejection, collision refusal, only our diagnostic enabled, missing capture reported incomplete, allowlisted ZIP roundtrip, native object-line filtering, edited config retained, unchanged loader removed, original executable/unrelated files preserved. UTF-16 without a BOM is tested because pinned UE4SS object output uses wchar_t; UTF-8/BOM variants are also accepted. Lua mock tests verify the key binding, reentrancy guard, call order and recovery after a capture error. Repository parser/JSON checks and diff checks pass. Windows PowerShell 5.1 runs the fixture in Project Validation. None proves game compatibility/persistence.

## Primary references

- [UE4SS release](https://github.com/UE4SS-RE/RE-UE4SS/releases/tag/v3.0.1), [pinned README](https://github.com/UE4SS-RE/RE-UE4SS/blob/v3.0.1/README.md), [MIT license](https://github.com/UE4SS-RE/RE-UE4SS/blob/v3.0.1/LICENSE).
- [Pinned keybinds](https://github.com/UE4SS-RE/RE-UE4SS/blob/v3.0.1/assets/Mods/Keybinds/Scripts/main.lua), [LuaMod.cpp](https://github.com/UE4SS-RE/RE-UE4SS/blob/v3.0.1/UE4SS/src/Mod/LuaMod.cpp): SDK output is CXXHeaderDump, not a guessed UE4SS_SDK directory.
- [Installation](https://docs.ue4ss.com/release/installation-guide.html), [dumpers](https://docs.ue4ss.com/release/feature-overview/dumpers.html).
- [MCD-PE inventory reconstruction](https://github.com/Minecraforever/MCD-PE/blob/main/_re/systems/item/Restored_ItemStashComponent.h), Apache-2.0; reference declaration, not proof of the Store ABI.
