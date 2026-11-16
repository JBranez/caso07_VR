// Reproduce el paso «Custom Main Gradle Template» de la guía de Cardboard, pero automáticamente al compilar:
// añade las dependencias de AndroidX/Material/Play Services Vision/Protobuf y activa AndroidX + Jetifier.
// Solo existe cuando el módulo de Android está instalado (UNITY_ANDROID).
#if UNITY_ANDROID
using System.IO;
using UnityEditor.Android;
using UnityEngine;

namespace LasPalmeras.EditorTools
{
    public class AndroidGradlePostprocess : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder { get { return 999; } }

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string gradle = Path.Combine(path, "build.gradle");
            if (File.Exists(gradle))
            {
                string text = File.ReadAllText(gradle);
                if (!text.Contains("androidx.appcompat:appcompat"))
                {
                    const string key = "dependencies {";
                    int i = text.IndexOf(key);
                    if (i >= 0)
                    {
                        string deps =
                            "\n    implementation 'androidx.appcompat:appcompat:1.6.1'" +
                            "\n    implementation 'com.google.android.gms:play-services-vision:20.1.3'" +
                            "\n    implementation 'com.google.android.material:material:1.12.0'" +
                            "\n    implementation 'com.google.protobuf:protobuf-javalite:3.19.4'\n";
                        text = text.Insert(i + key.Length, deps);
                        File.WriteAllText(gradle, text);
                        Debug.Log("[ANDES-SIM] Dependencias de Cardboard añadidas a " + gradle);
                    }
                    else Debug.LogWarning("[ANDES-SIM] No se encontró el bloque dependencies en " + gradle);
                }
            }
            string root = Directory.GetParent(path).FullName;
            string props = Path.Combine(root, "gradle.properties");
            string pt = File.Exists(props) ? File.ReadAllText(props) : "";
            if (!pt.Contains("android.useAndroidX")) pt += "\nandroid.useAndroidX=true";
            if (!pt.Contains("android.enableJetifier")) pt += "\nandroid.enableJetifier=true";
            File.WriteAllText(props, pt + "\n");
        }
    }
}
#endif
