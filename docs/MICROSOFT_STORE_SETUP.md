# Microsoft Store / Xbox App Setup

The project's primary target is the Microsoft Store / Xbox app version of Minecraft Dungeons 1.

## Locate the game

Open the Xbox app:

1. Find Minecraft Dungeons in Installed games.
2. Open the three-dot menu.
3. Choose **Manage**.
4. Open the **Files** section and note the installation location.

A common default Paks path is:

```text
C:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks
```

If the game is installed on another drive, the same relative layout is commonly used there.

## Mods folder

Inside `Paks`, create:

```text
~mods
```

The project install script does this automatically.

## Blueprint Loader

Blueprint Loader must be installed separately. This repository does not redistribute it.

The original Dungeons 1 loader is available from Nexus Mods:
https://www.nexusmods.com/minecraftdungeons/mods/111

## Project install

After building:

```powershell
./scripts/Install.ps1
```

The script searches common Xbox app locations on mounted drives.

If auto-detection fails:

```powershell
./scripts/Install.ps1 -PaksPath "D:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks"
```

You can also set:

```powershell
$env:MCD_PAKS_PATH = "D:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks"
```

## Old UWP guides

Older guides describe dumping the protected UWP package and registering a separate modded app. That workflow was necessary for older Microsoft Store packaging. It is not our default approach for modern Xbox app installations with accessible game files.

## Source references

- Dokucraft Windows Store guide:
  https://stash.dokucraft.co.uk/pages/help/modding-dungeons-windows-store
- MCD Save Editor path reference:
  https://github.com/HollyGM/MinecraftDungeonsSaveEdit
