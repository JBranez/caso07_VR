// Sintetizador de efectos de sonido (sin archivos de audio de terceros).
using System;
using UnityEngine;

namespace LasPalmeras
{
    public static class Synth
    {
        public const int SR = 22050;
        static System.Random rng = new System.Random(7);

        static float N() { return (float)(rng.NextDouble() * 2.0 - 1.0); }
        static float Env(float t, float dur, float a, float r)
        {
            float e = 1f;
            if (a > 0f) e = Mathf.Min(e, t / a);
            if (r > 0f) e = Mathf.Min(e, (dur - t) / r);
            return Mathf.Clamp01(e);
        }

        static AudioClip Make(string name, float[] d)
        {
            for (int i = 0; i < d.Length; i++) d[i] = Mathf.Clamp(d[i], -1f, 1f);
            var c = AudioClip.Create(name, d.Length, 1, SR, false);
            c.SetData(d, 0);
            return c;
        }

        static void Add(float[] buf, float startSec, float[] src, float vol = 1f)
        {
            int o = (int)(startSec * SR);
            for (int i = 0; i < src.Length && o + i < buf.Length; i++) buf[o + i] += src[i] * vol;
        }

        // Tono con envolvente. wave: 0 seno, 1 cuadrada suave, 2 sierra, 3 triángulo
        static float[] Tone(float f0, float dur, int wave = 0, float vol = 0.5f, float a = 0.004f, float r = 0.08f, float f1 = 0f)
        {
            int n = (int)(dur * SR); var d = new float[n]; double ph = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float f = f1 > 0f ? Mathf.Lerp(f0, f1, t / dur) : f0;
                ph += f / SR; float p = (float)(ph - Math.Floor(ph));
                float s;
                switch (wave)
                {
                    case 1: s = Mathf.Clamp(Mathf.Sin(p * Mathf.PI * 2f) * 3f, -1f, 1f); break;
                    case 2: s = p * 2f - 1f; break;
                    case 3: s = Mathf.Abs(p * 4f - 2f) - 1f; break;
                    default: s = Mathf.Sin(p * Mathf.PI * 2f); break;
                }
                d[i] = s * vol * Env(t, dur, a, r);
            }
            return d;
        }

        static float[] NoiseBurst(float dur, float vol, float lp = 0.5f, float a = 0.002f, float r = 0.05f)
        {
            int n = (int)(dur * SR); var d = new float[n]; float y = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR; y += (N() - y) * lp;
                d[i] = y * vol * Env(t, dur, a, r);
            }
            return d;
        }

        static void LoopFade(float[] d, int xf)
        {
            for (int i = 0; i < xf && i < d.Length / 2; i++)
            {
                float k = i / (float)xf;
                d[i] = d[i] * k + d[d.Length - xf + i] * (1f - k);
            }
        }

        // ---------- clips ----------
        public static AudioClip Build(string id)
        {
            switch (id)
            {
                case "hover": return Make(id, Tone(1400, 0.04f, 0, 0.18f, 0.002f, 0.03f));
                case "click": return Make(id, Mix(0.12f, (0f, Tone(660, 0.05f, 1, 0.3f)), (0.04f, Tone(990, 0.08f, 1, 0.3f))));
                case "back":  return Make(id, Mix(0.12f, (0f, Tone(660, 0.05f, 1, 0.3f)), (0.04f, Tone(440, 0.08f, 1, 0.3f))));
                case "ding":  return Make(id, Mix(0.7f, (0f, Tone(880, 0.5f, 0, 0.3f, 0.003f, 0.4f)), (0.09f, Tone(1320, 0.55f, 0, 0.3f, 0.003f, 0.45f)), (0.18f, Tone(1760, 0.5f, 0, 0.22f, 0.003f, 0.4f))));
                case "wrong": return Make(id, Mix(0.35f, (0f, Tone(180, 0.28f, 2, 0.35f, 0.005f, 0.1f, 110))));
                case "stamp": return Make(id, Mix(0.35f, (0f, Tone(90, 0.2f, 0, 0.7f, 0.001f, 0.18f, 50)), (0f, NoiseBurst(0.08f, 0.5f, 0.35f))));
                case "teleport": return Make(id, Mix(0.9f, (0f, Tone(220, 0.8f, 0, 0.3f, 0.2f, 0.3f, 880)), (0f, NoiseBurst(0.8f, 0.18f, 0.12f, 0.3f, 0.4f))));
                case "portal": return Make(id, Mix(2.0f, (0f, Tone(110, 1.9f, 0, 0.35f, 0.4f, 0.8f, 440)), (0.1f, Tone(165, 1.8f, 0, 0.25f, 0.4f, 0.8f, 660)), (0f, NoiseBurst(1.9f, 0.16f, 0.1f, 0.6f, 0.9f)), (1.2f, Tone(1320, 0.7f, 0, 0.2f, 0.01f, 0.6f))));
                case "keys": { var b = new float[(int)(SR * 0.9f)]; for (int i = 0; i < 10; i++) Add(b, i * 0.085f + (float)rng.NextDouble() * 0.03f, NoiseBurst(0.02f, 0.45f, 0.6f, 0.001f, 0.015f)); return Make(id, b); }
                case "cash": { var b = new float[(int)(SR * 1.1f)]; for (int i = 0; i < 24; i++) Add(b, i * 0.042f, NoiseBurst(0.018f, 0.5f, 0.7f, 0.001f, 0.012f)); return Make(id, b); }
                case "vault": return Make(id, Mix(2.2f, (0f, NoiseBurst(1.6f, 0.25f, 0.06f, 0.2f, 0.5f)), (1.5f, Tone(60, 0.6f, 0, 0.8f, 0.003f, 0.5f, 35)), (1.5f, NoiseBurst(0.12f, 0.5f, 0.3f))));
                case "denied": return Make(id, Mix(0.8f, (0f, Tone(300, 0.18f, 1, 0.4f)), (0.22f, Tone(300, 0.18f, 1, 0.4f)), (0.44f, Tone(220, 0.32f, 1, 0.4f))));
                case "success": return Make(id, Mix(1.6f, (0f, Tone(523, 0.4f, 3, 0.3f, 0.01f, 0.3f)), (0.14f, Tone(659, 0.4f, 3, 0.3f, 0.01f, 0.3f)), (0.28f, Tone(784, 0.4f, 3, 0.3f, 0.01f, 0.3f)), (0.42f, Tone(1047, 0.9f, 3, 0.35f, 0.01f, 0.7f))));
                case "fail": return Make(id, Mix(1.6f, (0f, Tone(392, 0.5f, 3, 0.3f, 0.01f, 0.4f)), (0.35f, Tone(311, 0.5f, 3, 0.3f, 0.01f, 0.4f)), (0.7f, Tone(233, 0.9f, 3, 0.35f, 0.01f, 0.7f))));
                case "tick": return Make(id, Tone(2200, 0.012f, 0, 0.3f, 0.0005f, 0.01f));
                case "step": return Make(id, NoiseBurst(0.07f, 0.35f, 0.14f, 0.002f, 0.05f));
                case "box": return Make(id, Mix(0.3f, (0f, NoiseBurst(0.12f, 0.5f, 0.2f)), (0f, Tone(120, 0.15f, 0, 0.5f, 0.002f, 0.12f, 70))));
                case "hint": return Make(id, Mix(0.5f, (0f, Tone(1040, 0.15f, 0, 0.25f)), (0.1f, Tone(1560, 0.3f, 0, 0.25f, 0.003f, 0.2f))));
                case "alarm_loop": // sirena de 2 s (ciclo entero)
                {
                    int n = SR * 2; var d = new float[n]; double ph = 0;
                    for (int i = 0; i < n; i++)
                    {
                        float t = i / (float)SR; float f = 700f + 300f * Mathf.Sin(t * Mathf.PI * 2f * 1f);
                        ph += f / SR; d[i] = Mathf.Clamp(Mathf.Sin((float)ph * Mathf.PI * 2f) * 3f, -1f, 1f) * 0.28f;
                    }
                    return Make(id, d);
                }
                case "ambience": // zumbido de aire acondicionado + ruido suave (ciclo de 4 s)
                {
                    int n = SR * 4; var d = new float[n + SR / 4]; float y = 0;
                    for (int i = 0; i < d.Length; i++)
                    {
                        float t = i / (float)SR; y += (N() - y) * 0.03f;
                        d[i] = Mathf.Sin(t * Mathf.PI * 2f * 60f) * 0.05f + Mathf.Sin(t * Mathf.PI * 2f * 120f) * 0.03f + y * 0.55f;
                    }
                    var o = new float[n]; Array.Copy(d, o, n); LoopFade(o, SR / 8); return Make(id, o);
                }
                case "static_loop":
                {
                    int n = SR * 2; var d = new float[n + SR / 4]; float y = 0;
                    for (int i = 0; i < d.Length; i++) { y += (N() - y) * 0.5f; d[i] = y * 0.35f; }
                    var o = new float[n]; Array.Copy(d, o, n); LoopFade(o, SR / 8); return Make(id, o);
                }
                case "tension_loop": // drone grave que sube (4 s, sin loop duro)
                {
                    int n = SR * 4; var d = new float[n]; double p1 = 0, p2 = 0;
                    for (int i = 0; i < n; i++)
                    {
                        float t = i / (float)SR; float k = t / 4f;
                        p1 += (55f + 10f * k) / SR; p2 += (82.5f + 20f * k) / SR;
                        float s = ((float)(p1 - Math.Floor(p1)) * 2f - 1f) * 0.18f + Mathf.Sin((float)p2 * Mathf.PI * 2f) * 0.2f;
                        float pulse = 0.65f + 0.35f * Mathf.Sin(t * Mathf.PI * 2f * (1.2f + k * 1.5f));
                        d[i] = s * pulse;
                    }
                    var o = new float[n]; Array.Copy(d, o, n); LoopFade(o, SR / 4); return Make(id, o);
                }
                case "hum_loop":
                {
                    int n = SR * 2; var d = new float[n];
                    for (int i = 0; i < n; i++) { float t = i / (float)SR; d[i] = Mathf.Sin(t * Mathf.PI * 2f * 50f) * 0.3f + Mathf.Sin(t * Mathf.PI * 2f * 100f) * 0.12f; }
                    return Make(id, d);
                }
                case "voice_prompt": return Make(id, Mix(0.42f, (0f, Tone(740, 0.11f, 0, 0.14f, 0.004f, 0.08f)), (0.13f, Tone(990, 0.19f, 0, 0.12f, 0.004f, 0.12f))));
                case "ambience_lobby": return Ambient(id, 58f, 116f, 0.018f, 0.026f, 0.006f);
                case "ambience_teller": return Ambient(id, 72f, 144f, 0.015f, 0.022f, 0.012f);
                case "ambience_vault": return Ambient(id, 48f, 96f, 0.03f, 0.018f, 0.004f);
                case "ambience_security": return Ambient(id, 62f, 124f, 0.014f, 0.03f, 0.018f);
                case "ambience_office": return Ambient(id, 54f, 108f, 0.022f, 0.019f, 0.008f);
            }
            return Make(id, Tone(440, 0.1f));
        }

        static AudioClip Ambient(string name, float f0, float f1, float hum, float air, float texture)
        {
            int n = SR * 4; var d = new float[n + SR / 4]; float low = 0f, high = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)SR;
                low += (N() - low) * 0.015f;
                high += (N() - high) * 0.35f;
                float pulse = 0.75f + 0.25f * Mathf.Sin(t * Mathf.PI * 2f * 0.18f);
                d[i] = (Mathf.Sin(t * Mathf.PI * 2f * f0) * hum + Mathf.Sin(t * Mathf.PI * 2f * f1) * hum * 0.35f + low * air + high * texture) * pulse;
            }
            var o = new float[n]; Array.Copy(d, o, n); LoopFade(o, SR / 8); return Make(name, o);
        }

        static float[] Mix(float dur, params (float t, float[] s)[] parts)
        {
            var b = new float[(int)(SR * dur)];
            foreach (var p in parts) Add(b, p.t, p.s);
            return b;
        }
    }
}
