// Compilaciones: PC (Windows/Linux/macOS) y Android (Cardboard). También sirven por línea de comandos:
//   Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod LasPalmeras.EditorTools.BuildScripts.BuildAndroid
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LasPalmeras.EditorTools
{
    public static class BuildScripts
    {
        [MenuItem("Las Palmeras/Compilar/PC · Windows 64-bit")] public static void MenuWindows() { Build(BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone, "Builds/Windows/LasPalmeras.exe"); }
        [MenuItem("Las Palmeras/Compilar/PC · Linux 64-bit")] public static void MenuLinux() { Build(BuildTarget.StandaloneLinux64, BuildTargetGroup.Standalone, "Builds/Linux/LasPalmeras.x86_64"); }
        [MenuItem("Las Palmeras/Compilar/PC · macOS")] public static void MenuMac() { Build(BuildTarget.StandaloneOSX, BuildTargetGroup.Standalone, "Builds/macOS/LasPalmeras.app"); }
        [MenuItem("Las Palmeras/Compilar/Android · APK Cardboard (VR)")] public static void MenuAndroid() { Build(BuildTarget.Android, BuildTargetGroup.Android, "Builds/Android/LasPalmeras_Cardboard.apk"); }

        // Métodos para línea de comandos
        public static void BuildWindows() { Build(BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone, "Builds/Windows/LasPalmeras.exe"); }
        public static void BuildLinux() { Build(BuildTarget.StandaloneLinux64, BuildTargetGroup.Standalone, "Builds/Linux/LasPalmeras.x86_64"); }
        public static void BuildMac() { Build(BuildTarget.StandaloneOSX, BuildTargetGroup.Standalone, "Builds/macOS/LasPalmeras.app"); }
        public static void BuildAndroid() { Build(BuildTarget.Android, BuildTargetGroup.Android, "Builds/Android/LasPalmeras_Cardboard.apk"); }

        static void Build(BuildTarget target, BuildTargetGroup group, string output)
        {
            ProjectBootstrap.RunAll(false);
            if (ProjectBootstrap.LastRunRequiresRestart)
            {
                const string msg = "Unity cambió Active Input Handling y necesita reiniciarse para aplicar el ajuste. Cierra y vuelve a abrir el Editor antes de compilar.";
                Debug.LogError("[ANDES-SIM] " + msg);
                if (!Application.isBatchMode) EditorUtility.DisplayDialog("ANDES-SIM", msg, "OK");
                else EditorApplication.Exit(5);
                return;
            }
            if (!ProjectBootstrap.LastRunSucceeded || !File.Exists(ProjectBootstrap.ScenePath))
            {
                const string msg = "La configuración del proyecto no terminó correctamente o falta la escena Agencia_VR. No se iniciará la compilación.";
                Debug.LogError("[ANDES-SIM] " + msg);
                if (!Application.isBatchMode) EditorUtility.DisplayDialog("ANDES-SIM", msg, "OK");
                else EditorApplication.Exit(3);
                return;
            }
            if (target == BuildTarget.Android && (ProjectBootstrap.LastXrStatus == null || !ProjectBootstrap.LastXrStatus.Contains("activado")))
            {
                string msg = "Cardboard XR Loader no quedó confirmado para Android. " + ProjectBootstrap.LastXrStatus + " Revisa Project Settings > XR Plug-in Management > Android.";
                Debug.LogError("[ANDES-SIM] " + msg);
                if (!Application.isBatchMode) EditorUtility.DisplayDialog("ANDES-SIM", msg, "OK");
                else EditorApplication.Exit(4);
                return;
            }
            if (!BuildPipeline.IsBuildTargetSupported(group, target))
            {
                string msg = "Falta el módulo de compilación para " + target + ". Instálalo desde Unity Hub ▸ Installs ▸ Add modules.";
                Debug.LogError("[ANDES-SIM] " + msg);
                if (!Application.isBatchMode) EditorUtility.DisplayDialog("ANDES-SIM", msg, "OK"); else EditorApplication.Exit(2);
                return;
            }
            if (target == BuildTarget.Android && EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android && !Application.isBatchMode)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                EditorUtility.DisplayDialog("ANDES-SIM", "Se cambió la plataforma activa a Android. Cuando Unity termine de recompilar, vuelve a elegir: Las Palmeras ▸ Compilar ▸ Android.", "OK");
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ProjectBootstrap.ScenePath },
                locationPathName = output,
                target = target,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts);
            bool ok = report != null && report.summary.result == BuildResult.Succeeded;
            string buildResult = report != null ? report.summary.result.ToString() : "sin informe de BuildPipeline";
            Debug.Log("[ANDES-SIM] Compilación " + target + ": " + buildResult + " → " + output);
            if (!Application.isBatchMode)
            {
                if (ok) EditorUtility.RevealInFinder(output);
                else EditorUtility.DisplayDialog("ANDES-SIM", "La compilación falló. Revisa la consola.", "OK");
            }
            else EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
