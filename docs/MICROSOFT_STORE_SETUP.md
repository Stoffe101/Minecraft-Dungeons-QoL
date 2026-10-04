# Microsoft Store / Xbox App Setup

The project's primary target is Minecraft Dungeons 1 owned through a Microsoft account.

## Purchase source vs install source

Owning Dungeons through the Microsoft Store does **not** necessarily mean the game files live in the Xbox app folder.

If you launch/install Dungeons from the normal Minecraft Launcher, the launcher can use the same Microsoft-account entitlement. In that case, the game may be installed in the Minecraft Launcher location instead of the Xbox app location.

The mod only cares about the actual `Dungeons\Content\Paks` folder that the running game uses.

## Minecraft Launcher install

A common Minecraft Launcher Paks path is:

```text
%LOCALAPPDATA%\Mojang\products\dungeons\dungeons\Dungeons\Content\Paks
```

The launcher exposes the installation location on the Dungeons installation page. If the player owns the game through Microsoft Store but uses the Minecraft Launcher to install/run it, this is the path family we should prefer.

## Xbox app / Microsoft Store install

If Dungeons is installed through the Xbox app, a common default Paks path is:

```text
C:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks
```

If the game is installed on another drive, the same relative layout is commonly used there.

## Which one wins?

Whichever installation contains the executable that the player actually launches is the authoritative one.

Practical check:

1. Open Minecraft Launcher.
2. Open Minecraft Dungeons.
3. Check the installation location.
4. From that folder, locate:
   `Dungeons\Content\Paks`
5. Install Blueprint Loader and this mod into that installation's `~mods` folder.

Our PowerShell detector already checks both major path families:

- Minecraft Launcher: `%LOCALAPPDATA%\Mojang\products\dungeons\dungeons\Dungeons\Content\Paks`
- Xbox app: `<drive>:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks`

So owning through Microsoft Store while launching from the Minecraft Launcher requires no architectural change.

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

If auto-detection selects the wrong installation because both are present, pass the desired Paks path explicitly:

```powershell
./scripts/Install.ps1 -PaksPath "$env:LOCALAPPDATA\Mojang\products\dungeons\dungeons\Dungeons\Content\Paks"
```

or:

```powershell
./scripts/Install.ps1 -PaksPath "D:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks"
```

You can also set:

```powershell
$env:MCD_PAKS_PATH = "D:\Path\To\Dungeons\Content\Paks"
```

## Old UWP guides

Older guides describe dumping the protected UWP package and registering a separate modded app. That workflow was necessary for older Microsoft Store packaging. It is not our default approach for modern accessible installs.

## Source references

- Minecraft Help: the Minecraft Launcher can install/play Dungeons using Microsoft-account ownership.
- MCD Save Editor default Paks paths:
  https://github.com/HollyGM/MinecraftDungeonsSaveEdit
- Dokucraft Windows Store guide:
  https://stash.dokucraft.co.uk/pages/help/modding-dungeons-windows-store
