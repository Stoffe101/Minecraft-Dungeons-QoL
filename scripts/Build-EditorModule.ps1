param(
    [string]$UeEditorDirectory,
    [switch]$Clean
)

. (Join-Path $PSScriptRoot "Common.ps1")

$modKit = Get-ModKitPath
if (-not (Test-Path $modKit)) {
    throw "Dungeons Mod Kit not found. Run ./scripts/Bootstrap-ModKit.ps1 first."
}

$ueBin = Find-Ue422EditorDirectory -Override $UeEditorDirectory
$engineDir = Split-Path (Split-Path $ueBin -Parent) -Parent
$ubt = Join-Path $engineDir "Binaries\DotNET\UnrealBuildTool.exe"
$uproject = Join-Path $modKit "UE4Project\Dungeons.uproject"

if (-not (Test-Path $ubt)) {
    throw "UnrealBuildTool not found: $ubt"
}

& (Join-Path $PSScriptRoot "Sync-GameApi.ps1")

if ($Clean) {
    Write-Host "Cleaning DungeonsEditor target..."
    & $ubt DungeonsEditor Win64 Development -Project="$uproject" -Clean -WaitMutex
    if ($LASTEXITCODE -ne 0) {
        throw "UnrealBuildTool clean failed with exit code $LASTEXITCODE."
    }
}

Write-Host "Building DungeonsEditor (UE4.22 mirror SDK validation)..."
& $ubt DungeonsEditor Win64 Development -Project="$uproject" -WaitMutex
if ($LASTEXITCODE -ne 0) {
    throw "DungeonsEditor build failed with exit code $LASTEXITCODE."
}

Write-Host "[OK] DungeonsEditor built successfully."
Write-Host "     This validates UnrealHeaderTool + C++ compilation for the mirror SDK."
