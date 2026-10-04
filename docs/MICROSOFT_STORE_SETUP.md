# Minecraft Launcher / Microsoft Store / Xbox App Setup

Last updated: 2026-10-04

## The important distinction

Where Minecraft Dungeons was **purchased** and where its files are **installed** are not the same question.

A player can own Dungeons through Microsoft Store and press Play in the normal Minecraft Launcher while the actual game files are still the Xbox/Microsoft Store-managed installation.

The mod must be installed into the `Paks` directory belonging to the copy that actually runs.

## Case A: Microsoft Store / Xbox-managed installation

This is the expected case for the primary project tester.

In the Xbox app:

1. Open Minecraft Dungeons.
2. Choose **Manage**.
3. Open **Files**.
4. Note the installation directory.

A common path is:

```text
C:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks
```

or on another drive:

```text
D:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks
```

Create:

```text
~mods
```

inside `Paks`.

If Minecraft Launcher displays only a drive such as `C:` or `D:` for the Dungeons installation, community Dungeons modding guidance says to treat it as the Microsoft Store/Xbox installation and use this path family.

For this installation type, Dungeons can normally still be launched from Minecraft Launcher, Xbox app, Microsoft Store, or Start after mod installation.

## Case B: older standalone Minecraft Launcher installation

If Minecraft Launcher shows a real installation directory rather than only a drive, locate:

```text
<installation>\dungeons\dungeons\Dungeons\Content\Paks
```

and create:

```text
~mods
```

inside it.

Important caveat from Dungeons modding guidance: the old Launcher workflow may restore/delete modded files when starting the game through Minecraft Launcher.

For that layout:

1. install the mod
2. do not reopen/reinstall through the Launcher before testing
3. launch the installation's `Dungeons.exe` directly

## Installing Blueprint Loader

Blueprint Loader is a separate dependency:

https://www.nexusmods.com/minecraftdungeons/mods/111

Install it into the same active Dungeons installation.

The project does not redistribute it.

## Installing Minecraft Dungeons QoL

Put:

```text
MinecraftDungeonsQoL.pak
```

into:

```text
Dungeons\Content\Paks\~mods
```

Example:

```text
D:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks\~mods\MinecraftDungeonsQoL.pak
```

## Automatic installer

After building:

```powershell
./scripts/Install.ps1
```

The project currently probes common Xbox paths and the legacy Mojang path.

If more than one install exists, or the Launcher uses a custom folder, explicitly pass the correct path:

```powershell
./scripts/Install.ps1 -PaksPath "D:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks"
```

or set:

```powershell
$env:MCD_PAKS_PATH = "D:\Path\To\Dungeons\Content\Paks"
```

## Planned detector improvement

The installer should be improved to distinguish:

- Xbox/Store-managed install
- legacy standalone Launcher install
- Steam install

rather than relying only on common paths.

## Old UWP guides

Very old Dungeons modding guides describe dumping a protected UWP package and registering a modified app package.

That is not the preferred workflow for modern accessible Xbox app installations and is not the project's default target.

## References

- Dokucraft Dungeons Microsoft Store modding guide
- Dokucraft Dungeons Launcher modding guide
- Blueprint Loader for Dungeons 1
