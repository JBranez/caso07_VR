// Arranque: crea el GameManager automáticamente (en cualquier escena) y permite reiniciar sin recargar la escena.
using System.Collections;
using UnityEngine;

namespace LasPalmeras
{
    public static class Bootstrap
    {
        public static bool pendingStart;
        public static SimMode pendingMode;
        public static Difficulty pendingDifficulty = Difficulty.Normal;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Auto()
        {
            if (GameManager.I != null || Object.FindAnyObjectByType<GameManager>() != null) return;
            Create();
        }

        public static GameObject Create()
        {
            var g = new GameObject("ANDES_SIM_Escenario07");
            g.AddComponent<GameManager>();
            return g;
        }

        public static void Restart(bool straightToBriefing, SimMode m, Difficulty d)
        {
            pendingStart = straightToBriefing; pendingMode = m; pendingDifficulty = d;
            Runner.Get().StartCoroutine(RestartRoutine());
        }

        static IEnumerator RestartRoutine()
        {
            var old = GameManager.I;
            if (old != null) Object.Destroy(old.gameObject);
            yield return null; yield return null;
            Create();
        }

        class Runner : MonoBehaviour
        {
            static Runner inst;
            public static Runner Get()
            {
                if (inst == null) { var g = new GameObject("ANDES_SIM_Runner"); Object.DontDestroyOnLoad(g); inst = g.AddComponent<Runner>(); }
                return inst;
            }
        }
    }

}
