[CmdletBinding()]
param(
    [string]$UnityPath = '',
    [switch]$NoLaunch,
    [switch]$ForceDownload,
    [int]$TimeoutMinutes = 25
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$ManifestPath = Join-Path $ProjectPath 'Packages\manifest.json'
$CardboardManifestPath = Join-Path $ProjectPath 'Packages\com.google.xr.cardboard\package.json'
$VersionPath = Join-Path $ProjectPath 'ProjectSettings\ProjectVersion.txt'
$LocalPackagesPath = Join-Path $ProjectPath 'LocalPackages'
$LogDirectory = Join-Path $ProjectPath 'Builds\Validation'
$DownloadLog = Join-Path $LogDirectory 'package-download-preflight.log'
$CompileLog = Join-Path $LogDirectory 'unity65-preflight-compile.log'
New-Item -ItemType Directory -Force -Path $LocalPackagesPath, $LogDirectory | Out-Null

function Write-Log([string]$Message, [string]$Color = 'Gray') {
    $line = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] $Message"
    Add-Content -LiteralPath $DownloadLog -Value $line -Encoding UTF8
    Write-Host $Message -ForegroundColor $Color
}
function Stop-With([string]$Message) {
    Write-Log "ERROR: $Message" 'Red'
    throw $Message
}
function Get-JsonFile([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { Stop-With "No existe $Path" }
    try { return (Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json) }
    catch { Stop-With "JSON inválido en $Path : $($_.Exception.Message)" }
}
function Set-JsonDependency($Object, [string]$Name, [string]$Value) {
    $existing = $Object.PSObject.Properties[$Name]
    if ($null -ne $existing) { $existing.Value = $Value }
    else { $Object | Add-Member -MemberType NoteProperty -Name $Name -Value $Value }
}

# Start a fresh diagnostic log but preserve packages, Library and the lock file.
Set-Content -LiteralPath $DownloadLog -Value "Las Palmeras VR / Unity 6000.5.2f1 package preflight" -Encoding UTF8
Write-Log 'Inicio. Este proceso descarga dependencias, resuelve paquetes con Unity batchmode y solo entonces abre el Editor.' 'Cyan'

# 1) Validate the Editor version and the project manifests before network access.
if (-not (Test-Path -LiteralPath $VersionPath)) { Stop-With 'No existe ProjectSettings/ProjectVersion.txt.' }
$versionText = Get-Content -LiteralPath $VersionPath -Raw
if ($versionText -notmatch 'm_EditorVersion:\s*6000\.5\.2f1') {
    Stop-With 'El proyecto requiere exactamente Unity 6000.5.2f1.'
}
$manifest = Get-JsonFile $ManifestPath
$cardboard = Get-JsonFile $CardboardManifestPath
if ($null -eq $manifest.dependencies) { Stop-With 'El manifiesto no tiene el objeto dependencies.' }

# 2) Find the requested Editor. UNITY_EDITOR_PATH can override Hub's default path.
if ([string]::IsNullOrWhiteSpace($UnityPath)) {
    if ($env:UNITY_EDITOR_PATH -and (Test-Path -LiteralPath $env:UNITY_EDITOR_PATH)) {
        $UnityPath = $env:UNITY_EDITOR_PATH
    } else {
        $roots = @()
        if ($env:ProgramFiles) { $roots += (Join-Path $env:ProgramFiles 'Unity\Hub\Editor') }
        $roots += 'C:\Program Files\Unity\Hub\Editor'
        $roots += 'C:\Program Files (x86)\Unity\Hub\Editor'
        foreach ($rootPath in ($roots | Select-Object -Unique)) {
            $candidate = Join-Path $rootPath '6000.5.2f1\Editor\Unity.exe'
            if (Test-Path -LiteralPath $candidate) { $UnityPath = $candidate; break }
        }
    }
}
if (-not $UnityPath -or -not (Test-Path -LiteralPath $UnityPath)) {
    Stop-With 'No se encontró Unity.exe 6000.5.2f1. Instala esa versión desde Unity Hub o usa -UnityPath / UNITY_EDITOR_PATH.'
}
$UnityPath = (Resolve-Path -LiteralPath $UnityPath).Path
if ($UnityPath -notmatch '6000\.5\.2f1') {
    Write-Log 'ADVERTENCIA: la ruta no contiene 6000.5.2f1; comprueba la versión real del Editor.' 'Yellow'
}

# Do not let two Unity processes concurrently modify the same project/package cache.
$runningUnity = Get-Process -Name Unity -ErrorAction SilentlyContinue
if ($runningUnity) {
    Stop-With 'Cierra todas las ventanas/procesos Unity antes de ejecutar este preflight. No se modificó Library ni packages-lock.'
}

# 3) Download official UPM tarballs outside the broken Package Manager catalogue UI.
# Published SHA-1 values are used as transport-integrity checks; the files are retained in LocalPackages.
$packageSet = @(
    [pscustomobject]@{ Name='com.unity.inputsystem'; Version='1.20.1'; File='com.unity.inputsystem-1.20.1.tgz'; Url='https://download.packages.unity.com/com.unity.inputsystem/-/com.unity.inputsystem-1.20.1.tgz'; Sha1='2436e68c60c469edbccef8f6b3c319b9441f4675' },
    [pscustomobject]@{ Name='com.unity.xr.management'; Version='4.6.1'; File='com.unity.xr.management-4.6.1.tgz'; Url='https://download.packages.unity.com/com.unity.xr.management/-/com.unity.xr.management-4.6.1.tgz'; Sha1='df145a5bc89e37453667848cc4c11a49a7246aa3' },
    [pscustomobject]@{ Name='com.unity.xr.legacyinputhelpers'; Version='3.0.1'; File='com.unity.xr.legacyinputhelpers-3.0.1.tgz'; Url='https://download.packages.unity.com/com.unity.xr.legacyinputhelpers/-/com.unity.xr.legacyinputhelpers-3.0.1.tgz'; Sha1='3f62d634f63be463f9e1f7b0deea814a27526da4' },
    [pscustomobject]@{ Name='com.unity.xr.core-utils'; Version='2.6.0'; File='com.unity.xr.core-utils-2.6.0.tgz'; Url='https://download.packages.unity.com/com.unity.xr.core-utils/-/com.unity.xr.core-utils-2.6.0.tgz'; Sha1='a8b900321199038fe4200211a52f46d2f4bae786' }
)
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
foreach ($package in $packageSet) {
    $destination = Join-Path $LocalPackagesPath $package.File
    $isValid = $false
    if ((Test-Path -LiteralPath $destination) -and -not $ForceDownload) {
        $hash = (Get-FileHash -LiteralPath $destination -Algorithm SHA1).Hash.ToLowerInvariant()
        $isValid = ($hash -eq $package.Sha1)
        if ($isValid) { Write-Log "CACHE OK: $($package.Name) $($package.Version)" 'Green' }
        else { Write-Log "Checksum local inválido para $($package.File); se descargará otra copia." 'Yellow' }
    }
    if (-not $isValid) {
        $temporary = "$destination.download"
        if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Force -ErrorAction SilentlyContinue }
        $downloaded = $false
        for ($attempt = 1; $attempt -le 3 -and -not $downloaded; $attempt++) {
            try {
                Write-Log "Descargando $($package.Name) $($package.Version) (intento $attempt/3) desde download.packages.unity.com ..." 'Cyan'
                Invoke-WebRequest -Uri $package.Url -OutFile $temporary -TimeoutSec 240 -UseBasicParsing
                if (-not (Test-Path -LiteralPath $temporary)) { throw 'La respuesta no creó el archivo.' }
                $downloadHash = (Get-FileHash -LiteralPath $temporary -Algorithm SHA1).Hash.ToLowerInvariant()
                if ($downloadHash -ne $package.Sha1) {
                    throw "SHA-1 incorrecto. Esperado $($package.Sha1); recibido $downloadHash."
                }
                Move-Item -LiteralPath $temporary -Destination $destination -Force
                $downloaded = $true
                Write-Log "DESCARGADO + CHECKSUM OK: $($package.Name) $($package.Version)" 'Green'
            }
            catch {
                if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Force -ErrorAction SilentlyContinue }
                if ($attempt -lt 3) { Write-Log "Fallo de descarga: $($_.Exception.Message). Se reintentará." 'Yellow'; Start-Sleep -Seconds (3 * $attempt) }
                else { Stop-With "No se pudo descargar/verificar $($package.Name). Se necesita Internet HTTPS/443 hacia download.packages.unity.com. Detalle: $($_.Exception.Message)" }
            }
        }
    }
}

# 4) Build a deterministic manifest: local tarballs for external packages; Editor-core uGUI stays at 2.5.0.
# Unity docs classify uGUI as a Core package distributed with the Editor, so it must not be downloaded as a registry tarball.
$backupStamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$manifestBackup = "$ManifestPath.before-local-packages-$backupStamp.bak"
Copy-Item -LiteralPath $ManifestPath -Destination $manifestBackup
foreach ($package in $packageSet) {
    Set-JsonDependency $manifest.dependencies $package.Name ("file:../LocalPackages/$($package.File)")
}
Set-JsonDependency $manifest.dependencies 'com.unity.ugui' '2.5.0'
Set-JsonDependency $manifest.dependencies 'com.unity.modules.subsystems' '1.0.0'
Set-JsonDependency $manifest.dependencies 'com.unity.modules.uielements' '1.0.0'
Set-JsonDependency $manifest.dependencies 'com.unity.modules.xr' '1.0.0'
$manifest.PSObject.Properties.Remove('pinnedPackages')
$manifest.enableLockFile = $true
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($ManifestPath, (($manifest | ConvertTo-Json -Depth 100) + "`n"), $utf8NoBom)
$manifest = Get-JsonFile $ManifestPath
foreach ($package in $packageSet) {
    $actual = $manifest.dependencies.PSObject.Properties[$package.Name].Value
    $expected = "file:../LocalPackages/$($package.File)"
    if ($actual -ne $expected) { Stop-With "No se guardó correctamente la referencia local de $($package.Name)." }
}
if ($manifest.dependencies.'com.unity.ugui' -ne '2.5.0') { Stop-With 'uGUI debe quedar en 2.5.0, la versión Core de este Editor.' }
Write-Log "Manifiesto actualizado. Respaldo: $manifestBackup" 'Green'
Write-Log 'Se conservan Library/PackageCache, ProjectCache y LocalPackages.' 'Green'

# The manifest changes dependency source from registry to local tarball. Back up the old
# resolver snapshot and let UPM generate a fresh lockfile for the new source graph.
$lockPath = Join-Path $ProjectPath 'Packages\packages-lock.json'
if (Test-Path -LiteralPath $lockPath) {
    $lockBackup = "$lockPath.before-local-tarballs-$backupStamp.bak"
    Copy-Item -LiteralPath $lockPath -Destination $lockBackup -Force
    Remove-Item -LiteralPath $lockPath -Force
    Write-Log "Lockfile anterior respaldado y apartado para forzar la resolución desde tarballs locales: $lockBackup" 'Yellow'
} else {
    Write-Log 'No existía packages-lock.json; Unity generará uno nuevo.' 'Yellow'
}

# 5) Validate the embedded Cardboard package requirements.
$expectedCardboard = @{
    'com.unity.inputsystem'='1.20.1'
    'com.unity.xr.management'='4.6.1'
    'com.unity.xr.legacyinputhelpers'='3.0.1'
}
foreach ($name in $expectedCardboard.Keys) {
    $actual = $cardboard.dependencies.PSObject.Properties[$name].Value
    if ($actual -ne $expectedCardboard[$name]) { Stop-With "Cardboard declara $name $actual; se esperaba $($expectedCardboard[$name])." }
}

# 6) Let Unity UPM resolve the local tarballs and compile C# in batchmode before opening the GUI.
if (Test-Path -LiteralPath $CompileLog) { Remove-Item -LiteralPath $CompileLog -Force -ErrorAction SilentlyContinue }
Write-Log 'Ejecutando Unity batchmode: resolución UPM, carga de assemblies y verificación de UI Image/Text + XR/Cardboard.' 'Cyan'
$argumentLine = "-batchmode -nographics -accept-apiupdate -projectPath `"$ProjectPath`" -executeMethod LasPalmeras.EditorTools.CompileValidation.Run -logFile `"$CompileLog`" -quit"
$process = Start-Process -FilePath $UnityPath -ArgumentList $argumentLine -WorkingDirectory $ProjectPath -PassThru
$timeoutMs = [Math]::Max(1, $TimeoutMinutes) * 60 * 1000
if (-not $process.WaitForExit($timeoutMs)) {
    try { $process.Kill() } catch {}
    Stop-With "Unity batchmode superó $TimeoutMinutes minutos. Log: $CompileLog"
}
$process.Refresh()
$exitCode = $process.ExitCode
$logText = if (Test-Path -LiteralPath $CompileLog) { Get-Content -LiteralPath $CompileLog -Raw -ErrorAction SilentlyContinue } else { '' }

$lockPath = Join-Path $ProjectPath 'Packages\packages-lock.json'
if (-not (Test-Path -LiteralPath $lockPath)) {
    Stop-With "Unity no generó packages-lock.json; la resolución no terminó. Revisa $CompileLog y el log de descarga $DownloadLog"
}
$lock = Get-JsonFile $lockPath
foreach ($package in $packageSet) {
    $entryProperty = $lock.dependencies.PSObject.Properties[$package.Name]
    $entry = if ($null -eq $entryProperty) { $null } else { $entryProperty.Value }
    if ($null -eq $entry -or [string]$entry.version -ne $package.Version) {
        $resolved = if ($null -eq $entry) { '<ausente>' } else { [string]$entry.version }
        Stop-With "UPM no resolvió $($package.Name) $($package.Version). Versión actual: $resolved. Log: $CompileLog"
    }
}
if ($exitCode -ne 0 -or $logText -notmatch 'LAS_PALMERAS_COMPILE_VALIDATION_PASSED' -or $logText -match 'LAS_PALMERAS_COMPILE_VALIDATION_FAILED|error CS\d+|Failed to resolve packages|No packages loaded') {
    $tail = if ($logText.Length -gt 9000) { $logText.Substring($logText.Length - 9000) } else { $logText }
    Write-Log "Unity batchmode no pasó. Últimas líneas del Editor.log: `n$tail" 'Red'
    Stop-With "No se confirmó compilación C# + referencias de UI/XR. ExitCode=$exitCode. Log: $CompileLog"
}
Write-Log 'UPM: las cuatro dependencias externas quedaron resueltas desde tarballs locales.' 'Green'
Write-Log 'COMPILACIÓN: Unity confirmó UnityEngine.UI.Image, UnityEngine.UI.Text, Input System, XR Management y Cardboard.' 'Green'
Write-Log "Lockfile: $lockPath" 'Green'
Write-Log "Log batch Unity: $CompileLog" 'Green'

if (-not $NoLaunch) {
    Write-Log 'Preflight completo. Abriendo ahora el Editor Unity en modo gráfico.' 'Green'
    Start-Process -FilePath $UnityPath -ArgumentList "-projectPath `"$ProjectPath`"" -WorkingDirectory $ProjectPath
} else {
    Write-Log 'Preflight terminado correctamente; -NoLaunch evita abrir la ventana del Editor.' 'Green'
}
