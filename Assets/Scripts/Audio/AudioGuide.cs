// AudioGuide: indicaciones habladas y subtítulos. Android usa el TTS del sistema; Windows usa SAPI.
// En otras plataformas se conserva el subtítulo y la señal sonora, sin prometer una voz no disponible.
using System;
using System.Reflection;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Scripting;
#endif

namespace LasPalmeras
{
    public class AudioGuide : MonoBehaviour
    {
        public static AudioGuide I;
        public bool speechEnabled = true;
        public bool subtitlesEnabled = true;
        string pendingText = "";

#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaObject tts;
        AndroidJavaProxy initListener;
        bool ttsReady;

        sealed class InitListener : AndroidJavaProxy
        {
            readonly Action<int> callback;
            public InitListener(Action<int> cb) : base("android.speech.tts.TextToSpeech$OnInitListener") { callback = cb; }
            [Preserve] public void onInit(int status) { if (callback != null) callback(status); }
        }
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        object sapiVoice;
        Type sapiType;
#endif

        public static AudioGuide Create(Transform parent)
        {
            var g = new GameObject("AudioGuide");
            g.transform.SetParent(parent, false);
            I = g.AddComponent<AudioGuide>();
            return I;
        }

        void Awake() { I = this; }

        public static void Say(string text)
        {
            if (I != null) I.Speak(text);
        }

        void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            if (subtitlesEnabled && GameManager.I != null && GameManager.I.ui != null)
                GameManager.I.ui.Toast("GUÍA · " + text, Mathf.Clamp(2.8f + text.Length / 30f, 4f, 9f), ToastKind.Info);
            AudioManager.Play("voice_prompt", 0.45f);
            if (!speechEnabled) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            pendingText = text;
            EnsureAndroidTts();
            if (ttsReady) SpeakAndroid(text);
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            SpeakWindows(text);
#else
            Debug.Log("[AudioGuide · subtítulo] " + text);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        void EnsureAndroidTts()
        {
            if (tts != null || initListener != null) return;
            try
            {
                var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                initListener = new InitListener(status =>
                {
                    ttsReady = status == 0;
                    if (ttsReady)
                    {
                        try
                        {
                            using (var locale = new AndroidJavaObject("java.util.Locale", "es", "ES"))
                            {
                                tts.Call<int>("setLanguage", locale);
                                tts.Call<int>("setSpeechRate", 0.94f);
                            }
                            if (!string.IsNullOrEmpty(pendingText)) SpeakAndroid(pendingText);
                        }
                        catch (Exception e) { Debug.LogWarning("[AudioGuide] No se pudo configurar el TTS: " + e.Message); }
                    }
                    else Debug.LogWarning("[AudioGuide] El motor de voz de Android no está disponible.");
                });
                tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, initListener);
            }
            catch (Exception e) { Debug.LogWarning("[AudioGuide] No se pudo iniciar Android TTS: " + e.Message); }
        }

        void SpeakAndroid(string text)
        {
            if (tts == null || !ttsReady) { pendingText = text; return; }
            try
            {
                using (var bundle = new AndroidJavaObject("android.os.Bundle"))
                    tts.Call<int>("speak", text, 0, bundle, "laspalmeras-" + DateTime.UtcNow.Ticks);
            }
            catch (Exception e) { Debug.LogWarning("[AudioGuide] Error al reproducir voz Android: " + e.Message); }
        }
#endif

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        void SpeakWindows(string text)
        {
            try
            {
                if (sapiVoice == null)
                {
                    sapiType = Type.GetTypeFromProgID("SAPI.SpVoice");
                    if (sapiType == null) return;
                    sapiVoice = Activator.CreateInstance(sapiType);
                }
                sapiType.InvokeMember("Speak", BindingFlags.InvokeMethod, null, sapiVoice, new object[] { text, 1 });
            }
            catch (Exception e) { Debug.LogWarning("[AudioGuide] Windows SAPI no disponible: " + e.Message); }
        }
#endif

        void OnDestroy()
        {
            if (I == this) I = null;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (tts != null)
            {
                try { tts.Call("stop"); tts.Call("shutdown"); tts.Dispose(); }
                catch { }
                tts = null;
            }
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (sapiVoice != null && System.Runtime.InteropServices.Marshal.IsComObject(sapiVoice))
            {
                try { System.Runtime.InteropServices.Marshal.FinalReleaseComObject(sapiVoice); }
                catch { }
                sapiVoice = null;
            }
#endif
        }
    }
}
