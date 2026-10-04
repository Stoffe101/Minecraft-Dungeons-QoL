> The 2026-10-04 legacy upload succeeded for all 31 targets and has been analyzed. There is no need to rerun that same export. Current contracts and remaining runtime/identity gaps are in [GAME_API_CONTRACTS.md](GAME_API_CONTRACTS.md).

# Inspect the installed Dungeons 1 build

The next runtime work needs evidence from the **actual copy being launched**. Public Steam restoration sources and old SDK dumps do not establish current Store/Xbox reflection signatures. This inspection step does not install the mod, inject code or read hero saves.

## Collect metadata on Windows x64

**Use the legacy collector below for Dungeons 1.** The older `Collect-GameEvidence.ps1` remains useful for lists/catalogs, but its UeBlueprintDumper Blueprint export assumes newer FProperty metadata and failed on every targeted UE4.22 class/function in the user's installation.

```powershell
git pull origin main
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Collect-LegacyGameEvidence.ps1 -PaksPath "C:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks" -AesKey "0x7D5F892ECEBFA53CC22001DF48B871D51C0DF7C54CE41933BFB285219829B3A8"
```

Upload `.research/game-evidence-legacy.zip`, including failures if any. This route uses the verified archive reader plus the project's pinned legacy UAssetAPI parser. It compiles the small exporter with an existing .NET SDK 8/9/10; otherwise it downloads a checksum-pinned local .NET 8 SDK (about 200 MB) without a global installation. NuGet access is needed to restore UAssetAPI. No Unreal editor is required. Downloads/libraries remain under `.tools`.

The ZIP contains `Metadata/*.json` (imports, legacy property metadata and Kismet), `Metadata/EXPORT_REPORT.json`, build/export logs and `REPORT.json`. Native layouts/behavior still need corroboration. Raw asset companions are temporarily staged outside the game and deleted by the exporter on normal completion/failure; they are never included in the ZIP. Hero saves and optional protected executable hashes are not accessed by this route.

For another attempt, pass a fresh `-OutputDirectory`. Output inside the game directory or existing output is rejected. `-DotNetPath` can select an SDK host explicitly. `-AssetMatch` is an optional inspection override; normal collection uses the 31 identified targets. The old catalog route follows for reference.

Update this repository, or download and extract its main-branch ZIP. Open PowerShell in that folder and run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Collect-GameEvidence.ps1 -PaksPath "D:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks"
```

Replace the example with the active installation's `Paks` folder. Xbox app → Minecraft Dungeons → Manage → Files shows the installation location. This applies when Minecraft Launcher launches a Store/Xbox-managed installation too. Do not select `~mods` itself. Automatic detection is also available by omitting `-PaksPath`; it refuses ambiguous installations. An invalid explicit path never falls back to another copy.

The script downloads checksum-pinned UeBlueprintDumper 1.2.0 and a local Windows x64 .NET 8 runtime. No global runtime installation is needed. Internet access and read access to the installed archives are required. Tool/runtime files stay under `.tools`; output defaults to `.research/game-evidence.zip`. No Unreal editor or Git installation is required for collection. The Git commit is recorded if available.

Existing evidence is preserved. For another attempt use a fresh output path:

```powershell
.\scripts\Collect-GameEvidence.ps1 -PaksPath "D:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks" -OutputDirectory "$env:USERPROFILE\Desktop\MCD-evidence-2"
```

An already-installed dumper executable can be supplied with `-DumperExe`. If the actual archives require encryption configuration, `-AesKey` passes a provided key to the dumper. The collector does not discover keys or change game access permissions.

### All six asset lists empty with `AES key No`

The user's initial Store/Xbox retry on 2026-10-04 enumerated 47 pak files but exposed no matching assets. The explicit AES retry subsequently exposed 131,164 paths, confirming this key works for catalog enumeration on that installation. This **Dungeons 1** key appears in [an actual Dungeons localization mod's pak reader](https://github.com/Saad5400/minecraft-dungeons-arabic/blob/c1a8c20ea714ab63ea33b04025bc84c08747f12a/tools/pak.js) and a [Dungeons 1.17.0.0 extraction-tool comment](https://www.nexusmods.com/minecraftdungeons/mods/67?tab=posts). No key is silently assumed by the collector.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Collect-GameEvidence.ps1 -PaksPath "C:\XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks" -AesKey "0x7D5F892ECEBFA53CC22001DF48B871D51C0DF7C54CE41933BFB285219829B3A8" -OutputDirectory ".research\game-evidence-4"
```

Return `game-evidence-4.zip`, including failure logs if it still reports issues. This is an inspection setting, not a save edit or game installation change. A list-only `ArchiveCatalog` checks all visible `Dungeons/` paths. If its count is zero even with a key, investigate key validity, archive readability or parser configuration rather than assuming inventory names are absent. A nonzero catalog narrows the issue to search terms/export parsing. The report records whether a key was supplied, without storing its value.

The previous broad Inventory export stopped at a cosmetic parser error under Windows PowerShell 5.1. The updated collector captures native stdout/stderr through a child process, retains `[ERROR]` messages as issues and continues the remaining groups. It now targets actual UI/controller paths from the verified catalog, avoiding cosmetic widgets and texture icons. Parser errors can still mean some functions are unavailable, so send the partial archive even when the script returns code 1.

## Output and failure handling

| Output | Contents / purpose |
| --- | --- |
| `REPORT.json` | Tool source revision, engine selection, root pak names/sizes, adjacent executable versions/hashes, issues |
| Six targeted folders | Inventory, Salvage, ItemWidgets, ItemInspector, SlotGrid, PlayerController asset lists and Blueprint metadata/disassembly |
| `ArchiveCatalog` | List-only catalog of visible Dungeons paths, with count in the manifest; no broad asset dump |
| `Dumper.log` | Inspector output and parser/mount errors |
| ZIP archive | All of the above, ready to review and return for analysis |

Review the archive before sharing: metadata/logs can contain local installation/output paths. It contains extracted Blueprint metadata and disassembly, not raw `.pak`/`.uasset` assets or character saves. Outputs cannot be written into the game directory. The tool reads archives and adjacent executable metadata without modifying them.

A failed collection exits with code 1 and still creates a diagnostic ZIP once output initialization succeeds. Missing assets can mean a wrong directory, encryption, unsupported serialization or genuinely absent matching names. Empty matches are reported, never treated as verified APIs. Return the ZIP even if it reports issues; the logs determine the next inspection method. Early invalid-path/output errors require correcting the command first.

## What this unblocks

Xbox installations can deny reads of `Dungeons.exe` even when their pak archives are readable. Executable version/hash collection is optional: failures are recorded in `REPORT.json` warnings with unavailable fields set to null, and Blueprint inspection continues. Do not change ownership or permissions just to obtain this optional hash. If an older collector stopped there, update the repository and retry with a fresh `-OutputDirectory` (for example `.research/game-evidence-2`); the interrupted attempt may already have created the default folder.

Inspect native function references, slot/equipment UI patterns, argument expression types and inventory/controller access in the game's cooked Blueprints. These are evidence for the mirror API, not a complete native ABI dump. Native return/out-parameter signatures still need corroboration, and stable hero/physical-item identity may require a separate non-destructive runtime probe.

After static evidence, the diagnostic pak needs actual Camp/mission testing for loading, stash discovery, input, persistence and hero changes. Production work then covers physical item identity, equipment/loadout guards, review UI, loadout swapping and verified native salvage. The evidence collector alone does not make those features complete.

Primary source: [UeBlueprintDumper](https://github.com/CrystalFerrai/UeBlueprintDumper), reviewed commit `9726294772458eb6114946e967e204925f8b1b66`. Release checksum and Microsoft's runtime checksum from [official .NET 8 release metadata](https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/8.0/releases.json) are pinned in `config/evidence-tool.json`.
