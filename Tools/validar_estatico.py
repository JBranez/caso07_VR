#!/usr/bin/env python3
"""Checks static project metadata and basic C# delimiter/string integrity.
Not a substitute for Unity's C# compiler or a player build.
"""
from __future__ import annotations
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
errors: list[str] = []
checks: list[str] = []

# JSON assets
for path in sorted((ROOT / "Packages").glob("*.json")):
    try:
        json.loads(path.read_text(encoding="utf-8"))
        checks.append(f"JSON OK: {path.relative_to(ROOT)}")
    except Exception as exc:
        errors.append(f"JSON inválido {path.relative_to(ROOT)}: {exc}")

# Unity editor target and obsolete VR module
version_file = ROOT / "ProjectSettings/ProjectVersion.txt"
version_text = version_file.read_text(encoding="utf-8")
if "m_EditorVersion: 6000.5.2f1" not in version_text:
    errors.append("ProjectVersion.txt no apunta a 6000.5.2f1")
else:
    checks.append("Unity target OK: 6000.5.2f1")
manifest = json.loads((ROOT / "Packages/manifest.json").read_text(encoding="utf-8"))
if "com.unity.modules.vr" in manifest.get("dependencies", {}):
    errors.append("Sigue declarado el módulo legado com.unity.modules.vr")
else:
    checks.append("Módulo VR legado ausente")
if "com.unity.modules.xr" not in manifest.get("dependencies", {}):
    errors.append("Falta com.unity.modules.xr")
else:
    checks.append("Módulo XR declarado")

# Scenario transfer facts and new features
scenario = (ROOT / "Assets/Scripts/Core/ScenarioData.cs").read_text(encoding="utf-8")
story = (ROOT / "Assets/Scripts/Core/StoryDirector.cs").read_text(encoding="utf-8")
ui = (ROOT / "Assets/Scripts/UI/UIManager.cs").read_text(encoding="utf-8")
audio = (ROOT / "Assets/Scripts/Audio/AudioGuide.cs").read_text(encoding="utf-8")
audio_manager = (ROOT / "Assets/Scripts/Audio/AudioManager.cs").read_text(encoding="utf-8")
synth = (ROOT / "Assets/Scripts/Audio/Synth.cs").read_text(encoding="utf-8")
gaze = (ROOT / "Assets/Scripts/Player/GazePointer.cs").read_text(encoding="utf-8")
mats = (ROOT / "Assets/Scripts/World/Mats.cs").read_text(encoding="utf-8")
build_scripts = (ROOT / "Assets/Editor/BuildScripts.cs").read_text(encoding="utf-8")

# Regressions reported by Unity 6000.5.2f1 after the package preflight.
if re.search(r"\bpoint\b", gaze):
    errors.append("GazePointer.cs todavía contiene una referencia a point sin uso/declaración")
else:
    checks.append("GazePointer.cs: referencias point inexistentes eliminadas")
all_project_cs = "\n".join(path.read_text(encoding="utf-8-sig") for path in (ROOT / "Assets").rglob("*.cs"))
if re.search(r"\bGetInstanceID\s*\(", all_project_cs):
    errors.append("Queda una llamada GetInstanceID() obsoleta en Assets")
else:
    checks.append("API Unity 6.5: ninguna llamada GetInstanceID() en Assets")
if not all(token in mats for token in ("EntityId textureId", "texture.GetEntityId()", "Dictionary<MaterialKey, Material>", "IEquatable<MaterialKey>")):
    errors.append("Mats.cs no usa EntityId tipado en la clave de caché de materiales")
else:
    checks.append("Mats.cs: clave de caché migrada a EntityId tipado")
from decimal import Decimal
transfer_values = [Decimal("15215977.26"), Decimal("19951502.13"), Decimal("9769124.15"), Decimal("7236154.62"), Decimal("5985883.47")]
if sum(transfer_values) != Decimal("58158641.63"):
    errors.append("La aritmética de las cinco transferencias de referencia no suma US$58.158.641,63")
else:
    code_cents = re.search(r"long\[\] amountCents = \{([^}]+)\}", story)
    code_labels = re.search(r"string\[\] amountLabels = \{([^}]+)\}", story)
    if not code_cents or not code_labels:
        errors.append("No se encontró la lista exacta de centavos/etiquetas en StoryDirector")
    else:
        cents_values = [int(v) for v in re.findall(r"\d+", code_cents.group(1).replace("_", ""))]
        labels = re.findall(r'"([0-9.]+,[0-9]{2})"', code_labels.group(1))
        actual = sum((Decimal(v) / Decimal(100) for v in cents_values), Decimal(0))
        if len(cents_values) != 5 or len(labels) != 5 or actual != Decimal("58158641.63"):
            errors.append(f"Los arrays de StoryDirector no coinciden con el total documental: {actual}")
        else:
            checks.append("Arrays exactos de StoryDirector y aritmética OK: US$58.158.641,63")
for label, ok in (
    ("evidencia SWIFT_OFFLINE", "SWIFT_OFFLINE" in scenario and "SWIFT_OFFLINE" in story),
    ("pestaña terminal SWIFT", "SWIFT / órdenes" in ui),
    ("guía de voz TTS", "TextToSpeech" in audio and "SAPI.SpVoice" in audio),
    ("ambientes por zona", all(x in synth for x in ("ambience_lobby", "ambience_teller", "ambience_vault", "ambience_security", "ambience_office")) and "SetEnvironment" in audio_manager),
    ("HUD safe area", "Screen.safeArea" in ui),
    ("paneles adaptados a cámara", "fitCamera.fieldOfView" in (ROOT / "Assets/Scripts/UI/WorldPanel.cs").read_text(encoding="utf-8")),
    ("raycast VR sin asignación por frame", "Physics.RaycastNonAlloc" in gaze and "HitBuffer" in gaze and "Physics.RaycastAll(" not in gaze),
    ("precarga de ambientes", all(f'a.Clip("ambience_{z}")' in audio_manager for z in ("lobby", "teller", "vault", "security", "office"))),
    ("build protegido tras reinicio de Input Handling", "LastRunRequiresRestart" in build_scripts and "Active Input Handling" in build_scripts),
    ("entrega física separada de la retención", "boxHeld = gm.mode == SimMode.Defensa" in story and "bool cashSecured = boxDelivered && boxHeld" in story),
):
    if ok: checks.append(f"Funcionalidad presente: {label}")
    else: errors.append(f"Falta funcionalidad esperada: {label}")

# Unity 6.5 package graph. uGUI is an Editor-shipped Core package, not a registry tarball.
package_versions = {
    "com.unity.inputsystem": ("1.20.1", "com.unity.inputsystem-1.20.1.tgz"),
    "com.unity.xr.management": ("4.6.1", "com.unity.xr.management-4.6.1.tgz"),
    "com.unity.xr.legacyinputhelpers": ("3.0.1", "com.unity.xr.legacyinputhelpers-3.0.1.tgz"),
    "com.unity.xr.core-utils": ("2.6.0", "com.unity.xr.core-utils-2.6.0.tgz"),
}
for package, (version, filename) in package_versions.items():
    actual = manifest.get("dependencies", {}).get(package)
    expected_registry = version
    expected_local = f"file:../LocalPackages/{filename}"
    if actual not in (expected_registry, expected_local):
        errors.append(f"{package} debe ser {expected_registry} o su tarball local; encontrado: {actual}")
    else:
        checks.append(f"Dependencia base OK: {package} ({actual})")

if manifest.get("dependencies", {}).get("com.unity.ugui") != "2.5.0":
    errors.append("com.unity.ugui debe declararse en 2.5.0, versión Core distribuida con Unity 6000.5")
else:
    checks.append("uGUI Core 2.5.0 declarado para activar UnityEngine.UI")
for module in ("com.unity.modules.uielements", "com.unity.modules.xr", "com.unity.modules.subsystems", "com.unity.modules.ui", "com.unity.modules.audio", "com.unity.modules.imgui", "com.unity.modules.physics", "com.unity.modules.physics2d"):
    if manifest.get("dependencies", {}).get(module) != "1.0.0":
        errors.append(f"Falta módulo integrado requerido: {module}")
    else:
        checks.append(f"Módulo integrado OK: {module}")
if manifest.get("enableLockFile") is not True:
    errors.append("enableLockFile debe permanecer activado")
else:
    checks.append("Lockfile habilitado")

cardboard_manifest_path = ROOT / "Packages/com.google.xr.cardboard/package.json"
if not cardboard_manifest_path.exists():
    errors.append("Falta el manifiesto del paquete Cardboard embebido")
else:
    cardboard_manifest = json.loads(cardboard_manifest_path.read_text(encoding="utf-8-sig"))
    for package, expected in (("com.unity.inputsystem", "1.20.1"),
                              ("com.unity.xr.management", "4.6.1"),
                              ("com.unity.xr.legacyinputhelpers", "3.0.1")):
        actual = cardboard_manifest.get("dependencies", {}).get(package)
        if actual != expected:
            errors.append(f"Cardboard depende de {package} {actual}; se esperaba {expected}")
    if not any("Cardboard depende" in e for e in errors):
        checks.append("Dependencias del Cardboard embebido alineadas")

# The runtime asmdef must reference the Unity UI assembly and the project XR/input assemblies.
asmdef_path = ROOT / "Assets/Scripts/LasPalmeras.Runtime.asmdef"
try:
    asmdef = json.loads(asmdef_path.read_text(encoding="utf-8"))
    asmrefs = asmdef.get("references", [])
    for ref in ("UnityEngine.UI", "Unity.InputSystem", "Unity.XR.Management", "Google.XR.Cardboard"):
        if ref not in asmrefs:
            errors.append(f"LasPalmeras.Runtime.asmdef no referencia {ref}")
    if not any("LasPalmeras.Runtime.asmdef" in e for e in errors):
        checks.append("Assembly runtime referencia uGUI, Input System, XR Management y Cardboard")
except Exception as exc:
    errors.append(f"LasPalmeras.Runtime.asmdef inválido: {exc}")

for rel in ("Assets/Scripts/Interaction/UiButton3D.cs", "Assets/Scripts/UI/UIManager.cs", "Assets/Scripts/UI/WorldPanel.cs"):
    text = (ROOT / rel).read_text(encoding="utf-8-sig")
    if "using UnityEngine.UI;" not in text:
        errors.append(f"Falta using UnityEngine.UI en {rel}")
    else:
        checks.append(f"Import uGUI correcto: {rel}")

# Download-first bootstrap checks: tarball URLs, checksums, batch resolve, validation marker before GUI launch.
preflight_path = ROOT / "Tools/Prepare_And_Open_Unity65.ps1"
launcher_path = ROOT / "Tools/Preparar_y_Abrir_Unity65.bat"
repair_path = ROOT / "Tools/Repair_PackageManager_Unity65.ps1"
if not preflight_path.exists() or not launcher_path.exists() or not repair_path.exists():
    errors.append("Falta el preflight/launcher/reparador Package Manager")
else:
    preflight = preflight_path.read_text(encoding="utf-8-sig")
    launcher = launcher_path.read_text(encoding="utf-8-sig")
    repair = repair_path.read_text(encoding="utf-8-sig")
    for marker in (
        "download.packages.unity.com",
        "com.unity.inputsystem-1.20.1.tgz",
        "com.unity.xr.management-4.6.1.tgz",
        "com.unity.xr.legacyinputhelpers-3.0.1.tgz",
        "com.unity.xr.core-utils-2.6.0.tgz",
        "2436e68c60c469edbccef8f6b3c319b9441f4675",
        "df145a5bc89e37453667848cc4c11a49a7246aa3",
        "3f62d634f63be463f9e1f7b0deea814a27526da4",
        "a8b900321199038fe4200211a52f46d2f4bae786",
        "Get-FileHash",
        "-batchmode",
        "CompileValidation.Run",
        "Packages\\packages-lock.json",
        "LAS_PALMERAS_COMPILE_VALIDATION_PASSED",
    ):
        if marker not in preflight:
            errors.append(f"Preflight no contiene paso requerido: {marker}")
    if "Prepare_And_Open_Unity65.ps1" not in launcher:
        errors.append("El BAT no ejecuta el preflight download-first")
    if "Prepare_And_Open_Unity65.ps1" not in repair:
        errors.append("El reparador no delega al nuevo preflight")
    if "com.unity.ugui' '2.5.0" not in preflight:
        errors.append("El preflight no conserva uGUI como Core package 2.5.0")
    if "Se conservan Library/PackageCache, ProjectCache y LocalPackages" not in preflight:
        errors.append("El preflight no documenta conservación de las cachés de paquetes")
    if "$lockPath.before-local-tarballs-$backupStamp.bak" not in preflight or "Copy-Item -LiteralPath $lockPath -Destination $lockBackup" not in preflight or "Remove-Item -LiteralPath $lockPath -Force" not in preflight:
        errors.append("El preflight no respalda y regenera el lockfile al cambiar a tarballs locales")
    # Assert the interactive Editor launch is strictly after the batch validation gate.
    batch_at = preflight.find("Start-Process -FilePath $UnityPath -ArgumentList $argumentLine")
    marker_at = preflight.find("LAS_PALMERAS_COMPILE_VALIDATION_PASSED")
    gui_at = preflight.find("Abriendo ahora el Editor")
    if batch_at < 0 or marker_at < 0 or gui_at < marker_at:
        errors.append("El preflight no garantiza compilación batch antes de abrir el Editor gráfico")
    else:
        checks.append("Launcher descarga tarballs + verifica SHA-1 + compila en batch antes del Editor gráfico")
    if "com.unity.ugui" not in preflight or "UnityEngine.UI.Image" not in (ROOT / "Assets/Editor/CompileValidation.cs").read_text(encoding="utf-8"):
        errors.append("El gate de compilación no prueba el assembly uGUI real")
    if "Remove-Item -LiteralPath $cache" in preflight:
        errors.append("El preflight elimina una caché de paquetes")

compile_validation = ROOT / "Assets/Editor/CompileValidation.cs"
compile_runner = ROOT / "Tools/Verify_Compile_Unity65.ps1"
if not compile_validation.exists() or not compile_runner.exists():
    errors.append("Falta la validación ejecutable de compilación con Unity")
else:
    validation_text = compile_validation.read_text(encoding="utf-8-sig")
    runner_text = compile_runner.read_text(encoding="utf-8-sig")
    for marker in (
        "LAS_PALMERAS_COMPILE_VALIDATION_PASSED",
        "UnityEngine.UI.Image",
        "UnityEngine.UI.Text",
        "InputActionAsset",
        "Google.XR.Cardboard",
    ):
        if marker not in validation_text:
            errors.append(f"CompileValidation.cs no comprueba {marker}")
    if "Prepare_And_Open_Unity65.ps1" not in runner_text or "-NoLaunch" not in runner_text:
        errors.append("Verify_Compile_Unity65.ps1 no delega al preflight completo")
    else:
        checks.append("Validador batchmode usa el preflight que compila UI Image/Text + InputActionAsset + XR/Cardboard")
    windows_build = (ROOT / "Tools/Compilar.ps1").read_text(encoding="utf-8-sig")
    if "Prepare_And_Open_Unity65.ps1" not in windows_build or "BuildWindows" not in windows_build or "BuildAndroid" not in windows_build:
        errors.append("Tools/Compilar.ps1 no ejecuta preflight antes de los builds Windows/Android")
    else:
        checks.append("Build runner PowerShell descarga/resuelve paquetes y valida assemblies antes del build")
    shell_build = (ROOT / "Tools/compilar.sh").read_text(encoding="utf-8-sig")
    if "CompileValidation.Run" not in shell_build or "LAS_PALMERAS_COMPILE_VALIDATION_PASSED" not in shell_build:
        errors.append("Tools/compilar.sh no valida assemblies antes del build")
    else:
        checks.append("Build runner Bash valida assemblies antes del build")


local_cache_readme = ROOT / "LocalPackages/README.txt"
if not local_cache_readme.exists() or "download.packages.unity.com" not in local_cache_readme.read_text(encoding="utf-8"):
    errors.append("Falta la instrucción LocalPackages/README.txt para la caché de tarballs")
else:
    checks.append("Directorio de tarballs local documentado")

# The main scene is a committed Unity asset; editor startup must never create it additively.
scene_path = ROOT / "Assets/Scenes/Agencia_VR.unity"
scene_meta_path = ROOT / "Assets/Scenes/Agencia_VR.unity.meta"
scene_folder_meta = ROOT / "Assets/Scenes.meta"
entry_script = ROOT / "Assets/Scripts/Core/GameBootstrap.cs"
entry_meta = ROOT / "Assets/Scripts/Core/GameBootstrap.cs.meta"
build_settings_path = ROOT / "ProjectSettings/EditorBuildSettings.asset"
project_bootstrap_text = (ROOT / "Assets/Editor/ProjectBootstrap.cs").read_text(encoding="utf-8-sig")
compile_validation_text = (ROOT / "Assets/Editor/CompileValidation.cs").read_text(encoding="utf-8-sig")
if not all(p.exists() for p in (scene_path, scene_meta_path, scene_folder_meta, entry_script, entry_meta, build_settings_path)):
    errors.append("Falta la escena precreada, alguno de sus .meta, GameBootstrap o EditorBuildSettings.asset")
else:
    scene_text = scene_path.read_text(encoding="utf-8-sig")
    scene_meta_text = scene_meta_path.read_text(encoding="utf-8-sig")
    entry_meta_text = entry_meta.read_text(encoding="utf-8-sig")
    build_text = build_settings_path.read_text(encoding="utf-8-sig")
    scene_guid_match = re.search(r"guid:\s*([0-9a-f]{32})", scene_meta_text)
    entry_guid_match = re.search(r"guid:\s*([0-9a-f]{32})", entry_meta_text)
    if not scene_guid_match or scene_guid_match.group(1) not in scene_text or scene_guid_match.group(1) not in build_text:
        errors.append("GUID de Agencia_VR no coincide entre scene, .meta y EditorBuildSettings")
    elif not entry_guid_match or entry_guid_match.group(1) not in scene_text:
        errors.append("La escena no referencia el MonoScript GameBootstrap mediante su GUID")
    elif "ANDES-SIM Scene Entry" not in scene_text or "LasPalmeras.Runtime::LasPalmeras.GameBootstrap" not in scene_text:
        errors.append("La escena precreada no contiene el GameBootstrap esperado")
    elif "Assets/Scenes/Agencia_VR.unity" not in build_text:
        errors.append("Agencia_VR no está declarada en EditorBuildSettings")
    else:
        checks.append("Escena precreada válida: GUID, componente GameBootstrap y Build Settings coinciden")
    if "EditorSceneManager.NewScene(" in project_bootstrap_text or "NewSceneMode.Additive" in project_bootstrap_text:
        errors.append("ProjectBootstrap todavía intenta crear escenas durante el arranque")
    elif "ValidateExistingScene" in project_bootstrap_text and "OpenSceneMode.Single" in project_bootstrap_text:
        checks.append("ProjectBootstrap valida/abre la escena guardada; no crea escenas aditivas")
    else:
        errors.append("ProjectBootstrap no contiene el flujo de escena preexistente esperado")
    if "GetReferencedAssemblies()" in compile_validation_text:
        errors.append("CompileValidation todavía exige metadata AssemblyRef y puede rechazar referencias XR condicionales")
    elif "ValidateAsmdefReferences" in compile_validation_text and "ValidateScene()" in compile_validation_text:
        checks.append("CompileValidation valida el asmdef fuente, el componente de escena y Build Settings")
    else:
        errors.append("CompileValidation no valida asmdef y escena con el flujo actualizado")

# Project scripts version pins
for rel in ("Tools/Instalar_Unity.ps1", "Tools/instalar_unity.sh", "Tools/Compilar.ps1", "Tools/compilar.sh"):
    p = ROOT / rel
    if not p.exists() or "6000.5.2f1" not in p.read_text(encoding="utf-8"):
        errors.append(f"Versión de Unity ausente o incorrecta en {rel}")
    else:
        checks.append(f"Versión del script OK: {rel}")

# Minimal lexer to validate balanced delimiters outside comments/string/char literals.
def strip_csharp(src: str) -> str:
    out = []
    i, n = 0, len(src)
    state = "code"
    verbatim = False
    while i < n:
        c = src[i]
        nxt = src[i + 1] if i + 1 < n else ""
        if state == "code":
            if c == '/' and nxt == '/':
                state = "line"; out.extend("  "); i += 2; continue
            if c == '/' and nxt == '*':
                state = "block"; out.extend("  "); i += 2; continue
            # C# normal/verbatim/interpolated strings; interpolation bodies are intentionally
            # treated as strings in this lightweight delimiter check.
            if c == '@' and nxt == '"':
                state = "string"; verbatim = True; out.extend("  "); i += 2; continue
            if c == '"':
                state = "string"; verbatim = False; out.append(' '); i += 1; continue
            if c == "'":
                state = "char"; out.append(' '); i += 1; continue
            out.append(c); i += 1
        elif state == "line":
            if c == '\n': state = "code"; out.append('\n')
            else: out.append(' ')
            i += 1
        elif state == "block":
            if c == '*' and nxt == '/':
                out.extend("  "); i += 2; state = "code"
            else:
                out.append('\n' if c == '\n' else ' '); i += 1
        elif state == "string":
            if verbatim and c == '"' and nxt == '"':
                out.extend("  "); i += 2
            elif c == '\\' and not verbatim:
                out.append(' '); i += 1
                if i < n: out.append('\n' if src[i] == '\n' else ' '); i += 1
            elif c == '"':
                out.append(' '); i += 1; state = "code"
            else:
                out.append('\n' if c == '\n' else ' '); i += 1
        elif state == "char":
            if c == '\\':
                out.append(' '); i += 1
                if i < n: out.append(' '); i += 1
            elif c == "'":
                out.append(' '); i += 1; state = "code"
            else:
                out.append('\n' if c == '\n' else ' '); i += 1
    if state in ("block", "string", "char"):
        raise ValueError(f"literal/comentario sin cerrar ({state})")
    return ''.join(out)

pairs = {')': '(', ']': '[', '}': '{'}
for path in sorted((ROOT / "Assets").rglob("*.cs")):
    try:
        code = strip_csharp(path.read_text(encoding="utf-8-sig"))
        stack: list[tuple[str, int]] = []
        line = 1
        for ch in code:
            if ch == '\n': line += 1
            elif ch in "([{": stack.append((ch, line))
            elif ch in ")]}":
                if not stack or stack[-1][0] != pairs[ch]:
                    raise ValueError(f"delimitador {ch} sin pareja en línea {line}")
                stack.pop()
        if stack:
            raise ValueError(f"delimitador {stack[-1][0]} abierto en línea {stack[-1][1]}")
        checks.append(f"C# delimiters OK: {path.relative_to(ROOT)}")
    except Exception as exc:
        errors.append(f"C# sintaxis estructural {path.relative_to(ROOT)}: {exc}")

cs_count = len(list((ROOT / "Assets").rglob("*.cs")))
checks.append(f"Archivos C# bajo Assets: {cs_count}")

print("\n".join(checks))
if errors:
    print("\nERRORS:")
    print("\n".join(errors))
    sys.exit(1)
print("\nResultado: comprobaciones estáticas básicas superadas.")
print("Nota: esto NO compila C# ni sustituye la apertura/build en Unity.")
