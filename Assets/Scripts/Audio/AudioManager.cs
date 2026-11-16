using System.Collections.Generic;
using UnityEngine;

namespace LasPalmeras
{
    /// <summary>Gestor de audio del contrato común (AudioManager). Todo el sonido es sintetizado.</summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        readonly List<AudioSource> pool = new List<AudioSource>();
        readonly Dictionary<string, AudioSource> loops = new Dictionary<string, AudioSource>();
        readonly Dictionary<string, float> loopTarget = new Dictionary<string, float>();
        public float master = 1f;
        public string currentEnvironment = "lobby";

        public static AudioManager Create(Transform parent)
        {
            var g = new GameObject("AudioManager");
            g.transform.SetParent(parent, false);
            var a = g.AddComponent<AudioManager>();
            I = a;
            for (int i = 0; i < 10; i++)
            {
                var s = g.AddComponent<AudioSource>();
                s.playOnAwake = false; s.spatialBlend = 0f;
                a.pool.Add(s);
            }
            // Genera los clips durante la inicialización, no al entrar por primera vez en cada zona.
            a.Clip("ambience_lobby"); a.Clip("ambience_teller"); a.Clip("ambience_vault");
            a.Clip("ambience_security"); a.Clip("ambience_office"); a.Clip("static_loop");
            return a;
        }

        public AudioClip Clip(string id)
        {
            AudioClip c;
            if (!clips.TryGetValue(id, out c)) { c = Synth.Build(id); clips[id] = c; }
            return c;
        }

        AudioSource Free()
        {
            foreach (var s in pool) if (!s.isPlaying) return s;
            return pool[0];
        }

        public static void Play(string id, float vol = 1f, float pitch = 1f)
        {
            if (I == null) return;
            var s = I.Free(); s.spatialBlend = 0f; s.pitch = pitch;
            s.PlayOneShot(I.Clip(id), vol * I.master);
        }

        public static void PlayAt(string id, Vector3 pos, float vol = 1f)
        {
            if (I == null) return;
            AudioSource.PlayClipAtPoint(I.Clip(id), pos, vol * I.master);
        }

        /// <summary>Activa/desactiva un sonido en bucle (con fundido). Si pos != null es 3D.</summary>
        public static void Loop(string id, bool on, float vol = 0.5f, Transform at = null)
        {
            if (I == null) return;
            AudioSource s;
            if (!I.loops.TryGetValue(id, out s))
            {
                if (!on) return;
                var g = new GameObject("loop_" + id);
                g.transform.SetParent(at != null ? at : I.transform, false);
                s = g.AddComponent<AudioSource>();
                s.clip = I.Clip(id); s.loop = true; s.volume = 0f;
                if (at != null) { s.spatialBlend = 1f; s.minDistance = 1.5f; s.maxDistance = 14f; }
                s.Play();
                I.loops[id] = s;
            }
            I.loopTarget[id] = on ? vol : 0f;
        }

        public static void SetEnvironment(string zone)
        {
            if (I == null) return;
            string key = (zone ?? "").ToLowerInvariant();
            string env = key.Contains("ventanilla") ? "teller" :
                         key.Contains("bóveda") || key.Contains("boveda") ? "vault" :
                         key.Contains("seguridad") || key.Contains("cctv") ? "security" :
                         key.Contains("oficina") ? "office" : "lobby";
            I.currentEnvironment = env;
            Loop("ambience_" + env, true, 0.16f);
            string[] all = { "lobby", "teller", "vault", "security", "office" };
            foreach (var item in all) if (item != env) Loop("ambience_" + item, false);
            var world = GameManager.I != null ? GameManager.I.world : null;
            if (world != null && world.cctv != null)
                Loop("static_loop", env == "security", 0.14f, world.cctv.transform);
        }

        public static void SetMaster(float value)
        {
            if (I != null) I.master = Mathf.Clamp01(value);
        }

        void Update()
        {
            foreach (var kv in loops)
            {
                float t; if (!loopTarget.TryGetValue(kv.Key, out t)) continue;
                kv.Value.volume = Mathf.MoveTowards(kv.Value.volume, t * master, Time.unscaledDeltaTime * 0.8f);
            }
        }
    }
}
