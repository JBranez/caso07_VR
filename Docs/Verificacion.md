# Verificación técnica · Unity 6.5

## Estado de esta entrega

**Editor configurado como objetivo:** Unity `6000.5.2f1`, changeset `eb73d3b415a1`.

**Alcance comprobado en esta revisión:** inspección estática de los archivos fuente y configuración; edición del proyecto y de sus instrucciones de build. El entorno que realizó esta revisión no dispone del Editor Unity ni de un compilador C# compatible, por lo que no se declara una compilación Unity real ni una ejecución de la escena.

## Comprobaciones estáticas

Ejecutar `python Tools/validar_estatico.py` desde la raíz para repetir los chequeos locales de manifiesto, versión, aritmética, funcionalidades y estructura de delimitadores C#. Este validador no reemplaza al compilador C# de Unity.

| Comprobación | Resultado de esta revisión |
|---|---|
| Proyecto configurado en `ProjectSettings/ProjectVersion.txt` | `6000.5.2f1` y changeset correcto |
| Manifiesto `Packages/manifest.json` | JSON válido; Input System `1.20.1`, XR Management `4.6.1`, Legacy Helpers `3.0.1`, Core Utils `2.6.0`, uGUI Core `2.5.0`; módulos XR/UI/subsystems; módulo VR obsoleto ausente |
| Código del proyecto | 38 archivos C# bajo `Assets/`; revisados por dependencias, llamadas relevantes y coherencia del flujo. No equivale a compilación C#. |
| Escena fuente en el ZIP original | No existía archivo `.unity`; el Editor la genera con `ProjectBootstrap` |
| Configuración de build | Se reforzó para no continuar si falla el bootstrap, falta la escena o no se confirma Cardboard al compilar Android |
| Datos de las transferencias | Se compararon horas/montos del guion y se corrigió la secuencia a cinco operaciones (total US$58.158.641,63) |
| UI/audio | Se añadieron anclas/área segura, ajuste de escala para paneles de mundo, voz TTS/SAPI, subtítulos y ambientes procedurales precargados |
| Rendimiento de interacción | Puntero de mirada usa `Physics.RaycastNonAlloc` con buffer reutilizable; ambientes precargados |
| Protección de build | Se detiene si el ajuste de entrada acaba de cambiar y requiere reiniciar el Editor |


## Corrección adicional: Input System en Unity 6.5

La configuración anterior fijaba `com.unity.inputsystem` en `1.14.2`. Esa versión del código Editor usaba APIs antiguas basadas en `InstanceID` que Unity 6.5 promueve a errores `CS0619`. Input System corrigió estas incompatibilidades en versiones posteriores; esta entrega utiliza `1.20.1`, publicada para Editor `6000.5`. En primera ejecución, el preflight descarga el tarball oficial y lo conserva en `LocalPackages/`, en vez de depender de la consulta del catálogo de UPM.

La excepción `TypeLoadException` de `UnityEngine.InputSystem.InputActionAsset` aparece junto al fallo de compilación de la assembly `Unity.InputSystem`. El paquete 1.20.1 conserva esa API pública. La hipótesis principal es que el importador estaba intentando resolver un tipo de una assembly que no se había compilado correctamente. Esta entrega ya no borra ensamblados ni cachés. Si el error persiste después de confirmar que 1.20.1 está instalado y que no hay errores CS0619, debe analizarse como un problema independiente con el stack trace nuevo.

**Procedimiento actualizado:** cerrar Unity y ejecutar `Tools/Preparar_y_Abrir_Unity65.bat`. El lanzador descarga y verifica cuatro tarballs, cambia esas dependencias a rutas locales, ejecuta Unity `6000.5.2f1` en batchmode, valida el lockfile y compila antes de abrir la GUI. `Tools/Repair_PackageManager_Unity65.ps1` se conserva como alias al mismo flujo. No se borran `Library/PackageCache` ni `Library/PackageManager/ProjectCache`. Como cambia el origen de las dependencias (registro → tarball local), el lockfile anterior se respalda con sufijo `.before-local-tarballs-...bak` y Unity genera uno nuevo coherente con las rutas locales.

Fuentes oficiales:
- [Input System 1.20.1 publicado para Unity Editor 6000.5](https://docs.unity.com/en-us/engine/6000.5/manual/packages-list/packages-all/pack-safe/com-unity-inputsystem)
- [Changelog Input System: 1.18.0 corrige incidencias de compatibilidad Unity 6.4/6.5](https://github.com/needle-mirror/com.unity.inputsystem/releases)

## Qué falta ejecutar en Unity 6000.5.2f1

1. Ejecutar primero `Tools/Preparar_y_Abrir_Unity65.bat` con conexión HTTPS/443 al CDN oficial para descargar los tarballs que aún no estén en `LocalPackages/`.
2. Confirmar que el preflight emita `LAS_PALMERAS_COMPILE_VALIDATION_PASSED`; solo entonces se abre el Editor gráfico. Revisar la Consola y atender advertencias del plugin o APIs que dependan del dispositivo.
3. Ejecutar **Las Palmeras → 1 · Configurar proyecto**. Comprobar que se genera `Assets/Scenes/Agencia_VR.unity` y el loader Cardboard de Android aparece activo.
4. Abrir escena, probar ATAQUE y DEFENSA, pausa, bitácora, pestañas del terminal, interacciones con fichas, CCTV, teletransporte y todos los controles.
5. Cambiar la resolución/relación de aspecto en PC (16:9, 16:10, 4:3 y ventana estrecha) y confirmar que el HUD no cubre controles y que los paneles son legibles.
6. Probar en Android real: Cardboard QR, orientación horizontal, render estéreo, retícula, toque, TTS, volumen, subtítulos, ambientes, temperatura y FPS.
7. Compilar PC y Android desde el Editor. Examinar `Builds/` y los logs de los scripts; comprobar el APK instalado en un dispositivo destino.

## Limitaciones y riesgos pendientes

- La integración con Cardboard y el ajuste por campo de visión están diseñados para móvil Cardboard. No se declara compatibilidad con OpenXR, Quest o PC VR nativo.
- Android TTS depende de un motor instalado y de su idioma. Windows SAPI depende de la disponibilidad de SAPI/voz española; no se ha validado en este entorno.
- El cálculo de escala de paneles se basa en la cámara y su FOV; una escena ejecutándose en headset debe evaluarse visualmente, especialmente en FOV y texto pequeño.
- Los efectos de audio generados en tiempo de ejecución deben perfilarse en móvil para descartar sobrecarga de CPU y latencia.
- La configuración IL2CPP/ARM64/OpenGLES3 y Gradle del plugin requiere la compilación Android real para confirmar compatibilidad final.

**Conclusión:** esta entrega queda migrada a nivel de archivos/configuración y recibe correcciones y mejoras estáticas. No es honesto describirla como “compilada y probada en Unity 6.5” hasta completar los pasos anteriores en el Editor y hardware destino.

## Incidencia uGUI / Package Manager offline

uGUI es un Core package distribuido con Unity 6000.5; el manifiesto declara `com.unity.ugui: 2.5.0`, la versión fija del Editor. Se añadió referencia explícita a `UnityEngine.UI` en `Assets/Scripts/LasPalmeras.Runtime.asmdef` y el gate ahora comprueba `Image`, `Text`, `InputActionAsset`, XR Management y Cardboard desde Unity Editor.

El flujo actualizado descarga Input System, XR Management, Legacy Input Helpers y XR Core Utilities como tarballs del CDN de Unity, verifica sus checksums y los referencia desde `LocalPackages/`. Así evita usar la consulta de catálogo que causa el error `severity` para resolver esas cuatro dependencias. El panel Unity Registry todavía necesita Internet para cargar el catálogo cuando se abre manualmente; la resolución local y la compilación se confirman con el proceso batch. El preflight conserva las cachés (`Library/PackageCache`, `Library/PackageManager/ProjectCache`) y `LocalPackages`; el lockfile activo se respalda con fecha y se regenera porque cambia el origen de las dependencias. XR Core Utilities se fija en `2.6.0`, que incorpora la adaptación de `BuildValidator.SelectObject` a EntityId para Unity 6.5; no se mantiene la versión anterior `2.5.3`.


XR Core Utilities 2.6.0 evita mantener la ruta int-based de `EditorUtility.InstanceIdToObject` que el changelog de Unity actualiza para Unity 6.5. [Changelog de XR Core Utilities](https://github.com/needle-mirror/com.unity.xr.core-utils/blob/2.6.0/CHANGELOG.md).
