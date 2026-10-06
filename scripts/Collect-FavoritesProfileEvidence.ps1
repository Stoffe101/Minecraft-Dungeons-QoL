param([string]$PaksPath)
# Read only the exact character/profile Blueprint packages observed in the
# existing retail catalog. No loader, executable or character saves accessed.
$arguments = @{
    AesKey = '0x7D5F892ECEBFA53CC22001DF48B871D51C0DF7C54CE41933BFB285219829B3A8'
    CollectProfileEvidence = $true
}
if ($PaksPath) { $arguments.PaksPath = $PaksPath }
& (Join-Path $PSScriptRoot 'Collect-LegacyGameEvidence.ps1') @arguments
