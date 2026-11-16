# Auditoría técnica y funcional · Las Palmeras VR

**Proyecto recibido:** `LasPalmeras_VR_Unity6.3.zip`  
**Editor solicitado y configurado:** Unity `6000.5.2f1` (Unity 6.5)  
**Changeset:** `eb73d3b415a1`  
**Fecha de esta revisión:** 16 de noviembre de 2026

## 1. Dictamen ejecutivo

El proyecto es una simulación educativa de una agencia bancaria construida de forma procedural, con exploración en primera persona, interacción por mirada/toque/ratón, modo de escritorio y soporte Google Cardboard para Android. Tiene dos rutas principales (ATAQUE y DEFENSA), mecánicas KYC, CCTV, cuentas dormidas, reglas de alerta, retención, teletransporte, bitácora y resultado final.

La configuración del repositorio se cambió a Unity 6.5 `6000.5.2f1`; el manifiesto se limpió del módulo de VR heredado; se amplió el recorrido investigativo con la cronología SWIFT/STOPPAY y la secuencia exacta de cinco transferencias; se mejoraron HUD y paneles 3D para distintos tamaños/formatos; y se añadió narración por TTS, subtítulos y ambientes sonoros por zona.

**Límite decisivo:** esta revisión se realizó sin Unity Editor ni compilador C# disponible. Por eso el dictamen cubre archivos, configuración y lógica inspeccionable, pero **no certifica una compilación exitosa, cero errores de API, funcionamiento de Cardboard en un teléfono ni rendimiento en un visor**. La validación final requiere abrir el proyecto con el Editor pedido y seguir `Verificacion.md`.

## 2. Qué contiene el proyecto

| Área | Contenido |
|---|---|
| `Assets/Editor` | Bootstrap de proyecto, activación de XR Management, postprocesado Gradle y scripts de build |
| `Assets/Scripts/Core` | Estado global, reloj simulado, datos del escenario, entrada y director de historia |
| `Assets/Scripts/World` | Construcción procedural de la sucursal, NPC, geometría/materiales y asistente |
| `Assets/Scripts/Interaction` | Fichas, objetos de evidencia, botones, controles de defensa, portal y teletransporte |
| `Assets/Scripts/Gameplay` | KYC, CCTV, regla de cuentas inactivas y bitácora financiera |
| `Assets/Scripts/Player` | Rig, puntero de mirada, modo VR y fundido |
| `Assets/Scripts/UI` | HUD, menús, modales holográficos y paneles interactivos |
| `Assets/Scripts/Audio` | Síntesis de efectos, audio ambiental y guía hablada/subtitulada |
| `Assets/Resources/Shaders` | Shaders propios de escena, ruido CCTV y retícula |
| `Packages/com.google.xr.cardboard` | Plugin Google Cardboard embebido |

La carpeta `Assets` contiene 38 archivos C# del proyecto y tres shaders. El ZIP recibido no incluye una escena principal de juego en `Assets/Scenes` ni prefabs propios de la agencia; sí incluye dos escenas y dos prefabs de muestra bajo `Packages/com.google.xr.cardboard/Samples~`. `ProjectBootstrap` genera `Agencia_VR` y los recursos básicos en la primera apertura. `AgencyBuilder` instancia la agencia y el contenido jugable durante la ejecución.

## 3. Migración a Unity 6.5

- `ProjectSettings/ProjectVersion.txt`: cambiado de `6000.3.28f1` a `6000.5.2f1`, con changeset correspondiente.
- `Packages/manifest.json`: eliminado `com.unity.modules.vr`, retirado en esta línea del Editor; se conserva `com.unity.modules.xr`.
- Se mantiene Google Cardboard XR Plugin `1.36.0`, Input System `1.20.1`, XR Plug-in Management `4.6.1` y uGUI `2.5.0`.
- `Tools/Instalar_Unity.ps1`, `Tools/instalar_unity.sh`, `Tools/Compilar.ps1` y `Tools/compilar.sh` apuntan a la versión solicitada.
- El marcador del bootstrap cambia de versión para que el proyecto vuelva a aplicar sus ajustes y genere la escena con la configuración revisada.
- `BuildScripts` comprueba que el bootstrap no haya fallado, que la escena exista y, en builds Android, que la asignación de Cardboard aparezca confirmada. Si no, aborta en vez de informar falsamente que compiló.

## 4. Funcionalidades existentes

### Exploración e interacción

El `PlayerRig` admite movimiento de escritorio y modo de mirada; `GazePointer` controla selección por raycast y dwell; `TeleportPoint` mueve al jugador entre el vestíbulo, ventanilla, sala de seguridad, bóveda y oficina. `Interactable` sirve como base para fichas, evidencias, controles y elementos de escena.

### Investigación KYC

Se inspeccionan cinco fichas. `KycChecker` comprueba formato de documento con dígito verificador propio, domicilio contra una lista local, teléfono, carta devuelta, coherencia entre ingreso/deposito y patrón de depósitos iniciales. La lógica es educativa y ficticia, no sustituye controles normativos reales.

### Seguridad y defensa

`CctvFeed` permite revisar fechas; `DormantAlert` propone tres umbrales de detección con consecuencias diferentes; `DefenseControl` y `StoryDirector` controlan prerrequisitos, activación y evaluación de KYC, alertas y retención. El resultado explica por qué detectar una anomalía sin un mecanismo de retención no impide una transferencia.

### Bitácora y cierre

`WithdrawalLog` mantiene cuentas, saldos y operaciones. `UIManager` muestra menú, guía, pausa, terminal, controles y panel final. El asistente y las balizas ayudan a encontrar el objetivo activo.

## 5. Cambios y correcciones incorporadas

### Exactitud de la secuencia de transferencias

El escenario anterior resumía erróneamente US$58 millones como un retiro ocurrido a las 10:24. Ahora se registran cinco movimientos individuales:

| Hora | Monto (USD) |
|---|---:|
| 10:24 | 15.215.977,26 |
| 11:19 | 19.951.502,13 |
| 11:34 | 9.769.124,15 |
| 11:35 | 7.236.154,62 |
| 11:37 | 5.985.883,47 |
| **Total** | **58.158.641,63** |

El director de historia avanza por estas marcas temporales, añade operaciones individuales a la bitácora y recalcula los saldos por etapas. La lógica interna usa dólares enteros para el estado de cuenta y redondea el acumulado exacto al dólar más cercano en cada hito; así el débito de cada fila se obtiene por diferencia acumulada, mientras que las etiquetas conservan los centavos de cada orden. El saldo por ficha es una asignación proporcional didáctica, no una distribución histórica probada. La evaluación DEFENSA también distingue los créditos SWIFT rechazados de la caja de efectivo: bloquear los primeros no asegura por sí solo la custodia de esta última.

### Cronología SWIFT / STOPPAY

Se agregó evidencia `SWIFT_OFFLINE`, objetivo específico y tercera pestaña de terminal. Esta conecta la desconexión del servidor (07/02, 06:54), las órdenes STOPPAY del 08/02 (17:05–17:12, hora de Bangladesh), la reconexión (09/02, 09:11) y las cinco transferencias posteriores.

### Entrega de efectivo

El texto, animación y resultado describen la caja como una entrega a la sucursal para su recogida posterior. En DEFENSA, el control de custodia puede retener la caja después de que llegue; no se falsea el evento diciendo que la entrega nunca ocurrió. Se aclara que la hora de juego 15:30 es ilustrativa, ya que la fuente sitúa el evento antes de las 19:00 pero no fija ese minuto.

### Personajes / afirmaciones legales

Se corrigió el epílogo para no atribuir a Marta Vidal, personaje ficticio, la condena de una persona real. También se separan los cambios normativos posteriores a 2016 del marco vigente durante los eventos representados.

### Errores del flujo de configuración/build

El bootstrap ahora informa fallos de cada paso y no guarda el marcador de instalación si un paso crítico falló o la escena no se generó. El script de build comprueba esos resultados antes de llamar a `BuildPipeline.BuildPlayer`, aborta si acaba de cambiar Active Input Handling y el Editor necesita reiniciarse, y los APK requieren además confirmación del loader Cardboard.

## 6. UI adaptable

- HUD configurado con `CanvasScaler` y resolución de referencia 1280 × 720.
- Recalcula anclas usando `Screen.safeArea` y responde a cambios de ancho/alto.
- El HUD compacto escala objetivos y reloj según el aspecto de la ventana/pantalla.
- Los paneles `WorldPanel` calculan su tamaño relativo usando distancia, FOV y aspect ratio de la cámara.
- Los botones del panel 3D reducen tamaño tipográfico para ajustarse a sus cajas. El puntero de mirada usa `Physics.RaycastNonAlloc` con un buffer reutilizable y ordenación manual para evitar asignaciones por frame; los seis ambientes se precargan durante el arranque para reducir cambios de zona con tirones.

La adaptación es de diseño y código. Debe probarse en dispositivos reales y resoluciones muy estrechas; no puedo certificar legibilidad en cada visor sin ejecutar la escena.

## 7. Guía hablada y ambiente auditivo

- Nuevo `AudioGuide`: Android usa TextToSpeech del sistema en español; Windows usa SAPI cuando está disponible; subtítulos y señal sonora quedan como alternativa.
- Los objetivos, títulos de capítulo, pistas y zonas de teletransporte invocan mensajes hablados/subtitulados.
- Menú **Audio y accesibilidad** ofrece volumen maestro, silencio, narración y subtítulos.
- `AudioManager.SetEnvironment` conmuta con fundido cinco capas procedurales: vestíbulo, ventanilla, bóveda, seguridad y oficina. En seguridad añade estática CCTV.
- `Synth` incorpora el timbre de la guía y los clips ambientales sintetizados.

No son locuciones humanas pregrabadas. La voz y su calidad dependen de las voces instaladas en Android/Windows; falta una prueba auditiva en hardware.

## 8. Matriz de validación

| Nivel | Estado |
|---|---|
| Lectura de inventario y estructura ZIP | Realizado |
| Configuración y JSON de paquetes | Realizado por inspección/validación estática |
| Revisión del flujo histórico y aritmética de cinco transferencias | Realizado |
| Revisión estática de lógica UI/audio/build y referencias de versión | Realizado |
| Compilación de scripts C# contra Unity 6.5 assemblies | Pendiente: Unity Editor no disponible en este entorno |
| Importación completa del proyecto con Package Manager | Pendiente |
| Ejecución Play Mode en ATAQUE y DEFENSA | Pendiente |
| Build PC Windows/Linux/macOS | Pendiente |
| Build Android IL2CPP/ARM64 y validación Google Cardboard | Pendiente |
| Prueba de TTS, subtítulos, audio ambiente y FPS en dispositivo | Pendiente |

## 9. Procedimiento de validación final

1. Abrir con `6000.5.2f1`, esperar importación y revisar errores rojos en Console.
2. Ejecutar `Las Palmeras → 1 · Configurar proyecto`; validar `Agencia_VR.unity` y estado del loader Cardboard.
3. Probar las dos rutas completas: fichas, CCTV, terminal (Cuentas, Retiros y SWIFT), caja, reglas A/B/C, controles y resultado.
4. Probar resizing en 16:9, 16:10, 4:3 y ventana estrecha, y texto en distancia de uso real de Cardboard.
5. Compilar build Windows y APK Android; instalar APK y probar QR, tracking, controles, sonido, TTS, calor y FPS.

**No marcar la entrega como “compilación confirmada” hasta completar esas pruebas.** El estado actual es “migración y correcciones estáticas preparadas para validación en Unity 6.5”.


## Incidencia Input System / Unity 6.5

El manifiesto original de la entrega migrada fijaba `com.unity.inputsystem` en `1.14.2`. En Unity 6.5, APIs antiguas basadas en `InstanceID` pasan a producir `CS0619`; los mensajes reportados desde `InputSystem.cs`, `InputActionEditorWindow.cs`, `PlayerInputEditor.cs`, `InputActionImporter.cs` y los proveedores de búsqueda procedían del código Editor de esa versión del paquete. Unity/Input System corrigió las incompatibilidades de Unity 6.4/6.5 en Input System `1.18.0`; para esta revisión se fija `1.20.1`, versión publicada para Unity Editor `6000.5`.

La excepción `TypeLoadException` de `InputActionAsset` se considera inicialmente una consecuencia de que la assembly `Unity.InputSystem` no llegó a compilar/importar correctamente tras los errores CS0619. Se incluye `Tools/Repair_PackageManager_Unity65.ps1` para alinear los manifiestos y conservar el lockfile y las cachés del proyecto. No se afirma que el proyecto compile realmente hasta que el Editor `6000.5.2f1` resuelva el paquete y finalice la compilación local.

## Incidencia posterior: uGUI y resolución offline

En la entrega anterior, el manifiesto todavía pedía uGUI `2.0.0`, mientras que la línea de Unity 6000.5 actualizó uGUI a `2.5.0`. Esta versión declara además el módulo integrado `physics2d`. La entrega revisada fija `com.unity.ugui` en `2.5.0` y añade el assembly definition `LasPalmeras.Runtime.asmdef` con referencia explícita a `UnityEngine.UI`, además de Input System, XR Management y Cardboard. Los scripts `UiButton3D`, `UIManager` y `WorldPanel` mantienen los tipos correctos `Image` y `Text` del namespace `UnityEngine.UI`.

Se identificó que una versión previa del reparador eliminaba `Library/PackageCache`, `Library/PackageManager/ProjectCache` y `packages-lock.json`; esa estrategia podía empeorar un primer arranque sin conexión. El preflight actual conserva `Library/PackageCache`, `Library/PackageManager/ProjectCache` y `LocalPackages`. Como cambia el origen de los cuatro paquetes externos de Registry a tarball local, respalda el `packages-lock.json` anterior con un nombre fechado y retira solo el lockfile activo para que UPM genere uno nuevo con `source: local`. Si la descarga inicial no termina, no modifica el manifiesto ni el lockfile activo. Si la PC está totalmente sin conexión y los tarballs aún no existen, la descarga no puede completarse; hay que conseguirlos mediante una conexión HTTPS/443 o copiarlos previamente a `LocalPackages/`.

Se añadió `Assets/Editor/CompileValidation.cs` y `Tools/Verify_Compile_Unity65.ps1` para realizar una verificación real desde Unity batchmode. Esta verificación todavía debe ejecutarse en el equipo que tenga Unity 6000.5.2f1; el entorno de preparación no dispone del Editor Unity.


## Anexo de preflight de dependencias (16-11-2026)

La ruta recomendada ya no es abrir el proyecto directamente desde Unity Hub. Ejecutar `Tools/Preparar_y_Abrir_Unity65.bat` desde Windows: baja los paquetes UPM externos desde `download.packages.unity.com`, valida sus SHA-1, actualiza las dependencias a referencias `file:` en `LocalPackages/` y luego inicia Unity `6000.5.2f1` en batchmode. El Editor gráfico se abre solo después de resolver `packages-lock.json` y pasar `CompileValidation.Run`.

Paquetes descargados por el preflight: Input System 1.20.1, XR Plug-in Management 4.6.1, XR Legacy Input Helpers 3.0.1 y XR Core Utilities 2.6.0. uGUI 2.5.0 es un Core package distribuido con el Editor; se mantiene declarado a esa versión y se verifica dentro de Unity cargando `UnityEngine.UI.Image` y `UnityEngine.UI.Text`. Se agregan los módulos integrados `uielements`, `xr`, `subsystems` y `ui`.

La PC requiere conexión HTTPS/443 al CDN oficial en la primera ejecución. En el entorno de empaquetado no fue posible descargar los `.tgz` por una limitación de resolución de DNS; por eso los tarballs no se incluyen en el ZIP y el BAT los descarga en el equipo del usuario. Una vez presentes, quedan en `LocalPackages/` para ejecuciones posteriores sin volver a descargar. La ventana de catálogo Unity Registry puede seguir mostrando un error de lista offline si se abre sin conexión, pero eso es distinto de la resolución de las dependencias locales.

**Estado:** comprobaciones estáticas repetidas y aprobadas; compilación real y resolución UPM quedan por ejecutar en Unity del equipo del usuario. El preflight detiene el proceso y no abre la GUI si no obtiene el marcador `LAS_PALMERAS_COMPILE_VALIDATION_PASSED`.


### Ajuste XR Core Utilities para Unity 6.5

La revisión del changelog de XR Core Utilities encontró que `2.5.3` precedía al cambio que evita el uso del API int-based `EditorUtility.InstanceIdToObject` en Unity 6.5. Por esa razón, el preflight fija `com.unity.xr.core-utils` en `2.6.0`, versión estable que incorpora el cambio anunciado en `2.6.0-pre.2`. El descargador verifica el SHA-1 `a8b900321199038fe4200211a52f46d2f4bae786`. Esta actualización evita dejar activa una versión XR anterior con posible incompatibilidad de Editor, pero aún necesita validación real de compilación en Unity 6000.5.2f1.
