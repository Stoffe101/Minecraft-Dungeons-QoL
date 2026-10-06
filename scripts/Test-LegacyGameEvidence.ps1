. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Get-ProjectRoot
$fixture = Join-Path ([System.IO.Path]::GetTempPath()) ('MCD-legacy-' + [Guid]::NewGuid().ToString('N'))
try {
    $paks = Join-Path $fixture 'game with spaces/Dungeons/Content/Paks'
    New-Item -ItemType Directory -Force $paks | Out-Null
    $cfg = Get-Content (Join-Path $root 'config/cooked-template.json') -Raw | ConvertFrom-Json
    $zip = Join-Path $fixture 'LetMeMove.zip'
    Invoke-WebRequest $cfg.releaseUrl -UseBasicParsing -OutFile $zip
    if ((Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant() -ne $cfg.releaseSha256) { throw 'Legacy fixture checksum mismatch.' }
    Expand-Archive $zip $paks
    $pak = Join-Path $paks 'LetMeMove.pak'
    $before = (Get-FileHash $pak).Hash
    $out = Join-Path $fixture 'evidence with spaces'
    & (Join-Path $PSScriptRoot 'Collect-LegacyGameEvidence.ps1') -PaksPath $paks -AesKey ('0x' + ('0' * 64)) -OutputDirectory $out -DotNetPath (Get-Command dotnet).Source -AssetMatch 'Dungeons/Content/Mods/LetMeMove'
    if ($LASTEXITCODE -ne 0) { throw "Legacy collector failed; see $out" }
    $summary = Get-Content (Join-Path $out 'Metadata/EXPORT_REPORT.json') -Raw | ConvertFrom-Json
    if ($summary.candidateCount -ne 1 -or $summary.completed.Count -ne 1 -or $summary.errors.Count -ne 0) { throw 'Legacy pak fixture did not export exactly one actor.' }
    $json = @(Get-ChildItem (Join-Path $out 'Metadata') -Filter '*BP_WASD_Movement.json')
    if ($json.Count -ne 1) { throw 'Actor metadata missing.' }
    $actor = Get-Content $json[0].FullName -Raw | ConvertFrom-Json
    if ($actor.propertyCount -ne 34 -or $actor.functionCount -ne 2 -or $actor.errors.Count -ne 0) { throw 'Legacy property/function regression.' }
    if (@($actor.exports | Where-Object { $_.type -eq 'FunctionExport' -and $_.script }).Count -lt 1) { throw 'Kismet metadata missing.' }
    if ((Get-FileHash $pak).Hash -ne $before) { throw 'Input pak was modified.' }
    $check = Join-Path $fixture 'zip check'
    Expand-Archive "$out.zip" $check
    if (@(Get-ChildItem $check -Recurse -File | Where-Object { $_.Extension -in @('.pak', '.uasset', '.uexp', '.ubulk') }).Count -ne 0) { throw 'Raw assets were included in the evidence ZIP.' }
    Write-Host '[PASS] Actual UE4.22 legacy pak: 34 properties, 2 functions, Kismet metadata, input hash preserved, metadata-only ZIP'
    # A similarly cooked actor must never be picked up by the private UI-source allowlist.
    $sources = Join-Path $fixture 'patch sources with spaces'
    & (Join-Path $PSScriptRoot 'Collect-LegacyGameEvidence.ps1') -PaksPath $paks -AesKey ('0x' + ('0' * 64)) -OutputDirectory $sources -DotNetPath (Get-Command dotnet).Source -CollectInventoryPatchSources
    if ($LASTEXITCODE -ne 1) { throw 'Missing required UI sources must fail collection.' }
    $sourceReport = Get-Content (Join-Path $sources 'Metadata/EXPORT_REPORT.json') -Raw | ConvertFrom-Json
    if (-not $sourceReport.inventoryPatchSources -or $sourceReport.candidateCount -ne 0 -or $sourceReport.sourceFiles.Count -ne 0) { throw 'Patch-source allowlist included an unrelated asset.' }
    if (@($sourceReport.errors | Where-Object { $_ -like 'Required patch source missing:*' }).Count -ne 7) { throw 'Collector did not identify all seven missing packages.' }
    if (@(Get-ChildItem $sources -Recurse -File | Where-Object { $_.Extension -in @('.pak', '.uasset', '.uexp', '.ubulk') }).Count -ne 0) { throw 'Unexpected cooked asset collected.' }
    if ((Get-FileHash $pak).Hash -ne $before) { throw 'Patch-source collection modified the input pak.' }
    Write-Host '[PASS] Patch-source collection rejects unrelated packages and reports missing required sources'
    $persistence = Join-Path $fixture 'persistence metadata with spaces'
    & (Join-Path $PSScriptRoot 'Collect-LegacyGameEvidence.ps1') -PaksPath $paks -AesKey ('0x' + ('0' * 64)) -OutputDirectory $persistence -DotNetPath (Get-Command dotnet).Source -CollectPersistenceEvidence
    if ($LASTEXITCODE -ne 1) { throw 'Missing persistence targets must report failure, not a verified API.' }
    $persistenceReport = Get-Content (Join-Path $persistence 'Metadata/EXPORT_REPORT.json') -Raw | ConvertFrom-Json
    if (-not $persistenceReport.persistenceEvidence -or $persistenceReport.inventoryPatchSources -or $persistenceReport.candidateCount -ne 0 -or $persistenceReport.sourceFiles.Count -ne 0) { throw 'Persistence mode selected unrelated or raw assets.' }
    if ($persistenceReport.targetMatches -notcontains 'BP_GameInstance' -or $persistenceReport.targetMatches -notcontains 'Storage') { throw 'Persistence target manifest missing.' }
    if (@(Get-ChildItem $persistence -Recurse -File | Where-Object { $_.Extension -in @('.pak', '.uasset', '.uexp', '.ubulk') }).Count -ne 0) { throw 'Persistence mode exported game assets.' }
    if ((Get-FileHash $pak).Hash -ne $before) { throw 'Persistence collection modified the input pak.' }
    Write-Host '[PASS] Persistence evidence reports missing targets and preserves metadata-only/read-only boundaries'
    $profile = Join-Path $fixture 'profile metadata with spaces'
    & (Join-Path $PSScriptRoot 'Collect-LegacyGameEvidence.ps1') -PaksPath $paks -AesKey ('0x' + ('0' * 64)) -OutputDirectory $profile -DotNetPath (Get-Command dotnet).Source -CollectProfileEvidence
    if ($LASTEXITCODE -ne 1) { throw 'Absent profile packages must fail rather than establish a native API.' }
    $profileReport = Get-Content (Join-Path $profile 'Metadata/EXPORT_REPORT.json') -Raw | ConvertFrom-Json
    if (-not $profileReport.profileEvidence -or $profileReport.persistenceEvidence -or $profileReport.inventoryPatchSources -or $profileReport.candidateCount -ne 0 -or $profileReport.sourceFiles.Count -ne 0) { throw 'Profile evidence selected unrelated or raw packages.' }
    if ($profileReport.targetMatches.Count -ne 26 -or @($profileReport.errors | Where-Object { $_ -like 'Required profile metadata missing:*' }).Count -ne 26) { throw 'All 26 exact profile packages must be checked.' }
    if ($profileReport.targetMatches -notcontains 'Dungeons/Content/UI/Character/UMG_CharacterPicker.uasset' -or $profileReport.targetMatches -notcontains 'Dungeons/Content/UI/Character/UICharacterDataBind.uasset') { throw 'Catalog-observed profile targets missing.' }
    if (@(Get-ChildItem $profile -Recurse -File | Where-Object { $_.Extension -in @('.pak', '.uasset', '.uexp', '.ubulk') }).Count -ne 0) { throw 'Profile collection exported game assets.' }
    if ((Get-FileHash $pak).Hash -ne $before) { throw 'Profile collection modified the input pak.' }
    Write-Host '[PASS] Profile evidence rejects unrelated packages, lists every missing target, preserves input and exports metadata only'

} catch {
    foreach ($log in Get-ChildItem $fixture -Recurse -Filter '*.log') { Write-Host (Get-Content $log.FullName -Raw) }
    throw
} finally { if (Test-Path $fixture) { Remove-Item $fixture -Recurse -Force } }
exit 0
