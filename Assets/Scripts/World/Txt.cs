using UnityEngine;

namespace LasPalmeras
{
    /// <summary>Texto 3D simple (TextMesh con la fuente integrada).</summary>
    public static class Txt
    {
        /// <param name="size">altura aproximada de una línea, en metros</param>
        public static TextMesh Make(Transform parent, string text, Vector3 pos, float size, Color color,
                                    TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold,
                                    Vector3 euler = default(Vector3))
        {
            var g = new GameObject("txt");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localEulerAngles = euler;
            var tm = g.AddComponent<TextMesh>();
            tm.font = Mats.Font;
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = size / 6.4f;
            tm.anchor = anchor;
            tm.alignment = anchor == TextAnchor.MiddleLeft || anchor == TextAnchor.UpperLeft || anchor == TextAnchor.LowerLeft
                ? TextAlignment.Left : TextAlignment.Center;
            tm.color = color;
            tm.fontStyle = style;
            var mr = g.GetComponent<MeshRenderer>();
            mr.sharedMaterial = Mats.Font.material;
            return tm;
        }
    }
}
