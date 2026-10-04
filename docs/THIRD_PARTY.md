# Third-Party Components and Licensing

## Dungeons Mod Kit

Repository:
https://github.com/Dokucraft/Dungeons-Mod-Kit

License: MIT.

Used as an external development dependency and pinned for reproducibility.

Pinned commit:
`c30e88ec5e99e401eadedddbe82af0265a056fe7`

## LetMeMove!

Repository:
https://github.com/StainlessStasis/LetMeMove

License: MIT.

Use: modern Dungeons 1 Blueprint Loader compatibility and package-layout reference. Any future direct reuse of its MIT assets must retain the required MIT notice.

## Blueprint Loader for Dungeons 1

Nexus:
https://www.nexusmods.com/minecraftdungeons/mods/111

Blueprint Loader is a separate runtime dependency.

Its Nexus permissions do not permit us to simply redistribute it as part of this project's release, therefore users install it separately.

## MCD-PE

Repository:
https://github.com/Minecraforever/MCD-PE

License: Apache-2.0.

Use: research/reference for final-build Dungeons 1 native class/function architecture, particularly inventory, item slots, salvage, and UI event wiring.

The repository states that restored class architecture was checked against the final game binary. If code is copied or adapted rather than merely referenced, Apache-2.0 attribution/notice requirements must be followed.

## KismetKompiler

Repository:
https://github.com/tge-was-taken/KismetKompiler

License: MIT.

Potential use: decompile/recompile existing UE4 Blueprint bytecode in automated build/research workflows.

Current status: research dependency only until UE4.22 compatibility with Dungeons assets is proven.

## UeBlueprintDumper

Repository:
https://github.com/CrystalFerrai/UeBlueprintDumper

License: Apache-2.0.

Potential use: offline research against locally installed game assets.

## MCDSaveEdit

Repository:
https://github.com/CutFlame/MCDSaveEdit

Use: save-format research only, including `uniqueSaveId`, `inventoryIndex`, `equipmentSlot`, and item fingerprint fields.

Before copying any implementation code, its applicable license must be checked for the exact version/repository content being reused.

## Minecraft / Minecraft Dungeons assets

Game assets belong to their respective rights holders.

Do not commit extracted proprietary game assets merely because they are technically useful. Raw extraction/research output belongs under ignored local research folders. Commit only project-owned assets, permitted third-party assets with correct attribution, and documentation of necessary technical findings.
