// Puente con Google Cardboard XR Plugin (solo Android, fuera del editor).
// Sigue el sample "Hello Cardboard / VrMode" del plugin 1.36.0.
using System.Collections;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using Google.XR.Cardboard;
using UnityEngine.XR.Management;
#endif

namespace LasPalmeras
{
    public static class VrMode
    {
        /// <summary>Verdadero cuando Cardboard está inicializado y renderizando en estéreo.</summary>
        public static bool Active { get; private set; }

        /// <summary>El dispositivo puede usar Cardboard (Android con el plugin).</summary>
        public static bool Supported
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>Se pulsó el disparador de Cardboard (toque de pantalla / botón).</summary>
        public static bool TriggerPressed
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return Active && Api.IsTriggerPressed;
#else
                return false;
#endif
            }
        }

        /// <summary>Detecta si XR está activo (XR Plug-in Management → Initialize XR on Startup). Es barato: se llama cada frame.</summary>
        public static void Refresh()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var s = XRGeneralSettings.Instance;
            bool now = s != null && s.Manager != null && s.Manager.isInitializationComplete && s.Manager.activeLoader != null;
            if (now && !Active) OnEnterVr();
            Active = now;
#else
            Active = false;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        // Como en el sample «Hello Cardboard»: pantalla siempre encendida y escaneo del QR del visor una sola vez.
        static void OnEnterVr()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            if (!Api.HasDeviceParams()) Api.ScanDeviceParams();
        }
#endif

        /// <summary>Se llama una vez por frame mientras Cardboard está activo.</summary>
        public static void Tick()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Active) return;
            if (Api.IsGearButtonPressed) Api.ScanDeviceParams();
            if (Api.IsCloseButtonPressed) Exit();
            if (Api.IsTriggerHeldPressed) Api.Recenter();
            if (Api.HasNewDeviceParams()) Api.ReloadDeviceParams();
            Api.UpdateScreenParams();
#endif
        }

        public static IEnumerator Enter()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var s = XRGeneralSettings.Instance;
            if (s == null || s.Manager == null) { Debug.LogWarning("[ANDES-SIM] XR Plug-in Management no está configurado (activa Cardboard en Android)."); yield break; }
            if (!s.Manager.isInitializationComplete) yield return s.Manager.InitializeLoader();
            if (s.Manager.activeLoader == null) { Debug.LogWarning("[ANDES-SIM] No se pudo iniciar Cardboard."); yield break; }
            s.Manager.StartSubsystems();
            Refresh();
#else
            yield break;
#endif
        }

        public static void Exit()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var s = XRGeneralSettings.Instance;
            if (s != null && s.Manager != null && s.Manager.isInitializationComplete)
            {
                s.Manager.StopSubsystems();
                s.Manager.DeinitializeLoader();
            }
            Active = false;
            var cam = Camera.main;
            if (cam != null)
            {
                cam.ResetAspect(); cam.fieldOfView = 72f;
                cam.transform.localRotation = Quaternion.identity; cam.transform.localPosition = Vector3.zero;
            }
#endif
        }
    }
}
