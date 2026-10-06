# Modding options and reusable projects — 2026-10-05

2026-10-06 correction: withdraw UE4SS as a recommended retail diagnostic route after the user's immediate startup crash. Upstream [#1219](https://github.com/UE4SS-RE/RE-UE4SS/issues/1219) reports retail Dungeons UE4.22.3 failing in UE4SS.dll with 0xc0000005 and sometimes no startup log, even with empty Mods; it is open when checked. The closed self-compiled-build issue [#1211](https://github.com/UE4SS-RE/RE-UE4SS/issues/1211) is not proof of retail compatibility. Earlier engine-range/signature-directory research missed these game-specific reports. The actual user's faulting function remains unknown. Collect/remove; do not guess settings or reinstall. See FAVORITES_PERSISTENCE_INVESTIGATION.md.

Research focuses on Minecraft Dungeons 1 (Unreal 4.22), not Fabric/Forge mods that port Dungeons items into Java Minecraft. Repository search results such as Dungeons-Gear/JavaDungeons do not implement this game's runtime. No ready-made, licensed implementation of this complete lock/loadout/multi-salvage UI was established in this review.

| Project | License/permission checked | Applicable use and decision |
|---|---|---|
| Dokucraft/Dungeons-Mod-Kit | MIT; existing pinned c30e88e | Already used for project setup and packaging. UE4.22 editor cooking remains preferred for production widgets. |
| StainlessStasis/LetMeMove | MIT; inspected source 49ac4067ab13d1aeebfad362a892a49d98c0b3dd and pinned release 1.1.0 | Already reused for actor/level shells, retaining the MIT notice. Movement code is not an inventory implementation. |
| atenfyr/UAssetAPI | MIT | Already reused to inspect/write cooked assets. Parsing and graph tests do not execute Unreal. |
| Minecraforever/MCD-PE | Apache-2.0 | Already used for final-build native API research. Recovered offsets do not automatically match the user's Store build. |
| EvenTorset/Camera-Coordinates-Overlay | No standalone repo license; author Nexus asset permissions permit reuse | Source commit 6aab9a742d034ed6511fef87034a132cfe7f05fd inspected. Demonstrates authored actor → Create Widget → Add To Viewport. Assets need UE4.22 cooking and fonts reference game content; no assets copied this pass. |
| Dungeons GUI X | AGPLv3 on author CurseForge page | Cooked widget inspected for typed local string/text flow; no implementation/assets copied. Adopting it wholesale would require corresponding source/license compliance and still uses Blueprint Loader. |
| UE4SS-RE/RE-UE4SS | MIT, copyright 2022 Narknon | Strong alternative for live reflection, Lua/C++ diagnostics and client startup independent of a server GameMode. Official project says compatibility may require custom AOBs. No Dungeons entry found in the 59 official CustomGameConfigs directories inspected. This absence is not proof of incompatibility; Store 4.22.3 startup/signatures need a real test. Not silently installed or bundled as a proven dependency. |
| LukeFZ/DungeonsLevelLoader | No root license observed; bundled MinHook/ImGui licenses do not license the author's whole loader | Source 92ccc731fd104352e35024635d452eb5395c25ed inspected only. Uses a native injector/custom-level UI; not reusable wholesale without permission or a license. |
| DokucraftSaga/Custom-Skins-Loader | No root license observed | Source 9279dfd064a0c9c07daebb60e603c2d88755f328 inspected only. Skin loading is not a gear manager; no copy. |
| CarJem/MCD-SMF | No root license observed | Source 1726598e6f26a748a7ec3ffa6b9c642ee376e57e inspected. Mod-folder/symlink management does not repair runtime code; no copy. |

## Implementation route

Keep the known licensed actor/package tooling while repairing the demonstrable VM defect. Before adding production features, establish a working local UI and inventory-read path. Prefer a project-owned UE4.22 cooked widget and client lifecycle bootstrap for the final overlay; generating increasingly complex raw bytecode without a live VM makes native ABI regressions difficult to detect. A native scripting route through MIT UE4SS is a viable research candidate for logging and function resolution, not a verified Dungeons solution.

For client bootstrap, first verify GameMode behavior on actual host/join sessions. If the external loader cannot start on clients, use project-owned local UI/controller startup assets or a validated UE4SS client startup hook. Do not fork or redistribute restricted Blueprint Loader assets. No offline-only or splitscreen-only workaround satisfies the requirement.

## Sources and next work

Primary source repositories: https://github.com/Dokucraft/Dungeons-Mod-Kit ; https://github.com/StainlessStasis/LetMeMove ; https://github.com/atenfyr/UAssetAPI ; https://github.com/Minecraforever/MCD-PE ; https://github.com/EvenTorset/Camera-Coordinates-Overlay ; https://github.com/UE4SS-RE/RE-UE4SS ; https://github.com/LukeFZ/DungeonsLevelLoader ; https://github.com/DokucraftSaga/Custom-Skins-Loader ; https://github.com/CarJem/MCD-SMF

Author GUI X page: https://www.curseforge.com/minecraft-dungeons/mods/dungeons-gui-x

Next: retail text repair confirmation, client startup evidence, UE4.22 cooking/runtime diagnostics, then implement the remaining lock/loadout/native salvage/controller UI features with their tests. None of those production features is marked done by this pass.

## External reflection source review — 2026-10-06

See [EXTERNAL_REFLECTION_REVIEW.md](EXTERNAL_REFLECTION_REVIEW.md) for pinned MIT source/license checks on Unreal-eXternalrEsolve, McDaived/UE-Dumper and Spuckwaffel/UEDumper. These are inspection references only: no code, binary or runtime dependency is copied, executed or bundled. Legacy UProperty parameter/layout gaps, overbroad process access and unconfigured game offsets prevent declaring a ready Dungeons Store collector. The withdrawn UE4SS Install remains disabled.
