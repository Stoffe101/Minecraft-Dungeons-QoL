param(
    [string]$PaksPath,
    [Parameter(Mandatory=$true)][string]$AesKey,
    [string]$OutputDirectory,
    [string]$DotNetPath,
    [string]$AssetMatch,
    [switch]$CollectInventoryPatchSources,
    [switch]$CollectPersistenceEvidence,
    [switch]$CollectProfileEvidence
)
. (Join-Path $PSScriptRoot 'Common.ps1')
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw 'This collector requires Windows x64.' }
if ($AesKey -notmatch '^(0x)?[0-9a-fA-F]{64}$') { throw 'Supply a 256-bit hexadecimal -AesKey.' }
if ($CollectInventoryPatchSources -and $AssetMatch) { throw 'Patch-source collection uses an exact seven-package allowlist; do not combine it with -AssetMatch.' }
if ($CollectPersistenceEvidence -and ($CollectInventoryPatchSources -or $AssetMatch)) { throw 'Persistence collection uses its own metadata-only targets; do not combine selection modes.' }
if ($CollectProfileEvidence -and ($CollectPersistenceEvidence -or $CollectInventoryPatchSources -or $AssetMatch)) { throw 'Profile collection uses an exact metadata-only allowlist; do not combine selection modes.' }
$root = Get-ProjectRoot
$paks = Find-McdPaksPath -Override $PaksPath
if (-not $OutputDirectory) {
    $folder = if ($CollectInventoryPatchSources) { 'inventory-patch-sources-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') } elseif ($CollectPersistenceEvidence) { 'persistence-evidence-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') } elseif ($CollectProfileEvidence) { 'profile-evidence-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') } else { 'game-evidence-legacy' }
    $OutputDirectory = Join-Path $root ('.research/' + $folder)
}
$out = [System.IO.Path]::GetFullPath($OutputDirectory)
$game = [System.IO.Path]::GetFullPath((Split-Path (Split-Path $paks -Parent) -Parent)).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
if (($out + [System.IO.Path]::DirectorySeparatorChar).StartsWith($game, [StringComparison]::OrdinalIgnoreCase)) { throw 'Output must be outside the game directory.' }
if ((Test-Path $out) -or (Test-Path "$out.zip")) { throw "Output exists: $out. Choose a fresh -OutputDirectory." }
New-Item -ItemType Directory -Force $out | Out-Null
$issues = New-Object System.Collections.Generic.List[string]
$cfg = Get-Content (Join-Path $root 'config/evidence-tool.json') -Raw | ConvertFrom-Json
try {
    $archive = Join-Path $root '.tools/UeBlueprintDumper-1.2.0.zip'
    $toolDir = Join-Path $root '.tools/UeBlueprintDumper-1.2.0'
    New-Item -ItemType Directory -Force (Split-Path $archive -Parent) | Out-Null
    if (-not (Test-Path $archive)) { Invoke-WebRequest $cfg.url -UseBasicParsing -OutFile $archive }
    if ((Get-FileHash $archive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $cfg.sha256) { throw 'Inspector library checksum mismatch.' }
    if (-not (Test-Path $toolDir)) { Expand-Archive $archive $toolDir }
    $lib = @(Get-ChildItem $toolDir -Recurse -Filter CUE4Parse.dll)
    if ($lib.Count -ne 1) { throw 'Expected one pinned archive-reader library.' }
    $libraries = Split-Path $lib[0].FullName -Parent
    if (-not $DotNetPath) {
        $installed = Get-Command dotnet -ErrorAction SilentlyContinue
        if ($installed) {
            $code = Invoke-EvidenceProcess $installed.Source @('--list-sdks') (Join-Path $out 'SDK.log')
            if ($code -eq 0 -and (Get-Content (Join-Path $out 'SDK.log') -Raw) -match '(?m)^(8|9|10)\.') { $DotNetPath = $installed.Source }
        }
    }
    if (-not $DotNetPath) {
        $sdk = Join-Path $root ".tools/dotnet-evidence-sdk-$($cfg.sdkVersion)"
        $DotNetPath = Join-Path $sdk 'dotnet.exe'
        if (-not (Test-Path $DotNetPath)) {
            Write-Host '[INFO] Downloading a local .NET 8 SDK (about 200 MB); no global installation.'
            $sdkZip = "$sdk.zip"
            Invoke-WebRequest $cfg.sdkUrl -UseBasicParsing -OutFile $sdkZip
            if ((Get-FileHash $sdkZip -Algorithm SHA512).Hash.ToLowerInvariant() -ne $cfg.sdkSha512) { throw 'SDK checksum mismatch.' }
            Expand-Archive $sdkZip $sdk -Force
        }
    }
    $buildDir = Join-Path $root '.tools/LegacyEvidenceExporter'
    $project = Join-Path $root 'tools/LegacyEvidenceExporter/LegacyEvidenceExporter.csproj'
    $buildArgs = @('build', $project, '-c', 'Release', '-o', $buildDir, "-p:InspectorDirectory=$libraries")
    $code = Invoke-EvidenceProcess $DotNetPath $buildArgs (Join-Path $out 'Build.log')
    if ($code -ne 0) { throw 'Legacy inspector build failed; see Build.log.' }
    $data = Join-Path $out 'Metadata'
    $arguments = @((Join-Path $buildDir 'LegacyEvidenceExporter.dll'), '--paks', $paks, $AesKey, $data, $libraries)
    if ($AssetMatch) { $arguments += $AssetMatch }
    if ($CollectInventoryPatchSources) { $arguments += '--inventory-patch-sources' }
    if ($CollectPersistenceEvidence) { $arguments += '--persistence-evidence' }
    if ($CollectProfileEvidence) { $arguments += '--profile-evidence' }
    $code = Invoke-EvidenceProcess $DotNetPath $arguments (Join-Path $out 'Exporter.log')
    if ($code -ne 0) { $issues.Add("Legacy exporter returned $code; partial metadata and logs retained.") }
    if (-not (Test-Path (Join-Path $data 'EXPORT_REPORT.json'))) { $issues.Add('Exporter produced no completion manifest.') }
} catch { $issues.Add($_.Exception.Message) }
[ordered]@{
    schemaVersion = 1
    collectedUtc = [DateTime]::UtcNow.ToString('o')
    game = 'Minecraft Dungeons 1'
    parser = 'UAssetAPI 1.1.0 legacy UProperty / UE4_22'
    archiveReader = 'CUE4Parse from pinned UeBlueprintDumper 1.2.0'
    aesKeyProvided = $true
    inventoryPatchSources = [bool]$CollectInventoryPatchSources
    profileEvidence = [bool]$CollectProfileEvidence
    issues = @($issues.ToArray())
    note = $(if ($CollectInventoryPatchSources) { 'Includes seven allowlisted cooked inventory UI packages and available .uexp companions for private patch development. No saves or executables inspected. Do not publish these game-owned files.' } else { 'Metadata/imports/Kismet only. Raw package companions are held in a temporary directory and removed by the exporter. No saves inspected.' })
} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $out 'REPORT.json')
Compress-Archive -Path (Join-Path $out '*') -DestinationPath "$out.zip"
Write-Host "[OK] Legacy evidence archive: $out.zip"
if ($issues.Count -gt 0) { Write-Warning ($issues -join '; '); exit 1 }
exit 0
