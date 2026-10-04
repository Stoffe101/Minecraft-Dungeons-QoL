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

    $engineDir = Split-Path (Split-Path $ue -Parent) -Parent
    $ubt = Join-Path $engineDir "Binaries\DotNET\UnrealBuildTool.exe"
    if (Test-Path $ubt) {
        Write-Host "[OK] UnrealBuildTool: $ubt"
    } else {
        Write-Warning "UnrealBuildTool was not found at $ubt"
    }
} catch {
    Write-Warning $_
}

$programFilesX86 = [Environment]::GetFolderPath("ProgramFilesX86")
$vswhere = Join-Path $programFilesX86 "Microsoft Visual Studio\Installer\vswhere.exe"
if (Test-Path $vswhere) {
    $vs = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath 2>$null
    if ($vs) {
        Write-Host "[OK] Visual Studio C++ tools: $vs"
    } else {
        Write-Warning "Visual Studio was found, but the MSVC C++ toolchain was not detected."
    }
} else {
    Write-Warning "vswhere.exe was not found. UE4.22 editor-module builds require Visual Studio 2017/2019 C++ build tools."
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
