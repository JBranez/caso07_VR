using UnityEngine;

namespace LasPalmeras
{
    /// <summary>Hace que un texto/objeto mire siempre hacia la cámara del jugador.</summary>
    public class Billboard : MonoBehaviour
    {
        public bool yOnly = true;
        void LateUpdate()
        {
            var gm = GameManager.I;
            if (gm == null || gm.player == null || gm.player.cam == null) return;
            Vector3 d = transform.position - gm.player.cam.transform.position;
            if (yOnly) d.y = 0f;
            if (d.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(d);
        }
    }
}
