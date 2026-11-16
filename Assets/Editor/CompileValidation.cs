#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LasPalmeras.EditorTools
{
    /// <summary>Batch-mode gate: Unity must resolve packages and compile every assembly before this can run.</summary>
    public static class CompileValidation
    {
        const string SuccessMarker = "LAS_PALMERAS_COMPILE_VALIDATION_PASSED";

        [MenuItem("Las Palmeras/Diagnóstico/Verificar compilación y referencias")]
        public static void Run()
        {
            try
            {
                Assembly ui = RequireAssembly("UnityEngine.UI");
                RequireType(ui, "UnityEngine.UI.Image");
                RequireType(ui, "UnityEngine.UI.Text");
                Require(typeof(Image));
                Require(typeof(Text));

                Assembly input = RequireAssembly("Unity.InputSystem");
                RequireType(input, "UnityEngine.InputSystem.InputActionAsset");
                Assembly xr = RequireAssembly("Unity.XR.Management");
                Assembly cardboard = RequireAssembly("Google.XR.Cardboard");

                Assembly runtime = typeof(LasPalmeras.UIManager).Assembly;
                if (!string.Equals(runtime.GetName().Name, "LasPalmeras.Runtime", StringComparison.Ordinal))
                    throw new InvalidOperationException("UIManager no está dentro de LasPalmeras.Runtime.");
                RequireType(runtime, "LasPalmeras.UIManager");
                RequireType(runtime, "LasPalmeras.WorldPanel");
                RequireType(runtime, "LasPalmeras.UiButton3D");

                // The XR Management code in VrMode is guarded by UNITY_ANDROID && !UNITY_EDITOR.
                // Therefore the Editor compiler may omit the metadata AssemblyRef even when the
                // asmdef correctly declares the dependency. Validate the asmdef source itself.
                ValidateAsmdefReferences("Assets/Scripts/LasPalmeras.Runtime.asmdef",
                    new[] { "UnityEngine.UI", "Unity.InputSystem", "Unity.XR.Management", "Google.XR.Cardboard" });
                RequireType(xr, "UnityEngine.XR.Management.XRGeneralSettings");
                RequireType(cardboard, "Google.XR.Cardboard.Api");
                ValidateScene();

                Debug.Log(SuccessMarker + " | uGUI Image/Text, InputActionAsset, XR Management, Cardboard, scripts UI y escena Agencia_VR compilados/importados.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError("LAS_PALMERAS_COMPILE_VALIDATION_FAILED | " + ex);
                if (Application.isBatchMode) EditorApplication.Exit(20);
            }
        }

        [Serializable]
        public sealed class AssemblyDefinitionInfo
        {
            public string name;
            public string[] references;
        }

        static void ValidateAsmdefReferences(string path, string[] required)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("No existe el asmdef runtime: " + path);
            AssemblyDefinitionInfo info = JsonUtility.FromJson<AssemblyDefinitionInfo>(File.ReadAllText(path));
            if (info == null || string.IsNullOrEmpty(info.name) || info.references == null)
                throw new InvalidDataException("El asmdef no contiene name/references válidos: " + path);
            foreach (string reference in required)
            {
                if (!info.references.Contains(reference, StringComparer.Ordinal))
                    throw new InvalidOperationException(path + " no declara la referencia " + reference + ".");
            }
        }

        static void ValidateScene()
        {
            const string path = "Assets/Scenes/Agencia_VR.unity";
            if (!File.Exists(path)) throw new FileNotFoundException("No existe la escena precreada: " + path);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                throw new InvalidDataException("Unity no pudo importar " + path + ".");

            const string scriptPath = "Assets/Scripts/Core/GameBootstrap.cs";
            MonoScript entryScript = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
            if (entryScript == null || entryScript.GetClass() != typeof(LasPalmeras.GameBootstrap))
                throw new InvalidOperationException("El script GameBootstrap no se pudo resolver como MonoBehaviour: " + scriptPath);
            string scriptGuid = AssetDatabase.AssetPathToGUID(scriptPath);
            string serializedScene = File.ReadAllText(path);
            if (string.IsNullOrEmpty(scriptGuid) || !serializedScene.Contains("guid: " + scriptGuid))
                throw new InvalidOperationException("Agencia_VR.unity no contiene una referencia válida al componente GameBootstrap.");

            if (!EditorBuildSettings.scenes.Any(s => s.enabled && string.Equals(s.path, path, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Agencia_VR.unity no está incluida en Build Settings.");
        }

        static void Require(Type type)
        {
            if (type == null) throw new TypeLoadException("No se pudo cargar un tipo requerido.");
        }

        static Assembly RequireAssembly(string name)
        {
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => string.Equals(a.GetName().Name, name, StringComparison.Ordinal));
            if (assembly == null) assembly = Assembly.Load(new AssemblyName(name));
            if (assembly == null) throw new FileNotFoundException("No se pudo cargar el assembly " + name + ".");
            return assembly;
        }

        static void RequireType(Assembly assembly, string fullName)
        {
            if (assembly.GetType(fullName, false) == null)
                throw new TypeLoadException("No se encontró " + fullName + " en " + assembly.GetName().Name + ".");
        }
    }
}
#endif
