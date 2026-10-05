param([string]$PaksPath)
# Read game archive metadata only: no character saves, executable memory or
# ownership changes. The public archive key is already verified for this game.
$arguments = @{
    AesKey = '0x7D5F892ECEBFA53CC22001DF48B871D51C0DF7C54CE41933BFB285219829B3A8'
    CollectPersistenceEvidence = $true
}
if ($PaksPath) { $arguments.PaksPath = $PaksPath }
& (Join-Path $PSScriptRoot 'Collect-LegacyGameEvidence.ps1') @arguments
