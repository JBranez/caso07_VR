@echo off
setlocal
cd /d "%~dp0.."
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Prepare_And_Open_Unity65.ps1" %*
if errorlevel 1 (
  echo.
  echo La preparacion no termino correctamente. Unity no se abrio.
  echo Revisa estos logs si existen:
  echo   Builds\Validation\package-download-preflight.log
  echo   Builds\Validation\unity65-preflight-compile.log
  pause
  exit /b 1
)
endlocal
