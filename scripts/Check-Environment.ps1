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
    # UE4.22 officially integrates with VS 2017 and VS 2019.
    $supportedVs = & $vswhere -latest -version "[15.0,17.0)" -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath 2>$null

    if ($supportedVs) {
        $catalogJson = & $vswhere -latest -version "[15.0,17.0)" -products * -format json
        $catalog = $catalogJson | ConvertFrom-Json
        $display = if ($catalog.displayName) { $catalog.displayName } else { "Visual Studio 2017/2019" }
        Write-Host "[OK] Supported UE4.22 C++ toolchain: $display"
        Write-Host "     $supportedVs"
    } else {
        $newerVs = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath 2>$null

        if ($newerVs) {
            Write-Warning "MSVC was found at '$newerVs', but UE4.22 officially integrates with Visual Studio 2017 or 2019. Install VS 2019 C++ tools if editor-module compilation fails."
        } else {
            Write-Warning "MSVC C++ tools were not detected. Install Visual Studio 2019 with the C++ workload for the safest UE4.22 setup."
        }
    }
} else {
    Write-Warning "vswhere.exe was not found. UE4.22 editor-module builds require Visual Studio 2017/2019 C++ build tools."
}

try {
    $kitsRoot = (Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows Kits\Installed Roots" -ErrorAction Stop).KitsRoot10
    if ($kitsRoot -and (Test-Path $kitsRoot)) {
        Write-Host "[OK] Windows 10 SDK root: $kitsRoot"
    } else {
        Write-Warning "Windows 10 SDK root was not detected."
    }
} catch {
    Write-Warning "Windows 10 SDK was not detected. UE4 C++ builds normally require a Windows SDK."
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
