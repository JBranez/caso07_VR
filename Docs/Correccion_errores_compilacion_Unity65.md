# Corrección de errores C# en Unity 6.5

**Editor objetivo:** Unity 6000.5.2f1.

## Errores corregidos

### `GazePointer.cs` — CS0103: `point` no existe en el contexto actual

Dos ramas del bucle de raycast asignaban `point = h.point`, pero no había una variable `point` declarada y el valor tampoco se utilizaba después. Se eliminaron ambas asignaciones muertas. La selección del `Interactable`, la distancia del impacto y el cálculo de la retícula siguen basándose en `RaycastHit.distance`, como antes.

### `Mats.cs` — CS0619: `Object.GetInstanceID()`

Unity 6.5 retiró la API entera basada en `InstanceID` y exige `GetEntityId()`. La caché de materiales ya no concatena un identificador antiguo en una cadena. Ahora usa una clave tipada `MaterialKey : IEquatable<MaterialKey>` que contiene un `EntityId` para la textura y compara los campos de la clave. Esto evita truncar `EntityId` a `int` o utilizar `ToString()` como identificador persistente. La clave vive únicamente en memoria durante la sesión.

## Mensaje del Editor: `Maximized serialized file backup not found`

El stack trace mostrado procede de `UnityEditor.GUIView`, `DockArea` y `MaximizedHostView`. No señala una línea de `Assets/Scripts` ni es un error C# del proyecto. Es compatible con un estado de layout/ventana maximizada del Editor incompleto o dañado.

Pasos recomendados, con Unity abierto: `Window > Layouts > Default` (o `Reset All Layouts`, si sigue ocurriendo). Si persiste, cierra Unity y ejecuta `Tools/Reset_Editor_Layout_Unity65.ps1`. El script respalda primero, sin borrar, estos archivos si existen: `Library/CurrentLayout-default.dwlt`, `UserSettings/Layouts/CurrentMaximizeLayout.dwlt` y `UserSettings/Layouts/CurrentLayout-default.dwlt`. No elimina toda la carpeta `Library` ni toca `Assets`. Unity recreará el estado del Editor al abrir el proyecto.

## Validaciones realizadas en este paquete

- El validador estático se ejecuta y comprueba las dos regresiones reportadas.
- No quedan llamadas `GetInstanceID()` en los scripts de `Assets`.
- Se verificó que las claves de caché usan `EntityId` tipado.
- Se verificó la integridad del ZIP y la estructura básica de los 38 scripts C#.

**No se ejecutó Unity Editor ni un compilador C# en el entorno de preparación.** Por tanto, esta entrega no se presenta como una compilación real completada. El preflight del proyecto sigue preparado para ejecutar `CompileValidation.Run` en batchmode con el Editor instalado.

## Referencias técnicas

- Unity 6000.5: `Object.GetEntityId()` — https://docs.unity.com/en-us/engine/6000.5/script-reference/unityengine/object/getentityid
- Unity 6000.5: migración de `InstanceID` a `EntityId` — https://docs.unity.com/en-us/engine/6000.5/manual/scripting/fundamental-unity-types/instanceid-to-entityid-migration
- Unity Editor `WindowLayout.cs` (rutas y reset de layouts) — https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/GUI/WindowLayout.cs
- Unity Issue Tracker: recuperación de layouts de ventana dañados — https://issuetracker-mig.prd.it.unity3d.com/issues/window-layout-cannot-load-editor-layout-stuck-in-infinite-loop-of-failed-to-load-window-layout
