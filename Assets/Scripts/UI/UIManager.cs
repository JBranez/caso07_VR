// UIManager (contrato común): HUD + paneles holográficos. Funciona en PC (overlay) y en VR (espacio de mundo).
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace LasPalmeras
{
    public enum ToastKind { Info, Good, Warn, Bad }

    public class UIManager : MonoBehaviour
    {
        // ---- paleta ANDES-SIM ----
        public static readonly Color Bg = new Color(0.01f, 0.05f, 0.035f, 0.95f);
        public static readonly Color BgLight = new Color(0.02f, 0.11f, 0.075f, 0.96f);
        public static readonly Color Neon = new Color(0.22f, 0.95f, 0.54f);
        public static readonly Color Red = new Color(1f, 0.35f, 0.31f);
        public static readonly Color Amber = new Color(1f, 0.76f, 0.29f);
        public static readonly Color Txt = new Color(0.88f, 0.98f, 0.92f);
        public static readonly Color Dim = new Color(0.55f, 0.72f, 0.62f);
        static string Hex(Color c) { return ColorUtility.ToHtmlStringRGB(c); }
        static string Col(string s, Color c) { return "<color=#" + Hex(c) + ">" + s + "</color>"; }

        GameManager gm;
        Camera Cam { get { return gm.player.cam; } }

        // ---- HUD ----
        Canvas hudCanvas; RectTransform hudRect, hudRoot;
        Text tClock, tObj, tPrompt, tToast, tTitleBig, tTitleSmall, tMode, tMoney;
        Image toastBg, alarmFlash, titleBg; RectTransform objPanel, clockPanel;
        CanvasGroup toastGroup, titleGroup;
        float toastUntil; readonly Queue<KeyValuePair<string, KeyValuePair<float, ToastKind>>> toastQ = new Queue<KeyValuePair<string, KeyValuePair<float, ToastKind>>>();
        bool lastVr; bool alarm; float objTimer;
        Rect lastSafeArea; int lastScreenW, lastScreenH;
        string promptText = "";

        // ---- modal ----
        WorldPanel modal; Action<WorldPanel> builder; string modalId = ""; bool modalClosable;
        int page, tab, resultPage; Difficulty menuDifficulty = Difficulty.Normal;
        FichaData fichaCur; string fichaMsg = ""; Color fichaMsgCol = Txt;
        ResultData lastResult; int lastLogVersion;

        public bool ModalOpen { get { return modal != null; } }
        public string ModalId { get { return modalId; } }

        public static UIManager Create(GameManager gm)
        {
            var g = new GameObject("UI"); g.transform.SetParent(gm.transform, false);
            var u = g.AddComponent<UIManager>(); u.gm = gm; u.BuildHud();
            return u;
        }

        // =====================================================================
        //  HUD
        // =====================================================================
        Text HText(Transform parent, float x, float y, float w, float h, int size, Color c, TextAnchor a, bool bold = false, bool topRight = false)
        {
            var g = new GameObject("t", typeof(RectTransform)); var r = g.GetComponent<RectTransform>(); r.SetParent(parent, false);
            if (topRight) { r.anchorMin = r.anchorMax = new Vector2(1f, 1f); r.pivot = new Vector2(1f, 1f); r.anchoredPosition = new Vector2(-x, -y); }
            else { r.anchorMin = r.anchorMax = new Vector2(0f, 1f); r.pivot = new Vector2(0f, 1f); r.anchoredPosition = new Vector2(x, -y); }
            r.sizeDelta = new Vector2(w, h);
            var t = g.AddComponent<Text>(); t.font = Mats.Font; t.fontSize = size; t.color = c; t.alignment = a; t.supportRichText = true; t.raycastTarget = false;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        Image HRect(Transform parent, float x, float y, float w, float h, Color c, bool stretch = false)
        {
            var g = new GameObject("r", typeof(RectTransform)); var r = g.GetComponent<RectTransform>(); r.SetParent(parent, false);
            if (stretch) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
            else { r.anchorMin = r.anchorMax = new Vector2(0f, 1f); r.pivot = new Vector2(0f, 1f); r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); }
            var i = g.AddComponent<Image>(); i.color = c; i.raycastTarget = false; return i;
        }

        void BuildHud()
        {
            var g = new GameObject("HUD"); g.transform.SetParent(transform, false);
            hudCanvas = g.AddComponent<Canvas>(); hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay; hudCanvas.sortingOrder = 20;
            var cs = g.AddComponent<CanvasScaler>(); cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; cs.referenceResolution = new Vector2(1280f, 720f); cs.matchWidthOrHeight = 0.5f;
            hudRect = g.GetComponent<RectTransform>();
            var rg = new GameObject("raiz", typeof(RectTransform)); hudRoot = rg.GetComponent<RectTransform>(); hudRoot.SetParent(hudRect, false);
            hudRoot.anchorMin = Vector2.zero; hudRoot.anchorMax = Vector2.one; hudRoot.offsetMin = hudRoot.offsetMax = Vector2.zero;

            alarmFlash = HRect(hudRoot, 0, 0, 0, 0, new Color(1f, 0f, 0f, 0f), true);
            // objetivos (contenedor para mostrarlo/ocultarlo)
            var og = new GameObject("objetivos", typeof(RectTransform)); objPanel = og.GetComponent<RectTransform>(); objPanel.SetParent(hudRoot, false);
            objPanel.anchorMin = objPanel.anchorMax = new Vector2(0f, 1f); objPanel.pivot = new Vector2(0f, 1f); objPanel.anchoredPosition = Vector2.zero; objPanel.sizeDelta = new Vector2(600f, 260f);
            HRect(objPanel, 16, 16, 560, 230, new Color(0.01f, 0.05f, 0.035f, 0.72f));
            HRect(objPanel, 16, 16, 4, 230, Neon);
            HText(objPanel, 32, 22, 520, 28, 20, Neon, TextAnchor.UpperLeft, true).text = "OBJETIVOS";
            tObj = HText(objPanel, 32, 52, 530, 190, 21, Txt, TextAnchor.UpperLeft);
            // reloj + dinero (arriba a la derecha)
            var cg = new GameObject("reloj", typeof(RectTransform)); clockPanel = cg.GetComponent<RectTransform>(); clockPanel.SetParent(hudRoot, false);
            clockPanel.anchorMin = clockPanel.anchorMax = new Vector2(1f, 1f); clockPanel.pivot = new Vector2(1f, 1f);
            clockPanel.anchoredPosition = new Vector2(-16f, -16f); clockPanel.sizeDelta = new Vector2(420f, 96f);
            var cbg = cg.AddComponent<Image>(); cbg.color = new Color(0.01f, 0.05f, 0.035f, 0.72f); cbg.raycastTarget = false;
            tClock = HText(clockPanel, 14, 8, 392, 40, 30, Neon, TextAnchor.UpperRight, true);
            tMoney = HText(clockPanel, 14, 52, 392, 40, 19, Amber, TextAnchor.UpperRight, false);
            // modo (abajo izquierda)
            tMode = HText(hudRoot, 20, 690, 700, 26, 17, Dim, TextAnchor.UpperLeft);
            // prompt
            tPrompt = HText(hudRoot, 240, 610, 800, 40, 26, Txt, TextAnchor.MiddleCenter, true);
            // toast
            toastBg = HRect(hudRoot, 190, 540, 900, 62, new Color(0.01f, 0.05f, 0.035f, 0.9f));
            toastGroup = toastBg.gameObject.AddComponent<CanvasGroup>(); toastGroup.alpha = 0f;
            tToast = HText(toastBg.transform, 16, 6, 868, 52, 22, Txt, TextAnchor.MiddleCenter, true);
            // tarjeta de título
            titleBg = HRect(hudRoot, 0, 230, 1280, 200, new Color(0f, 0f, 0f, 0.62f));
            titleGroup = titleBg.gameObject.AddComponent<CanvasGroup>(); titleGroup.alpha = 0f;
            tTitleBig = HText(titleBg.transform, 40, 34, 1200, 80, 56, Neon, TextAnchor.MiddleCenter, true);
            tTitleSmall = HText(titleBg.transform, 40, 118, 1200, 60, 28, Txt, TextAnchor.MiddleCenter, false);

            // Controles inferiores y elementos centrados usan anclas; no dependen de 1280x720 exactos.
            var modeRt = tMode.rectTransform;
            modeRt.anchorMin = modeRt.anchorMax = Vector2.zero; modeRt.pivot = Vector2.zero;
            modeRt.anchoredPosition = new Vector2(20f, 14f);
            var promptRt = tPrompt.rectTransform;
            promptRt.anchorMin = new Vector2(0.08f, 0.035f); promptRt.anchorMax = new Vector2(0.92f, 0.12f);
            promptRt.offsetMin = promptRt.offsetMax = Vector2.zero;
            var toastRt = toastBg.rectTransform;
            toastRt.anchorMin = toastRt.anchorMax = new Vector2(0.5f, 0.16f); toastRt.pivot = new Vector2(0.5f, 0.5f);
            toastRt.anchoredPosition = Vector2.zero;
            var titleRt = titleBg.rectTransform;
            titleRt.anchorMin = new Vector2(0.02f, 0.5f); titleRt.anchorMax = new Vector2(0.98f, 0.5f);
            titleRt.pivot = new Vector2(0.5f, 0.5f); titleRt.anchoredPosition = Vector2.zero; titleRt.sizeDelta = new Vector2(0f, 200f);
        }

        void ApplyHudMode()
        {
            bool vr = VrMode.Active;
            if (vr == lastVr) return;
            lastVr = vr;
            if (vr)
            {
                hudCanvas.renderMode = RenderMode.WorldSpace; hudCanvas.worldCamera = Cam;
                hudRect.SetParent(Cam.transform, false);
                hudRect.sizeDelta = new Vector2(1280f, 720f);
                hudRect.localPosition = new Vector3(0f, -0.1f, 1.5f); hudRect.localRotation = Quaternion.identity;
                hudRect.localScale = Vector3.one * (1.7f / 1280f);
                hudRoot.localScale = Vector3.one * 0.8f;
            }
            else
            {
                hudRect.SetParent(transform, false);
                hudRoot.localScale = Vector3.one;
                hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                hudRect.localScale = Vector3.one;
            }
        }

        void ApplyHudSafeAreaAndScale()
        {
            if (hudRoot == null || Screen.width <= 0 || Screen.height <= 0) return;
            bool vr = VrMode.Active;
            Rect safe = vr ? new Rect(0f, 0f, Screen.width, Screen.height) : Screen.safeArea;
            if (safe.width <= 0f || safe.height <= 0f) safe = new Rect(0f, 0f, Screen.width, Screen.height);
            if (lastScreenW == Screen.width && lastScreenH == Screen.height && lastSafeArea == safe) return;
            lastScreenW = Screen.width; lastScreenH = Screen.height; lastSafeArea = safe;
            hudRoot.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            hudRoot.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            hudRoot.offsetMin = hudRoot.offsetMax = Vector2.zero;
            float aspect = Screen.width / (float)Screen.height;
            float compact = Mathf.Clamp(aspect / (16f / 9f), 0.72f, 1.08f);
            if (objPanel != null) objPanel.localScale = Vector3.one * compact;
            if (clockPanel != null) clockPanel.localScale = Vector3.one * compact;
            if (tMode != null) tMode.rectTransform.sizeDelta = new Vector2(Mathf.Max(400f, 700f / compact), 28f);
        }

        void UpdateHud()
        {
            bool playing = gm.phase == GameManager.Phase.Playing;
            tClock.text = playing ? gm.clock.Label : "";
            tMoney.text = playing && gm.story != null ? gm.story.MoneyLine() : "";
            tPrompt.text = (!ModalOpen && playing) ? promptText : (ModalOpen ? "" : promptText);
            string modeTxt = gm.mode == SimMode.Ataque ? "MODO ATAQUE" : "MODO DEFENSA";
            bool gaze = gm.player.controlMode == PlayerRig.ControlMode.Gaze;
            tMode.text = playing ? modeTxt + "   ·   " + (gaze ? "mira un objeto para activarlo" : "WASD mover · E/clic usar · H pista · Tab bitácora · F1 guía") : "";
            // objetivos
            objTimer -= Time.unscaledDeltaTime;
            if (objTimer <= 0f)
            {
                objTimer = 0.25f;
                var sb = new StringBuilder(); bool first = true;
                foreach (var o in gm.objectives)
                {
                    bool d = o.done != null && o.done();
                    string t = o.text();
                    if (d) sb.Append(Col("[X] " + t, Neon)).Append('\n');
                    else if (first) { sb.Append("<b>► " + t + "</b>").Append('\n'); first = false; }
                    else sb.Append(Col("[ ] " + t, Dim)).Append('\n');
                }
                tObj.text = sb.ToString();
            }
            bool showObj = playing && gm.objectives.Count > 0;
            objPanel.gameObject.SetActive(showObj);
            clockPanel.gameObject.SetActive(playing);
            // alarma
            float a = alarm ? 0.10f + 0.10f * Mathf.Sin(Time.time * 6f) : 0f;
            alarmFlash.color = new Color(1f, 0f, 0f, a);
        }

        public void SetPrompt(string s) { promptText = s ?? ""; }
        public void SetAlarm(bool on) { alarm = on; }

        public void Toast(string text, float secs = 3f, ToastKind kind = ToastKind.Info)
        {
            toastQ.Enqueue(new KeyValuePair<string, KeyValuePair<float, ToastKind>>(text, new KeyValuePair<float, ToastKind>(secs, kind)));
        }

        void ToastTick()
        {
            if (Time.unscaledTime > toastUntil && toastQ.Count > 0)
            {
                var it = toastQ.Dequeue();
                tToast.text = it.Key;
                Color c = it.Value.Value == ToastKind.Good ? Neon : it.Value.Value == ToastKind.Warn ? Amber : it.Value.Value == ToastKind.Bad ? Red : Txt;
                tToast.color = c; toastUntil = Time.unscaledTime + it.Value.Key;
            }
            float target = Time.unscaledTime < toastUntil ? 1f : 0f;
            toastGroup.alpha = Mathf.MoveTowards(toastGroup.alpha, target, Time.unscaledDeltaTime * 4f);
        }

        public IEnumerator TitleCard(string big, string small, float secs = 3.4f)
        {
            tTitleBig.text = big; tTitleSmall.text = small;
            AudioGuide.Say(big + ". " + small);
            float t = 0f;
            while (t < 0.5f) { t += Time.unscaledDeltaTime; titleGroup.alpha = t / 0.5f; yield return null; }
            titleGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(secs);
            t = 0f;
            while (t < 0.6f) { t += Time.unscaledDeltaTime; titleGroup.alpha = 1f - t / 0.6f; yield return null; }
            titleGroup.alpha = 0f;
        }

        // =====================================================================
        //  Teclas globales
        // =====================================================================
        void Update()
        {
            if (gm.player == null) return;
            ApplyHudMode();
            ApplyHudSafeAreaAndScale();
            UpdateHud();
            ToastTick();
            if (GameInput.Pause)
            {
                if (ModalOpen && modalClosable) CloseToContext();
                else if (!ModalOpen && gm.phase == GameManager.Phase.Playing) OpenPause();
            }
            if (GameInput.Help && (gm.phase == GameManager.Phase.Playing || gm.phase == GameManager.Phase.Menu) && modalId != "guide") { page = 0; OpenGuide(); }
            if (GameInput.Hint && !ModalOpen && gm.phase == GameManager.Phase.Playing && gm.story != null) gm.story.ShowHint();
            if (GameInput.Journal && !ModalOpen && gm.phase == GameManager.Phase.Playing) OpenJournal();
            // paneles "vivos": el terminal se actualiza cuando cambia el registro
            if (modalId == "terminal" && gm.log != null && gm.log.version != lastLogVersion)
            {
                lastLogVersion = gm.log.version;
                if (gm.story != null) gm.story.OnLedgerViewed(tab);
                Rebuild();
            }
        }

        // =====================================================================
        //  Gestión del modal
        // =====================================================================
        void Open(string id, float w, float h, float meters, float yOff, bool closable, Action<WorldPanel> build)
        {
            CloseModal(false);
            modalId = id; modalClosable = closable; builder = build;
            modal = WorldPanel.Create("Panel_" + id, w, h, meters, Cam);
            modal.transform.SetParent(gm.transform, true);
            Place(modal.transform, 1.75f, yOff);
            build(modal);
            lastLogVersion = gm.log != null ? gm.log.version : 0;
            AudioManager.Play("click", 0.7f);
        }

        void Place(Transform t, float dist, float yOff)
        {
            Vector3 eye = Cam.transform.position;
            Vector3 fwd = gm.player.FlatForward;
            Vector3 pos = eye + fwd * dist + Vector3.up * yOff;
            t.position = pos;
            t.rotation = Quaternion.LookRotation(pos - eye, Vector3.up);
        }

        public void Rebuild()
        {
            if (modal == null || builder == null) return;
            modal.Clear(); builder(modal);
        }

        /// <summary>Cierra el panel y, si estamos en el menú, vuelve a mostrarlo (así nunca queda la pantalla sin menú).</summary>
        public void CloseToContext()
        {
            CloseModal();
            if (gm.phase == GameManager.Phase.Menu) OpenMenu();
        }

        public void CloseModal(bool sound = true)
        {
            if (modal != null) { Destroy(modal.gameObject); modal = null; if (sound) AudioManager.Play("back", 0.7f); }
            modalId = ""; builder = null;
        }

        void Chrome(WorldPanel p, string title, string sub = null)
        {
            p.Rect(0, 0, p.W, p.H, Bg);
            p.Rect(0, 0, p.W, 6, Neon);
            p.Rect(0, p.H - 4, p.W, 4, new Color(0.22f, 0.95f, 0.54f, 0.5f));
            p.Rect(0, 0, 4, p.H, new Color(0.22f, 0.95f, 0.54f, 0.5f));
            p.Rect(p.W - 4, 0, 4, p.H, new Color(0.22f, 0.95f, 0.54f, 0.5f));
            p.Label(36, 20, p.W - 72, 52, title, 40, Neon, TextAnchor.UpperLeft, true, true);
            if (sub != null) p.Label(36, 72, p.W - 72, 32, sub, 22, Dim, TextAnchor.UpperLeft, false, true);
        }

        // =====================================================================
        //  Menú principal
        // =====================================================================
        public void OpenMenu()
        {
            Open("menu", 1100, 760, 1.9f, 0f, false, p =>
            {
                Chrome(p, "LAS PALMERAS", "ANDES-SIM · Escenario 07 · Agencia Las Palmeras · VR y PC");
                p.Label(36, 116, 1028, 120, gm.data.historia, 26, Txt, TextAnchor.UpperLeft, false, true);
                p.Label(36, 246, 1028, 30, "ELIGE CÓMO JUGAR", 20, Dim, TextAnchor.UpperLeft, true);
                p.Button(36, 280, 504, 122, "MODO ATAQUE\nRevive los hechos de 2016", () => gm.StartMode(SimMode.Ataque, menuDifficulty), 32, false, new Color(0.35f, 0.07f, 0.06f, 0.96f));
                p.Button(560, 280, 504, 122, "MODO DEFENSA\nActiva los controles a tiempo", () => gm.StartMode(SimMode.Defensa, menuDifficulty), 32, false, new Color(0.04f, 0.28f, 0.14f, 0.96f));
                p.Label(36, 418, 330, 44, "Dificultad (defensa):", 22, Dim, TextAnchor.MiddleLeft);
                string[] dn = { "Práctica (sin reloj)", "Normal", "Rápido" };
                for (int i = 0; i < 3; i++)
                {
                    int di = i; bool sel = menuDifficulty == (Difficulty)i;
                    p.Button(370 + i * 235, 418, 225, 44, dn[i], () => { menuDifficulty = (Difficulty)di; Rebuild(); }, 20, false, sel ? new Color(0.1f, 0.5f, 0.28f, 1f) : (Color?)null);
                }
                // controles / VR
                if (VrMode.Supported)
                {
                    bool on = VrMode.Active;
                    p.Button(36, 478, 1028, 52, on ? "VR CARDBOARD: ACTIVO  (tocar para salir a pantalla normal)" : "ENTRAR A VR (Cardboard)", () => StartCoroutine(ToggleVr()), 24);
                }
                else
                {
                    bool gz = gm.player.controlMode == PlayerRig.ControlMode.Gaze;
                    p.Button(36, 478, 1028, 52, gz ? "CONTROLES: MIRADA (simula VR)  ·  F2 para cambiar" : "CONTROLES: TECLADO Y RATÓN  ·  F2 para simular VR", () => { gm.player.ToggleGazeSim(); Rebuild(); }, 24);
                }
                p.Button(36, 548, 245, 60, "Cómo se juega", () => { page = 0; OpenGuide(); }, 22);
                p.Button(296, 548, 245, 60, "Créditos", () => OpenCredits(), 22);
                p.Button(556, 548, 245, 60, "Bitácora", () => OpenJournal(), 22);
                p.Button(816, 548, 248, 60, "Audio y accesibilidad", () => OpenSettings(), 21);
                if (!Application.isMobilePlatform)
                    p.Button(36, 632, 330, 56, "Salir", () => { Application.Quit(); }, 24, false, new Color(0.25f, 0.1f, 0.08f, 0.96f));
                p.Label(385, 628, 679, 70, "Mira el botón y mantén la mirada 1 s (VR), o apunta con el punto central y pulsa E / clic (PC). Con el cursor libre, usa el ratón.", 19, Dim, TextAnchor.MiddleLeft, false, true);
            });
        }

        IEnumerator ToggleVr()
        {
            if (VrMode.Active) VrMode.Exit(); else yield return VrMode.Enter();
            yield return null;
            if (modalId == "menu") Rebuild();
        }

        // =====================================================================
        //  Guía de juego
        // =====================================================================
        static readonly string[] GuideTitle = { "TU MISIÓN", "CONTROLES", "DETECTA DATOS FALSOS (KYC)", "CÓMO DEFENDER" };

        string GuideText(int i, int col)
        {
            switch (i)
            {
                case 0:
                    return "Eres analista de la Unidad de Respuesta del <b>BANCO ANDINO</b>. ANDES-SIM te lleva a la <b>Agencia Las Palmeras</b> del Banco Pacífico Insular, donde «aterrizó» el dinero del robo cibernético de febrero de 2016.\n\n" +
                           Col("MODO ATAQUE", Red) + ": revive los hechos en 4 momentos y reúne las pistas de lo que falló.\n\n" +
                           Col("MODO DEFENSA", Neon) + ": historia alternativa. Activa 3 controles antes de que salga el dinero.\n\n" +
                           Col("Todo es simulación: personas, cuentas y datos son inventados.", Dim);
                case 1:
                    if (col == 0)
                        return Col("PC · teclado y ratón", Neon) + "\n\nWASD / flechas: caminar\nShift: correr\nRatón: mirar\nE o clic: usar lo que apuntas\n1 a 5: viajar a una zona\nH: pista del asistente\nTab: bitácora de pistas\nF1: esta guía   Esc: pausa\nF2: modo mirada (simula VR)";
                    return Col("VR · Cardboard", Neon) + "\n\nGira la cabeza para mirar.\nMira un anillo verde del suelo 1 s: te teletransportas.\nMira un objeto con halo brillante: el círculo se llena y se activa.\nToca la pantalla (botón del visor) para activar al instante.\nMira la esfera azul (asistente) para pedir una pista.";
                case 2:
                    return "En la ventanilla revisa cada ficha y marca los datos que no resisten una verificación:\n\n" +
                           "• <b>Documento</b>: el dígito verificador no coincide.\n• <b>Domicilio</b>: no existe en el registro de direcciones.\n• <b>Teléfono</b>: patrón repetido (999 999 999).\n• <b>Carta devuelta</b>: nadie vive en ese domicilio.\n• <b>Perfil</b>: declara US$45.000 al mes y abre con US$500.\n• <b>Patrón</b>: cuentas en serie con el mismo depósito exacto.\n\n" +
                           Col("Marcar un dato válido es una falsa alarma y resta puntaje.", Amber);
                default:
                    return "Es el viernes 5 de febrero de 2016, 09:00. El reloj corre mientras exploras y se detiene al leer un panel.\n" +
                           "• Mañana del viernes: llegan US$81 millones   • antes de las 19:00: se entrega la caja de 20 millones de pesos\n• Martes 10:24–11:37: cinco transferencias por US$58,16 millones\n\n" +
                           "En el <b>Panel de alertas</b> (sala de seguridad) activa 3 controles. Cada uno se desbloquea al revisar su evidencia:\n" +
                           "1. KYC → fichas (3 datos falsos)\n2. Alerta de cuentas inactivas → registro (terminal de la bóveda) y una regla equilibrada\n3. Retención + CCTV → cámaras\n\n" +
                           Col("Una alerta sin retención no detiene el dinero.", Amber);
            }
        }

        public void OpenGuide()
        {
            Open("guide", 1100, 720, 1.9f, 0f, true, p =>
            {
                Chrome(p, GuideTitle[page], "Cómo se juega · página " + (page + 1) + " de " + GuideTitle.Length);
                if (page == 1)
                {
                    p.Rect(36, 120, 500, 470, BgLight); p.Rect(564, 120, 500, 470, BgLight);
                    p.Label(56, 134, 460, 440, GuideText(1, 0), 25, Txt, TextAnchor.UpperLeft, false, true);
                    p.Label(584, 134, 460, 440, GuideText(1, 1), 25, Txt, TextAnchor.UpperLeft, false, true);
                }
                else p.Label(46, 124, 1008, 470, GuideText(page, 0), 27, Txt, TextAnchor.UpperLeft, false, true);
                p.Button(36, 624, 280, 62, "< Anterior", () => { page = Mathf.Max(0, page - 1); Rebuild(); }, 26, page == 0);
                p.Button(410, 624, 280, 62, "Cerrar", () => CloseToContext(), 26, false, new Color(0.25f, 0.1f, 0.08f, 0.96f));
                p.Button(784, 624, 280, 62, "Siguiente >", () => { page = Mathf.Min(GuideTitle.Length - 1, page + 1); Rebuild(); }, 26, page == GuideTitle.Length - 1);
            });
        }

        // =====================================================================
        //  Briefing
        // =====================================================================
        public void OpenBriefing()
        {
            bool atk = gm.mode == SimMode.Ataque;
            Open("briefing", 1000, 700, 1.8f, 0f, false, p =>
            {
                Chrome(p, atk ? "MODO ATAQUE · REVIVIR 2016" : "MODO DEFENSA · HISTORIA ALTERNATIVA", "ANDES-SIM · Escenario 07 · Agencia Las Palmeras");
                string body = atk
                    ? "Eres analista de la Unidad de Respuesta del <b>BANCO ANDINO</b>. Lazarus necesitaba un lugar donde <b>«aterrizar»</b> el dinero robado: cinco cuentas abiertas con identidades falsas en la Agencia Las Palmeras.\n\nViajarás por 4 momentos reales: " +
                      Col("15 may 2015 · vie 5 feb 2016 · 4–9 feb · mar 9 feb 09:11–11:37", Amber) + ".\n\nTu tarea: <b>encontrar qué falló</b> en la agencia y entender por qué nadie lo detuvo. No puedes cambiar lo que pasó: eres testigo."
                    : "Es el " + Col("viernes 5 de febrero de 2016, 09:00", Amber) + ". Esta vez tienes el control de la agencia.\n\nEl dinero llega durante la mañana (US$81 millones); antes de las 19:00 se entrega a la sucursal una caja con 20 millones de pesos. El martes ocurren cinco transferencias por US$58,16 millones entre las 10:24 y las 11:37.\n\nActiva los <b>3 controles</b> del Panel de alertas antes de que ocurra cada hecho. " + Col("La hora de juego de la entrega es ilustrativa; el 9:11 y las cinco transferencias de 10:24 a 11:37 se basan en la demanda.", Dim) +
                      "\n\nDificultad: " + Col(gm.difficulty.ToString(), Neon);
                p.Label(40, 120, 920, 400, body, 28, Txt, TextAnchor.UpperLeft, false, true);
                p.Label(40, 530, 920, 40, "Primero cruza el portal ANDES-SIM que está en el centro del lobby.", 23, Neon, TextAnchor.MiddleLeft, true, true);
                p.Button(40, 596, 440, 74, "COMENZAR", () => gm.BeginPlay(), 34);
                p.Button(520, 596, 440, 74, "Volver al menú", () => { CloseModal(); OpenMenu(); }, 26, false, new Color(0.25f, 0.1f, 0.08f, 0.96f));
            });
        }

        // =====================================================================
        //  Tarjeta genérica / pista
        // =====================================================================
        public void OpenCard(string title, string body)
        {
            Open("card", 900, 580, 1.6f, 0f, true, p =>
            {
                Chrome(p, title);
                p.Label(40, 100, 820, 370, body, 27, Txt, TextAnchor.UpperLeft, false, true);
                p.Button(40, 490, 820, 70, "Cerrar", () => CloseModal(), 28);
            });
        }

        public void OpenHint(string text)
        {
            Open("card", 900, 480, 1.6f, 0f, true, p =>
            {
                Chrome(p, "PISTA DE ANDES-SIM", "Asistente");
                p.Label(40, 116, 820, 250, text, 28, Txt, TextAnchor.UpperLeft, false, true);
                p.Button(40, 390, 820, 70, "Entendido", () => CloseModal(), 28);
            });
            AudioManager.Play("hint");
        }

        public void OpenPause()
        {
            Open("pause", 760, 640, 1.5f, 0f, true, p =>
            {
                Chrome(p, "PAUSA");
                p.Button(40, 110, 680, 62, "Reanudar", () => CloseModal(), 28);
                p.Button(40, 184, 680, 62, "Cómo se juega", () => { page = 0; OpenGuide(); }, 26);
                p.Button(40, 258, 680, 62, "Bitácora de pistas", () => OpenJournal(), 26);
                p.Button(40, 332, 680, 62, "Audio y accesibilidad", () => OpenSettings(), 26);
                p.Button(40, 406, 680, 62, "Reiniciar este modo", () => gm.Restart(), 26);
                p.Button(40, 480, 680, 62, "Volver al menú principal", () => gm.ReturnToMenu(), 26);
                if (!Application.isMobilePlatform) p.Button(40, 554, 680, 62, "Salir de la aplicación", () => Application.Quit(), 26, false, new Color(0.25f, 0.1f, 0.08f, 0.96f));
            });
        }

        public void OpenSettings()
        {
            Open("settings", 900, 600, 1.65f, 0f, true, p =>
            {
                Chrome(p, "AUDIO Y ACCESIBILIDAD", "Ajustes aplicables durante esta sesión");
                float master = AudioManager.I != null ? AudioManager.I.master : 1f;
                bool voice = AudioGuide.I != null && AudioGuide.I.speechEnabled;
                bool subtitles = AudioGuide.I != null && AudioGuide.I.subtitlesEnabled;
                p.Label(40, 120, 820, 48, "Volumen general: " + Mathf.RoundToInt(master * 100f) + "%", 26, Txt, TextAnchor.MiddleLeft, true);
                p.Button(40, 178, 250, 64, "− 20%", () => { AudioManager.SetMaster(master - 0.2f); Rebuild(); }, 24);
                p.Button(315, 178, 250, 64, "+ 20%", () => { AudioManager.SetMaster(master + 0.2f); Rebuild(); }, 24);
                p.Button(590, 178, 270, 64, master <= 0.001f ? "Activar sonido" : "Silenciar", () => { AudioManager.SetMaster(master <= 0.001f ? 1f : 0f); Rebuild(); }, 24);
                p.Button(40, 280, 820, 64, voice ? "Narración hablada: ACTIVADA" : "Narración hablada: DESACTIVADA", () => { if (AudioGuide.I != null) AudioGuide.I.speechEnabled = !voice; Rebuild(); }, 24);
                p.Button(40, 364, 820, 64, subtitles ? "Subtítulos de instrucciones: ACTIVADOS" : "Subtítulos de instrucciones: DESACTIVADOS", () => { if (AudioGuide.I != null) AudioGuide.I.subtitlesEnabled = !subtitles; Rebuild(); }, 24);
                p.Label(40, 450, 820, 70, "Android: utiliza el motor de voz del sistema en español. Windows: intenta usar Microsoft SAPI. En otros sistemas se mantienen subtítulos y señales de audio.", 21, Dim, TextAnchor.UpperLeft, false, true);
                p.Button(40, 540, 820, 48, "Cerrar", () => CloseModal(), 24);
            });
        }

        public void OpenJournal()
        {
            Open("journal", 1000, 760, 1.8f, 0f, true, p =>
            {
                Chrome(p, "BITÁCORA DE PISTAS", gm.EvidenceCount + " de " + gm.data.evidencias.Length + " pistas · pista = vulnerabilidad que la agencia dejó abierta");
                int i = 0;
                foreach (var e in gm.data.evidencias)
                {
                    bool got = gm.evidences.Contains(e.id);
                    float y = 116 + i * 58;
                    p.Rect(36, y, 928, 52, got ? new Color(0.05f, 0.25f, 0.14f, 0.9f) : new Color(0.06f, 0.1f, 0.09f, 0.9f));
                    p.Label(52, y + 4, 640, 44, (got ? "[X] " : "[ ] ") + (got ? e.titulo : "???"), 24, got ? Neon : Dim, TextAnchor.MiddleLeft, true, true);
                    p.Label(700, y + 4, 250, 44, got ? e.vulnerabilidad : "", 20, Amber, TextAnchor.MiddleRight, false, true);
                    i++;
                }
                p.Button(36, 706, 928, 46, "Cerrar", () => CloseToContext(), 24);
            });
        }

        // =====================================================================
        //  Ficha de apertura
        // =====================================================================
        public void OpenFicha(FichaData f)
        {
            fichaCur = f; fichaMsg = gm.mode == SimMode.Ataque ? "Marca los datos que no resisten una verificación. Encuentra al menos 3 datos falsos en total." : "Revisa los datos. El control KYC los verifica por ti cuando lo actives.";
            fichaMsgCol = Dim;
            Open("ficha", 1100, 736, 1.95f, 0f, true, BuildFicha);
        }

        void BuildFicha(WorldPanel p)
        {
            var f = fichaCur; var kyc = KycChecker.I;
            bool defKyc = gm.mode == SimMode.Defensa && gm.IsDefenseApplied("kyc");
            Chrome(p, "FICHA DE APERTURA N.º " + f.id, gm.data.bancoReceptor + " · Agencia Las Palmeras · apertura 15/05/2015 · datos ficticios");
            p.Label(700, 24, 364, 36, "Datos falsos hallados: " + kyc.correctFlags, 22, kyc.correctFlags >= 3 ? Neon : Amber, TextAnchor.MiddleRight, true, true);
            for (int i = 0; i < KycChecker.Kinds.Length; i++)
            {
                var k = KycChecker.Kinds[i]; float y = 118 + i * 58;
                bool flagged = kyc.IsFlagged(f, k) || (defKyc);
                var res = KycChecker.Check(f, k, gm.data);
                string state = ""; Color sc = Dim;
                if (flagged) { if (res.Flaggable) { state = res.status == FieldStatus.Falso ? (defKyc ? "RECHAZADO" : "FALSO") : "SOSPECHOSO"; sc = res.status == FieldStatus.Falso ? Red : Amber; } else { state = "VÁLIDO"; sc = Dim; } }
                Color tint = new Color(0.03f, 0.2f, 0.12f, 0.96f);
                if (flagged) tint = res.Flaggable ? new Color(0.3f, 0.07f, 0.06f, 0.96f) : new Color(0.1f, 0.12f, 0.11f, 0.96f);
                var fk = k;
                bool dis = defKyc;
                var b = p.Button(36, y, 760, 52, "<b>" + KycChecker.LabelOf(k) + ":</b>  " + KycChecker.ValueOf(f, k), () => OnFlag(fk), 23, dis, tint, TextAnchor.MiddleLeft);
                p.Label(808, y, 256, 52, state, 24, sc, TextAnchor.MiddleLeft, true, true);
            }
            p.Rect(36, 530, 1028, 100, BgLight);
            p.Label(52, 536, 996, 90, fichaMsg, 24, fichaMsgCol, TextAnchor.MiddleLeft, false, true);
            if (gm.mode == SimMode.Defensa)
            {
                bool unlocked = gm.Unlocked("kyc"); bool applied = gm.IsDefenseApplied("kyc");
                p.Button(36, 650, 600, 62, applied ? "CONTROL KYC ACTIVO" : (unlocked ? "ACTIVAR VERIFICACIÓN AUTOMÁTICA (KYC)" : "KYC bloqueado: halla 3 datos falsos"), () => { if (!applied) { gm.TryApply("kyc"); Rebuild(); } }, 22, applied || !unlocked, applied ? new Color(0.1f, 0.5f, 0.28f, 1f) : (Color?)null);
            }
            else p.Button(36, 650, 600, 62, kyc.correctFlags >= 3 ? "Objetivo cumplido: datos falsos ≥ 3" : "Objetivo: encontrar al menos 3 datos falsos", () => { }, 22, true);
            p.Button(660, 650, 404, 62, "Cerrar ficha", () => CloseModal(), 26);
        }

        void OnFlag(FieldKind k)
        {
            var kyc = KycChecker.I; var f = fichaCur;
            if (kyc.IsFlagged(f, k)) return;
            var r = kyc.Flag(f, k);
            if (r.Flaggable) { fichaMsg = (r.status == FieldStatus.Falso ? "DATO FALSO · " : "SOSPECHOSO · ") + r.razon; fichaMsgCol = r.status == FieldStatus.Falso ? Red : Amber; }
            else { fichaMsg = "FALSA ALARMA · " + r.razon; fichaMsgCol = Dim; }
            Rebuild();
        }

        // =====================================================================
        //  Selector de cámaras (CCTV)
        // =====================================================================
        public void OpenCctv()
        {
            Open("cctv", 1000, 340, 1.7f, -0.38f, true, p =>
            {
                var feed = gm.world.cctv;
                Chrome(p, "SELECTOR DE FECHAS · CAM-01", "Revisa las grabaciones del 4 al 9 de febrero de 2016");
                for (int i = 0; i < 6; i++)
                {
                    int di = i; bool v = feed.visited[i];
                    p.Button(30 + i * 158, 116, 148, 74, CctvFeed.DaysShort[i] + (v ? "\n[visto]" : ""), () => { feed.Show(di); AudioManager.Play("keys", 0.5f); Rebuild(); }, 24, false, feed.selected == i ? new Color(0.1f, 0.5f, 0.28f, 1f) : (Color?)null);
                }
                string st = gm.mode == SimMode.Ataque ? "Fechas revisadas: " + feed.VisitedCount + " de 6" : "Revisa al menos 2 fechas para desbloquear el control de retención y CCTV.";
                p.Label(30, 204, 940, 40, st, 22, Txt, TextAnchor.MiddleLeft, false, true);
                p.Button(30, 262, 940, 56, "Cerrar", () => CloseModal(), 26);
            });
        }

        // =====================================================================
        //  Terminal: cuentas y retiros
        // =====================================================================
        public void OpenTerminal(int startTab)
        {
            tab = startTab; gm.ledgerViewed = true;
            if (gm.story != null) gm.story.OnLedgerViewed(tab);
            Open("terminal", 1100, 740, 1.95f, 0f, true, BuildTerminal);
        }

        static string Money(long v) { return "US$ " + v.ToString("N0"); }

        void BuildTerminal(WorldPanel p)
        {
            var log = gm.log;
            Chrome(p, "REGISTRO DE CUENTAS Y RETIROS", gm.data.bancoReceptor + " · Agencia Las Palmeras · terminal de la bóveda");
            p.Button(36, 112, 300, 48, "Cuentas", () => { tab = 0; Rebuild(); }, 24, false, tab == 0 ? new Color(0.1f, 0.5f, 0.28f, 1f) : (Color?)null);
            p.Button(346, 112, 300, 48, "Retiros y movimientos", () => { tab = 1; gm.story.OnLedgerViewed(1); Rebuild(); }, 22, false, tab == 1 ? new Color(0.1f, 0.5f, 0.28f, 1f) : (Color?)null);
            p.Button(656, 112, 408, 48, "SWIFT / órdenes", () => { tab = 2; gm.story.OnLedgerViewed(2); Rebuild(); }, 22, false, tab == 2 ? new Color(0.1f, 0.5f, 0.28f, 1f) : (Color?)null);
            if (tab == 0)
            {
                float y0 = 176;
                p.Rect(36, y0, 1028, 40, BgLight);
                p.Label(46, y0, 190, 40, "Cuenta", 20, Dim, TextAnchor.MiddleLeft, true);
                p.Label(240, y0, 330, 40, "Titular (ficticio)", 20, Dim, TextAnchor.MiddleLeft, true);
                p.Label(575, y0, 150, 40, "Últ. mov.", 20, Dim, TextAnchor.MiddleLeft, true);
                p.Label(730, y0, 190, 40, "Saldo", 20, Dim, TextAnchor.MiddleRight, true);
                p.Label(930, y0, 130, 40, "Alerta", 20, Dim, TextAnchor.MiddleLeft, true);
                for (int i = 0; i < log.accounts.Count; i++)
                {
                    var a = log.accounts[i]; float y = y0 + 46 + i * 54;
                    bool big = a.saldoUsd > 1000000;
                    p.Rect(36, y, 1028, 50, big ? new Color(0.25f, 0.18f, 0.03f, 0.9f) : new Color(0.04f, 0.1f, 0.08f, 0.9f));
                    p.Label(46, y, 190, 50, a.numero, 22, Txt, TextAnchor.MiddleLeft);
                    p.Label(240, y, 330, 50, a.titular, 22, Txt, TextAnchor.MiddleLeft, false, true);
                    p.Label(575, y, 150, 50, a.ultimoMov, 22, Txt, TextAnchor.MiddleLeft);
                    p.Label(730, y, 190, 50, a.saldoUsd.ToString("N0"), 24, big ? Amber : Txt, TextAnchor.MiddleRight, true, true);
                    Color ac = a.alerta == "—" || a.alerta == "Sin alerta" ? (a.saldoUsd > 1000000 ? Amber : Dim) : Red;
                    p.Label(930, y, 134, 50, a.alerta, 21, ac, TextAnchor.MiddleLeft, true, true);
                }
                string note;
                if (!log.credited) note = "Cinco cuentas abiertas el 15/05/2015 con US$ 500 cada una. Llevan " + DormantAlert.DiasDormidas + " días sin ningún movimiento: cuentas dormidas.";
                else note = "Cuatro cuentas dormidas durante " + DormantAlert.DiasDormidas + " días reciben US$ 81 millones en un solo día. Total en cuentas: " + Money(log.TotalBalance) + ". " + Col("(montos por cuenta ilustrativos; el total es el dato histórico)", Dim);
                p.Rect(36, 520, 1028, 120, BgLight);
                p.Label(52, 526, 996, 110, note, 24, Txt, TextAnchor.MiddleLeft, false, true);
            }
            else if (tab == 1)
            {
                float y0 = 176;
                p.Rect(36, y0, 1028, 40, BgLight);
                p.Label(46, y0, 230, 40, "Cuándo", 20, Dim, TextAnchor.MiddleLeft, true);
                p.Label(280, y0, 400, 40, "Concepto", 20, Dim, TextAnchor.MiddleLeft, true);
                p.Label(690, y0, 200, 40, "Monto", 20, Dim, TextAnchor.MiddleRight, true);
                p.Label(900, y0, 160, 40, "Estado", 20, Dim, TextAnchor.MiddleLeft, true);
                if (log.rows.Count == 0) p.Label(46, y0 + 70, 1000, 80, "Sin movimientos registrados todavía.", 26, Dim, TextAnchor.UpperLeft);
                int shown = Mathf.Min(7, log.rows.Count);
                for (int i = 0; i < shown; i++)
                {
                    var r = log.rows[log.rows.Count - 1 - i]; float y = y0 + 46 + i * 62;
                    p.Rect(36, y, 1028, 58, new Color(0.04f, 0.1f, 0.08f, 0.9f));
                    p.Label(46, y, 230, 58, r.cuando, 20, Txt, TextAnchor.MiddleLeft, false, true);
                    p.Label(280, y, 400, 58, r.concepto, 21, Txt, TextAnchor.MiddleLeft, false, true);
                    p.Label(690, y, 200, 58, r.monto, 21, Amber, TextAnchor.MiddleRight, true, true);
                    p.Label(900, y, 162, 58, r.estado, 20, r.color, TextAnchor.MiddleLeft, true, true);
                }
            }
            else
            {
                p.Rect(36, 176, 1028, 442, BgLight);
                p.Label(56, 194, 988, 40, "TRAZA DE SESIÓN · SERVIDOR SWIFT RCBC", 25, Neon, TextAnchor.MiddleLeft, true);
                p.Label(56, 244, 988, 310,
                    "07 FEB 2016 · 06:54\nEl servidor SWIFT aparece desconectado, según la demanda.\n\n" +
                    "08 FEB 2016 · 17:05–17:12 (hora de Bangladesh)\nBangladesh Bank envía mensajes STOPPAY / solicitudes de detener pagos.\n\n" +
                    "09 FEB 2016 · 09:11\nEl servidor vuelve a iniciar sesión y los mensajes quedan disponibles para el personal.\n\n" +
                    "09 FEB 2016 · 10:24–11:37\nSe realizan cinco transferencias por US$58.158.641,63.", 23, Txt, TextAnchor.UpperLeft, false, true);
                p.Label(56, 564, 988, 42, "Nota: esta pestaña resume alegaciones de la demanda de 2019; no es un registro forense original del banco.", 17, Amber, TextAnchor.MiddleLeft, false, true);
            }
            p.Button(36, 664, 1028, 58, "Cerrar", () => CloseModal(), 26);
        }

        // =====================================================================
        //  Panel de alertas y controles
        // =====================================================================
        public void OpenControls()
        {
            if (gm.mode == SimMode.Defensa) Open("controls", 1100, 780, 1.95f, 0f, true, BuildControlsDefense);
            else Open("controls", 1000, 640, 1.8f, 0f, true, BuildControlsAttack);
        }

        void BuildControlsAttack(WorldPanel p)
        {
            Chrome(p, "PANEL DE ALERTAS · AGENCIA LAS PALMERAS", "Estado de los controles de la agencia en la reconstrucción de 2016");
            string[] names = gm.data.defensas;
            for (int i = 0; i < 3; i++)
            {
                float y = 120 + i * 82;
                p.Rect(36, y, 928, 74, new Color(0.18f, 0.05f, 0.04f, 0.92f));
                p.Label(52, y, 640, 74, names[i], 24, Txt, TextAnchor.MiddleLeft, true, true);
                p.Label(700, y, 250, 74, "SIN REGLAS", 26, Red, TextAnchor.MiddleRight, true, true);
            }
            p.Label(36, 372, 928, 120, "Ninguna alerta por cuentas inactivas, ninguna retención automática de fondos y ningún aviso cuando las cámaras dejan de grabar. " + Col("Nada podía detener el dinero.", Amber), 25, Txt, TextAnchor.UpperLeft, false, true);
            bool got = gm.evidences.Contains("PANEL_SIN_REGLAS");
            p.Button(36, 510, 928, 60, got ? "Intentado: «ACCESO DENEGADO · no existe ningún procedimiento»" : "Intentar activar la retención de fondos", () =>
            {
                Toast("ACCESO DENEGADO · no existe ninguna regla ni responsable asignado.", 3.5f, ToastKind.Bad); AudioManager.Play("denied");
                gm.RegisterEvidence("PANEL_SIN_REGLAS"); Rebuild();
            }, 24, false, got ? new Color(0.25f, 0.1f, 0.08f, 0.96f) : (Color?)null);
            p.Button(36, 580, 928, 48, "Cerrar", () => CloseModal(), 24);
        }

        void BuildControlsDefense(WorldPanel p)
        {
            Chrome(p, "PANEL DE ALERTAS · ACTIVA LOS 3 CONTROLES", "Cada control se desbloquea cuando revisas su evidencia");
            var story = gm.story;
            for (int i = 0; i < 3; i++)
            {
                var d = gm.data.controles[i]; string id = d.id; float y = 116 + i * 186;
                bool applied = gm.IsDefenseApplied(id), unlocked = gm.Unlocked(id);
                p.Rect(36, y, 1028, 176, applied ? new Color(0.05f, 0.25f, 0.14f, 0.92f) : new Color(0.04f, 0.1f, 0.08f, 0.92f));
                p.Label(52, y + 6, 740, 38, (i + 1) + ". " + d.nombre, 26, applied ? Neon : Txt, TextAnchor.MiddleLeft, true, true);
                p.Label(52, y + 46, 740, 52, d.descripcion, 21, Dim, TextAnchor.UpperLeft, false, true);
                if (id == "dormant")
                {
                    for (int r = 0; r < DormantAlert.Presets.Length; r++)
                    {
                        int ri = r; bool sel = gm.world.dormant.selected == r;
                        p.Button(52 + r * 330, y + 104, 320, 56, DormantAlert.Presets[r].nombre, () => { story.SetDormantRule(ri); if (!applied) gm.TryApply("dormant"); else story.ReevaluateRule(); Rebuild(); }, 20, !unlocked,
                                 sel ? new Color(0.1f, 0.5f, 0.28f, 1f) : (Color?)null);
                    }
                    if (applied) p.Label(790, y + 8, 260, 90, DormantAlert.Presets[gm.world.dormant.selected].nota, 18, DormantAlert.Presets[gm.world.dormant.selected].ruidosa || !gm.world.dormant.Effective ? Amber : Neon, TextAnchor.MiddleLeft, false, true);
                    else p.Label(790, y + 8, 260, 90, unlocked ? "Elige una regla equilibrada:" : "BLOQUEADO\n" + d.requisito, 19, unlocked ? Neon : Amber, TextAnchor.MiddleLeft, true, true);
                }
                else
                {
                    string label = applied ? "ACTIVO" : (unlocked ? "ACTIVAR" : "BLOQUEADO");
                    p.Button(790, y + 28, 260, 70, label, () => { gm.TryApply(id); Rebuild(); }, 28, applied || !unlocked, applied ? new Color(0.1f, 0.5f, 0.28f, 1f) : (Color?)null);
                    p.Label(52, y + 112, 990, 56, applied ? Col("Control activo.", Neon) : (unlocked ? Col("Listo para activar.", Neon) : Col("Requisito: " + d.requisito, Amber)), 21, Txt, TextAnchor.MiddleLeft, false, true);
                }
            }
            bool wait = gm.difficulty == Difficulty.Practica;
            p.Button(36, 684, 640, 62, wait ? "AVANZAR AL DESENLACE (práctica: el reloj está detenido)" : "Avanzar el tiempo hasta el desenlace", () => { story.FastForward(); CloseModal(); }, 22);
            p.Button(700, 684, 364, 62, "Cerrar", () => CloseModal(), 26);
        }

        // =====================================================================
        //  Resultado
        // =====================================================================
        public void OpenResult(ResultData r)
        {
            lastResult = r; resultPage = 0;
            Open("result", 1100, 780, 1.95f, 0f, false, BuildResult);
        }

        void BuildResult(WorldPanel p)
        {
            var r = lastResult; var d = gm.data;
            Chrome(p, r.titulo, r.subtitulo);
            p.Rect(36, 112, 1028, 6, r.color);
            string stars = "";
            for (int i = 0; i < 3; i++) stars += i < r.estrellas ? "■" : "□";
            p.Label(700, 20, 364, 50, stars + "  " + r.puntaje + "/100", 34, r.color, TextAnchor.MiddleRight, true, true);
            if (resultPage == 0)
            {
                p.Label(36, 130, 520, 30, "CIFRAS DEL ESCENARIO", 20, Dim, TextAnchor.UpperLeft, true);
                var sb = new StringBuilder(); foreach (var c in r.cifras) sb.Append(c).Append("\n");
                p.Label(36, 164, 520, 420, sb.ToString(), 24, Txt, TextAnchor.UpperLeft, false, true);
                p.Label(580, 130, 484, 30, "LO QUE APRENDISTE", 20, Dim, TextAnchor.UpperLeft, true);
                var sb2 = new StringBuilder(); foreach (var c in r.lecciones) sb2.Append(c).Append("\n\n");
                p.Label(580, 164, 484, 420, sb2.ToString(), 22, Txt, TextAnchor.UpperLeft, false, true);
            }
            else if (resultPage == 1)
            {
                p.Label(36, 130, 1028, 30, "EPÍLOGO HISTÓRICO", 20, Dim, TextAnchor.UpperLeft, true);
                var sb = new StringBuilder(); foreach (var c in d.epilogo) sb.Append("• ").Append(c).Append("\n\n");
                p.Label(36, 166, 1028, 450, sb.ToString(), 24, Txt, TextAnchor.UpperLeft, false, true);
            }
            else
            {
                p.Label(36, 130, 1028, 30, "LÍNEA DE TIEMPO REAL DE LAS PALMERAS", 20, Dim, TextAnchor.UpperLeft, true);
                var sb = new StringBuilder(); foreach (var t in d.timeline) sb.Append("<b>" + Col(t.fecha, Amber) + "</b>  " + t.texto).Append("\n");
                p.Label(36, 166, 1028, 450, sb.ToString(), 22, Txt, TextAnchor.UpperLeft, false, true);
            }
            p.Button(36, 640, 330, 58, resultPage == 0 ? "Epílogo histórico >" : (resultPage == 1 ? "Línea de tiempo >" : "< Resumen"), () => { resultPage = (resultPage + 1) % 3; Rebuild(); }, 22);
            bool atk = gm.mode == SimMode.Ataque;
            p.Button(386, 640, 330, 58, atk ? "Probar MODO DEFENSA" : "Probar MODO ATAQUE", () => { Bootstrap.Restart(true, atk ? SimMode.Defensa : SimMode.Ataque, gm.difficulty); }, 22, false, atk ? new Color(0.04f, 0.28f, 0.14f, 0.96f) : new Color(0.35f, 0.07f, 0.06f, 0.96f));
            p.Button(736, 640, 328, 58, "Créditos", () => OpenCredits(), 22);
            p.Button(36, 708, 330, 58, "Repetir este modo", () => gm.Restart(), 22);
            p.Button(386, 708, 330, 58, "Menú principal", () => gm.ReturnToMenu(), 22);
            if (!Application.isMobilePlatform) p.Button(736, 708, 328, 58, "Salir", () => Application.Quit(), 22, false, new Color(0.25f, 0.1f, 0.08f, 0.96f));
        }

        // =====================================================================
        //  Créditos
        // =====================================================================
        public void OpenCredits()
        {
            bool fromResult = gm.phase == GameManager.Phase.Result;
            Open("credits", 1100, 780, 1.95f, 0f, true, p =>
            {
                Chrome(p, "CRÉDITOS Y REGLAS ÉTICAS", "ANDES-SIM · Escenario 07");
                var sb = new StringBuilder(); foreach (var c in gm.data.creditos) sb.Append("• ").Append(c).Append("\n");
                p.Label(36, 116, 540, 530, "<b>Créditos</b>\n" + sb, 21, Txt, TextAnchor.UpperLeft, false, true);
                var sb2 = new StringBuilder(); foreach (var c in gm.data.reglasEticas) sb2.Append("• ").Append(c).Append("\n");
                p.Label(596, 116, 468, 530, "<b>Reglas éticas</b>\n" + sb2, 21, Txt, TextAnchor.UpperLeft, false, true);
                p.Button(36, 690, 1028, 60, fromResult ? "Volver al resultado" : "Cerrar", () => { CloseToContext(); if (fromResult && lastResult != null) OpenResult(lastResult); }, 26);
            });
        }
    }
}
