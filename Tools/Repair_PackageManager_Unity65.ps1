[CmdletBinding()]
param([string]$UnityPath = '', [switch]$NoLaunch, [switch]$ForceDownload, [int]$TimeoutMinutes = 25)
$ErrorActionPreference = 'Stop'
$script = Join-Path $PSScriptRoot 'Prepare_And_Open_Unity65.ps1'
if (-not (Test-Path -LiteralPath $script)) { throw "No se encontró $script" }
& $script -UnityPath $UnityPath -NoLaunch:$NoLaunch -ForceDownload:$ForceDownload -TimeoutMinutes $TimeoutMinutes
