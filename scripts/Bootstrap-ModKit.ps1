param(
    [string]$UeEditorDirectory,
    [string]$PaksPath,
    [switch]$SkipEditorBuild
)

. (Join-Path $PSScriptRoot "Common.ps1")

Assert-Command git
Assert-Command python

$root = Get-ProjectRoot
$toolsRoot = Join-Path $root ".tools"
$modKit = Get-ModKitPath
$pinnedCommit = "c30e88ec5e99e401eadedddbe82af0265a056fe7"

New-Item -ItemType Directory -Force -Path $toolsRoot | Out-Null

if (-not (Test-Path $modKit)) {
    Write-Host "Cloning Dungeons Mod Kit..."
    & git clone "https://github.com/Dokucraft/Dungeons-Mod-Kit.git" $modKit
    if ($LASTEXITCODE -ne 0) { throw "git clone failed." }

    Push-Location $modKit
    try {
        & git checkout $pinnedCommit
        if ($LASTEXITCODE -ne 0) { throw "Could not checkout pinned Mod Kit commit." }
    } finally {
        Pop-Location
    }
} else {
    Write-Host "Using existing Mod Kit: $modKit"
    Write-Host "It is not reset automatically to avoid destroying local Unreal work."
}

$ue = Find-Ue422EditorDirectory -Override $UeEditorDirectory
$settingsDir = Join-Path $modKit "Tools\user_settings"
New-Item -ItemType Directory -Force -Path $settingsDir | Out-Null

Set-Content -Path (Join-Path $settingsDir "editor_directory.txt") -Value $ue -NoNewline

$distDir = Join-Path $root "dist"
New-Item -ItemType Directory -Force -Path $distDir | Out-Null
$packageOutput = Get-DistPakPath
Set-Content -Path (Join-Path $settingsDir "package_output.txt") -Value $packageOutput -NoNewline

$sampleFiles = @(
    (Join-Path $modKit "UE4Project\Content\Decor\Prefabs\Lever\T_Lever.png"),
    (Join-Path $modKit "UE4Project\Content\Decor\Prefabs\Lever\T_Lever.uasset"),
    (Join-Path $modKit "Dungeons\Content\data\resourcepacks\squidcoast\blocks.json")
)
foreach ($sample in $sampleFiles) {
    if (Test-Path $sample) { Remove-Item -Force $sample }
}

Write-Host "[OK] UE editor directory: $ue"
Write-Host "[OK] Package output: $packageOutput"

& (Join-Path $PSScriptRoot "Sync-GameApi.ps1")

if (-not $SkipEditorBuild) {
    & (Join-Path $PSScriptRoot "Build-ModKit-Editor.ps1") -UeEditorDirectory $ue
}

try {
    $resolvedPaks = Find-McdPaksPath -Override $PaksPath
    Write-Host "[OK] Minecraft Dungeons Paks: $resolvedPaks"
} catch {
    Write-Warning "Game path was not detected yet. Build setup is still usable. $($_.Exception.Message)"
}

Write-Host "Bootstrap complete."
