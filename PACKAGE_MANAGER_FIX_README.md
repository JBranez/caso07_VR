# Preflight de dependencias y compilación — Las Palmeras VR / Unity 6.5

## Causa y diseño de la solución

El error `Cannot read properties of null (reading 'severity')` ocurre en la ventana del Package Manager al no poder procesar la consulta del catálogo cuando este está fuera de línea. No es un error C#. A la vez, los errores `UnityEngine.UI`, `Image` y `Text` aparecen cuando uGUI no se encuentra habilitado/cargado. Los tres scripts importan el namespace correcto.

En Unity 6000.5, uGUI es un Core package distribuido con el Editor y la versión fija del Editor es 2.5.0; no debe tratarse como un tarball de registro. El manifiesto conserva `com.unity.ugui: 2.5.0` para cargarlo.

El lanzador `Tools/Preparar_y_Abrir_Unity65.bat` ejecuta `Prepare_And_Open_Unity65.ps1`. Antes de iniciar el Editor gráfico, descarga directamente del CDN oficial los cuatro paquetes UPM externos, verifica los SHA-1 de publicación y los guarda dentro de `LocalPackages/`. Luego cambia las dependencias del manifiesto a referencias locales `file:`. Unity se inicia en modo batch, resuelve el grafo y compila los assemblies; el proceso solo continúa hacia el Editor gráfico si se creó el lockfile y se emitió el marcador de validación.

## Paquetes externos incluidos en el preflight

| Paquete | Versión | SHA-1 publicado |
|---|---:|---|
| `com.unity.inputsystem` | 1.20.1 | `2436e68c60c469edbccef8f6b3c319b9441f4675` |
| `com.unity.xr.management` | 4.6.1 | `df145a5bc89e37453667848cc4c11a49a7246aa3` |
| `com.unity.xr.legacyinputhelpers` | 3.0.1 | `3f62d634f63be463f9e1f7b0deea814a27526da4` |
| `com.unity.xr.core-utils` | 2.6.0 | `a8b900321199038fe4200211a52f46d2f4bae786` |

El manifiesto también declara los módulos integrados `uielements`, `xr`, `subsystems` y `ui`. Cardboard sigue embebido en `Packages/com.google.xr.cardboard` y sus dependencias están alineadas con los paquetes externos anteriores.

## Ejecución

1. Instala Unity `6000.5.2f1`. Para Android, instala sus módulos desde Unity Hub.
2. Cierra Unity Editor.
3. Ejecuta `Tools/Preparar_y_Abrir_Unity65.bat` desde el proyecto extraído.
4. La primera ejecución necesita Internet HTTPS/443 hacia `download.packages.unity.com`. Si se descargaron y verificaron los cuatro tarballs, se conservan en `LocalPackages/` para futuras ejecuciones.
5. El lanzador ejecuta Unity con `-batchmode`, `-projectPath`, `-executeMethod LasPalmeras.EditorTools.CompileValidation.Run` y `-logFile`. Solo después de validar el lockfile y el marcador `LAS_PALMERAS_COMPILE_VALIDATION_PASSED` inicia Unity con interfaz gráfica.

## Qué se comprueba dentro de Unity

`Assets/Editor/CompileValidation.cs` comprueba explícitamente:

- el assembly `UnityEngine.UI` y los tipos `UnityEngine.UI.Image` y `UnityEngine.UI.Text`;
- `Unity.InputSystem` y `UnityEngine.InputSystem.InputActionAsset`;
- `Unity.XR.Management` y `Google.XR.Cardboard`;
- que el assembly runtime `LasPalmeras.Runtime` contenga `UIManager`, `WorldPanel` y `UiButton3D`;
- que `LasPalmeras.Runtime` declare referencias a uGUI, Input System y XR/Cardboard.

Esto constituye una compilación real solamente cuando el script se ejecuta en la computadora que tiene Unity. La validación estática realizada al empaquetar no se presenta como compilación.

## Logs

- `Builds/Validation/package-download-preflight.log`: diagnóstico de búsqueda de Editor, descargas y checksums.
- `Builds/Validation/unity65-preflight-compile.log`: log completo de Unity batchmode y compilación.

Si la descarga falla, la computadora necesita conexión/proxy/firewall habilitado para `download.packages.unity.com:443`. Si el preflight pasa pero el panel Unity Registry sigue mostrando `Error fetching package list offline`, esa es una consulta del catálogo en línea; la resolución local y la compilación se verifican con el marcador de éxito, independientemente de que la lista del catálogo esté disponible.

## Integridad y cachés

El preflight conserva `Library/PackageCache`, `Library/PackageManager/ProjectCache` y `LocalPackages/`. Hace una copia fechada del manifiesto y del `packages-lock.json` anterior. Como cambia el origen de los paquetes de registro a tarball local, aparta el lockfile activo para que UPM genere uno nuevo con las fuentes correctas; no elimina las cachés de paquetes.

## Pruebas ejecutadas durante la preparación del ZIP

- JSON de `Packages/manifest.json` y `Packages/com.google.xr.cardboard/package.json`: parseo correcto.
- Versiones y fuentes de las dependencias: comprobadas estáticamente.
- Referencias del `.asmdef`, imports `UnityEngine.UI` y tipos que valida el gate: comprobados estáticamente.
- SHA-1 esperados escritos en el downloader: comparados con los valores publicados por Unity.
- 38 archivos C#: comprobación estructural de cadenas/comentarios y delimitadores.
- ZIP: integridad comprobada tras crearlo.

No se ejecutó Unity Editor en el entorno de empaquetado (no está instalado y no hay compilador C# disponible). Por tanto, el resultado de compilación real se obtiene en la primera ejecución del BAT en el PC del usuario; el BAT no abre la GUI si la validación no pasa.
