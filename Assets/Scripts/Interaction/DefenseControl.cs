using UnityEngine;

namespace LasPalmeras
{
    /// <summary>Panel de alertas (contrato común: DefenseControl). Abre el panel de controles del escenario.</summary>
    public class DefenseControl : Interactable
    {
        public Renderer[] leds;          // 3 LEDs: kyc, dormant, hold
        Material[] ledMats;

        public void SetupLeds(Renderer[] l)
        {
            leds = l; ledMats = new Material[l.Length];
            for (int i = 0; i < l.Length; i++) { ledMats[i] = new Material(l[i].sharedMaterial); l[i].sharedMaterial = ledMats[i]; }
        }

        protected override void Interact()
        {
            GameManager.I.ui.OpenControls();
        }

        protected override void Update()
        {
            base.Update();
            if (ledMats == null) return;
            var gm = GameManager.I; if (gm == null) return;
            string[] ids = { "kyc", "dormant", "hold" };
            for (int i = 0; i < ledMats.Length && i < ids.Length; i++)
            {
                bool on = gm.mode == SimMode.Defensa && gm.IsDefenseApplied(ids[i]);
                ledMats[i].color = on ? new Color(0.2f, 1f, 0.5f) : new Color(0.55f, 0.1f, 0.08f);
            }
        }
    }
}
