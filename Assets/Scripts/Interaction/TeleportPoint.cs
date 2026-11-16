using UnityEngine;

namespace LasPalmeras
{
    /// <summary>Plataforma de teletransporte por mirada (requisito de VR: TeleportPoints).</summary>
    public class TeleportPoint : Interactable
    {
        public string zoneName = "Zona";
        public float yaw;
        public Renderer disc;
        public Renderer ring;
        Material discMat, ringMat;
        public static TeleportPoint Current;

        protected override void OnAwake() { dwellTime = 1.1f; maxDistance = 14f; }

        public void Setup(Renderer d, Renderer r)
        {
            disc = d; ring = r;
            discMat = new Material(d.sharedMaterial); d.sharedMaterial = discMat;
            ringMat = new Material(r.sharedMaterial); r.sharedMaterial = ringMat;
        }

        protected override void Interact()
        {
            var gm = GameManager.I;
            if (gm == null || gm.player == null) return;
            gm.player.TeleportTo(transform.position, yaw, () =>
            {
                Current = this;
                AudioManager.SetEnvironment(zoneName);
                AudioGuide.Say("Has llegado a " + zoneName + ".");
            });
        }

        protected override void Update()
        {
            base.Update();
            if (discMat == null) return;
            var gm = GameManager.I;
            if (Current == this && gm != null && gm.player != null && (gm.player.transform.position - transform.position).sqrMagnitude > 2.5f) Current = null;
            bool gaze = gm != null && gm.player != null && gm.player.controlMode == PlayerRig.ControlMode.Gaze;
            float baseA = gaze ? 0.35f : 0.12f;
            float a = Available ? (Hovered ? 0.9f : baseA + 0.1f * Mathf.Sin(Time.time * 2.5f)) : 0f;
            Color c = new Color(0.1f, 0.9f, 0.7f);
            discMat.color = new Color(c.r * a * 0.5f, c.g * a * 0.5f, c.b * a * 0.5f, 1f);
            ringMat.color = new Color(c.r * a, c.g * a, c.b * a, 1f);
            disc.enabled = ring.enabled = a > 0.01f && Current != this;
        }
    }
}
