using UnityEngine;

namespace LasPalmeras
{
    /// <summary>Portal de entrada a la agencia (requisito de VR: «portal de entrada»).</summary>
    public class PortalRing : Interactable
    {
        public Transform swirl;
        public bool used;

        protected override void OnAwake() { dwellTime = 1.2f; maxDistance = 14f; prompt = "Cruzar el portal"; }

        protected override void Interact()
        {
            var gm = GameManager.I;
            if (gm == null || gm.story == null) return;
            gm.story.OnPortalEntered();
        }

        protected override void Update()
        {
            base.Update();
            if (swirl != null) swirl.Rotate(0f, 0f, -40f * Time.deltaTime);
        }
    }
}
