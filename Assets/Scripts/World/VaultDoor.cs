using System.Collections;
using UnityEngine;

namespace LasPalmeras
{
    /// <summary>Puerta circular de la bóveda con lámpara de estado.</summary>
    public class VaultDoor : MonoBehaviour
    {
        public Transform pivot; public Renderer lamp; public Light inside;
        public bool IsOpen { get; private set; }
        Material lampMat; Coroutine co;

        public void Setup(Transform p, Renderer l, Light inner)
        {
            pivot = p; lamp = l; inside = inner;
            lampMat = new Material(l.sharedMaterial); l.sharedMaterial = lampMat;
            SetLamp(new Color(1f, 0.2f, 0.15f));
            if (inside != null) inside.enabled = false;
        }

        public void SetLamp(Color c) { if (lampMat != null) lampMat.color = c; }

        public void Open(bool open)
        {
            if (IsOpen == open) return;
            IsOpen = open;
            if (co != null) StopCoroutine(co);
            co = StartCoroutine(Swing(open ? -100f : 0f));
            AudioManager.PlayAt("vault", transform.position, 0.9f);
            SetLamp(open ? new Color(0.2f, 1f, 0.4f) : new Color(1f, 0.2f, 0.15f));
            if (inside != null) inside.enabled = open;
        }

        IEnumerator Swing(float target)
        {
            float start = pivot.localEulerAngles.y; if (start > 180f) start -= 360f;
            float t = 0f, dur = 2.4f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / dur);
                pivot.localRotation = Quaternion.Euler(0f, Mathf.Lerp(start, target, k), 0f);
                yield return null;
            }
            pivot.localRotation = Quaternion.Euler(0f, target, 0f);
        }
    }
}
