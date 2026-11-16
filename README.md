# Las Palmeras VR · ANDES-SIM · Escenario 07

Simulador educativo de investigación y respuesta ante un fraude financiero inspirado en el caso del Bangladesh Bank / RCBC (2016). El escenario combina exploración de una sucursal bancaria, verificación KYC, CCTV, análisis de registros, alertas por inactividad, decisiones de retención y reconstrucción de una secuencia de transferencias. Incluye modo PC y modo móvil VR con Google Cardboard.

> **Precisión histórica:** la fuente principal utilizada para reconstruir hechos concretos es una demanda civil presentada por Bangladesh Bank contra RCBC en 2019. Por tanto, sus descripciones se presentan como alegaciones de la demanda, no como hechos judiciales probados por el mero hecho de aparecer allí. Los nombres, cuentas, banco del jugador y parte de la distribución monetaria del simulador son ficticios.

## Versión y migración

**Editor objetivo:** `Unity 6.5 (6000.5.2f1)` · changeset `eb73d3b415a1`.

El proyecto utiliza Google Cardboard XR Plugin `1.36.0` embebido, Input System `1.20.1`, XR Plug-in Management `4.6.1`, XR Legacy Input Helpers `3.0.1`, XR Core Utilities `2.6.0` y uGUI Core `2.5.0`. El módulo heredado `com.unity.modules.vr` se eliminó en Unity 6.5; se conserva `com.unity.modules.xr`. El BAT de preparación descarga y verifica los cuatro paquetes externos como tarballs locales, y hace que Unity los resuelva/compile en batchmode antes de abrir el Editor gráfico.

## 1. Instalar, descargar dependencias y abrir

1. Instala Unity Hub y el Editor **6000.5.2f1**. Para generar un APK instala también **Android Build Support**, **Android SDK & NDK Tools** y **OpenJDK** desde Unity Hub.
2. Extrae el proyecto y **no lo abras todavía desde Unity Hub**. Cierra cualquier instancia del Editor.
3. Haz doble clic en el acceso de arranque `Abrir_Proyecto_Unity65.bat` (también puedes ejecutar `Tools/Preparar_y_Abrir_Unity65.bat`). Ese lanzador realiza el preflight completo antes de mostrar el Editor:
   - localiza Unity `6000.5.2f1`;
   - descarga desde el CDN oficial de Unity los tarballs `Input System 1.20.1`, `XR Plug-in Management 4.6.1`, `XR Legacy Input Helpers 3.0.1` y `XR Core Utilities 2.6.0`;
   - verifica el SHA-1 publicado para cada archivo y los guarda en `LocalPackages/`;
   - reescribe las cuatro dependencias como referencias `file:../LocalPackages/...`, para que UPM no tenga que consultar el catálogo del registro para esos paquetes;
   - deja `com.unity.ugui` en `2.5.0`, que es el paquete Core suministrado con Unity 6000.5, y conserva los módulos integrados requeridos;
   - arranca Unity en **batchmode**, espera a que UPM resuelva el grafo local, compila todos los scripts y comprueba `UnityEngine.UI.Image`, `UnityEngine.UI.Text`, `InputActionAsset`, XR Management, Cardboard y las clases UI del proyecto;
   - **solo abre el Editor gráfico si el proceso batch genera `LAS_PALMERAS_COMPILE_VALIDATION_PASSED` y el `packages-lock.json` confirma las versiones descargadas**.
4. Cuando se abra el Editor, usa **Las Palmeras → 2 · Abrir escena Agencia_VR** y pulsa Play. La escena y los datos de escenario se generan mediante `Assets/Editor/ProjectBootstrap.cs`.
5. Si Unity pide reiniciar después de cambiar **Active Input Handling = Both**, acepta. El lanzador ya habrá comprobado la compilación antes de abrir el Editor gráfico.

**Importante para la primera ejecución:** se necesita Internet y acceso HTTPS/443 a `download.packages.unity.com`. El entorno desde el que se preparó este ZIP no permite descargar los `.tgz`; por ello el lanzador los obtiene en tu PC antes de abrir el proyecto. Después de una descarga válida, los paquetes quedan guardados en `LocalPackages/` y el preflight puede reutilizarlos sin volver a consultar el CDN. No elimines esa carpeta.

Si Unity está instalado en una ruta personalizada, define `UNITY_EDITOR_PATH` antes de ejecutar el BAT o invoca el script así:

```powershell
$env:UNITY_EDITOR_PATH = 'D:\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe'
.\Tools\Prepare_And_Open_Unity65.ps1
```

### Sobre el error de Package Manager y `UnityEngine.UI`

- uGUI (`com.unity.ugui`) es un **Core package** distribuido junto con cada versión del Editor; para Unity 6000.5 su versión corresponde a `2.5.0`. No se descarga como tarball independiente.
- `UIManager.cs`, `WorldPanel.cs` y `UiButton3D.cs` ya usan el namespace correcto, `UnityEngine.UI`. Los errores `Image`/`Text` aparecen cuando el assembly uGUI no se carga o la resolución UPM falla; no se corrigen quitando esos `using`.
- Input System y los tres paquetes XR externos se descargan primero como `.tgz` y luego se resuelven desde rutas locales. Así se evita depender de la consulta del catálogo de Unity que genera `Cannot read properties of null (reading 'severity')`.
- Si el propio panel **Unity Registry** sigue mostrando `Error fetching package list offline` después de una validación batch exitosa, ese mensaje corresponde a la consulta del catálogo en línea. Puede persistir mientras no haya conexión, aunque las dependencias locales del proyecto estén resueltas. La prueba decisiva es el preflight, no que la página del catálogo cargue.

### Logs y diagnóstico

Si el BAT se detiene, no abras el proyecto manualmente hasta revisar el motivo. Los registros quedan en:

- `Builds/Validation/package-download-preflight.log`: URL, intento, descarga y checksum de cada paquete.
- `Builds/Validation/unity65-preflight-compile.log`: resolución UPM, compilación C# y validación de assemblies ejecutadas por Unity.

Si falla la descarga, comprueba conexión/proxy/firewall para `download.packages.unity.com:443`. Si descarga bien pero falla el batchmode, comparte ambos logs; el segundo debe indicar si el problema restante es UPM, C# o carga de assemblies.

Para volver a ejecutar la verificación sin abrir el Editor gráfico:

```powershell
.\Tools\Verify_Compile_Unity65.ps1
```

Para validar además un build Windows x64:

```powershell
.\Tools\Verify_Compile_Unity65.ps1 -BuildWindows
```

También puedes compilar desde PowerShell después del preflight:

```powershell
.\Tools\Compilar.ps1 -Destino Windows
.\Tools\Compilar.ps1 -Destino Android
```

Los logs quedan en `Builds/Validation/`. Para un APK, asegúrate de haber instalado los módulos Android desde Unity Hub.

## 2. Contenido y jugabilidad

| Modo | Experiencia |
|---|---|
| **ATAQUE · Revivir 2016** | Explora la agencia, revisa cinco fichas KYC, inspecciona seis fechas de CCTV, consulta cuentas y movimientos, reconstruye la sesión SWIFT/STOPPAY y encuentra las pistas que explican las fallas de control. Las cinco transferencias del 9 de febrero se representan en su secuencia horaria real. |
| **DEFENSA · Historia alternativa** | Repite el escenario con capacidad de actuar: configura KYC, selecciona una regla de inactividad y activa retención/monitoreo. La selección de umbrales tiene consecuencias, incluyendo falsas alarmas o reglas demasiado laxas. |

### Controles

| PC | Android / Cardboard |
|---|---|
| WASD o flechas: moverse; Shift: correr; ratón: mirar | Mira para orientar la vista |
| E o clic: interactuar con el objeto apuntado | Mirada sostenida sobre el objeto o toque en pantalla |
| 1–5: teletransporte; H: pista; Tab: bitácora; Esc: pausa | Mira un anillo de teletransporte del suelo; la esfera azul ofrece pistas |
| F2: alternar simulación de mirada VR en PC | El visor se calibra con el QR del Cardboard |

F1 abre la guía. El menú incluye ajustes de volumen, narración y subtítulos. Las instrucciones se narran mediante TTS del sistema en Android y Windows; otras plataformas conservan subtítulos y señales sonoras. La disponibilidad/voz concreta depende del motor de voz instalado en el dispositivo.

## 3. Interfaz adaptable y audio

- HUD de pantalla con `CanvasScaler`, reajuste ante cambios de resolución, anclas y área segura (`Screen.safeArea`).
- Puntero de mirada con `Physics.RaycastNonAlloc` y buffer reutilizable para reducir asignaciones por frame en VR.
- Paneles holográficos en espacio de mundo que ajustan escala al campo de visión y relación de aspecto de la cámara.
- Menú de audio/accesibilidad: volumen maestro, silenciar, narración y subtítulos.
- Audio procedural: efectos de interacción y cinco ambientes precargados, con fundido entre vestíbulo, ventanilla, bóveda, seguridad y oficina; ruido de CCTV al entrar a seguridad.
- Las voces se generan con Android TextToSpeech o Windows SAPI, no son grabaciones de actores. Requieren validación en los equipos destino.

## 4. Compilar

**Desde el Editor:** `Las Palmeras → Compilar → PC · Windows 64-bit` o `Android · APK Cardboard (VR)`. Salidas dentro de `Builds/`.

**Windows PowerShell:**

```powershell
.\Tools\Compilar.ps1 -Destino Windows
.\Tools\Compilar.ps1 -Destino Android
```

**macOS/Linux:**

```bash
./Tools/compilar.sh linux
./Tools/compilar.sh mac
./Tools/compilar.sh android
```

Los scripts requieren Editor Unity 6000.5.2f1 instalado y licencia disponible. En Android, verifica primero que XR Plug-in Management muestre Cardboard activo y que esté instalado el módulo de compilación Android. El script de build ahora se detiene si la configuración general, la escena o la asignación del loader Cardboard no quedan confirmadas.

## 5. Estructura

```text
Assets/Editor/                 Bootstrap, configuración XR/Android y build scripts
Assets/Scripts/Core/           GameManager, ScenarioData, StoryDirector, reloj y flujo
Assets/Scripts/World/          AgencyBuilder, NPC, materiales y geometría procedural
Assets/Scripts/Interaction/    Interactuables, fichas, evidencias, defensas y teletransporte
Assets/Scripts/Gameplay/       KYC, CCTV, reglas de inactividad y bitácora
Assets/Scripts/Player/         PlayerRig, mirada, Cardboard y fundido de pantalla
Assets/Scripts/UI/             HUD, modales y paneles holográficos responsivos
Assets/Scripts/Audio/          AudioManager, síntesis procedural y AudioGuide
Assets/Resources/Shaders/      Shaders Unlit, CCTV noise y retícula
Packages/com.google.xr.cardboard/  Plugin Google Cardboard XR
Docs/                          Guía, fidelidad histórica, arquitectura y auditoría
Tools/                         Instalación y compilación desde consola
```

El ZIP original no contenía una escena propia del juego en `Assets/Scenes` ni prefabs propios de la agencia. Sí incluye las dos escenas y dos prefabs de ejemplo del paquete Cardboard bajo `Packages/.../Samples~`. La escena jugable `Agencia_VR` se construye al abrir el proyecto mediante el bootstrap del Editor. Después del primer arranque, guarda el proyecto para persistir la escena y los assets generados.

## 6. Límites conocidos

- Esta entrega fue revisada estáticamente en un entorno sin Unity Editor ni compilador C#. No se afirma que se haya completado una compilación real ni una prueba en visor.
- La compatibilidad final de Cardboard, Android TTS, shaders, escalado UI y rendimiento debe comprobarse en Unity 6000.5.2f1 y en al menos un dispositivo Android/visor objetivo.
- El proyecto está orientado a Google Cardboard en Android, no a OpenXR/Quest ni a un runtime PC VR nativo.
- El reparto de los US$81 millones entre las cuatro cuentas, los datos personales de las fichas y el flujo visual de la caja son elementos didácticos ficticios; no deben citarse como distribución probada del caso real.

## 7. Licencias y declaración

Google Cardboard XR Plugin `1.36.0` (Apache 2.0). Los modelos, materiales, shaders, ambientes y efectos del simulador se generan proceduralmente desde código. El equipo debe declarar las herramientas de IA que haya utilizado en la documentación académica correspondiente.


## Detalle del preflight de dependencias

Consulta [`Docs/Preflight_Dependencias_Unity65.md`](Docs/Preflight_Dependencias_Unity65.md) para ver las versiones, checksums, rutas locales, dependencias transitivas, limitaciones de red y criterios exactos de compilación.

## Escena principal ya incluida

La escena `Assets/Scenes/Agencia_VR.unity` está guardada en el proyecto y añadida a Build Settings. Ya no se crea mediante `EditorSceneManager.NewScene` ni con una operación aditiva durante el arranque. Contiene el componente `GameBootstrap` como punto de entrada. Si Unity arranca con una escena `Untitled` sin guardar, `ProjectBootstrap` abre esta escena guardada en modo Single; no crea una escena temporal ni intenta cargarla de forma aditiva.

La geometría interactiva de la agencia (lobby, ventanillas, CCTV, bóveda, oficina, NPC y terminales) se construye en tiempo de ejecución por `AgencyBuilder` cuando `GameManager` inicia. Esto evita serializar cientos de primitivas y no implica que falte la escena: la escena de entrada es un asset `.unity` normal y persistente.
