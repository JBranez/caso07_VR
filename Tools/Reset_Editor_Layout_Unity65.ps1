[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$unity = Get-Process -Name Unity -ErrorAction SilentlyContinue
if ($unity) {
    throw 'Cierra Unity Editor antes de restablecer el layout. No se modificó ningún archivo.'
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupRoot = Join-Path $ProjectPath "Builds\LayoutBackup-$stamp"
$targets = @(
    (Join-Path $ProjectPath 'Library\CurrentLayout-default.dwlt'),
    (Join-Path $ProjectPath 'UserSettings\Layouts\CurrentMaximizeLayout.dwlt'),
    (Join-Path $ProjectPath 'UserSettings\Layouts\CurrentLayout-default.dwlt')
)
$found = 0
foreach ($source in $targets) {
    if (Test-Path -LiteralPath $source -PathType Leaf) {
        $relative = $source.Substring($ProjectPath.Length).TrimStart([char[]]@('\','/'))
        $destination = Join-Path $backupRoot $relative
        $destinationFolder = Split-Path -Parent $destination
        New-Item -ItemType Directory -Force -Path $destinationFolder | Out-Null
        Move-Item -LiteralPath $source -Destination $destination
        Write-Host "Respaldado: $relative" -ForegroundColor Yellow
        $found++
    }
}
if ($found -eq 0) {
    Write-Host 'No se encontraron archivos locales de layout para mover.' -ForegroundColor Yellow
} else {
    Write-Host "Se respaldaron $found archivo(s) en: $backupRoot" -ForegroundColor Green
}
Write-Host 'Abre el proyecto de nuevo. Unity generará un layout de ventana nuevo.' -ForegroundColor Cyan
