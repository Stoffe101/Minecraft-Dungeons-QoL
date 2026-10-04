param(
    [string]$UeEditorDirectory,
    [switch]$StageUiReference,
    [switch]$SkipBuild
)

. (Join-Path $PSScriptRoot "Common.ps1")

$modKit = Get-ModKitPath
if (-not (Test-Path $modKit)) {
    throw "Dungeons Mod Kit not found. Run ./scripts/Bootstrap-ModKit.ps1 first."
}

$ueBin = Find-Ue422EditorDirectory -Override $UeEditorDirectory
$editor = Join-Path $ueBin "UE4Editor.exe"
$uproject = Join-Path $modKit "UE4Project\Dungeons.uproject"

if (-not (Test-Path $editor)) {
    throw "UE4Editor.exe not found: $editor"
}

& (Join-Path $PSScriptRoot "Sync-GameApi.ps1")

if ($StageUiReference) {
    & (Join-Path $PSScriptRoot "Stage-UIReference.ps1")
}

if (-not $SkipBuild) {
    & (Join-Path $PSScriptRoot "Build-EditorModule.ps1") -UeEditorDirectory $ueBin
}

Write-Host "Opening Dungeons Mod Kit in UE4.22..."
Start-Process -FilePath $editor -ArgumentList @($uproject)
