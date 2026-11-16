// Fábrica de materiales (Built-In Render Pipeline). Solo usa shaders que van incluidos en el build.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LasPalmeras
{
    public static class Mats
    {
        static Shader lit, unlit, cctv, reticle;
        static Font font;
        // La identidad de Textures usa EntityId (Unity 6.5), no InstanceID ni una conversión
        // a int/string. La clave solo vive en memoria durante esta sesión.
        struct MaterialKey : IEquatable<MaterialKey>
        {
            readonly string kind;
            readonly Color color;
            readonly EntityId textureId;
            readonly float a, b, tx, ty;

            public MaterialKey(string kind, Color color, Texture texture, float a, float b, float tx, float ty)
            {
                this.kind = kind;
                this.color = color;
                textureId = texture != null ? texture.GetEntityId() : default(EntityId);
                this.a = a;
                this.b = b;
                this.tx = tx;
                this.ty = ty;
            }

            public bool Equals(MaterialKey other)
            {
                return string.Equals(kind, other.kind, StringComparison.Ordinal)
                    && color.Equals(other.color)
                    && textureId.Equals(other.textureId)
                    && a.Equals(other.a) && b.Equals(other.b)
                    && tx.Equals(other.tx) && ty.Equals(other.ty);
            }

            public override bool Equals(object obj)
            {
                return obj is MaterialKey && Equals((MaterialKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + (kind != null ? kind.GetHashCode() : 0);
                    hash = hash * 31 + color.GetHashCode();
                    hash = hash * 31 + textureId.GetHashCode();
                    hash = hash * 31 + a.GetHashCode();
                    hash = hash * 31 + b.GetHashCode();
                    hash = hash * 31 + tx.GetHashCode();
                    hash = hash * 31 + ty.GetHashCode();
                    return hash;
                }
            }
        }

        static readonly Dictionary<MaterialKey, Material> cache = new Dictionary<MaterialKey, Material>();

        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        static Shader Res(string file, string find)
        {
            var s = Resources.Load<Shader>("Shaders/" + file);
            if (s == null) s = Shader.Find(find);
            return s;
        }

        static Shader LitShader
        {
            get
            {
                if (lit == null)
                {
                    var m = Resources.Load<Material>("Materials/StandardRef");
                    if (m != null) lit = m.shader;
                    if (lit == null) lit = Shader.Find("Standard");
                    if (lit == null) lit = Shader.Find("Legacy Shaders/Diffuse");
                }
                return lit;
            }
        }

        static Shader UnlitShader { get { if (unlit == null) unlit = Res("LasPalmerasUnlit", "LasPalmeras/Unlit"); if (unlit == null) unlit = Shader.Find("Sprites/Default"); return unlit; } }

        static MaterialKey Key(string kind, Color c, Texture tex, float a, float b, float tx, float ty)
        {
            return new MaterialKey(kind, c, tex, a, b, tx, ty);
        }

        /// <summary>Superficie iluminada (Standard sin keywords: no depende de variantes).</summary>
        public static Material Lit(Color c, float smooth = 0.3f, float metal = 0f, Texture tex = null, float tx = 1, float ty = 1)
        {
            MaterialKey k = Key("L", c, tex, smooth, metal, tx, ty);
            Material m;
            if (cache.TryGetValue(k, out m)) return m;
            m = new Material(LitShader);
            m.color = c;
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
            if (tex != null) { m.mainTexture = tex; m.mainTextureScale = new Vector2(tx, ty); }
            cache[k] = m; return m;
        }

        static Material Custom(string kind, Color c, Texture tex, float intensity, int src, int dst, int zwrite, int queue, int ztest = 4, int cull = 2)
        {
            MaterialKey k = Key(kind, c, tex, intensity, queue, 0, 0);
            Material m;
            if (cache.TryGetValue(k, out m)) return m;
            m = new Material(UnlitShader);
            m.color = c;
            if (tex != null) m.mainTexture = tex;
            if (m.HasProperty("_Intensity")) m.SetFloat("_Intensity", intensity);
            if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", src);
            if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", dst);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", zwrite);
            if (m.HasProperty("_ZTest")) m.SetFloat("_ZTest", ztest);
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", cull);
            if (m.HasProperty("_Premul")) m.SetFloat("_Premul", (src == (int)BlendMode.One && dst == (int)BlendMode.One) ? 1f : 0f);
            m.renderQueue = queue;
            cache[k] = m; return m;
        }

        public static Material Unlit(Color c, Texture tex = null, float intensity = 1f)
        { return Custom("U", c, tex, intensity, (int)BlendMode.One, (int)BlendMode.Zero, 1, 2000); }

        /// <summary>Sin iluminación y visible por ambos lados (paneles al aire libre).</summary>
        public static Material UnlitTwoSided(Color c, Texture tex = null, float intensity = 1f)
        { return Custom("U2", c, tex, intensity, (int)BlendMode.One, (int)BlendMode.Zero, 1, 2000, 4, 0); }

        public static Material Additive(Color c, Texture tex = null, float intensity = 1f)
        { return Custom("A", c, tex, intensity, (int)BlendMode.One, (int)BlendMode.One, 0, 3000); }

        public static Material AdditiveTwoSided(Color c, Texture tex = null, float intensity = 1f)
        { return Custom("A2", c, tex, intensity, (int)BlendMode.One, (int)BlendMode.One, 0, 3000, 4, 0); }

        public static Material Alpha(Color c, Texture tex = null, float intensity = 1f)
        { return Custom("T", c, tex, intensity, (int)BlendMode.SrcAlpha, (int)BlendMode.OneMinusSrcAlpha, 0, 3000); }

        /// <summary>Capa superpuesta que siempre se dibuja encima (retícula, fundido).</summary>
        public static Material Overlay(Color c, Texture tex = null)
        { return Custom("O", c, tex, 1f, (int)BlendMode.SrcAlpha, (int)BlendMode.OneMinusSrcAlpha, 0, 4000, (int)CompareFunction.Always); }

        /// <summary>Material único (no compartido) para un monitor CCTV con shader de ruido.</summary>
        public static Material Cctv()
        {
            if (cctv == null) cctv = Res("LasPalmerasCctv", "LasPalmeras/CctvNoise");
            Shader s = cctv != null ? cctv : UnlitShader;
            return new Material(s);
        }

        /// <summary>Material único para la retícula de mirada (anillo con progreso).</summary>
        public static Material Reticle()
        {
            if (reticle == null) reticle = Res("LasPalmerasReticle", "LasPalmeras/Reticle");
            Shader s = reticle != null ? reticle : UnlitShader;
            var m = new Material(s);
            m.renderQueue = 4001;
            return m;
        }
    }
}
