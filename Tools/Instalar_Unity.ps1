# Instala Unity Hub (si falta) y Unity 6000.5.2f1 (6.5) con los módulos de Android para Cardboard.
# Uso (PowerShell):  .\Instalar_Unity.ps1            -> editor + Android (SDK/NDK/OpenJDK)
#                    .\Instalar_Unity.ps1 -SinAndroid -> solo PC
param([switch]$SinAndroid)
$ErrorActionPreference = "Stop"
$version   = "6000.5.2f1"
$changeset = "eb73d3b415a1"
$hub = "C:\Program Files\Unity Hub\Unity Hub.exe"

if (-not (Test-Path $hub)) {
    Write-Host "Instalando Unity Hub con winget..."
    winget install --id Unity.UnityHub -e --accept-source-agreements --accept-package-agreements
    Start-Sleep -Seconds 5
}
if (-not (Test-Path $hub)) { throw "No se encontró Unity Hub en '$hub'. Instálalo desde https://unity.com/download y vuelve a ejecutar." }

$hubArgs = @("--", "--headless", "install", "--version", $version, "--changeset", $changeset)
if (-not $SinAndroid) { $hubArgs += @("-m", "android", "--childModules") }
Write-Host "Instalando Unity $version (puede tardar bastante)..."
$install = Start-Process -FilePath $hub -ArgumentList $hubArgs -Wait -PassThru -NoNewWindow
if ($install.ExitCode -ne 0) { throw "Unity Hub terminó con código $($install.ExitCode). Revisa la instalación en Unity Hub y vuelve a intentarlo." }
Write-Host ""
Write-Host "Listo. Inicia sesión en Unity Hub y activa una licencia Personal (gratuita) si aún no lo hiciste."
Write-Host "Luego: Unity Hub > Add > Add project from disk > carpeta de este proyecto."
