param(
    [switch]$SkipCook
)

. (Join-Path $PSScriptRoot "Common.ps1")

$root = Get-ProjectRoot
$modKit = Get-ModKitPath

if (-not (Test-Path $modKit)) {
    throw "Dungeons Mod Kit not found. Run ./scripts/Bootstrap-ModKit.ps1 first."
}

& (Join-Path $PSScriptRoot "Sync-GameApi.ps1")
& (Join-Path $PSScriptRoot "Sync-Assets.ps1")

$distDir = Join-Path $root "dist"
New-Item -ItemType Directory -Force -Path $distDir | Out-Null

Push-Location $modKit
try {
    if (-not $SkipCook) {
        Write-Host "Cooking Unreal assets..."
        & cmd.exe /c "cook_assets.bat"
        if ($LASTEXITCODE -ne 0) {
            throw "cook_assets.bat failed with exit code $LASTEXITCODE."
        }
    }

    Write-Host "Packaging mod..."
    & cmd.exe /c "package.bat"
    if ($LASTEXITCODE -ne 0) {
        throw "package.bat failed with exit code $LASTEXITCODE."
    }
} finally {
    Pop-Location
}

$pak = Get-DistPakPath
if (-not (Test-Path $pak)) {
    throw "Build finished without expected pak: $pak"
}

Write-Host "[OK] Built $pak"
