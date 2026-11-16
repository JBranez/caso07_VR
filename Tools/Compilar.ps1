[CmdletBinding()]
param(
    [ValidateSet('Windows','Android')][string]$Destino = 'Windows',
    [string]$UnityPath = ''
)

$ErrorActionPreference = 'Stop'
$ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$PrepareScript = Join-Path $PSScriptRoot 'Prepare_And_Open_Unity65.ps1'
if (-not (Test-Path -LiteralPath $PrepareScript)) { throw "No se encontró $PrepareScript" }

# Dependencias locales descargadas/verificadas + importación y compilación C# antes de crear un build.
& $PrepareScript -UnityPath $UnityPath -NoLaunch
if ([string]::IsNullOrWhiteSpace($UnityPath)) {
    if ($env:UNITY_EDITOR_PATH -and (Test-Path -LiteralPath $env:UNITY_EDITOR_PATH)) {
        $UnityPath = $env:UNITY_EDITOR_PATH
    } else {
        $UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe'
    }
}
if (-not (Test-Path -LiteralPath $UnityPath)) { throw "No se encontró Unity.exe: $UnityPath" }

New-Item -ItemType Directory -Force -Path (Join-Path $ProjectPath 'Builds\Validation') | Out-Null
if ($Destino -eq 'Android') {
    $method = 'LasPalmeras.EditorTools.BuildScripts.BuildAndroid'
    $log = Join-Path $ProjectPath 'Builds\Validation\build-android.log'
} else {
    $method = 'LasPalmeras.EditorTools.BuildScripts.BuildWindows'
    $log = Join-Path $ProjectPath 'Builds\Validation\build-windows-x64.log'
}
& $UnityPath -batchmode -nographics -projectPath $ProjectPath -executeMethod $method -logFile $log -quit
$code = $LASTEXITCODE
$txt = if (Test-Path -LiteralPath $log) { Get-Content -LiteralPath $log -Raw } else { '' }
if ($code -ne 0 -or $txt -match 'error CS\d+|BuildResult.Failed|Build completed with a result of .Failed.') {
    throw "La compilación $Destino falló (exit code $code). Revisa $log"
}
Write-Host "Compilación $Destino finalizada. Log: $log" -ForegroundColor Green
