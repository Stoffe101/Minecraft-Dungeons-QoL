param(
    [Parameter(Mandatory=$true)][string]$DumperExe,
    [Parameter(Mandatory=$true)][string]$GameRoot
)

. (Join-Path $PSScriptRoot "Common.ps1")

if (-not (Test-Path $DumperExe)) {
    throw "UeBlueprintDumper executable not found: $DumperExe"
}

if (-not (Test-Path $GameRoot)) {
    throw "Game root not found: $GameRoot"
}

$root = Get-ProjectRoot
$outRoot = Join-Path $root ".research\UeBlueprintDumper"
New-Item -ItemType Directory -Force -Path $outRoot | Out-Null

$terms = @(
    "Inventory",
    "Salvage",
    "Equipment",
    "Item",
    "Enchant"
)

foreach ($term in $terms) {
    $out = Join-Path $outRoot $term
    New-Item -ItemType Directory -Force -Path $out | Out-Null

    Write-Host "Listing assets matching '$term'..."
    & $DumperExe --list $GameRoot "UE4_22" $term $out

    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Dumper returned exit code $LASTEXITCODE for '$term'."
    }
}

Write-Host "Research output: $outRoot"
Write-Host "Keep raw extracted game data local. Summarize only the necessary findings in docs."
