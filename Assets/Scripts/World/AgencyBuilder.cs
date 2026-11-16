// Construye la Agencia Las Palmeras (escena «Agencia_VR») con primitivas y texturas procedurales.
// 4 zonas: Ventanilla, Sala de seguridad (CCTV), Bóveda y Oficina de la gerente + lobby con portal.
using UnityEngine;

namespace LasPalmeras
{
    public class LightFlicker : MonoBehaviour
    {
        public Light l; public float baseI = 1f; public float amount = 0.25f; float seed;
        void Start() { if (l == null) l = GetComponent<Light>(); seed = Random.value * 50f; }
        void Update() { if (l != null && l.enabled) l.intensity = baseI * (1f - amount * Mathf.PerlinNoise(Time.time * 9f, seed)); }
    }

    public class AgencyWorld
    {
        public Transform root, dyn, spawn;
        public TeleportPoint tpPortal, tpVentanilla, tpBoveda, tpCctv, tpOficina;
        public PortalRing portal;
        public FichaItem[] fichas = new FichaItem[5];
        public CctvFeed cctv; public EvidenceItem monitorItem, caja, terminal; public DefenseControl panel;
        public VaultDoor vault; public DormantAlert dormant;
        public Npc gerente, cajera, guardia, mensajero; public Npc[] titulares = new Npc[5];
        public GameObject cajaMensajero;
        public TextMesh clockBoard, terminalScreen, panelScreen;
        public Light cctvLight;
        public Vector3[] mensajeroPath;
    }

    public static class AgencyBuilder
    {
        static Transform S, D; static AgencyWorld W;
        static Material mFloor, mCeil, mWall, mTeal, mTealDark, mCoral, mSand, mWoodA, mWoodB, mSteel, mDark, mCarpet, mCarpetWarm, mGlass,
                        mPaper, mCardboard, mTape, mLeaf, mTrunk, mPot, mWarm, mScreen, mScreenGreen, mFabric, mConcrete, mGold, mBook1, mBook2, mBook3, mCash;

        static void InitMats()
        {
            mFloor = Mats.Lit(Color.white, 0.55f, 0f, Procedural.Tiles(256, 4, new Color(0.88f, 0.84f, 0.76f), new Color(0.78f, 0.74f, 0.66f), new Color(0.5f, 0.46f, 0.4f)), 5, 4.5f);
            mCeil = Mats.Lit(new Color(0.95f, 0.95f, 0.93f), 0.1f);
            mWall = Mats.Lit(Color.white, 0.12f, 0f, Procedural.Plaster(128, new Color(0.93f, 0.91f, 0.86f), 0.18f), 4, 1.5f);
            mTeal = Mats.Lit(new Color(0.04f, 0.43f, 0.43f), 0.4f);
            mTealDark = Mats.Lit(new Color(0.02f, 0.22f, 0.25f), 0.45f);
            mCoral = Mats.Lit(new Color(1f, 0.44f, 0.35f), 0.35f);
            mSand = Mats.Lit(new Color(0.95f, 0.88f, 0.72f), 0.2f);
            var woodTex = Procedural.Wood(128, new Color(0.40f, 0.24f, 0.12f), new Color(0.62f, 0.42f, 0.24f));
            mWoodA = Mats.Lit(Color.white, 0.5f, 0f, woodTex, 1, 1);
            mWoodB = Mats.Lit(new Color(0.8f, 0.7f, 0.6f), 0.35f, 0f, woodTex, 2, 1);
            mSteel = Mats.Lit(new Color(0.62f, 0.65f, 0.68f), 0.65f, 0.85f);
            mDark = Mats.Lit(new Color(0.07f, 0.08f, 0.09f), 0.4f);
            mCarpet = Mats.Lit(Color.white, 0.05f, 0f, Procedural.Carpet(128, new Color(0.08f, 0.26f, 0.28f)), 3, 3);
            mCarpetWarm = Mats.Lit(Color.white, 0.05f, 0f, Procedural.Carpet(128, new Color(0.42f, 0.2f, 0.16f)), 3, 3);
            mGlass = Mats.Alpha(new Color(0.65f, 0.88f, 0.95f, 0.16f));
            mPaper = Mats.Lit(Color.white, 0.2f, 0f, Procedural.Paper(64, 80));
            mCardboard = Mats.Lit(new Color(0.66f, 0.5f, 0.3f), 0.1f);
            mTape = Mats.Lit(new Color(0.85f, 0.75f, 0.5f), 0.4f);
            mLeaf = Mats.Lit(new Color(0.16f, 0.5f, 0.2f), 0.35f);
            mTrunk = Mats.Lit(new Color(0.42f, 0.3f, 0.2f), 0.2f);
            mPot = Mats.Lit(new Color(0.75f, 0.35f, 0.25f), 0.3f);
            mWarm = Mats.Unlit(new Color(1f, 0.93f, 0.78f), null, 1.3f);
            mScreen = Mats.Unlit(new Color(0.12f, 0.35f, 0.5f), null, 1.2f);
            mScreenGreen = Mats.Unlit(new Color(0.1f, 0.55f, 0.3f), null, 1.2f);
            mFabric = Mats.Lit(new Color(0.05f, 0.38f, 0.38f), 0.1f);
            mConcrete = Mats.Lit(new Color(0.55f, 0.57f, 0.58f), 0.15f);
            mGold = Mats.Lit(new Color(0.9f, 0.72f, 0.25f), 0.7f, 0.9f);
            mBook1 = Mats.Lit(new Color(0.6f, 0.15f, 0.15f), 0.2f); mBook2 = Mats.Lit(new Color(0.15f, 0.3f, 0.55f), 0.2f); mBook3 = Mats.Lit(new Color(0.8f, 0.7f, 0.3f), 0.2f);
            mCash = Mats.Lit(new Color(0.45f, 0.58f, 0.42f), 0.1f);
        }

        // ---------------- helpers ----------------
        static GameObject Prim(PrimitiveType t, Transform p, string n, Vector3 pos, Vector3 scale, Material m, bool col)
        {
            var g = GameObject.CreatePrimitive(t); g.name = n; g.transform.SetParent(p, false);
            g.transform.localPosition = pos; g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = m;
            if (!col) { var c = g.GetComponent<Collider>(); if (c != null) Object.Destroy(c); }
            return g;
        }
        static GameObject B(Transform p, float x, float y, float z, float sx, float sy, float sz, Material m, string n = "caja", bool col = true)
        { return Prim(PrimitiveType.Cube, p, n, new Vector3(x, y, z), new Vector3(sx, sy, sz), m, col); }
        static GameObject Cyl(Transform p, float x, float y, float z, float r, float h, Material m, string n = "cil", bool col = false, Vector3 euler = default(Vector3))
        { var g = Prim(PrimitiveType.Cylinder, p, n, new Vector3(x, y, z), new Vector3(r * 2f, h / 2f, r * 2f), m, col); g.transform.localEulerAngles = euler; return g; }
        static GameObject Sph(Transform p, float x, float y, float z, float sx, float sy, float sz, Material m, string n = "esf")
        { return Prim(PrimitiveType.Sphere, p, n, new Vector3(x, y, z), new Vector3(sx, sy, sz), m, false); }
        static GameObject Q(Transform p, Vector3 pos, Vector3 euler, float w, float h, Material m, string n = "quad")
        { var g = Prim(PrimitiveType.Quad, p, n, pos, new Vector3(w, h, 1f), m, false); g.transform.localEulerAngles = euler; return g; }
        static GameObject Glass(Transform p, float x, float y, float z, float sx, float sy, float sz)
        { var g = B(p, x, y, z, sx, sy, sz, mGlass, "vidrio"); g.layer = 2; return g; }
        static Transform Group(Transform p, string n, float x, float y, float z, float yaw)
        { var g = new GameObject(n); g.transform.SetParent(p, false); g.transform.localPosition = new Vector3(x, y, z); g.transform.localEulerAngles = new Vector3(0f, yaw, 0f); return g.transform; }
        static Transform Section(string n) { var g = new GameObject("Estatico_" + n); g.transform.SetParent(W.root, false); return g.transform; }
        static void Batch(Transform sec)
        {
            try { StaticBatchingUtility.Combine(sec.gameObject); } catch (System.Exception e) { Debug.Log("[ANDES-SIM] Static batching omitido: " + e.Message); }
        }
        static TextMesh T(string text, float x, float y, float z, float size, Color c, float yaw = 0f, TextAnchor a = TextAnchor.MiddleCenter)
        { return Txt.Make(D, text, new Vector3(x, y, z), size, c, a, FontStyle.Bold, new Vector3(0f, yaw, 0f)); }
        static Light PointLight(string n, float x, float y, float z, Color c, float intensity, float range)
        {
            var g = new GameObject(n); g.transform.SetParent(D, false); g.transform.localPosition = new Vector3(x, y, z);
            var l = g.AddComponent<Light>(); l.type = LightType.Point; l.color = c; l.intensity = intensity; l.range = range; l.shadows = LightShadows.None;
            return l;
        }
        static Collider TriggerBox(Transform p, Vector3 center, Vector3 size)
        { var b = p.gameObject.AddComponent<BoxCollider>(); b.center = center; b.size = size; b.isTrigger = true; return b; }

        static readonly Color Neon = new Color(0.22f, 0.95f, 0.54f);

        // ---------------- principal ----------------
        public static AgencyWorld Build(Transform parent)
        {
            W = new AgencyWorld();
            var rootG = new GameObject("Agencia_VR"); rootG.transform.SetParent(parent, false);
            W.root = rootG.transform;
            var dynG = new GameObject("Dinamico"); dynG.transform.SetParent(W.root, false); D = dynG.transform; W.dyn = D;
            InitMats();

            BuildShell();
            BuildLobby();
            BuildVentanilla();
            BuildCctvRoom();
            BuildVault();
            BuildOffice();
            BuildLights();
            BuildPads();
            BuildNpcs();

            var sp = new GameObject("Spawn"); sp.transform.SetParent(D, false); sp.transform.position = new Vector3(0f, 0f, 2.2f);
            W.spawn = sp.transform;
            return W;
        }

        // ---------------- cascarón ----------------
        static void BuildShell()
        {
            S = Section("Cascaron");
            B(S, 0, -0.05f, 9, 20, 0.1f, 18, mFloor, "Piso");
            B(S, 0, 3.45f, 9, 20, 0.1f, 18, mCeil, "Techo", false);
            B(S, 0, 1.7f, 18.1f, 20.4f, 3.4f, 0.2f, mWall, "Muro_Fondo");
            B(S, -10.1f, 1.7f, 14.75f, 0.2f, 3.4f, 6.5f, mWall, "Muro_Izq_Fondo");
            B(S, 10.1f, 1.7f, 14.75f, 0.2f, 3.4f, 6.5f, mWall, "Muro_Der_Fondo");
            // ventanales corridos (z 0 → 11.5)
            for (int s = -1; s <= 1; s += 2)
            {
                float x = 10.1f * s;
                B(S, x, 0.45f, 5.75f, 0.2f, 0.9f, 11.5f, mWall, "Zocalo");
                B(S, x, 3.0f, 5.75f, 0.2f, 0.8f, 11.5f, mWall, "Dintel", false);
                Glass(S, x, 1.75f, 5.75f, 0.06f, 1.7f, 11.5f);
                for (float z = 0f; z <= 11.6f; z += 2.3f) B(S, x, 1.75f, z, 0.14f, 1.7f, 0.1f, mTealDark, "Parante", false);
            }
            // frente de vidrio con puertas
            Glass(S, -5.8f, 1.5f, -0.05f, 8.4f, 3.0f, 0.06f);
            Glass(S, 5.8f, 1.5f, -0.05f, 8.4f, 3.0f, 0.06f);
            B(S, 0, 3.2f, -0.05f, 20f, 0.5f, 0.2f, mTeal, "Dintel_Frente", false);
            B(S, -1.6f, 1.5f, -0.05f, 0.14f, 3.0f, 0.14f, mTealDark, "Jamba", false);
            B(S, 1.6f, 1.5f, -0.05f, 0.14f, 3.0f, 0.14f, mTealDark, "Jamba", false);
            for (float x = -9.5f; x <= 9.6f; x += 2.1f) if (Mathf.Abs(x) > 1.2f) B(S, x, 1.5f, -0.05f, 0.1f, 3.0f, 0.1f, mTealDark, "Parante", false);
            // exterior: calle y panorama de ciudad al atardecer
            B(S, 0, -0.1f, -9f, 60, 0.1f, 20, Mats.Lit(new Color(0.12f, 0.13f, 0.16f), 0.1f), "Calle", false);
            B(S, 0, -0.1f, 9f, 60, 0.05f, 18f, Mats.Lit(new Color(0.12f, 0.13f, 0.16f), 0.1f), "Suelo_ext", false).transform.position = new Vector3(0f, -0.12f, 9f);
            var city = Procedural.City(512, 256); var mCity = Mats.UnlitTwoSided(Color.white, city, 1f);
            var bg = new GameObject("Panorama"); bg.transform.SetParent(D, false);
            Q(bg.transform, new Vector3(0f, 4.5f, -16f), new Vector3(0f, 180f, 0f), 46f, 14f, mCity, "pan_frente");
            Q(bg.transform, new Vector3(-15.5f, 4.5f, 6f), new Vector3(0f, -90f, 0f), 40f, 14f, mCity, "pan_izq");
            Q(bg.transform, new Vector3(15.5f, 4.5f, 6f), new Vector3(0f, 90f, 0f), 40f, 14f, mCity, "pan_der");
            // luminarias de techo
            for (float z = 2f; z <= 8f; z += 3f) for (float x = -6f; x <= 6f; x += 6f) B(S, x, 3.39f, z, 3.2f, 0.03f, 0.5f, mWarm, "Luminaria", false);
            Batch(S);
        }

        // ---------------- lobby ----------------
        static void BuildLobby()
        {
            S = Section("Lobby");
            // inlay circular en el piso
            Cyl(S, 0, 0.005f, 5.2f, 1.9f, 0.01f, mTealDark, "Inlay");
            Cyl(S, 0, 0.008f, 5.2f, 1.5f, 0.01f, mSand, "Inlay2");
            // sofás de espera
            for (int i = 0; i < 2; i++)
            {
                var g = Group(S, "Sofa", 8.2f, 0f, 2.2f + i * 3.2f, 90f);
                B(g, 0, 0.25f, 0, 2.2f, 0.4f, 0.9f, mFabric); B(g, 0, 0.62f, 0.38f, 2.2f, 0.5f, 0.2f, mFabric);
                B(g, -1.1f, 0.4f, 0, 0.18f, 0.65f, 0.9f, mTealDark); B(g, 1.1f, 0.4f, 0, 0.18f, 0.65f, 0.9f, mTealDark);
            }
            B(S, 6.7f, 0.22f, 3.8f, 0.9f, 0.04f, 1.4f, mWoodA, "Mesa_top"); B(S, 6.7f, 0.1f, 3.8f, 0.7f, 0.2f, 1.2f, mDark, "Mesa_base");
            // totem de turnos
            B(S, 3.4f, 0.8f, 1.8f, 0.5f, 1.6f, 0.4f, mTealDark, "Totem"); B(S, 3.4f, 1.15f, 1.59f, 0.36f, 0.5f, 0.02f, mScreenGreen, "Totem_pantalla", false);
            // plataforma de atención (decorativa)
            for (int i = 0; i < 2; i++)
            {
                var g = Group(S, "Plataforma", 5.6f + i * 2.6f, 0f, 7.6f, 180f);
                B(g, 0, 0.38f, 0, 1.8f, 0.76f, 0.8f, mWoodB); B(g, 0, 0.78f, 0.1f, 1.8f, 0.04f, 0.9f, mWoodA);
                B(g, 0, 1.05f, -0.1f, 0.5f, 0.32f, 0.03f, mDark); B(g, 0, 1.05f, -0.115f, 0.46f, 0.28f, 0.01f, mScreen, "pant", false);
                Chair(S, 5.6f + i * 2.6f, 8.5f, 0f);
            }
            // palmeras (¡Las Palmeras!)
            Palm(-9.2f, 1.0f, 3.2f, 1.1f); Palm(9.2f, 1.0f, 3.1f, 1.1f); Palm(-9.2f, 8.2f, 3.0f, 1.0f); Palm(9.2f, 8.2f, 3.3f, 1.15f);
            Palm(-2.8f, 1.0f, 2.6f, 0.9f); Palm(3.0f, 12.2f, 2.4f, 0.8f);
            Batch(S);

            // reloj de la agencia (pared derecha) — muestra la hora de la simulación
            var board = D;
            B(D, 9.93f, 2.5f, 5.0f, 0.05f, 0.7f, 2.5f, mDark, "Reloj_marco", false);
            W.clockBoard = T("--", 9.88f, 2.5f, 5.0f, 0.2f, Neon, 90f);
            T("RELOJ DE LA AGENCIA", 9.88f, 2.78f, 5.0f, 0.07f, new Color(0.6f, 0.8f, 0.7f), 90f);

            // Portal ANDES-SIM
            BuildPortal();
        }

        static void BuildPortal()
        {
            var g = new GameObject("Portal_ANDES_SIM"); g.transform.SetParent(D, false); g.transform.position = new Vector3(0f, 0f, 5.2f);
            var ringMat = Mats.Unlit(new Color(0.15f, 0.95f, 0.75f), null, 1.4f);
            int n = 28; float R = 1.3f;
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                var b = B(g.transform, Mathf.Cos(a) * R, 1.45f + Mathf.Sin(a) * R, 0f, 0.26f, 0.12f, 0.22f, ringMat, "arco", false);
                b.transform.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg);
            }
            Cyl(g.transform, 0f, 0.03f, 0f, 1.6f, 0.06f, Mats.Unlit(new Color(0.02f, 0.2f, 0.18f), null, 1f), "base");
            var sw = new GameObject("remolino"); sw.transform.SetParent(g.transform, false); sw.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            var swirl = Procedural.Swirl(128);
            var mat = Mats.AdditiveTwoSided(new Color(0.2f, 0.9f, 1f), swirl, 1.4f);
            var qd = Q(sw.transform, Vector3.zero, Vector3.zero, 2.5f, 2.5f, mat, "remolino_q");
            var glowTex = Procedural.Glow(64);
            Q(g.transform, new Vector3(0f, 1.45f, 0.02f), Vector3.zero, 2.2f, 2.2f, Mats.AdditiveTwoSided(new Color(0.05f, 0.35f, 0.4f), glowTex, 1f), "halo_portal");
            var p = g.AddComponent<PortalRing>();
            p.swirl = sw.transform;
            TriggerBox(g.transform, new Vector3(0f, 1.45f, 0f), new Vector3(2.8f, 2.9f, 0.8f));
            p.prompt = "Cruzar el portal ANDES-SIM";
            p.AddHalo(new Vector3(0f, 1.45f, -0.1f), 4.2f, new Color(0.2f, 0.9f, 1f));
            var lab = T("PORTAL ANDES-SIM", 0f, 3.05f, 5.2f, 0.2f, new Color(0.4f, 1f, 0.9f)); lab.gameObject.AddComponent<Billboard>();
            var lab2 = T("Escenario 07 · Agencia Las Palmeras", 0f, 2.82f, 5.2f, 0.1f, new Color(0.7f, 0.95f, 0.9f)); lab2.gameObject.AddComponent<Billboard>();
            W.portal = p;
        }

        // ---------------- ventanilla ----------------
        static void BuildVentanilla()
        {
            S = Section("Ventanilla");
            // mostrador
            B(S, -4.25f, 0.55f, 9.0f, 11.5f, 1.1f, 0.9f, mTeal, "Mostrador");
            B(S, -4.25f, 1.12f, 9.0f, 11.7f, 0.06f, 1.05f, mWoodA, "Mostrador_top", false);
            B(S, 1.55f, 0.55f, 9.0f, 0.15f, 1.1f, 1.05f, mTealDark, "Mostrador_extremo");
            // ventanillas de vidrio
            for (int i = 0; i < 3; i++)
            {
                float cx = -7.6f + i * 3.6f;
                Glass(S, cx, 1.75f, 9.35f, 3.1f, 1.1f, 0.03f);
                B(S, cx - 1.6f, 1.75f, 9.35f, 0.08f, 1.15f, 0.08f, mSteel, "marco", false);
                B(S, cx + 1.6f, 1.75f, 9.35f, 0.08f, 1.15f, 0.08f, mSteel, "marco", false);
                B(S, cx, 2.33f, 9.35f, 3.2f, 0.07f, 0.08f, mSteel, "marco", false);
            }
            // escritorios y pantallas del personal
            B(S, -4.5f, 0.4f, 10.7f, 9f, 0.8f, 0.8f, mWoodB, "Escritorios_personal");
            for (int i = 0; i < 3; i++)
            {
                float cx = -7.6f + i * 3.6f;
                B(S, cx, 1.02f, 10.55f, 0.55f, 0.34f, 0.03f, mDark, "monitor", false);
                B(S, cx, 1.02f, 10.535f, 0.5f, 0.3f, 0.01f, mScreen, "pantalla", false);
                Chair(S, cx, 11.4f, 180f);
            }
            Batch(S);

            // letrero colgante
            B(D, -4.25f, 3.0f, 9.2f, 6.2f, 0.8f, 0.1f, mTeal, "Letrero", false);
            B(D, -6.6f, 3.35f, 9.2f, 0.04f, 0.5f, 0.04f, mSteel, "tirante", false); B(D, -1.9f, 3.35f, 9.2f, 0.04f, 0.5f, 0.04f, mSteel, "tirante", false);
            T("AGENCIA LAS PALMERAS", -3.7f, 3.08f, 9.14f, 0.3f, Color.white, 180f);
            T("BANCO PACÍFICO INSULAR", -3.7f, 2.82f, 9.14f, 0.13f, new Color(1f, 0.8f, 0.7f), 180f);
            T("AGENCIA LAS PALMERAS", -3.7f, 3.08f, 9.26f, 0.3f, Color.white, 0f);
            T("BANCO PACÍFICO INSULAR", -3.7f, 2.82f, 9.26f, 0.13f, new Color(1f, 0.8f, 0.7f), 0f);
            // logo: palmera estilizada
            var lg = Group(D, "Logo", -6.55f, 3.0f, 9.27f, 0f);
            Cyl(lg, 0f, -0.05f, 0f, 0.025f, 0.45f, mSand, "tronco");
            for (int i = 0; i < 6; i++)
            {
                var piv = new GameObject("hoja"); piv.transform.SetParent(lg, false); piv.transform.localPosition = new Vector3(0f, 0.18f, 0f);
                piv.transform.localEulerAngles = new Vector3(0f, 0f, -75f + i * 30f);
                Sph(piv.transform, 0f, 0.14f, 0f, 0.05f, 0.3f, 0.03f, mCoral);
            }
            T("VENTANILLA", -7.6f, 2.55f, 9.3f, 0.12f, new Color(0.7f, 1f, 0.9f), 0f);

            // las 5 fichas de apertura (Ficha_Apertura_01..05)
            var data = GameManager.I.data;
            for (int i = 0; i < 5; i++)
            {
                float x = -6.4f + i * 1.2f;
                var g = new GameObject("Ficha_Apertura_0" + (i + 1)); g.transform.SetParent(D, false);
                g.transform.position = new Vector3(x, 1.16f, 8.7f); g.transform.rotation = Quaternion.Euler(0f, Random.Range(-14f, 14f), 0f);
                B(g.transform, 0, 0, 0, 0.3f, 0.012f, 0.4f, mPaper, "hoja", false);
                var tm = Txt.Make(g.transform, "FICHA " + (i + 1).ToString("00"), new Vector3(0f, 0.012f, 0.12f), 0.05f, new Color(0.1f, 0.2f, 0.2f), TextAnchor.MiddleCenter, FontStyle.Bold, new Vector3(90f, 0f, 0f));
                var fi = g.AddComponent<FichaItem>(); fi.data = data.fichas[i];
                TriggerBox(g.transform, new Vector3(0f, 0.06f, 0f), new Vector3(0.45f, 0.14f, 0.55f));
                fi.AddHalo(new Vector3(0f, 0.12f, 0f), 0.75f, new Color(1f, 0.9f, 0.5f));
                W.fichas[i] = fi;
            }
            // verificador KYC (componente de la ventanilla)
            var kg = new GameObject("KycChecker"); kg.transform.SetParent(D, false); kg.AddComponent<KycChecker>();
        }

        // ---------------- sala de seguridad (CCTV) ----------------
        static void BuildCctvRoom()
        {
            S = Section("CCTV");
            B(S, -4.0f, 1.7f, 14.75f, 0.2f, 3.4f, 6.5f, mWall, "Muro_der_cctv");
            Glass(S, -8.75f, 1.7f, 11.5f, 2.5f, 3.3f, 0.05f); Glass(S, -5.25f, 1.7f, 11.5f, 2.5f, 3.3f, 0.05f);
            B(S, -7.0f, 3.2f, 11.5f, 1.1f, 0.5f, 0.1f, mTealDark, "Dintel_puerta", false);
            B(S, -7.0f, 0.005f, 14.75f, 6f, 0.01f, 6.5f, mCarpet, "Alfombra", false).transform.position = new Vector3(-7f, 0.006f, 14.75f);
            // escritorio de monitoreo
            B(S, -7.0f, 0.4f, 16.3f, 4.2f, 0.8f, 0.9f, mWoodB, "Escritorio_cctv");
            B(S, -7.0f, 0.82f, 16.1f, 0.5f, 0.02f, 0.18f, mDark, "teclado", false);
            Chair(S, -7.0f, 15.3f, 0f);
            // pared de monitores
            B(S, -7.0f, 1.95f, 17.97f, 3.2f, 1.85f, 0.08f, mDark, "Marco_monitor_principal", false);
            B(S, -9.35f, 1.95f, 17.97f, 1.2f, 0.8f, 0.08f, mDark, "marco", false); B(S, -4.65f, 1.95f, 17.97f, 1.2f, 0.8f, 0.08f, mDark, "marco", false);
            B(S, -9.35f, 2.9f, 17.97f, 1.2f, 0.8f, 0.08f, mDark, "marco", false); B(S, -4.65f, 2.9f, 17.97f, 1.2f, 0.8f, 0.08f, mDark, "marco", false);
            Batch(S);

            // monitor principal: Monitor_CCTV con shader de ruido
            var mon = new GameObject("Monitor_CCTV"); mon.transform.SetParent(D, false); mon.transform.position = new Vector3(-7f, 1.95f, 17.9f);
            var scr = Q(mon.transform, Vector3.zero, Vector3.zero, 3.0f, 1.69f, Mats.Unlit(Color.black), "pantalla");
            var title = Txt.Make(mon.transform, "SELECCIONA UNA FECHA", new Vector3(-1.4f, 0.74f, -0.02f), 0.11f, new Color(0.8f, 1f, 0.85f), TextAnchor.MiddleLeft);
            var sub = Txt.Make(mon.transform, "", new Vector3(0f, 0f, -0.02f), 0.3f, new Color(1f, 0.35f, 0.3f), TextAnchor.MiddleCenter);
            var rec = Txt.Make(mon.transform, "● REC", new Vector3(1.38f, 0.74f, -0.02f), 0.12f, Color.red, TextAnchor.MiddleRight);
            rec.gameObject.SetActive(false);
            var feed = mon.AddComponent<CctvFeed>();
            feed.Setup(scr.GetComponent<Renderer>(), title, sub, rec);
            // pantallas pequeñas
            Vector3[] sp = { new Vector3(-9.35f, 1.95f, 17.9f), new Vector3(-4.65f, 1.95f, 17.9f), new Vector3(-9.35f, 2.9f, 17.9f), new Vector3(-4.65f, 2.9f, 17.9f) };
            feed.others = new System.Collections.Generic.List<Material>();
            for (int i = 0; i < sp.Length; i++)
            {
                var m = Mats.Cctv(); feed.others.Add(m);
                Q(mon.transform, sp[i] - mon.transform.position, Vector3.zero, 1.1f, 0.62f, m, "cam_" + (i + 2));
                T("CAM-0" + (i + 2), sp[i].x - 0.5f, sp[i].y + 0.26f, 17.87f, 0.06f, new Color(0.7f, 1f, 0.8f), 0f, TextAnchor.MiddleLeft);
            }
            var tr = mon.AddComponent<EvidenceItem>();
            tr.title = "Monitor de cámaras"; tr.prompt = "Revisar las grabaciones de las cámaras"; tr.maxDistance = 6f;
            tr.handler = it => GameManager.I.ui.OpenCctv();
            TriggerBox(mon.transform, Vector3.zero, new Vector3(3.2f, 1.9f, 0.5f));
            tr.AddHalo(new Vector3(0f, 0f, -0.3f), 4.2f, new Color(0.5f, 0.9f, 1f));
            W.cctv = feed; W.monitorItem = tr;
            W.cctvLight = PointLight("Luz_cctv", -7f, 2.8f, 14.5f, new Color(0.6f, 0.8f, 1f), 0.9f, 6.5f);
            var fl = W.cctvLight.gameObject.AddComponent<LightFlicker>(); fl.l = W.cctvLight; fl.baseI = 0.9f;
            T("SALA DE SEGURIDAD", -7f, 2.95f, 11.42f, 0.2f, new Color(0.6f, 1f, 0.9f), 180f);

            // Panel_Alertas (DefenseControl) en la pared derecha de la sala
            var pa = new GameObject("Panel_Alertas"); pa.transform.SetParent(D, false); pa.transform.position = new Vector3(-4.06f, 1.6f, 14.8f);
            B(pa.transform, 0f, 0f, 0f, 0.12f, 1.35f, 1.1f, mDark, "carcasa", false);
            B(pa.transform, -0.07f, 0.2f, 0f, 0.02f, 0.7f, 0.95f, Mats.Unlit(new Color(0.03f, 0.12f, 0.08f)), "pantalla", false);
            var leds = new Renderer[3];
            for (int i = 0; i < 3; i++)
            {
                var l = Cyl(pa.transform, -0.075f, -0.38f, -0.3f + i * 0.3f, 0.07f, 0.02f, Mats.Unlit(new Color(0.5f, 0.1f, 0.08f)), "led", false, new Vector3(0f, 0f, 90f));
                leds[i] = l.GetComponent<Renderer>();
            }
            W.panelScreen = T("PANEL DE ALERTAS", -4.14f, 1.95f, 14.8f, 0.09f, Neon, 90f);
            T("KYC · CUENTAS DORMIDAS · RETENCIÓN", -4.14f, 1.82f, 14.8f, 0.045f, new Color(0.7f, 0.95f, 0.8f), 90f);
            var dc = pa.AddComponent<DefenseControl>(); dc.SetupLeds(leds);
            dc.prompt = "Abrir el panel de alertas y controles"; dc.maxDistance = 5.5f;
            TriggerBox(pa.transform, Vector3.zero, new Vector3(0.5f, 1.5f, 1.3f));
            dc.AddHalo(new Vector3(-0.3f, 0.2f, 0f), 1.8f);
            W.panel = dc;
        }

        // ---------------- bóveda ----------------
        static void BuildVault()
        {
            S = Section("Boveda");
            B(S, -2.05f, 1.7f, 12.5f, 1.9f, 3.4f, 0.4f, mConcrete, "Boveda_frente_izq");
            B(S, 2.05f, 1.7f, 12.5f, 1.9f, 3.4f, 0.4f, mConcrete, "Boveda_frente_der");
            B(S, 0f, 2.95f, 12.5f, 2.2f, 0.9f, 0.4f, mConcrete, "Boveda_frente_sup");
            B(S, 0f, 0.07f, 12.5f, 2.2f, 0.14f, 0.5f, mSteel, "Umbral");
            B(S, -3.0f, 1.7f, 15.25f, 0.3f, 3.4f, 5.5f, mConcrete, "Boveda_izq");
            B(S, 3.0f, 1.7f, 15.25f, 0.3f, 3.4f, 5.5f, mConcrete, "Boveda_der");
            B(S, 0f, 0.01f, 15.25f, 5.7f, 0.02f, 5.5f, mDark, "Piso_boveda", false).transform.position = new Vector3(0f, 0.012f, 15.25f);
            // cajas de seguridad
            for (int s = -1; s <= 1; s += 2)
                for (int r = 0; r < 6; r++)
                    for (int c = 0; c < 8; c++)
                        B(S, 2.82f * s, 0.4f + r * 0.38f, 13.3f + c * 0.58f, 0.06f, 0.32f, 0.5f, mSteel, "cofre", false);
            // paquetes de billetes (decorativo)
            for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++) B(S, -1.0f + i * 1.0f, 0.2f + j * 0.22f, 17.2f, 0.7f, 0.2f, 0.4f, mCash, "fajo", false);
            Batch(S);

            // puerta circular articulada
            var pivot = new GameObject("Boveda_puerta_pivote"); pivot.transform.SetParent(D, false); pivot.transform.position = new Vector3(-1.1f, 1.25f, 12.3f);
            var door = Cyl(pivot.transform, 1.1f, 0f, 0f, 1.05f, 0.34f, mSteel, "Boveda", false, new Vector3(90f, 0f, 0f));
            Cyl(pivot.transform, 1.1f, 0f, -0.2f, 0.28f, 0.1f, mGold, "eje", false, new Vector3(90f, 0f, 0f));
            for (int i = 0; i < 3; i++)
            {
                var sp = B(pivot.transform, 1.1f, 0f, -0.24f, 1.1f, 0.07f, 0.07f, mGold, "rayo", false);
                sp.transform.localRotation = Quaternion.Euler(0f, 0f, i * 60f);
            }
            var dcol = new GameObject("colision_puerta"); dcol.transform.SetParent(pivot.transform, false);
            var dbc = dcol.AddComponent<BoxCollider>(); dbc.center = new Vector3(1.1f, 0f, 0f); dbc.size = new Vector3(2.1f, 2.1f, 0.34f);
            var lamp = B(D, 0f, 2.7f, 12.28f, 0.3f, 0.18f, 0.05f, Mats.Unlit(new Color(1f, 0.2f, 0.15f)), "Lampara_boveda", false);
            var inside = PointLight("Luz_boveda", 0f, 2.8f, 15.5f, new Color(0.85f, 0.92f, 1f), 0.9f, 7f);
            var vd = pivot.AddComponent<VaultDoor>(); vd.Setup(pivot.transform, lamp.GetComponent<Renderer>(), inside);
            W.vault = vd;
            T("BÓVEDA", 0f, 3.1f, 12.2f, 0.28f, new Color(1f, 0.85f, 0.4f), 180f);

            // terminal «Registro de retiros»
            var term = new GameObject("Terminal_Registro"); term.transform.SetParent(D, false); term.transform.position = new Vector3(-2.1f, 0f, 11.1f);
            B(term.transform, 0f, 0.55f, 0f, 0.6f, 1.1f, 0.45f, mDark, "base");
            var sc = B(term.transform, 0f, 1.2f, -0.05f, 0.62f, 0.38f, 0.05f, mDark, "marco", false);
            sc.transform.localRotation = Quaternion.Euler(-22f, 0f, 0f);
            var ps = B(term.transform, 0f, 1.2f, -0.082f, 0.55f, 0.31f, 0.01f, mScreenGreen, "pantalla", false);
            ps.transform.localRotation = Quaternion.Euler(-22f, 0f, 0f);
            W.terminalScreen = Txt.Make(term.transform, "REGISTRO\nDE RETIROS", new Vector3(0f, 1.2f, -0.095f), 0.07f, new Color(0.02f, 0.12f, 0.06f), TextAnchor.MiddleCenter, FontStyle.Bold, new Vector3(-22f, 0f, 0f));
            var ti = term.AddComponent<EvidenceItem>();
            ti.title = "Registro de cuentas y retiros"; ti.prompt = "Abrir el registro de cuentas y retiros"; ti.maxDistance = 5f;
            ti.handler = it => GameManager.I.ui.OpenTerminal(0);
            TriggerBox(term.transform, new Vector3(0f, 0.8f, 0f), new Vector3(0.9f, 1.8f, 0.8f));
            ti.AddHalo(new Vector3(0f, 1.2f, -0.3f), 1.6f);
            W.terminal = ti;

            // lámpara de alarma (se enciende con DormantAlert) en el techo del hall
            var al = new GameObject("Alerta_CuentasInactivas"); al.transform.SetParent(D, false); al.transform.position = new Vector3(0f, 3.25f, 9.5f);
            Cyl(al.transform, 0f, 0.1f, 0f, 0.14f, 0.2f, mDark, "base_alarma");
            var spin = new GameObject("giro"); spin.transform.SetParent(al.transform, false);
            var lampR = Cyl(spin.transform, 0f, -0.05f, 0f, 0.12f, 0.16f, Mats.Unlit(new Color(1f, 0.15f, 0.1f)), "bombilla");
            var rl = new GameObject("luz_roja"); rl.transform.SetParent(spin.transform, false); rl.transform.localPosition = new Vector3(0.4f, -0.1f, 0f);
            var light = rl.AddComponent<Light>(); light.type = LightType.Spot; light.color = new Color(1f, 0.1f, 0.05f); light.intensity = 3f; light.range = 9f; light.spotAngle = 70f;
            rl.transform.localRotation = Quaternion.Euler(80f, 90f, 0f); light.shadows = LightShadows.None;
            var da = al.AddComponent<DormantAlert>(); da.SetupLamp(lampR.GetComponent<Renderer>(), light, spin.transform);
            W.dormant = da;
        }

        // ---------------- oficina de la gerente ----------------
        static void BuildOffice()
        {
            S = Section("Oficina");
            B(S, 7.0f, 0.006f, 14.75f, 6f, 0.01f, 6.5f, mCarpetWarm, "Alfombra", false);
            Glass(S, 4.0f, 1.7f, 14.75f, 0.05f, 3.3f, 6.5f);
            Glass(S, 4.6f + 2.5f + 0.35f, 1.7f, 11.5f, 4.6f, 3.3f, 0.05f);
            B(S, 4.6f, 3.2f, 11.5f, 1.2f, 0.5f, 0.1f, mTealDark, "Dintel_puerta", false);
            // escritorio de la gerente
            B(S, 7.5f, 0.38f, 16.0f, 2.4f, 0.76f, 1.0f, mWoodA, "Escritorio_gerente");
            B(S, 7.2f, 1.0f, 16.1f, 0.5f, 0.32f, 0.03f, mDark, "monitor", false);
            B(S, 7.2f, 1.0f, 16.085f, 0.46f, 0.28f, 0.01f, mScreen, "pantalla", false);
            Chair(S, 7.5f, 16.95f, 180f);
            Chair(S, 6.7f, 14.7f, 0f); Chair(S, 8.3f, 14.7f, 0f);
            // librero
            B(S, 9.72f, 1.1f, 14.5f, 0.4f, 2.2f, 3.2f, mWoodB, "Librero");
            for (int i = 0; i < 18; i++)
            {
                var m = i % 3 == 0 ? mBook1 : (i % 3 == 1 ? mBook2 : mBook3);
                B(S, 9.5f, 0.45f + (i / 6) * 0.65f, 13.2f + (i % 6) * 0.5f, 0.12f, 0.42f, 0.34f, m, "libro", false);
            }
            // gráfico de metas en la pared del fondo
            B(S, 6.0f, 2.0f, 17.95f, 2.4f, 1.3f, 0.05f, mDark, "Tablero_metas", false);
            for (int i = 0; i < 5; i++) B(S, 5.1f + i * 0.45f, 1.55f + i * 0.14f, 17.9f, 0.3f, 0.3f + i * 0.28f, 0.03f, i == 4 ? mCoral : mTeal, "barra", false).transform.position = new Vector3(5.1f + i * 0.45f, 1.5f + (0.3f + i * 0.28f) / 2f, 17.9f);
            B(S, 8.9f, 2.2f, 17.95f, 0.8f, 0.6f, 0.04f, mSand, "Diploma", false);
            Batch(S);

            T("GERENCIA", 4.6f, 3.0f, 11.42f, 0.2f, new Color(1f, 0.85f, 0.7f), 180f);
            T("META DE CAPTACIÓN DEL TRIMESTRE", 6.0f, 2.52f, 17.9f, 0.07f, new Color(1f, 0.85f, 0.5f), 0f);
            B(D, 7.5f, 0.78f, 15.55f, 0.35f, 0.08f, 0.1f, mGold, "Placa", false);
            T("Marta Vidal · Gerente", 7.5f, 0.78f, 15.49f, 0.035f, Color.black, 180f);
            Palm(9.2f, 12.3f, 2.2f, 0.8f);

            // Caja_Carton (aparece cuando la historia la revela)
            var cj = new GameObject("Caja_Carton"); cj.transform.SetParent(D, false); cj.transform.position = new Vector3(5.6f, 0f, 14.5f);
            cj.transform.rotation = Quaternion.Euler(0f, 18f, 0f);
            B(cj.transform, 0f, 0.19f, 0f, 0.58f, 0.38f, 0.42f, mCardboard, "caja", false);
            B(cj.transform, 0f, 0.385f, 0f, 0.1f, 0.01f, 0.43f, mTape, "cinta", false);
            B(cj.transform, 0f, 0.19f, 0.211f, 0.1f, 0.38f, 0.01f, mTape, "cinta2", false);
            var ci = cj.AddComponent<EvidenceItem>();
            ci.evidenceId = "CAJA_CARTON"; ci.title = "La caja de cartón"; ci.prompt = "Examinar la caja de cartón"; ci.maxDistance = 6f;
            ci.body = "El viernes 5 de febrero, la demanda alega que se entregaron 20 millones de pesos en efectivo a la sucursal dentro de una caja de cartón. El efectivo quedó allí para que William So Go lo recogiera; el expediente citado no fija aquí la hora exacta.\n\nLa demanda también alega que las cámaras de la sucursal no funcionaban del 4 al 9 de febrero, por lo que la reconstrucción visual de la entrega y la recogida queda limitada.\n\nLección: una entrega extraordinaria en efectivo sin verificación reforzada, trazabilidad ni video deja un punto ciego de control.";
            TriggerBox(cj.transform, new Vector3(0f, 0.25f, 0f), new Vector3(0.8f, 0.6f, 0.7f));
            ci.AddHalo(new Vector3(0f, 0.3f, 0f), 1.3f, new Color(1f, 0.8f, 0.4f));
            cj.SetActive(false);
            W.caja = ci;
            W.cajaMensajero = null;
        }

        // ---------------- luces ----------------
        static void BuildLights()
        {
            var sun = new GameObject("Luz_ocaso"); sun.transform.SetParent(D, false);
            sun.transform.rotation = Quaternion.Euler(38f, -35f, 0f);
            var l = sun.AddComponent<Light>(); l.type = LightType.Directional; l.color = new Color(1f, 0.8f, 0.62f); l.intensity = 0.42f; l.shadows = LightShadows.None;
            Color warm = new Color(1f, 0.92f, 0.8f);
            PointLight("Luz_hall_1", -5f, 3.0f, 3.5f, warm, 0.8f, 9f); PointLight("Luz_hall_2", 5f, 3.0f, 3.5f, warm, 0.8f, 9f);
            PointLight("Luz_hall_3", -4f, 3.0f, 7.0f, warm, 0.8f, 9f); PointLight("Luz_hall_4", 5.5f, 3.0f, 7.0f, warm, 0.8f, 9f);
            PointLight("Luz_personal", -4.5f, 3.0f, 10.6f, warm, 0.7f, 7f);
            PointLight("Luz_oficina", 7f, 2.8f, 14.5f, new Color(1f, 0.85f, 0.65f), 1.0f, 7f);
            PointLight("Luz_pasillo", 0f, 3.0f, 10.2f, warm, 0.5f, 6f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.6f, 0.7f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.41f, 0.42f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.2f, 0.2f);
            RenderSettings.fog = false;
        }

        // ---------------- plataformas de teletransporte ----------------
        static TeleportPoint Pad(string n, string zone, float x, float z, float yaw)
        {
            var g = new GameObject("TP_" + n); g.transform.SetParent(D, false); g.transform.position = new Vector3(x, 0.02f, z);
            var d = Q(g.transform, Vector3.zero, new Vector3(90f, 0f, 0f), 1.25f, 1.25f, Mats.Additive(new Color(0.1f, 0.9f, 0.7f), Procedural.Glow(64), 1f), "disco");
            var r = Q(g.transform, new Vector3(0f, 0.005f, 0f), new Vector3(90f, 0f, 0f), 1.4f, 1.4f, Mats.Additive(new Color(0.1f, 0.9f, 0.7f), Procedural.Ring(128), 1f), "anillo");
            var tp = g.AddComponent<TeleportPoint>(); tp.zoneName = zone; tp.yaw = yaw; tp.prompt = "Ir a: " + zone; tp.quiet = true;
            tp.Setup(d.GetComponent<Renderer>(), r.GetComponent<Renderer>());
            TriggerBox(g.transform, new Vector3(0f, 0.12f, 0f), new Vector3(1.3f, 0.3f, 1.3f));
            var lab = Txt.Make(g.transform, zone, new Vector3(0f, 1.0f, 0f), 0.1f, new Color(0.6f, 1f, 0.9f)); lab.gameObject.AddComponent<Billboard>();
            return tp;
        }

        static void BuildPads()
        {
            W.tpPortal = Pad("Portal", "Entrada", 0f, 2.2f, 0f);
            W.tpVentanilla = Pad("Ventanilla", "Ventanilla", -4.0f, 6.3f, 0f);
            W.tpBoveda = Pad("Boveda", "Bóveda", 0.2f, 10.5f, 0f);
            W.tpCctv = Pad("CCTV", "Sala de seguridad", -7.0f, 13.9f, 0f);
            W.tpOficina = Pad("Oficina", "Oficina de la gerente", 6.4f, 12.9f, 0f);
        }

        // ---------------- personajes ----------------
        static void BuildNpcs()
        {
            W.gerente = Npc.Create(D, "Marta", "Marta Vidal · Gerente", new Color(0.85f, 0.65f, 0.5f), new Color(0.65f, 0.12f, 0.2f), new Color(0.12f, 0.12f, 0.16f), false, new Color(0.15f, 0.08f, 0.05f));
            W.gerente.transform.position = new Vector3(8.9f, 0f, 14.9f); W.gerente.Face(new Vector3(6.4f, 0f, 12.9f));
            W.cajera = Npc.Create(D, "Cajera", "Cajera", new Color(0.7f, 0.5f, 0.38f), new Color(0.04f, 0.43f, 0.43f), new Color(0.1f, 0.1f, 0.14f), false, new Color(0.08f, 0.05f, 0.04f));
            W.cajera.transform.position = new Vector3(-7.6f, 0f, 10.0f); W.cajera.Face(new Vector3(-7.6f, 0f, 6f));
            W.guardia = Npc.Create(D, "Guardia", "Guardia de seguridad", new Color(0.8f, 0.6f, 0.45f), new Color(0.12f, 0.14f, 0.2f), new Color(0.1f, 0.1f, 0.13f), false, new Color(0.1f, 0.07f, 0.05f));
            W.guardia.transform.position = new Vector3(-5.1f, 0f, 15.6f); W.guardia.Face(new Vector3(-7f, 0f, 13.9f));
            for (int i = 0; i < 5; i++)
            {
                var d = GameManager.I.data.fichas[i];
                var n = Npc.Create(D, "Titular" + (i + 1), "Titular " + (i + 1) + " (identidad falsa)", Color.white, d.tinte, d.tinte * 0.5f, true);
                n.transform.position = new Vector3(-5.8f + i * 1.2f, 0f, 7.7f); n.Face(new Vector3(-5.8f + i * 1.2f, 0f, 12f));
                n.gameObject.SetActive(false);
                W.titulares[i] = n;
            }
            W.mensajero = Npc.Create(D, "Mensajero", "", new Color(0.78f, 0.6f, 0.46f), new Color(0.2f, 0.2f, 0.24f), new Color(0.12f, 0.12f, 0.15f), false);
            W.mensajero.gameObject.SetActive(false);
            var carried = new GameObject("caja_mensajero"); carried.transform.SetParent(W.mensajero.transform, false);
            carried.transform.localPosition = new Vector3(0f, 1.1f, 0.34f);
            B(carried.transform, 0f, 0f, 0f, 0.55f, 0.36f, 0.4f, mCardboard, "caja", false);
            B(carried.transform, 0f, 0.185f, 0f, 0.1f, 0.01f, 0.41f, mTape, "cinta", false);
            carried.SetActive(false); W.cajaMensajero = carried;
            W.mensajeroPath = new[] { new Vector3(5.0f, 0f, 14.2f), new Vector3(4.6f, 0f, 11.0f), new Vector3(3.0f, 0f, 9.8f), new Vector3(2.8f, 0f, 4.0f), new Vector3(0f, 0f, 0.6f), new Vector3(0f, 0f, -3f) };
        }

        // ---------------- muebles ----------------
        static void Chair(Transform p, float x, float z, float yaw)
        {
            var g = Group(p, "Silla", x, 0f, z, yaw);
            B(g, 0f, 0.48f, 0f, 0.5f, 0.07f, 0.5f, mDark, "asiento", false);
            B(g, 0f, 0.78f, 0.23f, 0.48f, 0.5f, 0.06f, mDark, "espaldar", false);
            Cyl(g, 0f, 0.24f, 0f, 0.04f, 0.48f, mSteel, "pie"); Cyl(g, 0f, 0.03f, 0f, 0.28f, 0.04f, mSteel, "base");
        }

        static void Palm(float x, float z, float h, float scale)
        {
            var g = Group(S, "Palmera", x, 0f, z, Random.Range(0f, 360f));
            Cyl(g, 0f, 0.25f, 0f, 0.34f * scale, 0.5f, mPot, "maceta", true);
            int seg = 5; float sh = (h - 0.4f) / seg; Vector3 pos = new Vector3(0f, 0.5f, 0f);
            for (int i = 0; i < seg; i++)
            {
                float r = Mathf.Lerp(0.09f, 0.055f, i / (float)seg) * scale;
                pos += new Vector3(0.025f * i, sh * 0.5f, 0.01f);
                Cyl(g, pos.x, pos.y, pos.z, r, sh * 1.05f, mTrunk, "tronco");
                pos += new Vector3(0.0f, sh * 0.5f, 0f);
            }
            for (int i = 0; i < 9; i++)
            {
                var piv = new GameObject("hoja"); piv.transform.SetParent(g, false); piv.transform.localPosition = pos;
                piv.transform.localRotation = Quaternion.Euler(14f + (i % 3) * 14f, i * 40f, 0f);
                Sph(piv.transform, 0f, -0.04f, 0.62f * scale, 0.2f * scale, 0.035f, 1.2f * scale, mLeaf, "hoja");
            }
        }
    }
}
