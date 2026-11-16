using System;
using System.Collections.Generic;
using UnityEngine;

namespace LasPalmeras
{
    /// <summary>
    /// Base de todo lo que se puede mirar/pulsar. Equivale a la capa "Interactive" de la guía de Cardboard:
    /// el puntero (retícula) solo reacciona a objetos que tengan un Interactable.
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();
        static Texture2D glowTex;

        public string prompt = "Interactuar";
        public float maxDistance = 4.5f;      // alcance en PC (en VR se amplía)
        public float dwellTime = 1.4f;        // segundos mirando para activar en VR
        public Func<bool> unlocked;           // null = siempre disponible
        public string lockedMessage;
        public bool quiet;                    // sin halo ni baliza (botones de UI)
        public bool Hovered { get; private set; }

        bool beacon;
        GameObject halo; Material haloMat; TextMesh chevron; Transform chevronT;
        float haloSize = 0.8f; Color haloColor = new Color(0.22f, 0.95f, 0.54f);

        public bool Available { get { return isActiveAndEnabled && (unlocked == null || unlocked()); } }

        void Awake()
        {
            int l = LayerMask.NameToLayer("Interactive");
            if (l >= 0) gameObject.layer = l;
            OnAwake();
        }
        protected virtual void OnAwake() { }
        protected virtual void OnEnable() { All.Add(this); }
        protected virtual void OnDisable() { All.Remove(this); Hovered = false; }
        protected virtual void OnHoverChanged(bool on) { }
        protected abstract void Interact();

        public void SetHover(bool on)
        {
            if (Hovered == on) return;
            Hovered = on;
            OnHoverChanged(on);
            if (on && !quiet && Available) AudioManager.Play("hover", 0.5f);
        }

        /// <summary>Intenta activar. Si está bloqueado explica por qué.</summary>
        public void Activate()
        {
            if (!Available)
            {
                if (isActiveAndEnabled && !string.IsNullOrEmpty(lockedMessage) && GameManager.I != null && GameManager.I.ui != null)
                    GameManager.I.ui.Toast(lockedMessage, 3f, ToastKind.Warn);
                AudioManager.Play("wrong", 0.6f);
                return;
            }
            Interact();
        }

        // ---------- halo y baliza ----------
        public void AddHalo(Vector3 localOffset, float size, Color? color = null)
        {
            if (quiet) return;
            if (glowTex == null) glowTex = Procedural.Glow(64);
            haloSize = size; if (color.HasValue) haloColor = color.Value;
            halo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            halo.name = "halo";
            var c = halo.GetComponent<Collider>(); if (c != null) Destroy(c);
            halo.transform.SetParent(transform, false);
            halo.transform.localPosition = localOffset;
            haloMat = new Material(Mats.Additive(haloColor, glowTex));
            halo.GetComponent<Renderer>().sharedMaterial = haloMat;
            halo.transform.localScale = Vector3.one * size;
        }

        public void SetBeacon(bool on)
        {
            beacon = on;
            if (on && chevron == null)
            {
                chevron = Txt.Make(transform, "▼", Vector3.zero, 0.34f, new Color(0.22f, 0.95f, 0.54f), TextAnchor.MiddleCenter, FontStyle.Bold);
                chevronT = chevron.transform;
            }
            if (chevron != null) chevron.gameObject.SetActive(on);
        }

        public Bounds WorldBounds()
        {
            var rs = GetComponentsInChildren<Renderer>();
            Bounds b = new Bounds(transform.position, Vector3.one * 0.3f);
            bool first = true;
            foreach (var r in rs)
            {
                if (r.gameObject == halo || (chevron != null && r.gameObject == chevron.gameObject)) continue;
                if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
            }
            return b;
        }

        protected virtual void Update()
        {
            var gm = GameManager.I;
            Camera cam = (gm != null && gm.player != null) ? gm.player.cam : null;
            if (halo != null)
            {
                bool avail = Available;
                float a = avail ? 0.10f : 0f;
                if (Hovered && avail) a = 0.6f;
                else if (beacon && avail) a = 0.22f + 0.18f * Mathf.Sin(Time.time * 4f);
                haloMat.color = new Color(haloColor.r * a, haloColor.g * a, haloColor.b * a, 1f);
                if (cam != null) halo.transform.rotation = cam.transform.rotation;
                halo.SetActive(a > 0.01f);
            }
            if (chevron != null && chevron.gameObject.activeSelf)
            {
                var b = WorldBounds();
                chevronT.position = new Vector3(b.center.x, b.max.y + 0.35f + 0.08f * Mathf.Sin(Time.time * 3f), b.center.z);
                if (cam != null) chevronT.rotation = cam.transform.rotation;
            }
        }
    }
}
