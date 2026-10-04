# Third-Party Components and Licensing

## Dungeons Mod Kit

Repository:
https://github.com/Dokucraft/Dungeons-Mod-Kit

License: MIT.

We use it as an external development dependency. The bootstrap script clones it into `.tools/`, which is ignored by Git.

Pinned commit:
`c30e88ec5e99e401eadedddbe82af0265a056fe7`

## LetMeMove!

Repository:
https://github.com/StainlessStasis/LetMeMove

License: MIT.

Use in this project: reference implementation/layout only at this stage. No LetMeMove binary assets are currently copied into this repository.

## Blueprint Loader for Dungeons 1

Nexus:
https://www.nexusmods.com/minecraftdungeons/mods/111

Blueprint Loader is a separate runtime dependency.

The Nexus permissions prohibit redistributing/modifying its assets without the author's permission. Therefore:

- do not vendor it
- do not upload it as part of our releases
- instruct users to install it separately

## UeBlueprintDumper

Repository:
https://github.com/CrystalFerrai/UeBlueprintDumper

License: Apache-2.0.

Potential use: offline research against locally installed game assets to discover Blueprint classes/functions. It is not vendored by this repository.

## Minecraft / Minecraft Dungeons assets

Game assets belong to their respective rights holders.

Do not commit extracted proprietary game assets to this repository merely because they are useful for technical research. Store raw research output locally under `.research/` and document only the necessary findings.
