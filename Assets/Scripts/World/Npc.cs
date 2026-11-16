// Personajes de maniquí estilizado (todos ficticios). Se construyen con primitivas.
using System.Collections;
using UnityEngine;

namespace LasPalmeras
{
    public class Npc : MonoBehaviour
    {
        public Transform body, head, armL, armR, legL, legR;
        public bool walking;
        public string display;
        float phase, seed;
        Vector3 bodyBase;
        TextMesh nameTag;

        static GameObject Part(Transform p, PrimitiveType t, Vector3 pos, Vector3 scale, Material m)
        {
            var g = GameObject.CreatePrimitive(t);
            var c = g.GetComponent<Collider>(); if (c != null) Object.Destroy(c);
            g.transform.SetParent(p, false); g.transform.localPosition = pos; g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g;
        }

        public static Npc Create(Transform parent, string name, string label, Color skin, Color top, Color bottom, bool ghost = false, Color? hair = null)
        {
            var root = new GameObject("NPC_" + name);
            root.transform.SetParent(parent, false);
            var n = root.AddComponent<Npc>();
            n.display = label; n.seed = Random.value * 10f;
            Material mSkin, mTop, mBot, mHair;
            if (ghost)
            {
                Color gc = new Color(0.35f, 0.95f, 1f, 0.45f);
                mSkin = Mats.Alpha(new Color(gc.r, gc.g, gc.b, 0.55f)); mTop = Mats.Alpha(new Color(top.r * 0.6f + 0.2f, top.g * 0.6f + 0.35f, top.b * 0.6f + 0.4f, 0.5f));
                mBot = Mats.Alpha(new Color(0.3f, 0.7f, 0.8f, 0.45f)); mHair = mBot;
            }
            else
            {
                mSkin = Mats.Lit(skin, 0.25f); mTop = Mats.Lit(top, 0.3f); mBot = Mats.Lit(bottom, 0.3f);
                mHair = Mats.Lit(hair ?? new Color(0.12f, 0.08f, 0.06f), 0.4f);
            }
            var b = new GameObject("cuerpo"); b.transform.SetParent(root.transform, false); n.body = b.transform;
            Part(b.transform, PrimitiveType.Capsule, new Vector3(0f, 1.15f, 0f), new Vector3(0.36f, 0.34f, 0.22f), mTop);
            var h = new GameObject("cabeza"); h.transform.SetParent(b.transform, false); h.transform.localPosition = new Vector3(0f, 1.6f, 0f); n.head = h.transform;
            Part(h.transform, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.23f, 0.26f, 0.24f), mSkin);
            Part(h.transform, PrimitiveType.Sphere, new Vector3(0f, 0.07f, -0.015f), new Vector3(0.25f, 0.17f, 0.26f), mHair);
            n.armL = Limb(b.transform, new Vector3(-0.25f, 1.42f, 0f), 0.3f, 0.085f, mTop);
            n.armR = Limb(b.transform, new Vector3(0.25f, 1.42f, 0f), 0.3f, 0.085f, mTop);
            n.legL = Limb(root.transform, new Vector3(-0.1f, 0.84f, 0f), 0.4f, 0.12f, mBot);
            n.legR = Limb(root.transform, new Vector3(0.1f, 0.84f, 0f), 0.4f, 0.12f, mBot);
            n.bodyBase = b.transform.localPosition;
            if (!string.IsNullOrEmpty(label))
            {
                n.nameTag = Txt.Make(root.transform, label, new Vector3(0f, 2.05f, 0f), 0.1f, ghost ? new Color(0.5f, 1f, 1f) : new Color(0.85f, 1f, 0.9f));
                n.nameTag.gameObject.AddComponent<Billboard>();
            }
            return n;
        }

        static Transform Limb(Transform parent, Vector3 pivotPos, float half, float thick, Material m)
        {
            var p = new GameObject("pivote"); p.transform.SetParent(parent, false); p.transform.localPosition = pivotPos;
            Part(p.transform, PrimitiveType.Capsule, new Vector3(0f, -half, 0f), new Vector3(thick * 2f, half, thick * 2f), m);
            return p.transform;
        }

        public void Face(Vector3 worldPos)
        {
            Vector3 d = worldPos - transform.position; d.y = 0f;
            if (d.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(d);
        }

        public IEnumerator WalkTo(Vector3 target, float speed = 1.3f)
        {
            walking = true;
            while ((new Vector3(target.x, 0, target.z) - new Vector3(transform.position.x, 0, transform.position.z)).magnitude > 0.08f)
            {
                Vector3 d = target - transform.position; d.y = 0f;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 6f);
                transform.position += d.normalized * speed * Time.deltaTime;
                yield return null;
            }
            walking = false;
        }

        public IEnumerator WalkPath(Vector3[] pts, float speed = 1.3f)
        {
            foreach (var p in pts) yield return WalkTo(p, speed);
        }

        void Update()
        {
            float t = Time.time + seed;
            if (walking)
            {
                phase += Time.deltaTime * 7f;
                float s = Mathf.Sin(phase) * 28f;
                legL.localRotation = Quaternion.Euler(s, 0f, 0f); legR.localRotation = Quaternion.Euler(-s, 0f, 0f);
                armL.localRotation = Quaternion.Euler(-s * 0.7f, 0f, 0f); armR.localRotation = Quaternion.Euler(s * 0.7f, 0f, 0f);
                body.localPosition = bodyBase + new Vector3(0f, Mathf.Abs(Mathf.Sin(phase)) * 0.03f, 0f);
            }
            else
            {
                legL.localRotation = Quaternion.identity; legR.localRotation = Quaternion.identity;
                armL.localRotation = Quaternion.Euler(Mathf.Sin(t * 0.9f) * 3f, 0f, 0f); armR.localRotation = Quaternion.Euler(-Mathf.Sin(t * 0.9f) * 3f, 0f, 0f);
                body.localPosition = bodyBase + new Vector3(0f, Mathf.Sin(t * 1.6f) * 0.006f, 0f);
                head.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 0.45f) * 18f, 0f);
            }
        }
    }
}
