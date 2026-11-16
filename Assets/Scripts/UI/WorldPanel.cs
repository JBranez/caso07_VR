// Panel holográfico en el mundo (uGUI en espacio de mundo). Sus botones llevan un collider para
// que funcionen con el ratón en PC y con la mirada / toque en VR (sin necesidad de EventSystem).
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LasPalmeras
{
    public class WorldPanel : MonoBehaviour
    {
        public Canvas canvas;
        public RectTransform rt;
        public float W, H, meters;
        Camera fitCamera;
        float baseScale;
        float fitScale = 1f;
        public Vector3 FittedScale { get { return Vector3.one * (baseScale * fitScale); } }

        public static WorldPanel Create(string name, float w, float h, float widthMeters, Camera cam)
        {
            var g = new GameObject(name);
            var p = g.AddComponent<WorldPanel>();
            p.W = w; p.H = h; p.meters = widthMeters; p.fitCamera = cam; p.baseScale = widthMeters / w;
            p.canvas = g.AddComponent<Canvas>();
            p.canvas.renderMode = RenderMode.WorldSpace;
            p.canvas.worldCamera = cam;
            var cs = g.AddComponent<CanvasScaler>();
            cs.dynamicPixelsPerUnit = 3f; cs.referencePixelsPerUnit = 100f;
            p.rt = g.GetComponent<RectTransform>();
            p.rt.sizeDelta = new Vector2(w, h);
            p.rt.localScale = Vector3.one * p.baseScale;
            g.AddComponent<WorldPanelPop>();
            return p;
        }

        void LateUpdate()
        {
            if (fitCamera == null || rt == null) return;
            float distance = Vector3.Dot(transform.position - fitCamera.transform.position, fitCamera.transform.forward);
            if (distance <= 0.15f) return;
            float halfHeight = distance * Mathf.Tan(fitCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.84f;
            float halfWidth = halfHeight * Mathf.Max(0.5f, fitCamera.aspect);
            float fitX = (halfWidth * 2f) / Mathf.Max(0.01f, meters);
            float panelHeight = meters * (H / Mathf.Max(1f, W));
            float fitY = (halfHeight * 2f) / Mathf.Max(0.01f, panelHeight);
            fitScale = Mathf.Clamp(Mathf.Min(1f, fitX, fitY), 0.5f, 1f);
            var pop = GetComponent<WorldPanelPop>();
            if (pop == null || !pop.enabled) rt.localScale = FittedScale;
        }

        public void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--) { var c = transform.GetChild(i).gameObject; c.SetActive(false); Destroy(c); }
        }

        RectTransform NewRect(string n, Transform parent, float x, float y, float w, float h)
        {
            var g = new GameObject(n, typeof(RectTransform));
            var r = g.GetComponent<RectTransform>();
            r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = new Vector2(0f, 1f); r.pivot = new Vector2(0f, 1f);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h);
            return r;
        }

        public Image Rect(float x, float y, float w, float h, Color c)
        {
            var r = NewRect("rect", transform, x, y, w, h);
            var img = r.gameObject.AddComponent<Image>(); img.color = c; img.raycastTarget = false;
            return img;
        }

        public Text Label(float x, float y, float w, float h, string s, int size, Color c, TextAnchor a = TextAnchor.UpperLeft, bool bold = false, bool fit = false)
        {
            var r = NewRect("texto", transform, x, y, w, h);
            return MakeText(r, s, size, c, a, bold, fit);
        }

        static Text MakeText(RectTransform r, string s, int size, Color c, TextAnchor a, bool bold, bool fit)
        {
            var t = r.gameObject.AddComponent<Text>();
            t.font = Mats.Font; t.text = s; t.fontSize = size; t.color = c; t.alignment = a;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
            t.supportRichText = true; t.raycastTarget = false; t.lineSpacing = 1.05f;
            if (fit) { t.resizeTextForBestFit = true; t.resizeTextMinSize = 12; t.resizeTextMaxSize = size; }
            return t;
        }

        public UiButton3D Button(float x, float y, float w, float h, string s, Action onClick, int size = 28, bool disabled = false,
                                 Color? tint = null, TextAnchor align = TextAnchor.MiddleCenter, bool fit = true)
        {
            var r = NewRect("boton", transform, x, y, w, h);
            var img = r.gameObject.AddComponent<Image>(); img.raycastTarget = false;
            var b = r.gameObject.AddComponent<UiButton3D>();
            Color baseC = tint ?? new Color(0.03f, 0.2f, 0.12f, 0.96f);
            b.normal = baseC; b.hover = Color.Lerp(baseC, new Color(0.3f, 1f, 0.6f, 1f), 0.45f);
            b.bg = img; b.disabled = disabled; b.onClick = onClick;
            var tr = NewRect("etiqueta", r, 14f, 4f, w - 28f, h - 8f);
            b.label = MakeText(tr, s, size, disabled ? new Color(0.55f, 0.6f, 0.58f) : new Color(0.92f, 1f, 0.95f), align, true, fit);
            // borde
            var edge = NewRect("borde", r, 0f, 0f, w, 3f); var ei = edge.gameObject.AddComponent<Image>(); ei.color = disabled ? new Color(0.3f, 0.35f, 0.33f) : new Color(0.22f, 0.95f, 0.54f, 0.9f); ei.raycastTarget = false;
            var col = r.gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true; col.center = new Vector3(w / 2f, -h / 2f, 0f); col.size = new Vector3(w, h, 24f);
            b.Refresh();
            return b;
        }
    }

    /// <summary>Animación breve de aparición del panel.</summary>
    public class WorldPanelPop : MonoBehaviour
    {
        float t;
        void Start()
        {
            var panel = GetComponent<WorldPanel>();
            Vector3 target = panel != null ? panel.FittedScale : transform.localScale;
            transform.localScale = target * 0.88f;
        }
        void Update()
        {
            t += Time.unscaledDeltaTime * 7f;
            var panel = GetComponent<WorldPanel>();
            Vector3 target = panel != null ? panel.FittedScale : transform.localScale;
            transform.localScale = target * Mathf.Lerp(0.88f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
            if (t >= 1f) { transform.localScale = target; enabled = false; }
        }
    }
}
