using UnityEngine;

namespace LasPalmeras
{
    /// <summary>Entry point serialized in Agencia_VR. Ensures the simulation runtime exists.</summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        void Awake()
        {
            if (GameManager.I == null && FindAnyObjectByType<GameManager>() == null)
                Bootstrap.Create();
        }
    }
}
