// Puntero de mirada: raycast desde el centro de la cámara (o desde el ratón si el cursor está libre).
// Dibuja la retícula (punto → círculo con progreso) y activa Interactables por clic, toque o mirada sostenida.
using System;
using UnityEngine;

namespace LasPalmeras
{
    [DefaultExecutionOrder(50)]
    public class GazePointer : MonoBehaviour
    {
        // Buffer reutilizable: evita asignaciones de RaycastAll cada frame en Cardboard.
        static readonly RaycastHit[] HitBuffer = new RaycastHit[96];
        PlayerRig rig; Transform reticle; Renderer reticleRend; Material mat;
        Interactable cur; float dwell; float cooldown;
        public Interactable Current { get { return cur; } }
        public float Progress { get; private set; }

        public static GazePointer Create(PlayerRig rig)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Quad);
            g.name = "Reticula";
            var c = g.GetComponent<Collider>(); if (c != null) Destroy(c);
            g.transform.SetParent(rig.transform.parent, false);
            var p = rig.gameObject.AddComponent<GazePointer>();
            p.rig = rig; p.reticle = g.transform; p.reticleRend = g.GetComponent<Renderer>();
            p.mat = Mats.Reticle(); p.reticleRend.sharedMaterial = p.mat;
            return p;
        }

        void OnDestroy() { if (reticle != null) Destroy(reticle.gameObject); }

        void LateUpdate()
        {
            var gm = GameManager.I;
            if (gm == null || rig.cam == null) return;
            Camera cam = rig.cam;
            bool mouseRay = rig.UsesMouseRay && GameInput.PointerValid;
            Ray ray = mouseRay ? cam.ScreenPointToRay(GameInput.PointerPos) : new Ray(cam.transform.position, cam.transform.forward);

            int hitCount = Physics.RaycastNonAlloc(ray, HitBuffer, 40f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            // Ordenación por inserción sobre el buffer fijo: no crea arrays ni delegates en cada frame.
            for (int i = 1; i < hitCount; i++)
            {
                RaycastHit key = HitBuffer[i]; int j = i - 1;
                while (j >= 0 && HitBuffer[j].distance > key.distance) { HitBuffer[j + 1] = HitBuffer[j]; j--; }
                HitBuffer[j + 1] = key;
            }
            Interactable target = null; float dist = 2.4f; bool anyHit = false;
            for (int i = 0; i < hitCount; i++)
            {
                var h = HitBuffer[i];
                if (h.collider.transform.IsChildOf(rig.transform)) continue;
                var it = h.collider.GetComponentInParent<Interactable>();
                if (it != null && it.isActiveAndEnabled) { target = it; dist = h.distance; anyHit = true; break; }
                if (!h.collider.isTrigger) { dist = h.distance; anyHit = true; break; }
            }

            bool gazeMode = rig.controlMode == PlayerRig.ControlMode.Gaze || VrMode.Active;
            if (target != null && gm.ui != null && gm.ui.ModalOpen && !(target is UiButton3D)) target = null;
            if (target != null)
            {
                float reach = target.maxDistance * (gazeMode ? 1.6f : 1f);
                if (dist > reach) target = null;
            }
            if (target != cur)
            {
                if (cur != null) cur.SetHover(false);
                cur = target; dwell = 0f; Progress = 0f;
                if (cur != null) cur.SetHover(true);
            }

            // ---- activación ----
            cooldown -= Time.unscaledDeltaTime;
            bool pressed;
            if (VrMode.Active) pressed = VrMode.TriggerPressed || GameInput.TouchBegan;
            else if (Application.isMobilePlatform) pressed = GameInput.TouchTap;
            else pressed = GameInput.Interact;

            if (cur != null && cooldown <= 0f)
            {
                if (pressed) { Fire(); }
                else if (gazeMode && !mouseRay && cur.Available)
                {
                    dwell += Time.unscaledDeltaTime;
                    Progress = Mathf.Clamp01(dwell / cur.dwellTime);
                    if (dwell >= cur.dwellTime) Fire();
                }
                else { dwell = 0f; Progress = 0f; }
            }
            else if (cur == null) { dwell = 0f; Progress = 0f; }

            // ---- prompt ----
            if (gm.ui != null)
            {
                string pr = "";
                if (cur != null && !(cur is UiButton3D))
                {
                    string how = gazeMode ? "mira " : "[E / clic] ";
                    pr = cur.Available ? how + cur.prompt : "Bloqueado · " + cur.prompt;
                }
                gm.ui.SetPrompt(pr);
            }

            // ---- retícula ----
            if (mouseRay) { reticleRend.enabled = false; return; }
            reticleRend.enabled = true;
            float d = anyHit ? Mathf.Clamp(dist - 0.04f, 0.4f, 14f) : 2.4f;
            reticle.position = cam.transform.position + ray.direction * d;
            reticle.rotation = cam.transform.rotation;
            float ang = (cur != null ? 0.055f : 0.03f) * (gazeMode ? 1.35f : 1f);
            reticle.localScale = Vector3.one * d * ang;
            if (mat.HasProperty("_Progress")) mat.SetFloat("_Progress", Progress);
            if (mat.HasProperty("_Hover")) mat.SetFloat("_Hover", cur != null ? (cur.Available ? 1f : 0.4f) : 0f);
            Color col = cur != null && cur.Available ? new Color(0.25f, 1f, 0.6f, 1f) : new Color(1f, 1f, 1f, 0.85f);
            mat.color = col;
        }

        void Fire()
        {
            cooldown = 0.45f; dwell = 0f; Progress = 0f;
            var c = cur;
            if (c != null) c.Activate();
        }
    }
}
