#!/usr/bin/env bash
# Instala Unity 6000.5.2f1 (6.5) mediante Unity Hub (macOS/Linux). Requiere Unity Hub ya instalado.
# Uso: ./instalar_unity.sh [--sin-android]
set -euo pipefail
VERSION="6000.5.2f1"; CHANGESET="eb73d3b415a1"
HUB=""
for c in "/Applications/Unity Hub.app/Contents/MacOS/Unity Hub" "/opt/unityhub/unityhub" "$(command -v unityhub || true)"; do
  [ -n "$c" ] && [ -x "$c" ] && HUB="$c" && break
done
if [ -z "$HUB" ]; then
  echo "No se encontró Unity Hub. Instálalo desde https://unity.com/download (Linux: https://docs.unity3d.com/hub/manual/InstallHub.html#install-hub-linux) y vuelve a ejecutar."; exit 1
fi
MODS=(-m android --childModules)
[ "${1:-}" = "--sin-android" ] && MODS=()
echo "Instalando Unity $VERSION ..."
"$HUB" -- --headless install --version "$VERSION" --changeset "$CHANGESET" "${MODS[@]}"
echo "Listo. Abre Unity Hub > Add > Add project from disk y elige esta carpeta."
