# Paquetes y compilación — Unity 6.5

El proyecto usa Unity `6000.5.2f1`. Las dependencias externas se gestionan con el preflight: Input System `1.20.1`, XR Plug-in Management `4.6.1`, XR Legacy Input Helpers `3.0.1` y XR Core Utilities `2.6.0`. uGUI `2.5.0` es un Core package incluido con el Editor `6000.5` y está declarado con la versión que corresponde a ese Editor.

Los scripts `UiButton3D`, `UIManager` y `WorldPanel` usan correctamente `UnityEngine.UI.Image` y `UnityEngine.UI.Text`. El assembly `LasPalmeras.Runtime` declara una referencia explícita a `UnityEngine.UI`. La comprobación en Unity no se limita a inspeccionar los imports: carga el assembly `UnityEngine.UI` y valida `Image`, `Text`, `InputActionAsset`, XR Management, Cardboard y los tres componentes UI del proyecto.

## Flujo recomendado

1. Cierra Unity Editor.
2. Ejecuta `Tools/Preparar_y_Abrir_Unity65.bat`.
3. El script descarga los cuatro tarballs UPM oficiales desde `download.packages.unity.com`, verifica sus SHA-1 publicados y los conserva en `LocalPackages/`.
4. Reescribe las dependencias como referencias locales `file:` y lanza Unity en batchmode para resolver `packages-lock.json` y compilar.
5. Solo abre el Editor gráfico si `CompileValidation.Run` emite `LAS_PALMERAS_COMPILE_VALIDATION_PASSED`.

La primera ejecución necesita Internet HTTPS/443 hacia el CDN. En ejecuciones posteriores el preflight reutiliza los `.tgz` verificados en `LocalPackages/`. No se borran `Library/PackageCache` ni `Library/PackageManager/ProjectCache`; el lockfile anterior se guarda como backup y Unity genera otro para las rutas locales.

La excepción `Cannot read properties of null (reading 'severity')` se produce en la ventana que intenta recuperar el catálogo del Package Manager cuando no hay conexión. El preflight no usa esa consulta del catálogo para instalar los cuatro paquetes externos: baja sus tarballs y deja que UPM los resuelva desde disco. Si el catálogo Unity Registry sigue mostrando un aviso offline pero el preflight emite el marcador de éxito, la resolución local y la compilación de las dependencias del proyecto han pasado.

## Registros

- `Builds/Validation/package-download-preflight.log`: descargas y checksums.
- `Builds/Validation/unity65-preflight-compile.log`: resolución UPM y compilación desde Unity batchmode.

La compilación real solo queda confirmada cuando el preflight se ejecuta en el PC que tiene instalado Unity `6000.5.2f1`. La validación estática al preparar el ZIP no sustituye esa ejecución.
