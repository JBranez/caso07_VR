# Preflight de dependencias — Las Palmeras VR / Unity 6.5

## Cómo abrir el proyecto

No abras este proyecto directamente desde Unity Hub en el primer arranque. Cierra todas las instancias de Unity y ejecuta con doble clic:

`Tools/Preparar_y_Abrir_Unity65.bat`

El BAT invoca `Prepare_And_Open_Unity65.ps1`. El proceso:

1. Verifica que el proyecto pida exactamente Unity `6000.5.2f1` y localiza `Unity.exe`.
2. Comprueba que no haya otro proceso Unity modificando el proyecto.
3. Descarga cuatro paquetes oficiales UPM desde `download.packages.unity.com` cuando no exista una copia local válida.
4. Comprueba el SHA-1 esperado y guarda cada `.tgz` en `LocalPackages/`.
5. Hace una copia de seguridad de `Packages/manifest.json` y cambia las cuatro dependencias externas a referencias `file:../LocalPackages/...`.
6. Mantiene uGUI `2.5.0` como Core package de Unity 6000.5, y deja los módulos integrados `ui`, `uielements`, `xr` y `subsystems`.
7. Conserva `Library/PackageCache`, `Library/PackageManager/ProjectCache` y `LocalPackages`. Como el origen de dependencias cambia de Registry a tarball local, respalda el `packages-lock.json` anterior en un archivo con fecha y retira solamente el lock activo para que Unity genere uno nuevo.
8. Ejecuta Unity en `-batchmode` para resolver UPM y compilar todos los assemblies. `CompileValidation.Run` debe cargar `UnityEngine.UI.Image`, `UnityEngine.UI.Text`, `InputActionAsset`, XR Management, Cardboard y los tres scripts de UI.
9. Solo abre la ventana gráfica de Unity si el batchmode termina con código 0, confirma las cuatro versiones en el nuevo lockfile y escribe `LAS_PALMERAS_COMPILE_VALIDATION_PASSED`.

## Paquetes fijados y URL oficiales

| Paquete | Versión | Archivo local | SHA-1 |
|---|---:|---|---|
| Input System | 1.20.1 | [`com.unity.inputsystem-1.20.1.tgz`](https://download.packages.unity.com/com.unity.inputsystem/-/com.unity.inputsystem-1.20.1.tgz) | `2436e68c60c469edbccef8f6b3c319b9441f4675` |
| XR Plug-in Management | 4.6.1 | [`com.unity.xr.management-4.6.1.tgz`](https://download.packages.unity.com/com.unity.xr.management/-/com.unity.xr.management-4.6.1.tgz) | `df145a5bc89e37453667848cc4c11a49a7246aa3` |
| XR Legacy Input Helpers | 3.0.1 | [`com.unity.xr.legacyinputhelpers-3.0.1.tgz`](https://download.packages.unity.com/com.unity.xr.legacyinputhelpers/-/com.unity.xr.legacyinputhelpers-3.0.1.tgz) | `3f62d634f63be463f9e1f7b0deea814a27526da4` |
| XR Core Utilities | 2.6.0 | [`com.unity.xr.core-utils-2.6.0.tgz`](https://download.packages.unity.com/com.unity.xr.core-utils/-/com.unity.xr.core-utils-2.6.0.tgz) | `a8b900321199038fe4200211a52f46d2f4bae786` |

XR Management 4.6.1 declara XR Core Utilities (mínimo 2.2.1) y XR Legacy Input Helpers (mínimo 2.1.11) como dependencias. Se fija XR Core Utilities 2.6.0 porque su changelog incluye la adaptación de `BuildValidator.SelectObject` a EntityId en Unity 6.5; la versión 2.5.3 no incluía ese ajuste. XR Legacy Input Helpers 3.0.1 satisface la dependencia mínima. Input System 1.20.1 depende de UIElements, suministrado como módulo integrado. XR Core Utilities y Legacy Input Helpers dependen del módulo XR integrado. uGUI Core 2.5.0 requiere además los módulos integrados de audio, IMGUI, physics, physics2d y UI, que están declarados en el manifiesto. El paquete Cardboard está embebido en `Packages/com.google.xr.cardboard`.

## Por qué esto corrige la parte de UI

`UIManager.cs`, `WorldPanel.cs` y `UiButton3D.cs` usan `UnityEngine.UI` correctamente. `Image` y `Text` pertenecen al assembly uGUI (`UnityEngine.UI`), no a Input System. El Editor Unity 6000.5 distribuye uGUI `2.5.0` como Core package; no se debe intentar descargar un `.tgz` de uGUI para este Editor. El `.asmdef` `LasPalmeras.Runtime` declara referencias explícitas a `UnityEngine.UI`, `Unity.InputSystem`, `Unity.XR.Management` y `Google.XR.Cardboard`.

Si `Image`/`Text` no se resuelven después del preflight, el batchmode se detiene y escribe el error de compilación; el Editor gráfico no se abre. Esto permite separar un problema de dependencia de un error real en los scripts.

## Red y modo sin conexión

La primera ejecución necesita acceso HTTPS/443 a `download.packages.unity.com`. El entorno usado para preparar este ZIP no pudo resolver el DNS del CDN, así que los cuatro `.tgz` no están incluidos. El BAT los descargará en el PC del usuario antes de abrir el proyecto. Si ese PC no tiene Internet, copia los cuatro archivos indicados arriba desde otro equipo a `LocalPackages/`; el script verificará los hashes antes de continuar.

Con los tarballs locales y el lockfile regenerado, la resolución de dependencias del proyecto no necesita consultar el catálogo remoto para esos cuatro paquetes. La lista del catálogo Unity Registry puede seguir mostrando un mensaje offline si la ventana de Package Manager intenta actualizar el catálogo; ese panel no demuestra por sí solo que la resolución de las dependencias locales haya fallado. La evidencia determinante son el exit code del batch, el lockfile y el marcador de compilación.

## Logs

- `Builds/Validation/package-download-preflight.log`: descargas, reintentos, checksums, backups y errores del script.
- `Builds/Validation/unity65-preflight-compile.log`: Unity batchmode, resolución UPM, compilación C# y comprobación de assemblies.

Si el BAT falla, no abras el proyecto manualmente todavía. Conserva y comparte ambos registros.

## Alcance de la validación de esta entrega

El entorno de empaquetado no dispone de Unity Editor, PowerShell ni compilador C#. Por ello esta entrega puede validar manifiestos, rutas, hashes declarados, estructura de scripts y flujo del preflight, pero no ejecutar el descargador de Windows ni certificar una compilación Unity real. La compilación solo quedará confirmada en el PC con Unity `6000.5.2f1` cuando el BAT termine con el marcador de éxito y código de salida cero.
