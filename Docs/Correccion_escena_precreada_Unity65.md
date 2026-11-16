# Corrección: escena precreada y validación de ensamblados — Unity 6.5

## Cambios realizados

1. Se añadió `Assets/Scenes/Agencia_VR.unity` como escena serializada real, con un objeto de entrada `ANDES-SIM Scene Entry` y el componente `GameBootstrap`.
2. Se añadió `Assets/Scripts/Core/GameBootstrap.cs` como archivo independiente cuyo nombre coincide con la clase MonoBehaviour serializada.
3. `ProjectSettings/EditorBuildSettings.asset` incluye `Agencia_VR.unity` como primera escena habilitada.
4. `ProjectBootstrap` ya no llama a `EditorSceneManager.NewScene`, no crea escenas aditivas y no crea escenas durante el arranque. Solo valida/importa la escena preexistente y la conserva en Build Settings. Si el Editor arranca con una escena `Untitled` sin guardar, abre el asset preexistente en modo Single.
5. `OpenScene` abre el asset existente en modo Single; si falta, muestra un error de restauración en lugar de intentar generarlo.
6. Se corrigió `CompileValidation`: el chequeo previo de `Assembly.GetReferencedAssemblies()` era incorrecto para las referencias XR guardadas bajo `#if UNITY_ANDROID && !UNITY_EDITOR`, porque esas referencias pueden no aparecer en la metadata de la compilación del Editor. Ahora se comprueba el contenido real del `.asmdef`, la carga de los assemblies y los tipos XR.
7. La validación batch comprueba que `Agencia_VR.unity` se puede importar y está habilitada en Build Settings.

## Flujo de escena

La escena `.unity` existe antes de abrir Unity. `GameBootstrap` es el entry point serializado. Al entrar en Play, `Bootstrap`/`GameManager` inicia la simulación. `AgencyBuilder` crea el escenario tridimensional en runtime; esto es la arquitectura procedural del juego, no una generación de la escena de Editor.

## Pruebas realizadas en este entorno

- JSON de `LasPalmeras.Runtime.asmdef` válido y referencias esperadas presentes.
- Escena, Scene Meta, script Meta y GUID del Build Settings coinciden.
- Scene YAML contiene documentos Unity esperados y referencia a `GameBootstrap`.
- `ProjectBootstrap.cs` no contiene `NewScene` ni `NewSceneMode.Additive`.
- `CompileValidation.cs` valida referencias desde el `.asmdef`, y escena + Build Settings.
- Integridad del ZIP comprobada.

No se afirma compilación real de Unity: este entorno no dispone de Unity Editor ni de compilador C#. La compilación batch debe ejecutarse en el equipo del usuario mediante `Abrir_Proyecto_Unity65.bat`.
