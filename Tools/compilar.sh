#!/usr/bin/env bash
# Compila por consola. Uso: ./compilar.sh android|linux|mac
set -euo pipefail
DEST="${1:-linux}"; V="6000.5.2f1"
PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
case "$(uname -s)" in
  Darwin) UNITY="/Applications/Unity/Hub/Editor/$V/Unity.app/Contents/MacOS/Unity" ;;
  *)      UNITY="$HOME/Unity/Hub/Editor/$V/Editor/Unity" ;;
esac
[ -x "$UNITY" ] || { echo "No se encontró Unity en $UNITY. Ejecuta antes instalar_unity.sh"; exit 1; }
mkdir -p "$PROJECT/Builds"
"$UNITY" -batchmode -quit -projectPath "$PROJECT" -executeMethod LasPalmeras.EditorTools.ProjectBootstrap.RunAllMenu -logFile "$PROJECT/Builds/setup.log"
if [ $? -ne 0 ]; then echo "Falló la configuración. Revisa Builds/setup.log"; exit 1; fi
"$UNITY" -batchmode -quit -projectPath "$PROJECT" -executeMethod LasPalmeras.EditorTools.CompileValidation.Run -logFile "$PROJECT/Builds/compile-validation.log" -nographics
if [ $? -ne 0 ] || ! grep -q "LAS_PALMERAS_COMPILE_VALIDATION_PASSED" "$PROJECT/Builds/compile-validation.log"; then
  echo "La validación de compilación falló. Revisa Builds/compile-validation.log"; exit 1
fi
case "$DEST" in
  android) T=Android;      M=BuildAndroid ;;
  mac)     T=OSXUniversal; M=BuildMac ;;
  *)       T=Linux64;      M=BuildLinux ;;
esac
"$UNITY" -batchmode -quit -projectPath "$PROJECT" -buildTarget "$T" -executeMethod "LasPalmeras.EditorTools.BuildScripts.$M" -logFile "$PROJECT/Builds/build_$DEST.log"
echo "Compilación terminada. Salida en $PROJECT/Builds"
