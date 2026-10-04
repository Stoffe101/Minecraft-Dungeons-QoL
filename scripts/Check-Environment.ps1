param(
    [string]$PaksPath,
    [string]$UeEditorDirectory
)

. (Join-Path $PSScriptRoot "Common.ps1")

$root = Get-ProjectRoot
Write-Host "Project: $root"

try {
    Assert-Command git
    Write-Host "[OK] git"
} catch {
    Write-Warning $_
}

try {
    Assert-Command python
    $pythonVersion = & python --version 2>&1
    Write-Host "[OK] $pythonVersion"
} catch {
    Write-Warning $_
}

try {
    $ue = Find-Ue422EditorDirectory -Override $UeEditorDirectory
    Write-Host "[OK] UE 4.22: $ue"
} catch {
    Write-Warning $_
}

try {
    $paks = Find-McdPaksPath -Override $PaksPath
    Write-Host "[OK] Minecraft Dungeons Paks: $paks"
    Write-Host "     Mods folder: $(Join-Path $paks '~mods')"
} catch {
    Write-Warning $_
}

$modKit = Get-ModKitPath
if (Test-Path $modKit) {
    Write-Host "[OK] Dungeons Mod Kit: $modKit"
} else {
    Write-Host "[--] Dungeons Mod Kit not bootstrapped yet. Run ./scripts/Bootstrap-ModKit.ps1"
}
