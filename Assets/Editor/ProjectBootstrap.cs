// Configura los ajustes del proyecto al abrir Unity 6.5. La escena ya viene guardada en Assets/Scenes:
//  • Player Settings de la guía de Cardboard (IL2CPP, ARM64, API 26, OpenGLES3, Landscape Left, Activity).
//  • Capa «Interactive», assets en Resources y validación de la escena Agencia_VR ya guardada.
//  • XR Plug-in Management: loader «Cardboard XR Plugin» para Android.
// Menú: Las Palmeras ▸ 1 · Configurar proyecto (todo)
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LasPalmeras.EditorTools
{
    [InitializeOnLoad]
    public static class ProjectBootstrap
    {
        public const string ScenePath = "Assets/Scenes/Agencia_VR.unity";
        const string Marker = "ProjectSettings/LasPalmeras.setup";
        const string Version = "v3-unity65-precreated-scene";
        const string SessionFlag = "LasPalmeras.Bootstrap.Session";
        public static bool LastRunSucceeded { get; private set; } = true;
        public static bool LastRunRequiresRestart { get; private set; }
        public static string LastXrStatus { get; private set; } = "XR aún no configurado";

        static ProjectBootstrap() { EditorApplication.delayCall += AutoRun; }

        static void AutoRun()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (SessionState.GetBool(SessionFlag, false)) return;
            SessionState.SetBool(SessionFlag, true);
            if (NeedsSetup()) RunAll(false);
            if (!Application.isBatchMode && LastRunSucceeded && !LastRunRequiresRestart)
                EditorApplication.delayCall += OpenMainSceneIfUntitled;
        }

        static void OpenMainSceneIfUntitled()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var active = EditorSceneManager.GetActiveScene();
            if (active.IsValid() && !string.IsNullOrEmpty(active.path)) return;
            if (!File.Exists(ScenePath) || AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) return;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Debug.Log("[ANDES-SIM] Escena precreada Agencia_VR abierta. No se generó ninguna escena temporal.");
            }
        }

        static bool NeedsSetup()
        {
            return !File.Exists(Marker) || File.ReadAllText(Marker).Trim() != Version || !File.Exists(ScenePath);
        }

        [MenuItem("Las Palmeras/1 · Configurar proyecto (todo)")]
        public static void RunAllMenu() { RunAll(true); }

        [MenuItem("Las Palmeras/2 · Abrir escena Agencia_VR")]
        public static void OpenScene()
        {
            AssetDatabase.Refresh();
            if (!File.Exists(ScenePath) || AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                Debug.LogError("[ANDES-SIM] La escena guardada no está presente o no se pudo importar: " + ScenePath + ". Restaura Assets/Scenes/Agencia_VR.unity desde el proyecto; esta versión no crea escenas automáticamente.");
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        public static void RunAll(bool verbose)
        {
            bool restart = false;
            bool success = true;
            success &= Step("Capas", SetupLayers);
            success &= Step("Player Settings", () => restart = SetupPlayerSettings());
            success &= Step("Recursos (ScenarioData + shader Standard)", CreateResourceAssets);
            success &= Step("Escena Agencia_VR (preexistente)", ValidateExistingScene);
            string xr = "";
            success &= Step("XR Plug-in Management (Cardboard)", () => xr = XRSetup.Run());
            LastXrStatus = xr;
            if (!File.Exists(ScenePath) || AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                Debug.LogError("[ANDES-SIM] Falta la escena guardada requerida: " + ScenePath);
                success = false;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            LastRunSucceeded = success;
            LastRunRequiresRestart = restart;
            if (success) File.WriteAllText(Marker, Version);
            else Debug.LogError("[ANDES-SIM] Configuración incompleta. No se guardó el marcador de instalación; corrige los errores y vuelve a ejecutar Las Palmeras ▸ 1 · Configurar proyecto.");
            Debug.Log("[ANDES-SIM] Configuración " + (success ? "completada" : "INCOMPLETA") + ". " + xr);
            if (restart && !Application.isBatchMode)
            {
                if (EditorUtility.DisplayDialog("ANDES-SIM · Las Palmeras",
                    "Se activó «Active Input Handling = Both» (lo exige el plugin Cardboard).\nUnity debe reiniciarse para aplicarlo.", "Reiniciar ahora", "Más tarde"))
                    EditorApplication.OpenProject(Directory.GetCurrentDirectory());
            }
            else if (verbose && !Application.isBatchMode)
                EditorUtility.DisplayDialog("ANDES-SIM · Las Palmeras", (success ? "Proyecto configurado." : "La configuración quedó incompleta; revisa la Consola.") + "\n\n" + xr + "\n\nAbre la escena con: Las Palmeras ▸ 2 · Abrir escena Agencia_VR y pulsa Play.", "OK");
        }

        static bool Step(string name, Action a)
        {
            try { a(); return true; }
            catch (Exception e)
            {
                Debug.LogError("[ANDES-SIM] Paso «" + name + "» falló: " + e.Message + "\n" + e.StackTrace);
                return false;
            }
        }

        // ---------------------------------------------------------------- capas
        static void SetupLayers()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            var layers = so.FindProperty("layers");
            if (layers == null || layers.arraySize <= 8) return;
            var l8 = layers.GetArrayElementAtIndex(8);
            if (string.IsNullOrEmpty(l8.stringValue)) { l8.stringValue = "Interactive"; so.ApplyModifiedProperties(); }
        }

        // ---------------------------------------------------------------- Player Settings
        static void Try(string what, Action a)
        {
            try { a(); }
            catch (Exception e) { Debug.LogWarning("[ANDES-SIM] Ajuste «" + what + "» no se pudo aplicar: " + e.Message + " (revísalo a mano en Project Settings)."); }
        }

        static bool SetupPlayerSettings()
        {
            Try("datos del producto", () =>
            {
                PlayerSettings.companyName = "ANDES-SIM";
                PlayerSettings.productName = "Las Palmeras VR";
                PlayerSettings.bundleVersion = "1.0.0";
                PlayerSettings.colorSpace = ColorSpace.Gamma;   // los colores del proyecto están pensados en gamma (móvil)
            });

            Try("escritorio", () =>
            {
                PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
                PlayerSettings.defaultScreenWidth = 1600;
                PlayerSettings.defaultScreenHeight = 900;
                PlayerSettings.resizableWindow = true;
                PlayerSettings.runInBackground = true;
            });

            // Android / Cardboard (guía de VR de la Semana 10)
            Try("Android · identificador y backend", () =>
            {
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.andessim.laspalmeras");
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);
            });
            Try("Android · arquitectura y API", () =>
            {
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
                PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            });
            Try("Android · gráficos OpenGLES3", () =>
            {
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            });
            Try("Android · orientación y frame pacing", () =>
            {
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
                PlayerSettings.Android.optimizedFramePacing = false;
            });

            // «Application Entry Point: Activity» (el plugin Cardboard no funciona con GameActivity)
            Try("Android · Application Entry Point = Activity", () =>
            {
                var prop = typeof(PlayerSettings.Android).GetProperty("applicationEntry");
                if (prop != null) prop.SetValue(null, Enum.ToObject(prop.PropertyType, 1), null);
            });

            // Active Input Handling = Both (new + old); Cardboard usa el Input System nuevo
            bool restart = false;
            Try("Active Input Handling = Both", () =>
            {
                var ps = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
                if (ps != null && ps.Length > 0)
                {
                    var so = new SerializedObject(ps[0]);
                    var p = so.FindProperty("activeInputHandler");
                    if (p != null && p.intValue != 2) { p.intValue = 2; so.ApplyModifiedProperties(); restart = true; }
                }
            });
            return restart;
        }

        // ---------------------------------------------------------------- recursos
        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static void CreateResourceAssets()
        {
            EnsureFolder("Assets/Resources/Materials");
            // Un material que usa «Standard» dentro de Resources obliga a Unity a incluir ese shader en el build
            // (los materiales del juego se crean por código y, si no, el shader se eliminaría).
            const string mat = "Assets/Resources/Materials/StandardRef.mat";
            if (!File.Exists(mat))
            {
                var sh = Shader.Find("Standard");
                if (sh != null) AssetDatabase.CreateAsset(new Material(sh), mat);
            }
            const string sd = "Assets/Resources/ScenarioData_LasPalmeras.asset";
            if (!File.Exists(sd))
            {
                var d = ScenarioData.CreateDefault();
                AssetDatabase.CreateAsset(d, sd);
            }
        }

        // ---------------------------------------------------------------- escena ya guardada en el repositorio
        // Esta versión NO llama a NewScene ni crea escenas additivas. Solo valida e incluye
        // la escena serializada Assets/Scenes/Agencia_VR.unity en Build Settings.
        static void ValidateExistingScene()
        {
            if (!File.Exists(ScenePath))
                throw new FileNotFoundException("No se encontró la escena precreada " + ScenePath + ". Restaura el archivo desde el ZIP del proyecto.");

            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
                throw new InvalidDataException("Unity no pudo importar la escena serializada " + ScenePath + ". Comprueba su YAML y su archivo .meta.");

            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int index = scenes.FindIndex(s => string.Equals(s.path, ScenePath, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
                scenes[index] = new EditorBuildSettingsScene(ScenePath, true);
            else
                scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
