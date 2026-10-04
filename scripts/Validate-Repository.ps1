param()

. (Join-Path $PSScriptRoot "Common.ps1")

$root = Get-ProjectRoot
$errors = New-Object System.Collections.Generic.List[string]

Write-Host "Validating PowerShell syntax..."
Get-ChildItem (Join-Path $root "scripts") -Filter "*.ps1" | ForEach-Object {
    $tokens = $null
    $parseErrors = $null
    [void][System.Management.Automation.Language.Parser]::ParseFile($_.FullName, [ref]$tokens, [ref]$parseErrors)
    if ($parseErrors.Count -gt 0) {
        foreach ($error in $parseErrors) {
            $errors.Add("$($_.Name): $($error.Message)")
        }
    }
}

Write-Host "Validating JSON..."
Get-ChildItem (Join-Path $root "config") -Filter "*.json" -Recurse | ForEach-Object {
    try {
        Get-Content $_.FullName -Raw | ConvertFrom-Json | Out-Null
    } catch {
        $errors.Add("$($_.Name): invalid JSON: $($_.Exception.Message)")
    }
}

Write-Host "Checking canonical mirror SDK..."
$mirrorHeader = Join-Path $root "sdk\modkit\Source\Dungeons\MCDQoLInventoryStubs.h"
$mirrorCpp = Join-Path $root "sdk\modkit\Source\Dungeons\MCDQoLInventoryStubs.cpp"
foreach ($path in @($mirrorHeader, $mirrorCpp)) {
    if (-not (Test-Path $path)) {
        $errors.Add("Missing mirror SDK file: $path")
    }
}

$obsoleteMirror = Get-ChildItem (Join-Path $root "sdk") -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -in @("MCDQoLGameAPI.h", "MCDQoLGameAPI.cpp") }
if ($obsoleteMirror) {
    $errors.Add("Obsolete duplicate mirror SDK files still exist: $($obsoleteMirror.FullName -join ', ')")
}

if (Test-Path $mirrorHeader) {
    $text = Get-Content $mirrorHeader -Raw
    foreach ($required in @(
        "UItemStashComponent",
        "UInventoryItemSlot",
        "SalvageItemInSlot",
        "GetInventorySlots",
        "GetEquipmentSlots",
        "FItemSalvageUndoInfo"
    )) {
        if (-not $text.Contains($required)) {
            $errors.Add("Mirror SDK is missing required symbol: $required")
        }
    }
}

Write-Host "Checking canonical docs..."
foreach ($doc in @(
    "docs\CURRENT_STATE.md",
    "docs\FINDINGS.md",
    "docs\MODDING_RESEARCH.md",
    "docs\ROADMAP.md",
    "docs\RESEARCH_LOG.md",
    "docs\TEST_PLAN.md"
)) {
    if (-not (Test-Path (Join-Path $root $doc))) {
        $errors.Add("Missing canonical doc: $doc")
    }
}

if ($errors.Count -gt 0) {
    Write-Host ""
    Write-Host "Validation failed:" -ForegroundColor Red
    foreach ($error in $errors) {
        Write-Host " - $error" -ForegroundColor Red
    }
    exit 1
}

Write-Host "[OK] Repository validation passed."
