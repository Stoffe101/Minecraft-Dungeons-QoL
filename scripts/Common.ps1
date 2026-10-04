Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-ProjectRoot {
    return (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}

function Find-McdPaksPath {
    param([string]$Override)

    $candidates = New-Object System.Collections.Generic.List[string]

    $explicit = $Override
    if (-not $explicit) { $explicit = $env:MCD_PAKS_PATH }
    if ($explicit) {
        if (-not (Test-Path $explicit -PathType Container)) {
            throw "Explicit Dungeons Paks path does not exist: $explicit"
        }
        return (Resolve-Path $explicit).Path
    }

    foreach ($drive in Get-PSDrive -PSProvider FileSystem) {
        $candidates.Add((Join-Path $drive.Root "XboxGames\Minecraft Dungeons\Content\Dungeons\Content\Paks"))
    }

    if ($env:LOCALAPPDATA) {
        $candidates.Add((Join-Path $env:LOCALAPPDATA "Mojang\products\dungeons\dungeons\Dungeons\Content\Paks"))
    }

    $matches = @($candidates | Select-Object -Unique | Where-Object { $_ -and (Test-Path $_ -PathType Container) })
    if ($matches.Count -eq 1) { return (Resolve-Path $matches[0]).Path }
    if ($matches.Count -gt 1) {
        throw "Multiple Dungeons installations found. Pass -PaksPath explicitly: $($matches -join '; ')"
    }

    throw "Minecraft Dungeons Paks folder was not found. Pass -PaksPath or set MCD_PAKS_PATH."
}

function Find-Ue422EditorDirectory {
    param([string]$Override)

    $candidates = New-Object System.Collections.Generic.List[string]

    if ($Override) { $candidates.Add($Override) }
    if ($env:UE4_22_EDITOR_DIR) { $candidates.Add($env:UE4_22_EDITOR_DIR) }

    $candidates.Add("C:\Program Files\Epic Games\UE_4.22\Engine\Binaries\Win64")

    foreach ($key in @(
        "HKLM:\SOFTWARE\EpicGames\Unreal Engine\4.22",
        "HKLM:\SOFTWARE\WOW6432Node\EpicGames\Unreal Engine\4.22"
    )) {
        try {
            $installed = (Get-ItemProperty -Path $key -ErrorAction Stop).InstalledDirectory
            if ($installed) {
                $candidates.Add((Join-Path $installed "Engine\Binaries\Win64"))
            }
        } catch {
            # Registry key is optional.
        }
    }

    foreach ($candidate in $candidates | Select-Object -Unique) {
        if ($candidate -and (Test-Path (Join-Path $candidate "UE4Editor-Cmd.exe"))) {
            return (Resolve-Path $candidate).Path
        }
    }

    throw "Unreal Engine 4.22 editor binaries were not found. Pass -UeEditorDirectory or set UE4_22_EDITOR_DIR."
}

function Assert-Command {
    param([Parameter(Mandatory=$true)][string]$Name)

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found in PATH."
    }
}

function Get-ModKitPath {
    $root = Get-ProjectRoot
    return (Join-Path $root ".tools\Dungeons-Mod-Kit")
}

function Get-DistPakPath {
    $root = Get-ProjectRoot
    return (Join-Path $root "dist\MinecraftDungeonsQoL.pak")
}

function Invoke-EvidenceProcess {
    param([string]$Executable, [string[]]$Arguments, [string]$LogPath)
    $info = New-Object System.Diagnostics.ProcessStartInfo
    $info.FileName = $Executable
    # Windows argv quoting also works with .NET Framework / PowerShell 5.1.
    $info.Arguments = (@($Arguments | ForEach-Object {
        $escaped = [regex]::Replace($_, '(\\*)"', '$1$1\"')
        $escaped = [regex]::Replace($escaped, '(\\+)$', '$1$1')
        '"' + $escaped + '"'
    }) -join ' ')
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $info
    try {
        if (-not $process.Start()) { throw "Could not start inspector: $Executable" }
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        $output = $stdout.GetAwaiter().GetResult()
        $errors = $stderr.GetAwaiter().GetResult()
        [System.IO.File]::WriteAllText($LogPath, $output + "`n--- Inspector stderr ---`n" + $errors)
        return $process.ExitCode
    } finally { $process.Dispose() }
}
