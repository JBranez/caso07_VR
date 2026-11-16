// Jugador: sirve para PC (teclado + ratón) y para VR (Cardboard: giro de cabeza + teletransporte por mirada).
using System;
using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LasPalmeras
{
    [DefaultExecutionOrder(-50)]
    public class PlayerRig : MonoBehaviour
    {
        public enum ControlMode { Desktop, Gaze }

        public ControlMode controlMode = ControlMode.Desktop;
        public Camera cam;
        public Transform offset;
        public bool frozen;
        public GazePointer gaze;
        public ScreenFader fader;

        const float EyeHeight = 1.62f;
        CharacterController cc;
        float yaw, pitch, stepTimer, bobT, gravityY;
        bool tpdTried;

        public Vector3 Eye { get { return cam.transform.position; } }
        public Vector3 FlatForward
        {
            get { Vector3 f = cam.transform.forward; f.y = 0f; return f.sqrMagnitude < 0.001f ? transform.forward : f.normalized; }
        }

        /// <summary>El ratón apunta cuando el cursor está libre (menús y paneles en PC).</summary>
        public bool UsesMouseRay
        {
            get { return !VrMode.Active && !Application.isMobilePlatform && Cursor.lockState != CursorLockMode.Locked; }
        }

        public static PlayerRig Create(Transform parent, Vector3 pos, float yawDeg)
        {
            var root = new GameObject("Jugador_XR_Rig");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            var rig = root.AddComponent<PlayerRig>();
            rig.cc = root.AddComponent<CharacterController>();
            rig.cc.height = 1.7f; rig.cc.radius = 0.28f; rig.cc.center = new Vector3(0f, 0.85f, 0f);
            rig.cc.stepOffset = 0.3f; rig.cc.skinWidth = 0.03f;

            var off = new GameObject("CameraOffset"); off.transform.SetParent(root.transform, false);
            off.transform.localPosition = new Vector3(0f, EyeHeight, 0f);
            rig.offset = off.transform;

            var cg = new GameObject("Main Camera"); cg.tag = "MainCamera";
            cg.transform.SetParent(off.transform, false);
            var cam = cg.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f; cam.farClipPlane = 80f; cam.fieldOfView = 72f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.1f, 0.2f);
            cg.AddComponent<AudioListener>();
            rig.cam = cam;
            rig.fader = ScreenFader.Create(cg.transform);
            rig.gaze = GazePointer.Create(rig);
            rig.yaw = yawDeg;
            root.transform.rotation = Quaternion.Euler(0f, yawDeg, 0f);
            return rig;
        }

        public void SetControlMode(ControlMode m)
        {
            controlMode = m;
            if (m == ControlMode.Gaze) { /* sin caminar: solo teletransporte */ }
        }

        public void ToggleGazeSim()
        {
            if (VrMode.Active) return;
            SetControlMode(controlMode == ControlMode.Desktop ? ControlMode.Gaze : ControlMode.Desktop);
            if (GameManager.I != null && GameManager.I.ui != null)
                GameManager.I.ui.Toast(controlMode == ControlMode.Gaze ? "Modo mirada (simula VR): mira un objeto 1,4 s para activarlo. F2 para volver." : "Modo teclado y ratón.", 3f, ToastKind.Info);
        }

        void Update()
        {
            GameInput.Poll();
            if (VrMode.Supported) { VrMode.Refresh(); VrMode.Tick(); }
            if (VrMode.Active && controlMode != ControlMode.Gaze) SetControlMode(ControlMode.Gaze);
            if (GameInput.GazeToggle) ToggleGazeSim();
            HandleCursor();

            var gm = GameManager.I;
            bool modal = gm != null && gm.ui != null && gm.ui.ModalOpen;
            if (!frozen && !VrMode.Active)
            {
                bool canLook = Application.isMobilePlatform || Cursor.lockState == CursorLockMode.Locked;
                if (canLook) ApplyLook(GameInput.Look);
                if (controlMode == ControlMode.Desktop && !modal) Walk();
            }
            if (GameInput.NumberKey > 0 && !frozen && gm != null && gm.phase == GameManager.Phase.Playing && !modal) QuickTeleport(GameInput.NumberKey);
        }

        void LateUpdate()
        {
            if (VrMode.Active) { TryAttachTrackedPoseDriver(); ReadHeadPose(); }
        }

        void HandleCursor()
        {
            if (Application.isMobilePlatform || VrMode.Active) { Cursor.visible = false; return; }
            var gm = GameManager.I;
            bool free = gm == null || gm.phase != GameManager.Phase.Playing || (gm.ui != null && gm.ui.ModalOpen);
            Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = free;
        }

        void ApplyLook(Vector2 d)
        {
            yaw += d.x; pitch = Mathf.Clamp(pitch - d.y, -80f, 80f);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            offset.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        void Walk()
        {
            Vector2 m = GameInput.Move;
            float speed = GameInput.Run ? 3.4f : 2.0f;
            Vector3 dir = transform.forward * m.y + transform.right * m.x;
            if (dir.sqrMagnitude > 1f) dir.Normalize();
            if (cc.isGrounded) gravityY = -1f; else gravityY -= 9.8f * Time.deltaTime;
            cc.Move((dir * speed + new Vector3(0f, gravityY, 0f)) * Time.deltaTime);
            bool moving = dir.sqrMagnitude > 0.01f;
            if (moving)
            {
                bobT += Time.deltaTime * (GameInput.Run ? 11f : 8f);
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f) { AudioManager.Play("step", 0.22f, UnityEngine.Random.Range(0.85f, 1.1f)); stepTimer = GameInput.Run ? 0.32f : 0.5f; }
            }
            offset.localPosition = new Vector3(0f, EyeHeight + (moving ? Mathf.Sin(bobT) * 0.018f : 0f), 0f);
        }

        void QuickTeleport(int n)
        {
            var w = GameManager.I.world; if (w == null) return;
            TeleportPoint tp = null;
            switch (n) { case 1: tp = w.tpPortal; break; case 2: tp = w.tpVentanilla; break; case 3: tp = w.tpBoveda; break; case 4: tp = w.tpCctv; break; case 5: tp = w.tpOficina; break; }
            if (tp != null) TeleportTo(tp.transform.position, tp.yaw, null);
        }

        // ---------- teletransporte ----------
        public void TeleportTo(Vector3 pos, float yawDeg, Action done)
        {
            if (frozen) return;
            StartCoroutine(TeleportRoutine(pos, yawDeg, done));
        }

        IEnumerator TeleportRoutine(Vector3 pos, float yawDeg, Action done)
        {
            frozen = true;
            AudioManager.Play("teleport", 0.7f);
            yield return fader.Fade(1f, 0.22f);
            Place(pos, yawDeg);
            yield return new WaitForSecondsRealtime(0.08f);
            yield return fader.Fade(0f, 0.28f);
            frozen = false;
            if (done != null) done();
        }

        public void Place(Vector3 pos, float yawDeg)
        {
            cc.enabled = false;
            transform.position = pos;
            yaw = yawDeg; pitch = 0f;
            transform.rotation = Quaternion.Euler(0f, yawDeg, 0f);
            offset.localRotation = Quaternion.identity;
            cc.enabled = true;
            gravityY = -1f;
        }

        // ---------- cabeza en VR ----------
        void ReadHeadPose()
        {
            var dev = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head);
            if (!dev.isValid) dev = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.CenterEye);
            if (!dev.isValid) return;
            Quaternion q; Vector3 p;
            if (dev.TryGetFeatureValue(UnityEngine.XR.CommonUsages.centerEyeRotation, out q) || dev.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out q))
                cam.transform.localRotation = q;
            if (dev.TryGetFeatureValue(UnityEngine.XR.CommonUsages.centerEyePosition, out p) || dev.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out p))
                cam.transform.localPosition = p;
        }

        /// <summary>Respaldo: añade el TrackedPoseDriver del Input System igual que el sample Hello Cardboard.</summary>
        void TryAttachTrackedPoseDriver()
        {
            if (tpdTried) return; tpdTried = true;
#if ENABLE_INPUT_SYSTEM
            try
            {
                var t = Type.GetType("UnityEngine.InputSystem.XR.TrackedPoseDriver, Unity.InputSystem");
                if (t == null) return;
                var comp = (Behaviour)cam.gameObject.AddComponent(t);
                comp.enabled = false;
                var posA = new InputAction("pos", InputActionType.Value, "<XRHMD>/centerEyePosition");
                var rotA = new InputAction("rot", InputActionType.Value, "<XRHMD>/centerEyeRotation");
                var pp = t.GetProperty("positionInput"); var rp = t.GetProperty("rotationInput"); var ip = t.GetProperty("ignoreTrackingState");
                if (pp != null) pp.SetValue(comp, new InputActionProperty(posA), null);
                if (rp != null) rp.SetValue(comp, new InputActionProperty(rotA), null);
                if (ip != null) ip.SetValue(comp, true, null);
                posA.Enable(); rotA.Enable();
                comp.enabled = true;
            }
            catch (Exception e) { Debug.Log("[ANDES-SIM] TrackedPoseDriver no disponible: " + e.Message); }
#endif
        }
    }
}
