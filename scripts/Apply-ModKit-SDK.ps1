param(
    [string]$ModKitPath
)

. (Join-Path $PSScriptRoot "Common.ps1")

if ($ModKitPath) {
    $expected = Get-ModKitPath
    if ((Resolve-Path $ModKitPath).Path -ne (Resolve-Path $expected).Path) {
        throw "Custom ModKitPath is no longer supported by this compatibility wrapper. Use the project bootstrap path: $expected"
    }
}

& (Join-Path $PSScriptRoot "Sync-GameApi.ps1")
