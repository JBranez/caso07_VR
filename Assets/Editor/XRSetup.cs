// Activa el loader «Cardboard XR Plugin» en XR Plug-in Management para Android.
// Equivale a: Edit ▸ Project Settings ▸ XR Plug-in Management ▸ Android ▸ ☑ Cardboard XR Plugin.
// Se usa reflexión para que el proyecto compile aunque cambie la API interna de XR Management.
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LasPalmeras.EditorTools
{
    public static class XRSetup
    {
        const string SettingsKey = "com.unity.xr.management.loader_settings";
        const string LoaderType = "Google.XR.Cardboard.XRLoader";

        [MenuItem("Las Palmeras/Reparar · activar Cardboard en XR Management")]
        public static void Menu() { Debug.Log("[ANDES-SIM] " + Run()); }

        static Type FindType(string full)
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = a.GetType(full, false);
                if (t != null) return t;
            }
            return null;
        }

        static object Call(object target, Type type, string method, params object[] args)
        {
            var m = type.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (m == null) throw new MissingMethodException(type.Name + "." + method);
            return m.Invoke(target, args);
        }

        public static string Run()
        {
            var tPer = FindType("UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget");
            var tGen = FindType("UnityEngine.XR.Management.XRGeneralSettings");
            var tMgr = FindType("UnityEngine.XR.Management.XRManagerSettings");
            var tStore = FindType("UnityEditor.XR.Management.Metadata.XRPackageMetadataStore");
            if (tPer == null || tGen == null || tMgr == null || tStore == null)
                return "XR Management aún no está cargado. Cuando Unity termine de importar los paquetes, ejecuta: Las Palmeras ▸ Reparar · activar Cardboard en XR Management.";
            try
            {
                if (!AssetDatabase.IsValidFolder("Assets/XR")) AssetDatabase.CreateFolder("Assets", "XR");
                UnityEngine.Object per = null;
                EditorBuildSettings.TryGetConfigObject(SettingsKey, out per);
                if (per == null)
                {
                    per = ScriptableObject.CreateInstance(tPer);
                    AssetDatabase.CreateAsset(per, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                    EditorBuildSettings.AddConfigObject(SettingsKey, per, true);
                }
                var group = BuildTargetGroup.Android;
                var general = Call(per, tPer, "SettingsForBuildTarget", group) as UnityEngine.Object;
                if (general == null)
                {
                    general = ScriptableObject.CreateInstance(tGen);
                    AssetDatabase.AddObjectToAsset(general, per);
                    Call(per, tPer, "SetSettingsForBuildTarget", group, general);
                    var mgr = ScriptableObject.CreateInstance(tMgr);
                    AssetDatabase.AddObjectToAsset(mgr, per);
                    tGen.GetProperty("Manager").SetValue(general, mgr, null);
                }
                var manager = tGen.GetProperty("Manager").GetValue(general, null);
                var init = tGen.GetProperty("InitManagerOnStart");
                if (init != null) init.SetValue(general, true, null);
                EditorUtility.SetDirty(per); EditorUtility.SetDirty(general);
                object ok = Call(null, tStore, "AssignLoader", manager, LoaderType, group);
                AssetDatabase.SaveAssets();
                return (ok is bool && (bool)ok)
                    ? "Cardboard XR Plugin activado para Android (XR Plug-in Management)."
                    : "No se pudo asignar el loader automáticamente: ve a Edit ▸ Project Settings ▸ XR Plug-in Management ▸ Android y marca «Cardboard XR Plugin».";
            }
            catch (Exception e)
            {
                return "Activa Cardboard a mano: Edit ▸ Project Settings ▸ XR Plug-in Management ▸ Android ▸ «Cardboard XR Plugin». (" + (e.InnerException != null ? e.InnerException.Message : e.Message) + ")";
            }
        }
    }
}
