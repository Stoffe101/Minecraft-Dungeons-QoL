param(
    [string]$PaksPath,
    [string]$DumperExe,
    [string]$AesKey,
    [string]$OutputDirectory
)
# Read-only game inspection. Outputs metadata, never hero saves or raw game assets.
. (Join-Path $PSScriptRoot "Common.ps1")
$root = Get-ProjectRoot
$paks = Find-McdPaksPath -Override $PaksPath
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $root ".research/game-evidence" }
$out = [System.IO.Path]::GetFullPath($OutputDirectory)
$dungeons = Split-Path (Split-Path $paks -Parent) -Parent
$gamePrefix = [System.IO.Path]::GetFullPath($dungeons).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
if (($out + [System.IO.Path]::DirectorySeparatorChar).StartsWith($gamePrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Evidence output must be outside the game directory: $out"
}
if (Test-Path $out) { throw "Output already exists: $out. Choose a new -OutputDirectory to preserve earlier evidence." }
if (Test-Path "$out.zip") { throw "Evidence archive already exists: $out.zip" }
New-Item -ItemType Directory -Force $out | Out-Null
$issues = New-Object System.Collections.Generic.List[string]
$pakFiles = @(Get-ChildItem $paks -File -Filter "*.pak")
$sourceCommit = "unknown (repository ZIP or Git unavailable)"
if (Get-Command git -ErrorAction SilentlyContinue) {
    try {
        $gitHead = & git -C $root rev-parse HEAD 2>$null
        if ($LASTEXITCODE -eq 0 -and $gitHead) { $sourceCommit = $gitHead.Trim() }
    } catch { # Repository ZIPs have no Git metadata.
    }
}
if ($pakFiles.Count -eq 0) { $issues.Add("No root-level game pak archives found in the selected directory.") }
$report = [ordered]@{
    schemaVersion = 1
    collectedUtc = [DateTime]::UtcNow.ToString("o")
    sourceCommit = $sourceCommit
    game = "Minecraft Dungeons 1"
    engine = "UE4_22"
    pakFiles = @($pakFiles | ForEach-Object { [ordered]@{ name = $_.Name; bytes = $_.Length } })
    executables = @()
    issues = @()
    note = "Metadata and Blueprint disassembly only; native ABI and runtime safety are not certified. No hero saves read or copied."
}
# The Paks folder is under Dungeons/Content. Only inspect the adjacent Dungeons/Binaries tree.
$bin = Join-Path $dungeons "Binaries"
if (Test-Path $bin) {
    $report.executables = @(Get-ChildItem $bin -Recurse -File -Filter "*.exe" | ForEach-Object {
        [ordered]@{ name = $_.Name; fileVersion = $_.VersionInfo.FileVersion; sha256 = (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }
    })
}
try {
    $runner = $null
    if (-not $DumperExe) {
        if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT -or -not [Environment]::Is64BitOperatingSystem) { throw "Automatic dumper setup requires Windows x64." }
        $cfg = Get-Content (Join-Path $root "config/evidence-tool.json") -Raw | ConvertFrom-Json
        $toolDir = Join-Path $root ".tools/UeBlueprintDumper-1.2.0"
        $toolZip = Join-Path $root ".tools/UeBlueprintDumper-1.2.0.zip"
        New-Item -ItemType Directory -Force (Split-Path $toolDir -Parent) | Out-Null
        if (-not (Test-Path $toolZip)) { Invoke-WebRequest $cfg.url -UseBasicParsing -OutFile $toolZip }
        if ((Get-FileHash $toolZip -Algorithm SHA256).Hash.ToLowerInvariant() -ne $cfg.sha256) { throw "Blueprint dumper checksum mismatch." }
        if (-not (Test-Path $toolDir)) { Expand-Archive $toolZip $toolDir }
        $dumper = @(Get-ChildItem $toolDir -Recurse -Filter "UeBlueprintDumper.dll")
        if ($dumper.Count -ne 1) { throw "Expected one dumper DLL in reviewed release." }
        $DumperExe = $dumper[0].FullName
        $runtimeDir = Join-Path $root ".tools/dotnet-evidence-runtime-$($cfg.runtimeVersion)"
        $runner = Join-Path $runtimeDir "dotnet.exe"
        if (-not (Test-Path $runner)) {
            $runtimeZip = "$runtimeDir.zip"
            Invoke-WebRequest $cfg.runtimeUrl -UseBasicParsing -OutFile $runtimeZip
            $expected = $cfg.runtimeSha512
            if ($expected -notmatch '^[0-9a-f]{128}$' -or (Get-FileHash $runtimeZip -Algorithm SHA512).Hash.ToLowerInvariant() -ne $expected) { throw "Local .NET runtime checksum mismatch." }
            Expand-Archive $runtimeZip $runtimeDir -Force
        }
    }
    if (-not (Test-Path $DumperExe -PathType Leaf)) { throw "Dumper not found: $DumperExe" }
    foreach ($term in @("Inventory", "Salvage", "Equipment", "ItemStash", "SlotGrid", "PlayerController")) {
        $termDir = Join-Path $out $term
        New-Item -ItemType Directory -Force $termDir | Out-Null
        $arguments = @("--list", "--dump")
        if ($AesKey) { $arguments += @("--key", $AesKey) }
        $arguments += @($paks, "UE4_22", $term, $termDir)
        if ($runner) { & $runner $DumperExe @arguments *> (Join-Path $termDir "Dumper.log") }
        else { & $DumperExe @arguments *> (Join-Path $termDir "Dumper.log") }
        if ($LASTEXITCODE -ne 0) { $issues.Add("$term dump returned exit code $LASTEXITCODE; see Dumper.log") }
        $assetList = Join-Path $termDir "AssetList.txt"
        if (-not (Test-Path $assetList) -or -not (Get-Content $assetList -ErrorAction SilentlyContinue)) { $issues.Add("$term matched no assets; archive may be encrypted or engine selection may need adjustment.") }
    }
} catch { $issues.Add($_.Exception.Message) }
$report.issues = @($issues.ToArray())
$report | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $out "REPORT.json")
@(
    "# Dungeons 1 evidence collection"
    "Blueprint metadata/disassembly only. No hero saves or raw game assets included."
    "Native reflection signatures and runtime item identity still need independent verification."
    "Issues: $($issues.Count)"
    "Review REPORT.json and Dumper.log files before sharing."
) | Set-Content (Join-Path $out "README.md")
Compress-Archive -Path (Join-Path $out "*") -DestinationPath "$out.zip"
Write-Host "[OK] Evidence archive: $out.zip"
if ($issues.Count -gt 0) {
    Write-Warning "Evidence collection had $($issues.Count) issue(s); archive includes diagnostics."
    exit 1
}
