param(
    [string]$UeEditorDirectory
)

. (Join-Path $PSScriptRoot "Common.ps1")

$modKit = Get-ModKitPath
if (-not (Test-Path $modKit)) {
    throw "Dungeons Mod Kit not found. Run ./scripts/Bootstrap-ModKit.ps1 first."
}

& (Join-Path $PSScriptRoot "Sync-GameApi.ps1")

$editorDir = Find-Ue422EditorDirectory -Override $UeEditorDirectory
$engineDir = Split-Path (Split-Path $editorDir -Parent) -Parent
$ubt = Join-Path $engineDir "Binaries\DotNET\UnrealBuildTool.exe"
$uproject = Join-Path $modKit "UE4Project\Dungeons.uproject"

if (-not (Test-Path $ubt)) {
    throw "UnrealBuildTool was not found at: $ubt"
}

if (-not (Test-Path $uproject)) {
    throw "Dungeons.uproject was not found at: $uproject"
}

Write-Host "Building DungeonsEditor with UE4.22 UnrealBuildTool..."
Write-Host "UBT:      $ubt"
Write-Host "Project:  $uproject"

& $ubt "DungeonsEditor" "Win64" "Development" "-Project=$uproject" "-WaitMutex" "-NoHotReload"
if ($LASTEXITCODE -ne 0) {
    throw "UnrealBuildTool failed with exit code $LASTEXITCODE. Ensure Visual Studio 2017/2019 C++ build tools and a Windows SDK supported by UE4.22 are installed."
}

$dll = Join-Path $modKit "UE4Project\Binaries\Win64\UE4Editor-Dungeons.dll"
if (-not (Test-Path $dll)) {
    throw "Editor build reported success but the Dungeons editor module was not found: $dll"
}

Write-Host "[OK] Rebuilt Dungeons editor reflection module: $dll"
