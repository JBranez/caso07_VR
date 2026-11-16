[CmdletBinding()]
param(
    [string]$UnityPath = '',
    [switch]$BuildWindows
)

$ErrorActionPreference = 'Stop'
$ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$PrepareScript = Join-Path $PSScriptRoot 'Prepare_And_Open_Unity65.ps1'
if (-not (Test-Path -LiteralPath $PrepareScript)) { throw "No se encontró el preflight: $PrepareScript" }

# First download/check local UPM packages, run Unity batchmode, and validate compilation.
# The preflight throws and does not open the GUI if any package or compile check fails.
& $PrepareScript -UnityPath $UnityPath -NoLaunch

if (-not $BuildWindows) {
    Write-Host 'VALIDACIÓN C# + PAQUETES + uGUI + XR: OK' -ForegroundColor Green
    return
}

if ([string]::IsNullOrWhiteSpace($UnityPath)) {
    if ($env:UNITY_EDITOR_PATH -and (Test-Path -LiteralPath $env:UNITY_EDITOR_PATH)) {
        $UnityPath = $env:UNITY_EDITOR_PATH
    } else {
        $UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe'
    }
}
if (-not (Test-Path -LiteralPath $UnityPath)) { throw "No se encontró Unity.exe: $UnityPath" }

$logDir = Join-Path $ProjectPath 'Builds\Validation'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$buildLog = Join-Path $logDir 'build-windows-x64.log'
if (Test-Path -LiteralPath $buildLog) { Remove-Item -LiteralPath $buildLog -Force -ErrorAction SilentlyContinue }
Write-Host 'Ejecutando build Windows x64 tras superar el preflight y la compilación C#...' -ForegroundColor Cyan
& $UnityPath -batchmode -nographics -projectPath $ProjectPath -executeMethod LasPalmeras.EditorTools.BuildScripts.BuildWindows -logFile $buildLog -quit
$buildExit = $LASTEXITCODE
$buildText = if (Test-Path -LiteralPath $buildLog) { Get-Content -LiteralPath $buildLog -Raw } else { '' }
if ($buildExit -ne 0 -or $buildText -notmatch 'Compilación StandaloneWindows64: Succeeded|Build completed with a result of .Succeeded.') {
    throw "Build Windows x64 no confirmado (exit code $buildExit). Revisa $buildLog"
}
Write-Host 'BUILD WINDOWS X64: OK' -ForegroundColor Green
