# Inspect the installed Dungeons 1 build

The next runtime work needs evidence from the **actual copy being launched**. Public Steam restoration sources and old SDK dumps do not establish current Store/Xbox reflection signatures. This inspection step does not install the mod, inject code or read hero saves.

## Collect metadata on Windows x64

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

## Output and failure handling

| Output | Contents / purpose |
| --- | --- |
| `REPORT.json` | Tool source revision, engine selection, root pak names/sizes, adjacent executable versions/hashes, issues |
| Six term folders | Inventory, Salvage, Equipment, ItemStash, SlotGrid, PlayerController asset lists and Blueprint metadata/disassembly |
| `Dumper.log` | Inspector output and parser/mount errors |
| ZIP archive | All of the above, ready to review and return for analysis |

Review the archive before sharing: metadata/logs can contain local installation/output paths. It contains extracted Blueprint metadata and disassembly, not raw `.pak`/`.uasset` assets or character saves. Outputs cannot be written into the game directory. The tool reads archives and adjacent executable metadata without modifying them.

A failed collection exits with code 1 and still creates a diagnostic ZIP once output initialization succeeds. Missing assets can mean a wrong directory, encryption, unsupported serialization or genuinely absent matching names. Empty matches are reported, never treated as verified APIs. Return the ZIP even if it reports issues; the logs determine the next inspection method. Early invalid-path/output errors require correcting the command first.

## What this unblocks

Inspect native function references, slot/equipment UI patterns, argument expression types and inventory/controller access in the game's cooked Blueprints. These are evidence for the mirror API, not a complete native ABI dump. Native return/out-parameter signatures still need corroboration, and stable hero/physical-item identity may require a separate non-destructive runtime probe.

After static evidence, the diagnostic pak needs actual Camp/mission testing for loading, stash discovery, input, persistence and hero changes. Production work then covers physical item identity, equipment/loadout guards, review UI, loadout swapping and verified native salvage. The evidence collector alone does not make those features complete.

Primary source: [UeBlueprintDumper](https://github.com/CrystalFerrai/UeBlueprintDumper), reviewed commit `9726294772458eb6114946e967e204925f8b1b66`. Release checksum and Microsoft's runtime checksum from [official .NET 8 release metadata](https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/8.0/releases.json) are pinned in `config/evidence-tool.json`.
