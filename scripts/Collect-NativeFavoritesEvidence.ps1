param([int]$GameProcessId, [string]$DotNetPath, [switch]$CollectSerializationContracts)
. (Join-Path $PSScriptRoot 'Common.ps1')
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT -or -not [Environment]::Is64BitOperatingSystem) { throw 'This experimental read-only collector requires Windows x64.' }
$root = Get-ProjectRoot
if (-not $DotNetPath) {
    $cfg = Get-Content (Join-Path $root 'config/evidence-tool.json') -Raw | ConvertFrom-Json
    $localSdk = Join-Path $root ".tools/dotnet-evidence-sdk-$($cfg.sdkVersion)/dotnet.exe"
    if (Test-Path $localSdk) { $DotNetPath = $localSdk }
    else { $installed = Get-Command dotnet -ErrorAction SilentlyContinue; if ($installed) { $DotNetPath = $installed.Source } }
}
if (-not $DotNetPath) { throw 'The existing .NET 8 SDK is needed. Supply -DotNetPath; no game or system permissions should be changed.' }
$games = @(Get-Process -Name Dungeons -ErrorAction SilentlyContinue)
if ($GameProcessId) { $games = @($games | Where-Object { $_.Id -eq $GameProcessId }) }
if ($games.Count -ne 1) { throw 'Expected one running Dungeons process in camp. For multiple instances supply -GameProcessId.' }
$build = Join-Path $root '.tools/LegacyNativeEvidence'
$logs = Join-Path $root ('.research/native-favorites-build-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $logs | Out-Null
$project = Join-Path $root 'tools/LegacyNativeEvidence/LegacyNativeEvidence.csproj'
if ((Invoke-EvidenceProcess $DotNetPath @('build', $project, '-c', 'Release', '-o', $build) (Join-Path $logs 'Build.log')) -ne 0) { throw "Reader build failed; see $logs. No game files changed." }
$out = Join-Path $root ('.research/native-favorites-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 6))
Push-Location $root
try {
    $readerArgs = @((Join-Path $build 'LegacyNativeEvidence.dll'), [string]$games[0].Id, $out)
    if ($CollectSerializationContracts) { $readerArgs += '--serialization-contracts' }
    $code = Invoke-EvidenceProcess $DotNetPath $readerArgs (Join-Path $logs 'Reader.log')
} finally { Pop-Location }
if (-not (Test-Path (Join-Path $out 'REPORT.json'))) { throw "No capture report; see $logs. Do not change game protections or install a loader." }
Compress-Archive -LiteralPath (Join-Path $out 'REPORT.json') -DestinationPath "$out.zip"
Write-Host "Private native declaration evidence: $out.zip"
if ($code -ne 0) { Write-Warning 'Capture is incomplete. Keep the report, including access denial or unsupported layout; do not retry with ACL/ownership changes.' }
