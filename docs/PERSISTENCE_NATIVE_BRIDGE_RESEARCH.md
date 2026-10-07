# Native persistence route and reuse review — 2026-10-07

> 2026-10-07 update: requested serialization capture and private character save received and inspected. No further collector run is requested. Source-only saved-record/journal validation is implemented; native runtime persistence remains unfinished. See [SERIALIZATION_CODE_FINDINGS.md](SERIALIZATION_CODE_FINDINGS.md).

## Outcome

Continue toward the agreed physical-item persistence with the working UI pak plus a small, version-checked native bridge and hero-scoped sidecar. This is a selected research/implementation route, not a working DLL release. No loader was installed, no injected code was run, and v8 is unchanged.

A native permanent UUID is not required in principle: assign project-owned item GUIDs and bind them to live objects during a session, then to the actual saved records at serialization and reconstruction. Saved array positions can be locators within a verified save generation, not permanent IDs. Name/power or even complete-state matching cannot replace this relationship for identical duplicates. Native save-generation completion, crash recovery and hero clone behavior must be established before shipping.

## Reviewed projects and reuse status

| Project | Source reviewed | License / reuse | Relevant result |
| --- | --- | --- | --- |
| [Minecraft-Dungeons-Apworld](https://github.com/BarbeucTheCat/Minecraft-Dungeons-Apworld) | `6a2ceb26df39eb84742ffd078357217fcd4b4ee6`, client reader, bridge, injector and README | Root and mcdungeons MIT licenses checked | Strongest native bridge reference: item reads, named-pipe calls/events and separate channel offsets. No durable item GUID or serialization/load binding found. |
| [MCDSaveEdit](https://github.com/CutFlame/MCDSaveEdit) | `58fe34efa4cd458992f7dc5af7258450f2e4a1bf`, Item, parser, submodules, license | Root MIT; DungeonTools dependency AGPL-3.0 | Save schema reference and potential parser reuse. Item model has inventoryIndex/equipmentSlot/state, not an instance UUID. Offline editing does not connect to the clicked live item. |
| [DungeonTools](https://github.com/CutFlame/DungeonTools) | README and dependency declaration | AGPL-3.0 | Separate conversion tool/library candidate; default CLI overwrite behavior must not be reused for live saves. Not bundled or copied. |
| [DungeonsEditor](https://github.com/hoonkun/DungeonsEditor) | `9be2daf663f165062dfc8e0de1c61fb374df6493`, JSON I/O, item moves and model | No root license file found; no implementation copied | Independently confirms positional inventoryIndex is reassigned by editor operations. Does not establish retail runtime ordering. |
| [DungeonsLevelLoader](https://github.com/LukeFZ/DungeonsLevelLoader) | `92ccc731fd104352e35024635d452eb5395c25ed`, injector/offsets/Unreal structures | No project-wide license found; bundled MinHook/ImGui notices do not license the project | Native loading precedent only. Historical signatures lack current Store validation; no code copied. |
| [LetMeMove](https://github.com/StainlessStasis/LetMeMove) | README and existing reviewed licensed actor fixture | MIT, already credited | Blueprint loading and online-client startup precedent; not a persistent physical-item solution. |
| [Dungeons Mod Kit](https://github.com/Dokucraft/Dungeons-Mod-Kit) | README blob `22a973becb4aeb11ad4db2cfd16321432e668348` | Existing pinned tooling/credits retained | UE4.22 cooking/packaging; does not add native item/save APIs. |
| [MinHook](https://github.com/TsudaKageyu/minhook) | README and LICENSE blob `74dea27229c05b53b095aa22b9ee7ee9f549e414` | BSD-2-Clause, includes HDE notices | Hook library candidate. It does not resolve target addresses, ABI, lifetimes or game-thread ownership. |

No reviewed third-party native DLL is added to the release. License-eligible code may be adapted after its contracts are checked; eligible does not mean compatible. Java Minecraft favorites mods and Dungeons II UE5 loaders do not provide Dungeons 1 UE4.22 runtime compatibility.

## Archipelago bridge review

The reader's item record/slot/stash offsets overlap the supplied retail reflection layout: InventoryItem.Item at 0x28, InventoryItemSlot.Item at 0x30, InventorySlots at 0x300, full record size 0x78. This corroborates those reflected fields, not all engine globals/functions or non-reflected serializer layout.

The DLL chooses channel RVAs from the executable basename. That is insufficient for our version gate: multiple builds share a basename. Confirm module identity and resolved function contracts/code before installing any hook. The upstream compiled DLL is not treated as a verified source build for this installation.

The bridge's header describes a Present scheduler, but the reviewed implementation removed Present hooking and drains work from hkProcessEvent_Impl. The current code therefore must be reviewed, not its obsolete header. ProcessEvent interception alone does not certify thread ownership or catch direct non-reflected C++ serialization/transfer calls. Our implementation needs a verified game-thread execution point, reentrancy/lifetime guards, precise synchronous favorites checks, and complete relevant native lifecycle coverage. Do not copy the generic arbitrary-call pipe as a public command surface for this feature.

## UE4SS findings

[Issue 1211](https://github.com/UE4SS-RE/RE-UE4SS/issues/1211), opened 2026-03-12, concerns a compiled game build. Maintainer comments identify custom UObjectBase/UField layout and supply a member-layout attachment, then report working with that configuration. The attachment was not retrieved; its bytes/configuration are not verified.

[Issue 1219](https://github.com/UE4SS-RE/RE-UE4SS/issues/1219), opened 2026-03-18, explicitly says that configuration worked on the compiled build but the retail Steam/Store build still crashes immediately. The issue remains open with no returned comments. This is not proof that UE4SS can never work, but it does not justify reinstalling it on this user's already crashing setup. Keep the current UE4SS install block. [ExecuteInGameThread](https://docs.ue4ss.com/release/lua-api/global-functions/executeingamethread.html) documents required game-thread scheduling for a compatible installation; it does not solve loader compatibility.

## Newer user evidence reused

Reviewed native-contracts-20261006-145714.zip: completed/control contracts passed; missing allowlisted classes and capture issues empty. It remains declarations, not serialization bodies.

Inspected only minidump stream/module/memory metadata in the already supplied UE4Minidump(7).dmp, not account/instance values. It contains 408 captured memory ranges totaling 415,836 bytes. Only two 256-byte ranges intersect the game module. It cannot provide the selected native function definitions or prove save/load mapping. No full dump request is made.

## Precise next input

Under the renewed request to do what is needed, ask for one existing bounded code-mode capture using Collect-NativeFavoritesEvidence.ps1 -CollectSerializationCode, while the unchanged game/v8 is idle in camp. This differs from earlier declaration scans: it records reachable instruction snippets for nine evidenced serialization/profile/slot roots and one direct-call level after verifying native metadata anchors. It does not install a DLL, invoke the named methods, write game memory, or edit saves. Keep the resulting archive private.

The existing tool passed Windows compilation and 112 core-reader / 28 decoder checks previously recorded in NATIVE_FAVORITES_EVIDENCE.md. Its retail code-mode completion is unverified. One run may be incomplete or leave indirect/deeper behavior unresolved; do not promise it is the last file needed or that capture success guarantees implementation. Inspect whatever it produces before proposing another request. Do not repeat the default or expanded declaration scans.

An optional closed-game COPY of the active hero .dat can verify actual save schema/hero scoping; two labeled copies before/after a single known equipment/storage move would improve mapping validation. It cannot substitute for native lifecycle evidence. Do not require using an editor or changing an item. Save files stay private; originals remain untouched.

## Implementation sequence

1. Establish serializer/load/transfer mapping and native execution point from code and actual save schema.
2. Build a minimal bridge that observes these paths and performs no item/stat mutation; validate loading, travel and shutdown on this Store build before enabling persistence hooks.
3. Add hero-scoped sidecar commits tied to save generations, live item GUID bindings, restored favorite lookup and explicit unfavorite. Unresolved/mismatched state must not silently forget protection or attach it to another item; block affected salvage until reconciled.
4. Connect existing clicked-item/UI actions and all vanilla/batch salvage guards. No optional bypass for favorites.
5. Verify independent identical duplicates, mission/camp/restart, equipment/storage, upgrades/rerolls, hero switching/clone behavior, interrupted saves and host/join. Only then ship a persistence build.

## Validation of this pass

Online primary-source search, pinned repository inspection and existing private evidence review completed. No new native runtime behavior tested. Documentation and the collector's output label were updated; repository syntax/JSON/SDK/doc and whitespace checks pass. Third-party source remains in ignored research checkouts; no foreign implementation or binary is committed.
