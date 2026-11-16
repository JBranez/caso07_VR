// Texturas procedurales (sin recursos de terceros).
using UnityEngine;

namespace LasPalmeras
{
    public static class Procedural
    {
        static Texture2D New(int w, int h, bool repeat = true)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, true, false)
            {
                wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 2
            };
        }

        static void Done(Texture2D t, Color32[] px) { t.SetPixels32(px); t.Apply(true, true); }

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        static float VNoise(float x, float y, int seed)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            float a = Hash(xi, yi, seed), b = Hash(xi + 1, yi, seed), c = Hash(xi, yi + 1, seed), d = Hash(xi + 1, yi + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        static float Fbm(float x, float y, int seed, int oct)
        {
            float v = 0, amp = 0.5f, f = 1;
            for (int i = 0; i < oct; i++) { v += VNoise(x * f, y * f, seed + i * 17) * amp; f *= 2; amp *= 0.5f; }
            return v;
        }

        static Color32 C(Color c) { return (Color32)c; }

        /// <summary>Piso de losetas pulidas con vetas suaves.</summary>
        public static Texture2D Tiles(int size, int n, Color a, Color b, Color grout)
        {
            var t = New(size, size); var px = new Color32[size * size];
            int cell = size / n;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int cx = x / cell, cy = y / cell;
                    float tone = Hash(cx, cy, 3);
                    Color baseC = Color.Lerp(a, b, tone * 0.8f);
                    float vein = Fbm(x * 0.03f, y * 0.03f, 9, 3);
                    baseC = Color.Lerp(baseC, baseC * 0.82f, vein * 0.6f);
                    int lx = x % cell, ly = y % cell;
                    bool g = lx < 2 || ly < 2;
                    px[y * size + x] = C(g ? grout : baseC);
                }
            Done(t, px); return t;
        }

        public static Texture2D Wood(int size, Color a, Color b)
        {
            var t = New(size, size); var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float warp = Fbm(x * 0.012f, y * 0.04f, 5, 3) * 6f;
                    float g = Mathf.Sin((y * 0.11f) + warp * 3f) * 0.5f + 0.5f;
                    float fine = VNoise(x * 0.4f, y * 0.05f, 7) * 0.25f;
                    px[y * size + x] = C(Color.Lerp(a, b, Mathf.Clamp01(g * 0.7f + fine)));
                }
            Done(t, px); return t;
        }

        public static Texture2D Plaster(int size, Color baseC, float amt)
        {
            var t = New(size, size); var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float n = Fbm(x * 0.05f, y * 0.05f, 11, 3) - 0.5f;
                    float s = Hash(x, y, 2) - 0.5f;
                    px[y * size + x] = C(baseC * (1f + n * amt + s * amt * 0.25f));
                }
            Done(t, px); return t;
        }

        public static Texture2D Carpet(int size, Color c)
        {
            var t = New(size, size); var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float n = Hash(x, y, 4) * 0.18f + Fbm(x * 0.08f, y * 0.08f, 6, 2) * 0.12f;
                    px[y * size + x] = C(c * (0.88f + n));
                }
            Done(t, px); return t;
        }

        /// <summary>Hoja de la ficha de apertura (papel crema con franja y renglones).</summary>
        public static Texture2D Paper(int w, int h)
        {
            var t = New(w, h, false); var px = new Color32[w * h];
            Color paper = new Color(0.96f, 0.93f, 0.84f), band = new Color(0.04f, 0.43f, 0.43f), line = new Color(0.55f, 0.58f, 0.6f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color c = paper * (0.97f + Hash(x, y, 8) * 0.04f);
                    if (y > h * 0.86f) c = band;
                    else if (y < h * 0.80f && y > h * 0.1f && (y % (h / 9)) < 2 && x > w * 0.08f && x < w * 0.92f) c = line;
                    px[y * w + x] = C(c);
                }
            Done(t, px); return t;
        }

        public static Texture2D Glow(int size)
        {
            var t = New(size, size, false); var px = new Color32[size * size];
            float r0 = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(r0, r0)) / r0;
                    float a = Mathf.Clamp01(1f - d); a = a * a * (3 - 2 * a);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            Done(t, px); return t;
        }

        public static Texture2D Ring(int size, float inner = 0.72f, float outer = 0.95f)
        {
            var t = New(size, size, false); var px = new Color32[size * size];
            float r0 = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(r0, r0)) / r0;
                    float a = Mathf.Clamp01(Mathf.Min((d - inner) / 0.05f, (outer - d) / 0.05f));
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            Done(t, px); return t;
        }

        /// <summary>Remolino del portal ANDES-SIM.</summary>
        public static Texture2D Swirl(int size)
        {
            var t = New(size, size, false); var px = new Color32[size * size];
            float r0 = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - r0) / r0, dy = (y - r0) / r0;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float ang = Mathf.Atan2(dy, dx);
                    float s = Mathf.Sin(ang * 3f + r * 14f) * 0.5f + 0.5f;
                    float fall = Mathf.Clamp01(1f - r);
                    float a = Mathf.Clamp01(s * 0.7f + 0.2f) * fall * Mathf.Clamp01(r * 3f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            Done(t, px); return t;
        }

        /// <summary>Panorama de ciudad al atardecer que se ve por las ventanas.</summary>
        public static Texture2D City(int w, int h)
        {
            var t = New(w, h, false); var px = new Color32[w * h];
            Color top = new Color(0.10f, 0.14f, 0.32f), mid = new Color(0.85f, 0.45f, 0.38f), low = new Color(0.98f, 0.72f, 0.45f);
            int cols = 26; var heights = new float[cols];
            for (int i = 0; i < cols; i++) heights[i] = 0.18f + Hash(i, 1, 21) * 0.42f;
            for (int y = 0; y < h; y++)
            {
                float v = y / (float)h;
                Color sky = v > 0.35f ? Color.Lerp(mid, top, (v - 0.35f) / 0.65f) : Color.Lerp(low, mid, v / 0.35f);
                for (int x = 0; x < w; x++)
                {
                    int ci = x * cols / w;
                    Color c = sky;
                    if (v < heights[ci])
                    {
                        c = new Color(0.07f, 0.09f, 0.16f);
                        int wx = (x * cols * 4 / w), wy = (int)(v * 70);
                        if ((wx % 4) != 0 && (wy % 3) != 0 && Hash(wx / 1, wy, 33) > 0.55f) c = new Color(1f, 0.82f, 0.45f);
                    }
                    px[y * w + x] = C(c);
                }
            }
            Done(t, px); return t;
        }

        public static Texture2D Gradient(Color top, Color bottom, int h = 64)
        {
            var t = New(4, h, false); var px = new Color32[4 * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < 4; x++) px[y * 4 + x] = C(Color.Lerp(bottom, top, y / (float)(h - 1)));
            Done(t, px); return t;
        }
    }
}
