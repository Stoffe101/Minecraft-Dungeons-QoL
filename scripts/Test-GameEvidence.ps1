param([switch]$TestBootstrap)
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$fixture = Join-Path ([System.IO.Path]::GetTempPath()) ([Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force $fixture | Out-Null
$oldEnv = $env:MCD_PAKS_PATH
try {
    $env:MCD_PAKS_PATH = $null
    . (Join-Path $PSScriptRoot "Common.ps1")
    $paks = Join-Path $fixture "game with spaces/Dungeons/Content/Paks"
    New-Item -ItemType Directory -Force $paks | Out-Null
    Set-Content (Join-Path $paks "base.pak") "test fixture, not a game archive"
    $hashBefore = (Get-FileHash (Join-Path $paks "base.pak")).Hash
    if ((Find-McdPaksPath -Override $paks) -ne (Resolve-Path $paks).Path) { throw "Explicit path failed" }
    try { Find-McdPaksPath -Override (Join-Path $fixture "missing") | Out-Null; throw "Missing explicit path accepted" }
    catch { if ($_.Exception.Message -notlike "Explicit Dungeons Paks path does not exist:*") { throw } }
    $mock = Join-Path $fixture "mock dumper.ps1"
    @'
param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Values)
if ($Values[0] -ne '--list') { throw 'Missing list flag' }
$dump = $Values[1] -eq '--dump'
$skip = 1
if ($dump) { $skip++ }
if ($Values[$skip] -eq '--key') {
    if ($Values[$skip+1] -ne ('0x' + ('a' * 64))) { throw 'Key argument corrupted' }
    $skip += 2
}
$positional = @($Values[$skip..($Values.Count-1)])
if ($positional.Count -ne 4 -or $positional[1] -ne 'UE4_22' -or (-not $dump -and $positional[2] -ne 'Dungeons/')) { throw "Incorrect dumper argument layout: $Values" }
Set-Content (Join-Path $positional[3] 'AssetList.txt') "Dungeons/Content/$($positional[2])/fixture.uasset"
if ($dump) { Set-Content (Join-Path $positional[3] 'Class_fixture.txt') 'Synthetic metadata only' }
exit 0
'@ | Set-Content $mock
    $out = Join-Path $fixture "evidence output"
    & (Join-Path $PSScriptRoot "Collect-GameEvidence.ps1") -PaksPath $paks -DumperExe $mock -OutputDirectory $out
    if ($LASTEXITCODE -ne 0) { throw "Evidence collection failed" }
    $report = Get-Content (Join-Path $out "REPORT.json") -Raw | ConvertFrom-Json
    if ($report.issues.Count -ne 0 -or $report.pakFiles.Count -ne 1 -or $report.archiveCatalogCount -ne 1 -or $report.aesKeyProvided) { throw "Invalid evidence manifest" }
    $keyOut = Join-Path $fixture 'key evidence'
    & (Join-Path $PSScriptRoot 'Collect-GameEvidence.ps1') -PaksPath $paks -DumperExe $mock -AesKey ('0x' + ('a' * 64)) -OutputDirectory $keyOut
    $keyReport = Get-Content (Join-Path $keyOut 'REPORT.json') -Raw | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or -not $keyReport.aesKeyProvided -or $keyReport.archiveCatalogCount -ne 1) { throw 'AES argument forwarding failed' }
    if ((Get-Content (Join-Path $keyOut 'REPORT.json') -Raw).Contains('a' * 64)) { throw 'Key leaked into report' }
    if ((Get-FileHash (Join-Path $paks "base.pak")).Hash -ne $hashBefore) { throw "Collector mutated input archive" }
    $unzip = Join-Path $fixture "check"
    Expand-Archive "$out.zip" $unzip
    if (@(Get-ChildItem $unzip -Recurse -Filter '*.pak').Count -ne 0) { throw "Raw archives leaked into evidence" }
    if (@(Get-ChildItem $unzip -Recurse -Filter 'Class_fixture.txt').Count -ne 6) { throw "Missing metadata outputs" }
    try { & (Join-Path $PSScriptRoot "Collect-GameEvidence.ps1") -PaksPath $paks -DumperExe $mock -OutputDirectory $out; throw "Existing evidence overwritten" }
    catch { if ($_.Exception.Message -notlike "Output already exists:*") { throw } }
    try { & (Join-Path $PSScriptRoot "Collect-GameEvidence.ps1") -PaksPath $paks -DumperExe $mock -OutputDirectory (Join-Path $paks 'evidence'); throw "Game directory output accepted" }
    catch { if ($_.Exception.Message -notlike "Evidence output must be outside the game directory:*") { throw } }
    $bin = Join-Path $fixture 'game with spaces/Dungeons/Binaries/Win64'
    New-Item -ItemType Directory -Force $bin | Out-Null
    Set-Content (Join-Path $bin 'Dungeons.exe') 'synthetic executable fixture'
    # Model Xbox executable read denial without changing file permissions or relying on admin rights.
    function Get-FileHash {
        param([string]$Path, [string]$Algorithm = 'SHA256')
        if ((Split-Path $Path -Leaf) -eq 'Dungeons.exe') { throw [System.UnauthorizedAccessException]::new('Synthetic Xbox executable access denied') }
        Microsoft.PowerShell.Utility\Get-FileHash -Path $Path -Algorithm $Algorithm
    }
    try {
        $deniedOut = Join-Path $fixture 'denied exe evidence'
        & (Join-Path $PSScriptRoot 'Collect-GameEvidence.ps1') -PaksPath $paks -DumperExe $mock -OutputDirectory $deniedOut
        if ($LASTEXITCODE -ne 0) { throw 'Optional executable denial failed collection' }
        $deniedReport = Get-Content (Join-Path $deniedOut 'REPORT.json') -Raw | ConvertFrom-Json
        if ($deniedReport.issues.Count -ne 0 -or $deniedReport.warnings.Count -ne 1 -or $deniedReport.executables.Count -ne 1 -or $null -ne $deniedReport.executables[0].sha256) { throw 'Executable denial not accurately recorded' }
        if (@(Get-ChildItem $deniedOut -Recurse -Filter Class_fixture.txt).Count -ne 6 -or -not (Test-Path "$deniedOut.zip")) { throw 'Executable denial interrupted asset inspection' }
    } finally { Remove-Item Function:Get-FileHash }
    Remove-Item (Join-Path $bin 'Dungeons.exe')
    $failedMock = Join-Path $fixture 'failed dumper.ps1'
    'exit 23' | Set-Content $failedMock
    $failedOut = Join-Path $fixture 'failed evidence'
    & (Join-Path $PSScriptRoot "Collect-GameEvidence.ps1") -PaksPath $paks -DumperExe $failedMock -OutputDirectory $failedOut
    if ($LASTEXITCODE -ne 1) { throw "Failed dumper did not fail collection" }
    $failedReport = Get-Content (Join-Path $failedOut 'REPORT.json') -Raw | ConvertFrom-Json
    if ($failedReport.issues.Count -ne 14 -or -not (Test-Path "$failedOut.zip") -or $failedReport.archiveCatalogCount -ne 0) { throw "Failed dumper diagnostics were lost" }
    if ($TestBootstrap) {
        $nativeSource = Join-Path $fixture 'native stderr inspector.cs'
        $nativeExe = Join-Path $fixture 'native stderr inspector.exe'
        @'
using System;
using System.IO;
class FixtureInspector {
    static void Main(string[] args) {
        string directory = args[args.Length - 1];
        string match = args[args.Length - 2];
        File.WriteAllText(Path.Combine(directory, "AssetList.txt"), "Dungeons/Content/fixture.uasset\n");
        if (Array.IndexOf(args, "--dump") >= 0) File.WriteAllText(Path.Combine(directory, "Class_fixture.txt"), "Synthetic partial metadata");
        Console.WriteLine("UeBlueprintDumper synthetic stdout: " + match);
        Console.Error.WriteLine("[ERROR] Synthetic export parse error; other exports can continue");
    }
}
'@ | Set-Content $nativeSource
        $compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
        & $compiler /nologo /target:exe "/out:$nativeExe" $nativeSource
        if ($LASTEXITCODE -ne 0) { throw 'Native stderr fixture compilation failed' }
        $nativeOut = Join-Path $fixture 'native stderr evidence'
        & (Join-Path $PSScriptRoot 'Collect-GameEvidence.ps1') -PaksPath $paks -DumperExe $nativeExe -OutputDirectory $nativeOut
        $nativeReport = Get-Content (Join-Path $nativeOut 'REPORT.json') -Raw | ConvertFrom-Json
        if ($LASTEXITCODE -ne 1 -or $nativeReport.issues.Count -ne 7 -or $nativeReport.archiveCatalogCount -ne 1) { throw 'Native parser errors were not reported accurately' }
        if (@(Get-ChildItem $nativeOut -Recurse -Filter Class_fixture.txt).Count -ne 6 -or -not (Test-Path "$nativeOut.zip")) { throw 'Native stderr interrupted remaining groups' }
        foreach ($log in Get-ChildItem $nativeOut -Recurse -Filter Dumper.log) {
            if ((Get-Content $log.FullName -Raw) -notmatch 'Synthetic export parse error') { throw 'Native stderr was not preserved' }
        }
        Write-Host '[PASS] Actual native stdout/stderr capture, quoted paths, partial export retention and continuation across all groups'
        $bootstrapOut = Join-Path $fixture 'real tool evidence'
        & (Join-Path $PSScriptRoot "Collect-GameEvidence.ps1") -PaksPath $paks -OutputDirectory $bootstrapOut
        $bootstrapReport = Get-Content (Join-Path $bootstrapOut 'REPORT.json') -Raw | ConvertFrom-Json
        # The synthetic archive is intentionally invalid, but the reviewed tool must start.
        if ($LASTEXITCODE -ne 1 -or $bootstrapReport.issues.Count -lt 6) { throw "Invalid archive unexpectedly succeeded" }
        if (@(Get-ChildItem $bootstrapOut -Recurse -Filter Dumper.log).Count -ne 7) { throw "Real dumper bootstrap failed before the seven invocations: $($bootstrapReport.issues -join '; ')" }
        foreach ($log in Get-ChildItem $bootstrapOut -Recurse -Filter Dumper.log) {
            if ((Get-Content $log.FullName -Raw) -notmatch 'UeBlueprintDumper') { throw "Real dumper did not start: $($log.FullName)" }
        }
        Write-Host '[PASS] Pinned Windows tool/runtime downloads, checksum verification and real dumper rejection of invalid archives'
    }
    Write-Host "[PASS] Paths, archive catalog, six dumps, AES argument forwarding, metadata ZIP, read-only input, output guards, executable denial and failure diagnostics"
} finally {
    $env:MCD_PAKS_PATH = $oldEnv
    Remove-Item $fixture -Recurse -Force
}
exit 0
