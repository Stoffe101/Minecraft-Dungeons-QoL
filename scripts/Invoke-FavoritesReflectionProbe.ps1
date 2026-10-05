param(
    [Parameter(Mandatory = $true)] [ValidateSet('Install', 'Collect', 'Remove')] [string]$Action,
    [string]$Win64Path = 'C:\XboxGames\Minecraft Dungeons\Content\Dungeons\Binaries\Win64',
    [string]$ArchivePath,
    [string]$OutputRoot
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = Split-Path $PSScriptRoot -Parent
$game = [IO.Path]::GetFullPath($Win64Path)
$config = Get-Content (Join-Path $root 'config/reflection-probe.json') -Raw | ConvertFrom-Json
$statePath = Join-Path $game 'MCDQoLReflectionProbe-install.json'
$owned = @('UE4SS.dll', 'UE4SS-settings.ini', 'Mods/mods.txt',
    'Mods/MCDQoLReflection/Scripts/main.lua', 'Mods/MCDQoLReflection/LICENSE-UE4SS.txt', 'dwmapi.dll')
if (-not (Test-Path $game -PathType Container)) { throw "Win64 directory not found: $game" }
if (-not (Test-Path (Join-Path $game 'Dungeons.exe') -PathType Leaf) -and
    -not (Test-Path (Join-Path $game 'Dungeons-Win64-Shipping.exe') -PathType Leaf)) {
    throw 'This must be the Dungeons Binaries/Win64 directory containing the actual executable.'
}
if (@(Get-Process -Name 'Dungeons', 'Dungeons-Win64-Shipping' -ErrorAction SilentlyContinue).Count -gt 0) {
    throw 'Close Minecraft Dungeons before installing, collecting or removing the probe.'
}
function Write-Json($Path, $Value) {
    [IO.File]::WriteAllText($Path, ($Value | ConvertTo-Json -Depth 12), (New-Object Text.UTF8Encoding($false)))
}
function Read-State {
    if (-not (Test-Path $statePath -PathType Leaf)) { throw 'No probe install manifest. No game files will be changed.' }
    $state = Get-Content $statePath -Raw | ConvertFrom-Json
    if ($state.schemaVersion -ne 1 -or @($state.files).Count -ne $owned.Count) { throw 'Invalid probe manifest.' }
    $seen = @{}
    foreach ($file in $state.files) {
        if ($file.path -cnotin $owned -or $seen.ContainsKey($file.path) -or $file.sha256 -notmatch '^[0-9a-f]{64}$') {
            throw 'Invalid probe ownership entry. No game files will be changed.'
        }
        $seen[$file.path] = $true
    }
    return $state
}
if ($Action -eq 'Install') {
    # Do not replace another proxy, mod loader or configuration. Do not change
    # XboxGames ownership/ACLs if this Store installation denies writes.
    foreach ($name in @($owned) + @('Mods', 'xinput1_3.dll', 'UE4SS.log', 'UE4SS_ObjectDump.txt',
        'CXXHeaderDump', 'UE4SS_Signatures', 'MemberVariableLayout.ini', 'VTableLayout.ini',
        'UE4SS', 'ue4ss', 'MCDQoLReflectionProbe-install.json')) {
        if (Test-Path (Join-Path $game $name)) { throw "Existing loader/probe file detected: $name. Nothing installed." }
    }
    $scratch = Join-Path ([IO.Path]::GetTempPath()) ('MCDQoL-reflection-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory $scratch | Out-Null
    $created = New-Object 'System.Collections.Generic.List[string]'
    try {
        $zip = Join-Path $scratch 'UE4SS.zip'
        if ($ArchivePath) { Copy-Item -LiteralPath $ArchivePath -Destination $zip }
        else {
            [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
            Invoke-WebRequest -UseBasicParsing -Uri $config.url -OutFile $zip
        }
        if ((Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant() -ne $config.sha256) { throw 'UE4SS archive checksum mismatch.' }
        $unpacked = Join-Path $scratch 'upstream'
        Expand-Archive -LiteralPath $zip -DestinationPath $unpacked
        $stage = Join-Path $scratch 'stage'
        New-Item -ItemType Directory (Join-Path $stage 'Mods/MCDQoLReflection/Scripts') -Force | Out-Null
        foreach ($dll in @('dwmapi.dll', 'UE4SS.dll')) { Copy-Item (Join-Path $unpacked $dll) (Join-Path $stage $dll) }
        Copy-Item (Join-Path $root 'tools/FavoritesReflectionProbe/UE4SS-settings.ini') (Join-Path $stage 'UE4SS-settings.ini')
        Copy-Item (Join-Path $root 'tools/FavoritesReflectionProbe/main.lua') (Join-Path $stage 'Mods/MCDQoLReflection/Scripts/main.lua')
        Copy-Item (Join-Path $root 'third_party/UE4SS-LICENSE.txt') (Join-Path $stage 'Mods/MCDQoLReflection/LICENSE-UE4SS.txt')
        [IO.File]::WriteAllText((Join-Path $stage 'Mods/mods.txt'), "MCDQoLReflection : 1`n", (New-Object Text.UTF8Encoding($false)))
        $files = @($owned | ForEach-Object { @{ path = $_; sha256 = (Get-FileHash (Join-Path $stage $_) -Algorithm SHA256).Hash.ToLowerInvariant() } })
        # Save ownership first so an interrupted installation remains removable.
        Write-Json $statePath @{ schemaVersion = 1; installedUtc = [DateTime]::UtcNow.ToString('o'); source = $config; files = $files }
        $created.Add($statePath)
        foreach ($name in $owned) {
            $dest = Join-Path $game $name
            New-Item -ItemType Directory (Split-Path $dest -Parent) -Force | Out-Null
            # Copy without -Force; collisions fail rather than overwrite.
            [IO.File]::Copy((Join-Path $stage $name), $dest, $false)
            $created.Add($dest)
        }
        Write-Host 'Probe installed. Start the game, enter camp, open inventory, press Ctrl+H once, wait 30 seconds, then quit.'
        Write-Host 'Then run this script with -Action Collect, followed by -Action Remove.'
    } catch {
        foreach ($path in $created) { if (Test-Path $path -PathType Leaf) { Remove-Item -LiteralPath $path } }
        foreach ($dir in @('Mods/MCDQoLReflection/Scripts', 'Mods/MCDQoLReflection', 'Mods')) {
            $path = Join-Path $game $dir
            if ((Test-Path $path -PathType Container) -and @(Get-ChildItem -LiteralPath $path -Force).Count -eq 0) { Remove-Item -LiteralPath $path }
        }
        throw
    } finally { Remove-Item -LiteralPath $scratch -Recurse -Force }
    return
}
$state = Read-State
if ($Action -eq 'Remove') {
    $changed = New-Object 'System.Collections.Generic.List[string]'
    foreach ($file in $state.files) {
        $path = Join-Path $game $file.path
        if (-not (Test-Path $path -PathType Leaf)) { continue }
        if ((Get-FileHash $path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $file.sha256) {
            $changed.Add($file.path)
            continue
        }
        Remove-Item -LiteralPath $path
    }
    foreach ($dir in @('Mods/MCDQoLReflection/Scripts', 'Mods/MCDQoLReflection', 'Mods')) {
        $path = Join-Path $game $dir
        if ((Test-Path $path -PathType Container) -and @(Get-ChildItem -LiteralPath $path -Force).Count -eq 0) { Remove-Item -LiteralPath $path }
    }
    if ($changed.Count -gt 0) { throw "Modified files preserved; manifest retained: $($changed -join ', '). Check any retained loader DLL before restarting." }
    Remove-Item -LiteralPath $statePath
    Write-Host 'Probe removed. Generated headers/logs and every unrelated file were preserved.'
    return
}
if (-not $OutputRoot) { $OutputRoot = Join-Path $root '.research' }
$output = Join-Path ([IO.Path]::GetFullPath($OutputRoot)) ('reflection-evidence-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 6))
New-Item -ItemType Directory $output -Force | Out-Null
$files = New-Object 'System.Collections.Generic.List[object]'
$issues = New-Object 'System.Collections.Generic.List[string]'
$start = if ($state.installedUtc -is [DateTime]) { $state.installedUtc.ToUniversalTime() }
    else { [DateTime]::Parse($state.installedUtc, [Globalization.CultureInfo]::InvariantCulture,
        [Globalization.DateTimeStyles]::RoundtripKind).ToUniversalTime() }
foreach ($name in @('UE4SS.log', 'CXXHeaderDump/Dungeons.hpp', 'CXXHeaderDump/Dungeons_enums.hpp',
    'CXXHeaderDump/CoreUObject.hpp', 'CXXHeaderDump/Engine.hpp')) {
    $path = Join-Path $game $name
    if (-not (Test-Path $path -PathType Leaf)) { $issues.Add("Missing: $name"); continue }
    if ((Get-Item $path).LastWriteTimeUtc -lt $start) { $issues.Add("Stale: $name"); continue }
    $dest = Join-Path $output $name
    New-Item -ItemType Directory (Split-Path $dest -Parent) -Force | Out-Null
    Copy-Item -LiteralPath $path -Destination $dest
    $files.Add(@{ path = $name; bytes = (Get-Item $dest).Length; sha256 = (Get-FileHash $dest -Algorithm SHA256).Hash.ToLowerInvariant() })
}
$dump = Join-Path $game 'UE4SS_ObjectDump.txt'
if ((Test-Path $dump -PathType Leaf) -and (Get-Item $dump).LastWriteTimeUtc -ge $start) {
    # Keep native Dungeons declarations; omit the full object/address catalog.
    $dest = Join-Path $output 'Dungeons-reflection.txt'
    Get-Content -LiteralPath $dump | Where-Object { $_ -match '/Script/Dungeons[.:/]' } | Set-Content -LiteralPath $dest -Encoding UTF8
    $files.Add(@{ path = 'Dungeons-reflection.txt'; bytes = (Get-Item $dest).Length; sha256 = (Get-FileHash $dest -Algorithm SHA256).Hash.ToLowerInvariant() })
} else { $issues.Add('Missing or stale: UE4SS_ObjectDump.txt') }
$symbols = @{}
$header = Join-Path $output 'CXXHeaderDump/Dungeons.hpp'
$text = if (Test-Path $header) { Get-Content $header -Raw } else { '' }
foreach ($symbol in @('UInventoryItem', 'UInventoryItemSlot', 'UItemStashComponent', 'UDungeonsGameInstance', 'UDungeonsUserManager')) {
    $symbols[$symbol] = [bool]($text -match ('\bclass\s+' + $symbol + '\b'))
    if (-not $symbols[$symbol]) { $issues.Add("Native class declaration missing: $symbol") }
}
$log = Join-Path $output 'UE4SS.log'
$completed = (Test-Path $log) -and [bool]((Get-Content $log -Raw) -match '\[MCDQoLReflection\] Capture completed')
if (-not $completed) { $issues.Add('Probe did not report a completed capture; include this ZIP even if startup failed.') }
Write-Json (Join-Path $output 'REPORT.json') @{ schemaVersion = 1; collectedUtc = [DateTime]::UtcNow.ToString('o'); source = $config;
    captureCompleted = $completed; symbols = $symbols; issues = @($issues.ToArray()); files = @($files.ToArray());
    note = 'Private runtime reflection metadata/logs only. No saves, executables, crash memory or item values copied. Headers are not a stable identity or runtime ABI guarantee.' }
Compress-Archive -LiteralPath $output -DestinationPath ($output + '.zip')
if ($issues.Count -gt 0) { Write-Warning ($issues -join "`n") }
Write-Host "Upload this ZIP: $output.zip"
