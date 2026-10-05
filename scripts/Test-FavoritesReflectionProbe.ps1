param([string]$ArchivePath)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = Split-Path $PSScriptRoot -Parent
$invoke = Join-Path $PSScriptRoot 'Invoke-FavoritesReflectionProbe.ps1'
$temp = Join-Path ([IO.Path]::GetTempPath()) ('MCDQoL-probe-test-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $temp | Out-Null
function Assert($Condition, $Message) { if (-not $Condition) { throw $Message } }
function Reject($Body, $Expected) {
    $caught = $false
    try { & $Body } catch {
        $caught = $true
        Assert ($_.Exception.Message -match $Expected) "Unexpected error: $_"
    }
    Assert $caught "Expected rejection: $Expected"
}
try {
    $game = Join-Path $temp 'Win64'
    New-Item -ItemType Directory $game | Out-Null
    [IO.File]::WriteAllText((Join-Path $game 'Dungeons.exe'), 'fixture, not an executable')
    [IO.File]::WriteAllText((Join-Path $game 'unrelated.txt'), 'preserve me')
    $before = (Get-FileHash (Join-Path $game 'Dungeons.exe')).Hash
    # Rejection occurs before network/download or any game mutation.
    [IO.File]::WriteAllText((Join-Path $game 'dwmapi.dll'), 'another loader')
    Reject { & $invoke -Action Install -Win64Path $game } 'Existing loader'
    Assert ((Get-Content (Join-Path $game 'dwmapi.dll') -Raw) -eq 'another loader') 'Overwrote another proxy'
    Remove-Item (Join-Path $game 'dwmapi.dll')
    $bad = Join-Path $temp 'bad.zip'
    [IO.File]::WriteAllText($bad, 'not the pinned release')
    Reject { & $invoke -Action Install -Win64Path $game -ArchivePath $bad } 'checksum mismatch'
    Assert (-not (Test-Path (Join-Path $game 'MCDQoLReflectionProbe-install.json'))) 'Bad hash left an install'
    if (-not $ArchivePath) {
        $config = Get-Content (Join-Path $root 'config/reflection-probe.json') -Raw | ConvertFrom-Json
        $ArchivePath = Join-Path $temp 'UE4SS.zip'
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest -UseBasicParsing -Uri $config.url -OutFile $ArchivePath
    }
    & $invoke -Action Install -Win64Path $game -ArchivePath $ArchivePath
    $state = Get-Content (Join-Path $game 'MCDQoLReflectionProbe-install.json') -Raw | ConvertFrom-Json
    Assert (@($state.files).Count -eq 6) 'Wrong install allowlist'
    Assert ((Get-Content (Join-Path $game 'Mods/mods.txt') -Raw).Trim() -eq 'MCDQoLReflection : 1') 'Enabled an upstream gameplay mod'
    Assert (-not (Test-Path (Join-Path $game 'Mods/ConsoleEnablerMod'))) 'Copied unwanted mods'
    $outputs = Join-Path $temp 'output'
    & $invoke -Action Collect -Win64Path $game -OutputRoot $outputs -WarningAction SilentlyContinue
    $report = Get-Content (Get-ChildItem $outputs -Filter REPORT.json -Recurse | Select-Object -First 1).FullName -Raw | ConvertFrom-Json
    Assert (-not $report.captureCompleted -and @($report.issues).Count -gt 0) 'Missing runtime data reported success'
    $headers = Join-Path $game 'CXXHeaderDump'
    New-Item -ItemType Directory $headers | Out-Null
    [IO.File]::WriteAllText((Join-Path $game 'UE4SS.log'), '[MCDQoLReflection] Capture completed')
    $declarations = @('UInventoryItem','UInventoryItemSlot','UItemStashComponent','UDungeonsGameInstance','UDungeonsUserManager') | ForEach-Object { "class $_ {};" }
    [IO.File]::WriteAllText((Join-Path $headers 'Dungeons.hpp'), ($declarations -join "`n"))
    foreach ($name in @('Dungeons_enums.hpp','CoreUObject.hpp','Engine.hpp')) { [IO.File]::WriteAllText((Join-Path $headers $name), '// metadata') }
    [IO.File]::WriteAllText((Join-Path $game 'UE4SS_ObjectDump.txt'), "Class /Script/Dungeons.InventoryItem`nPrivate /Game/HeroName.Secret`n")
    [IO.File]::WriteAllText((Join-Path $headers 'private-save.dat'), 'must not collect')
    $complete = Join-Path $temp 'complete'
    & $invoke -Action Collect -Win64Path $game -OutputRoot $complete
    $reportPath = (Get-ChildItem $complete -Filter REPORT.json -Recurse | Select-Object -First 1).FullName
    $report = Get-Content $reportPath -Raw | ConvertFrom-Json
    Assert ($report.captureCompleted -and @($report.issues).Count -eq 0) 'Valid metadata reported incomplete'
    $unzip = Join-Path $temp 'roundtrip'
    Expand-Archive (Get-ChildItem $complete -Filter '*.zip' | Select-Object -First 1).FullName $unzip
    Assert (@(Get-ChildItem $unzip -File -Recurse).Count -eq 7) 'Evidence archive escaped allowlist'
    $filtered = Get-Content (Get-ChildItem $unzip -Filter Dungeons-reflection.txt -Recurse).FullName -Raw
    Assert ($filtered -match 'InventoryItem' -and $filtered -notmatch 'HeroName') 'Object dump was not filtered'
    # An edited owned file must remain, but the unchanged proxy must be removed.
    [IO.File]::AppendAllText((Join-Path $game 'UE4SS-settings.ini'), '; user change')
    Reject { & $invoke -Action Remove -Win64Path $game } 'Modified files preserved'
    Assert (Test-Path (Join-Path $game 'UE4SS-settings.ini')) 'Deleted edited configuration'
    Assert (-not (Test-Path (Join-Path $game 'dwmapi.dll'))) 'Left the unmodified proxy active'
    Copy-Item (Join-Path $root 'tools/FavoritesReflectionProbe/UE4SS-settings.ini') (Join-Path $game 'UE4SS-settings.ini') -Force
    & $invoke -Action Remove -Win64Path $game
    Assert (Test-Path (Join-Path $game 'unrelated.txt')) 'Removed unrelated file'
    Assert (Test-Path (Join-Path $headers 'Dungeons.hpp')) 'Deleted generated evidence'
    Assert ((Get-FileHash (Join-Path $game 'Dungeons.exe')).Hash -eq $before) 'Changed the executable'
    Write-Host '[PASS] Reflection probe ownership, checksum, collection allowlist, failure reporting and reversible removal.'
} finally { Remove-Item -LiteralPath $temp -Recurse -Force }
