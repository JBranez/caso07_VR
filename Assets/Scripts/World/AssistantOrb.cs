using UnityEngine;

namespace LasPalmeras
{
    /// <summary>Asistente ANDES-SIM: esfera azul que sigue al jugador, da pistas y, si te atascas, vuela hacia el siguiente objetivo.</summary>
    public class AssistantOrb : Interactable
    {
        Vector3 vel; float bob;

        public static AssistantOrb Create(GameManager gm)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            g.name = "Asistente_ANDES_SIM";
            g.transform.SetParent(gm.transform, false);
            g.transform.localScale = Vector3.one * 0.2f;
            var c = g.GetComponent<Collider>(); if (c != null) Object.Destroy(c);
            g.GetComponent<Renderer>().sharedMaterial = Mats.Unlit(new Color(0.3f, 0.75f, 1f), null, 1.4f);
            var o = g.AddComponent<AssistantOrb>();
            var sc = g.AddComponent<SphereCollider>(); sc.isTrigger = true; sc.radius = 1.6f;
            o.prompt = "Pedir una pista al asistente"; o.maxDistance = 4f; o.dwellTime = 1.2f;
            o.AddHalo(Vector3.zero, 6f, new Color(0.3f, 0.7f, 1f));
            var q = Txt.Make(g.transform, "?", Vector3.zero, 1.2f, new Color(0.02f, 0.1f, 0.2f)); q.gameObject.AddComponent<Billboard>();
            var cam = gm.player != null ? gm.player.cam : null;
            if (cam != null) g.transform.position = cam.transform.position + cam.transform.forward * 1f + Vector3.up * 0.2f;
            return o;
        }

        protected override void Interact()
        {
            var gm = GameManager.I;
            if (gm.phase != GameManager.Phase.Playing) { gm.ui.OpenHint("Elige un modo en el menú. Mira un botón 1 s (VR) o apúntalo y pulsa E / clic (PC)."); return; }
            gm.story.ShowHint();
        }

        protected override void Update()
        {
            base.Update();
            var gm = GameManager.I;
            if (gm == null || gm.player == null) return;
            var pl = gm.player; bob += Time.deltaTime;
            Vector3 fwd = pl.FlatForward;
            Vector3 left = -Vector3.Cross(Vector3.up, fwd).normalized;
            Vector3 target = pl.transform.position + fwd * 0.9f + left * 0.75f + Vector3.up * 1.45f;
            // si el jugador está atascado, el asistente va hacia el objetivo
            if (gm.phase == GameManager.Phase.Playing && gm.story != null && gm.story.IdleSeconds > 25f)
            {
                var tp = gm.story.CurrentTargetPosition();
                if (tp.HasValue && Vector3.Distance(pl.transform.position, tp.Value) > 3.5f) target = tp.Value + Vector3.up * 1.2f;
            }
            target += Vector3.up * Mathf.Sin(bob * 2f) * 0.05f;
            transform.position = Vector3.SmoothDamp(transform.position, target, ref vel, 0.45f);
        }
    }
}
