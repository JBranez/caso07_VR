using System.Collections;
using UnityEngine;

namespace LasPalmeras
{
    /// <summary>Fundido a negro que funciona en pantalla y en VR (quad pegado a la cámara).</summary>
    public class ScreenFader : MonoBehaviour
    {
        Material mat; Renderer rend;
        public float alpha;

        public static ScreenFader Create(Transform cam)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Quad);
            g.name = "Fader";
            var c = g.GetComponent<Collider>(); if (c != null) Destroy(c);
            g.transform.SetParent(cam, false);
            g.transform.localPosition = new Vector3(0f, 0f, 0.25f);
            g.transform.localScale = new Vector3(2f, 2f, 1f);
            var f = g.AddComponent<ScreenFader>();
            f.rend = g.GetComponent<Renderer>();
            f.mat = new Material(Mats.Overlay(Color.black));
            f.rend.sharedMaterial = f.mat;
            f.SetAlpha(0f);
            return f;
        }

        public void SetAlpha(float a)
        {
            alpha = a; mat.color = new Color(0f, 0f, 0f, a); rend.enabled = a > 0.001f;
        }

        public IEnumerator Fade(float to, float dur)
        {
            float from = alpha, t = 0f;
            while (t < dur) { t += Time.unscaledDeltaTime; SetAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(t / dur))); yield return null; }
            SetAlpha(to);
        }
    }
}
