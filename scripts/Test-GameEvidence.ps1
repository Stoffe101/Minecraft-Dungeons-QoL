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
if ($Values.Count -ne 6 -or $Values[0] -ne '--list' -or $Values[1] -ne '--dump' -or $Values[3] -ne 'UE4_22') { throw "Incorrect dumper argument layout: $Values" }
Set-Content (Join-Path $Values[5] 'AssetList.txt') "Dungeons/Content/$($Values[4])/fixture.uasset"
Set-Content (Join-Path $Values[5] 'Class_fixture.txt') 'Synthetic metadata only'
exit 0
'@ | Set-Content $mock
    $out = Join-Path $fixture "evidence output"
    & (Join-Path $PSScriptRoot "Collect-GameEvidence.ps1") -PaksPath $paks -DumperExe $mock -OutputDirectory $out
    if ($LASTEXITCODE -ne 0) { throw "Evidence collection failed" }
    $report = Get-Content (Join-Path $out "REPORT.json") -Raw | ConvertFrom-Json
    if ($report.issues.Count -ne 0 -or $report.pakFiles.Count -ne 1) { throw "Invalid evidence manifest" }
    if ((Get-FileHash (Join-Path $paks "base.pak")).Hash -ne $hashBefore) { throw "Collector mutated input archive" }
    $unzip = Join-Path $fixture "check"
    Expand-Archive "$out.zip" $unzip
    if (@(Get-ChildItem $unzip -Recurse -Filter '*.pak').Count -ne 0) { throw "Raw archives leaked into evidence" }
    if (@(Get-ChildItem $unzip -Recurse -Filter 'Class_fixture.txt').Count -ne 6) { throw "Missing metadata outputs" }
    try { & (Join-Path $PSScriptRoot "Collect-GameEvidence.ps1") -PaksPath $paks -DumperExe $mock -OutputDirectory $out; throw "Existing evidence overwritten" }
    catch { if ($_.Exception.Message -notlike "Output already exists:*") { throw } }
    try { & (Join-Path $PSScriptRoot "Collect-GameEvidence.ps1") -PaksPath $paks -DumperExe $mock -OutputDirectory (Join-Path $paks 'evidence'); throw "Game directory output accepted" }
    catch { if ($_.Exception.Message -notlike "Evidence output must be outside the game directory:*") { throw } }
    $failedMock = Join-Path $fixture 'failed dumper.ps1'
    'exit 23' | Set-Content $failedMock
    $failedOut = Join-Path $fixture 'failed evidence'
    & (Join-Path $PSScriptRoot "Collect-GameEvidence.ps1") -PaksPath $paks -DumperExe $failedMock -OutputDirectory $failedOut
    if ($LASTEXITCODE -ne 1) { throw "Failed dumper did not fail collection" }
    $failedReport = Get-Content (Join-Path $failedOut 'REPORT.json') -Raw | ConvertFrom-Json
    if ($failedReport.issues.Count -ne 12 -or -not (Test-Path "$failedOut.zip")) { throw "Failed dumper diagnostics were lost" }
    if ($TestBootstrap) {
        $bootstrapOut = Join-Path $fixture 'real tool evidence'
        & (Join-Path $PSScriptRoot "Collect-GameEvidence.ps1") -PaksPath $paks -OutputDirectory $bootstrapOut
        $bootstrapReport = Get-Content (Join-Path $bootstrapOut 'REPORT.json') -Raw | ConvertFrom-Json
        # The synthetic archive is intentionally invalid, but the reviewed tool must start.
        if ($LASTEXITCODE -ne 1 -or $bootstrapReport.issues.Count -lt 6) { throw "Invalid archive unexpectedly succeeded" }
        if (@(Get-ChildItem $bootstrapOut -Recurse -Filter Dumper.log).Count -ne 6) { throw "Real dumper bootstrap failed before the six invocations: $($bootstrapReport.issues -join '; ')" }
        foreach ($log in Get-ChildItem $bootstrapOut -Recurse -Filter Dumper.log) {
            if ((Get-Content $log.FullName -Raw) -notmatch 'UeBlueprintDumper') { throw "Real dumper did not start: $($log.FullName)" }
        }
        Write-Host '[PASS] Pinned Windows tool/runtime downloads, checksum verification and real dumper rejection of invalid archives'
    }
    Write-Host "[PASS] Explicit/missing paths, six dumps, metadata ZIP, read-only input, output guards and failure diagnostics"
} finally {
    $env:MCD_PAKS_PATH = $oldEnv
    Remove-Item $fixture -Recurse -Force
}
