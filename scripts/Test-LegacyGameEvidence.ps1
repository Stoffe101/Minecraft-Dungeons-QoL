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
} catch {
    foreach ($log in Get-ChildItem $fixture -Recurse -Filter '*.log') { Write-Host (Get-Content $log.FullName -Raw) }
    throw
} finally { if (Test-Path $fixture) { Remove-Item $fixture -Recurse -Force } }
exit 0
