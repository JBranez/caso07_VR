// StoryDirector: guion del escenario 07. Dos modos sobre la misma agencia:
//  ATAQUE  = reconstrucción de los hechos alegados (15 may 2015 → mar 9 feb 2016 11:37).
//  DEFENSA = historia alternativa con reloj: activar controles antes de cada hito.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LasPalmeras
{
    public static class Fx
    {
        /// <summary>Lluvia (o fuga) de billetes. Solo decorativo.</summary>
        public static void Bills(MonoBehaviour host, Vector3 center, int count, bool outward, Vector3 dir)
        { host.StartCoroutine(BillRoutine(center, count, outward, dir)); }

        static IEnumerator BillRoutine(Vector3 center, int count, bool outward, Vector3 dir)
        {
            var mat = Mats.Lit(new Color(0.4f, 0.72f, 0.42f), 0.3f);
            var items = new List<Transform>(); var vel = new List<Vector3>(); var spin = new List<Vector3>();
            for (int i = 0; i < count; i++)
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var c = g.GetComponent<Collider>(); if (c != null) UnityEngine.Object.Destroy(c);
                g.transform.localScale = new Vector3(0.17f, 0.004f, 0.08f);
                g.GetComponent<Renderer>().sharedMaterial = mat;
                Vector3 off = new Vector3(UnityEngine.Random.Range(-1.2f, 1.2f), UnityEngine.Random.Range(0f, 0.8f), UnityEngine.Random.Range(-1.2f, 1.2f));
                g.transform.position = center + off;
                items.Add(g.transform);
                vel.Add(outward ? dir.normalized * UnityEngine.Random.Range(2f, 4f) + Vector3.up * UnityEngine.Random.Range(0.5f, 2f) : new Vector3(UnityEngine.Random.Range(-0.2f, 0.2f), -UnityEngine.Random.Range(1.2f, 2.4f), UnityEngine.Random.Range(-0.2f, 0.2f)));
                spin.Add(new Vector3(UnityEngine.Random.Range(-240f, 240f), UnityEngine.Random.Range(-240f, 240f), UnityEngine.Random.Range(-240f, 240f)));
            }
            float t = 0f;
            while (t < 3.6f)
            {
                t += Time.deltaTime;
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] == null) continue;
                    if (outward) vel[i] += Vector3.down * 2.5f * Time.deltaTime;
                    Vector3 p = items[i].position + vel[i] * Time.deltaTime;
                    if (p.y < 0.02f) { p.y = 0.02f; vel[i] = Vector3.zero; spin[i] = Vector3.zero; }
                    items[i].position = p; items[i].Rotate(spin[i] * Time.deltaTime);
                }
                yield return null;
            }
            foreach (var tr in items) if (tr != null) UnityEngine.Object.Destroy(tr.gameObject);
        }
    }

    public class StoryDirector : MonoBehaviour
    {
        GameManager gm; AgencyWorld w;
        Coroutine main;
        // estado del escenario
        bool portalEntered, withdrawalDone, alarmOn, ff;
        public bool creditsArrived, creditRejected, held, boxDelivered, boxHeld;
        int stage;                      // defensa: 0 antes 10:30 · 1 antes 15:30 · 2 fin de semana · 3 cuenta atrás · 4 fin
        long lostUsd; float idleTimer;
        readonly List<Interactable> beaconed = new List<Interactable>();
        float beaconTimer;
        string lastNarratedObjective = "";

        static readonly DateTime FriMorning = new DateTime(2016, 2, 5, 10, 30, 0);
        static readonly DateTime FriBox = new DateTime(2016, 2, 5, 15, 30, 0); // hora de juego ilustrativa; la demanda solo sitúa la entrega antes de las 19:00
        static readonly DateTime TueRead = new DateTime(2016, 2, 9, 9, 11, 0);
        static readonly DateTime TueOut = new DateTime(2016, 2, 9, 10, 24, 0);

        public static StoryDirector Create(GameManager gm)
        {
            var g = new GameObject("StoryDirector"); g.transform.SetParent(gm.transform, false);
            var s = g.AddComponent<StoryDirector>(); s.gm = gm; s.w = gm.world;
            gm.EvidenceRegistered += s.OnEvidence;
            return s;
        }

        void OnDestroy() { if (gm != null) gm.EvidenceRegistered -= OnEvidence; }
        void OnEvidence(string id) { idleTimer = 0f; RefreshBeacons(); }

        void Update()
        {
            idleTimer += Time.deltaTime;
            beaconTimer -= Time.unscaledDeltaTime;
            if (beaconTimer <= 0f)
            {
                beaconTimer = 0.5f;
                RefreshBeacons();
                var objective = CurrentObjective();
                if (objective != null && objective.id != lastNarratedObjective)
                {
                    lastNarratedObjective = objective.id;
                    string narration = objective.text != null ? objective.text() : "";
                    if (!string.IsNullOrEmpty(objective.hint)) narration += ". " + objective.hint;
                    AudioGuide.Say(narration);
                }
            }
        }

        public float IdleSeconds { get { return idleTimer; } }

        // ---------------- objetivos y balizas ----------------
        Objective Obj(string id, Func<string> text, Func<bool> done, string hint, params Interactable[] targets)
        { return new Objective { id = id, text = text, done = done, hint = hint, targets = targets }; }

        void SetObjectives(params Objective[] list)
        {
            gm.objectives.Clear(); gm.objectives.AddRange(list);
            idleTimer = 0f; RefreshBeacons();
        }

        public Objective CurrentObjective()
        {
            foreach (var o in gm.objectives) if (o.done == null || !o.done()) return o;
            return null;
        }

        void RefreshBeacons()
        {
            foreach (var b in beaconed) if (b != null) b.SetBeacon(false);
            beaconed.Clear();
            if (gm.phase != GameManager.Phase.Playing) return;
            var cur = CurrentObjective();
            if (cur == null || cur.targets == null) return;
            foreach (var t in cur.targets)
            {
                if (t == null || !t.isActiveAndEnabled) continue;
                var f = t as FichaItem;
                if (f != null && KycChecker.I != null && KycChecker.I.FlagsInFicha(f.data) > 0 && KycChecker.I.correctFlags >= 3) continue;
                t.SetBeacon(true); beaconed.Add(t);
            }
        }

        public Vector3? CurrentTargetPosition()
        {
            var cur = CurrentObjective();
            if (cur == null || cur.targets == null) return null;
            foreach (var t in cur.targets) if (t != null && t.isActiveAndEnabled) return t.WorldBounds().center;
            return null;
        }

        public void ShowHint()
        {
            var o = CurrentObjective();
            string h = o != null && !string.IsNullOrEmpty(o.hint) ? o.hint : "Ya cumpliste los objetivos actuales. Sigue las novedades en pantalla.";
            gm.hintsUsed++; idleTimer = 0f;
            gm.ui.OpenHint(h);
            AudioGuide.Say(h);
        }

        // ---------------- inicio ----------------
        public void Begin()
        {
            if (main != null) StopCoroutine(main);
            portalEntered = false; withdrawalDone = false; alarmOn = false; ff = false; stage = 0; lostUsd = 0;
            lastNarratedObjective = "";
            creditsArrived = creditRejected = held = boxDelivered = boxHeld = false;
            main = StartCoroutine(gm.mode == SimMode.Ataque ? RunAttack() : RunDefense());
        }

        public void OnPortalEntered() { if (gm.phase == GameManager.Phase.Playing && !portalEntered && !gm.ui.ModalOpen) portalEntered = true; }

        IEnumerator CrossPortal()
        {
            w.portal.SetBeacon(false);
            var pl = gm.player; pl.frozen = true;
            AudioManager.Play("portal");
            yield return pl.fader.Fade(1f, 0.9f);
            pl.Place(w.tpVentanilla.transform.position, 0f);
            TeleportPoint.Current = w.tpVentanilla;
            yield return new WaitForSecondsRealtime(0.5f);
            yield return pl.fader.Fade(0f, 0.9f);
            pl.frozen = false;
        }

        IEnumerator Timelapse(DateTime from, DateTime to, float secs, bool showTime)
        {
            gm.clock.running = false; gm.clock.showTime = showTime;
            float t = 0f, tick = 0f;
            while (t < secs)
            {
                t += Time.deltaTime; tick -= Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / secs);
                gm.clock.now = from.AddTicks((long)((to - from).Ticks * (double)k));
                if (tick <= 0f) { AudioManager.Play("tick", 0.4f); tick = 0.1f; }
                yield return null;
            }
            gm.clock.now = to;
        }

        // =====================================================================
        //  MODO ATAQUE
        // =====================================================================
        IEnumerator RunAttack()
        {
            var kyc = KycChecker.I;
            gm.clock.running = false; gm.clock.showTime = false; gm.clock.now = new DateTime(2015, 5, 15);
            SetObjectives(Obj("portal", () => "Cruza el portal ANDES-SIM (mira el anillo)", () => portalEntered, "El portal es el anillo brillante en el centro del lobby. Mira hacia él y mantén la mirada, o apúntalo y pulsa E / clic.", w.portal));
            yield return new WaitUntil(() => portalEntered);
            yield return CrossPortal();

            // ---- Capítulo 1: 15 de mayo de 2015 ----
            yield return gm.ui.TitleCard("15 de mayo de 2015", "Ventanilla · se abren cinco cuentas con identidades falsas", 3.6f);
            foreach (var t in w.titulares) t.gameObject.SetActive(true);
            AudioManager.Play("keys");
            SetObjectives(
                Obj("fichas", () => "Halla al menos 3 datos falsos en las fichas (" + Mathf.Min(3, kyc.correctFlags) + "/3)", () => kyc.correctFlags >= 3,
                    "Acércate al mostrador (anillo «Ventanilla»), abre una ficha y marca los datos que no resisten una verificación: documento, domicilio, teléfono, carta devuelta.", w.fichas));
            yield return new WaitUntil(() => kyc.correctFlags >= 3);
            gm.ui.Toast("Nadie verificó nada: 4 de las 5 cartas de bienvenida volvieron devueltas y nadie hizo preguntas.", 6f, ToastKind.Warn);
            yield return new WaitForSeconds(4.5f);
            yield return new WaitUntil(() => !gm.ui.ModalOpen);

            // ---- nueve meses ----
            yield return gm.ui.TitleCard("Pasan nueve meses", "Las cuentas no registran ni un solo movimiento", 2.0f);
            yield return Timelapse(new DateTime(2015, 5, 15), new DateTime(2016, 2, 5), 4.5f, false);
            foreach (var t in w.titulares) t.gameObject.SetActive(false);

            // ---- Capítulo 2: viernes 5 de febrero ----
            yield return gm.ui.TitleCard("Viernes 5 de febrero de 2016", "Mañana · llegan US$ 81 millones a cuatro de las cuentas", 3.6f);
            yield return CreditEvent();
            SetObjectives(
                Obj("registro", () => "Revisa el registro de cuentas (terminal de la bóveda)", () => gm.evidences.Contains("CUENTAS_DORMIDAS"),
                    "Ve a la bóveda (anillo «Bóveda»), mira el terminal de la izquierda y abre la pestaña «Cuentas».", w.terminal));
            yield return new WaitUntil(() => gm.evidences.Contains("CUENTAS_DORMIDAS"));
            yield return new WaitUntil(() => !gm.ui.ModalOpen);
            yield return gm.ui.TitleCard("Viernes 5 de febrero · tarde", "Se entrega a la sucursal una caja con 20 millones de pesos", 3.2f);
            w.caja.gameObject.SetActive(true);
            yield return BoxEvent();
            SetObjectives(Obj("caja", () => "Examina la caja de cartón en la oficina de la gerente", () => gm.evidences.Contains("CAJA_CARTON"),
                "La caja está en la oficina de la gerente (anillo «Oficina»), cerca de las sillas.", w.caja));
            yield return new WaitUntil(() => gm.evidences.Contains("CAJA_CARTON"));
            yield return new WaitUntil(() => !gm.ui.ModalOpen);

            // ---- Capítulo 3: las cámaras ----
            gm.clock.now = new DateTime(2016, 2, 4); gm.clock.showTime = false;
            yield return gm.ui.TitleCard("Del 4 al 9 de febrero", "Las cámaras de la sucursal no registran imágenes", 3.2f);
            SetObjectives(Obj("cctv", () => "Revisa las 6 fechas en el monitor de cámaras (" + w.cctv.VisitedCount + "/6)", () => gm.evidences.Contains("CCTV_SIN_SENAL"),
                "Ve a la sala de seguridad (anillo «Sala de seguridad»), mira el monitor grande y elige cada fecha del selector.", w.monitorItem));
            yield return new WaitUntil(() => gm.evidences.Contains("CCTV_SIN_SENAL"));
            yield return new WaitUntil(() => !gm.ui.ModalOpen);
            SetObjectives(Obj("swift", () => "Examina el registro de sesión SWIFT y las órdenes STOPPAY", () => gm.evidences.Contains("SWIFT_OFFLINE"),
                "Abre el terminal de la bóveda y entra a la pestaña «SWIFT / órdenes» para reconstruir las desconexiones y mensajes.", w.terminal));
            yield return new WaitUntil(() => gm.evidences.Contains("SWIFT_OFFLINE"));
            yield return new WaitUntil(() => !gm.ui.ModalOpen);

            // ---- Capítulo 4: martes 9 de febrero ----
            yield return gm.ui.TitleCard("Martes 9 de febrero de 2016", "09:11 vuelve la sesión SWIFT · cinco transferencias entre 10:24 y 11:37", 3.6f);
            gm.clock.now = TueRead; gm.clock.showTime = true; gm.clock.minPerSec = 1f; gm.clock.running = true;
            AudioManager.Loop("tension_loop", true, 0.5f);
            SetObjectives(Obj("retiro", () => "Analiza las cinco transferencias de 10:24 a 11:37", () => gm.evidences.Contains("RETIRO_1024"),
                "Ve al terminal de la bóveda y abre «Retiros y movimientos». Identifica la primera operación y el total acumulado.", w.terminal));
            yield return new WaitUntil(() => gm.clock.now >= TueOut);
            gm.clock.running = true; gm.clock.now = TueOut;
            yield return WithdrawalEvent();
            gm.clock.running = false;
            AudioManager.Loop("tension_loop", false);
            yield return new WaitUntil(() => gm.evidences.Contains("RETIRO_1024"));
            yield return new WaitUntil(() => !gm.ui.ModalOpen);
            SetObjectives(Obj("panel", () => "Intenta frenar el retiro en el Panel de alertas", () => gm.evidences.Contains("PANEL_SIN_REGLAS"),
                "El panel está en la sala de seguridad, en la pared de la derecha. Ábrelo y pulsa «Intentar activar la retención».", w.panel));
            yield return new WaitUntil(() => gm.evidences.Contains("PANEL_SIN_REGLAS"));
            yield return new WaitUntil(() => !gm.ui.ModalOpen);
            yield return new WaitForSeconds(1.2f);
            gm.EndScenario();
        }

        // =====================================================================
        //  MODO DEFENSA
        // =====================================================================
        IEnumerator RunDefense()
        {
            var kyc = KycChecker.I;
            gm.clock.running = false; gm.clock.showTime = true; gm.clock.now = new DateTime(2016, 2, 5, 9, 0, 0);
            SetObjectives(Obj("portal", () => "Cruza el portal ANDES-SIM (mira el anillo)", () => portalEntered, "El portal es el anillo brillante en el centro del lobby. Mira hacia él y mantén la mirada, o apúntalo y pulsa E / clic.", w.portal));
            yield return new WaitUntil(() => portalEntered);
            yield return CrossPortal();
            yield return gm.ui.TitleCard("Viernes 5 de febrero de 2016 · 09:00", "Hoy puedes cambiar la historia: activa los 3 controles a tiempo", 3.4f);

            SetObjectives(
                Obj("kyc", () => "KYC: halla 3 datos falsos en las fichas y activa el control (" + Mathf.Min(3, kyc.correctFlags) + "/3)", () => gm.IsDefenseApplied("kyc"),
                    "Ventanilla: abre las fichas, marca 3 datos falsos y pulsa «Activar verificación automática (KYC)». Si lo haces antes de las 10:30 el dinero ni siquiera entra.", w.fichas),
                Obj("dormant", () => "Alerta: revisa el registro y activa una regla equilibrada", () => gm.IsDefenseApplied("dormant"),
                    "Bóveda: abre el terminal (pestaña Cuentas). Luego, en el Panel de alertas de la sala de seguridad, elige la regla B (90 días y US$100.000).", w.terminal, w.panel),
                Obj("hold", () => "CCTV y retención: revisa 2 fechas de cámaras y activa el control (" + Mathf.Min(2, w.cctv.VisitedCount) + "/2)", () => gm.IsDefenseApplied("hold"),
                    "Sala de seguridad: mira el monitor, revisa 2 fechas y activa «Retención de fondos y monitoreo de CCTV» en el Panel de alertas.", w.monitorItem, w.panel));

            ff = false; SetSpeedForStage();
            // 10:30 llegan los fondos
            stage = 0;
            yield return new WaitUntil(() => gm.clock.now >= FriMorning);
            gm.clock.now = FriMorning;
            yield return CreditEvent();
            // 15:30 · representación ilustrativa de la entrega durante la tarde (antes de las 19:00)
            stage = 1; SetSpeedForStage();
            yield return new WaitUntil(() => gm.clock.now >= FriBox);
            gm.clock.now = FriBox;
            yield return BoxEvent();
            // fin de semana
            stage = 2; SetSpeedForStage();
            StartCoroutine(gm.ui.TitleCard("Fin de semana · lunes 8 de febrero", "Bangladesh Bank envía órdenes STOPPAY; el servidor SWIFT receptor aún no está conectado, según la demanda", 2.8f));
            yield return new WaitUntil(() => gm.clock.now >= TueRead);
            gm.clock.now = TueRead;
            // cuenta atrás
            stage = 3; SetSpeedForStage();
            StartCoroutine(gm.ui.TitleCard("Martes 9 de febrero · 09:11", "El servidor SWIFT vuelve a conectarse. La primera transferencia llega a las 10:24", 3f));
            AudioManager.Loop("tension_loop", true, 0.5f);
            yield return new WaitUntil(() => gm.clock.now >= TueOut);
            gm.clock.running = true; gm.clock.now = TueOut;
            yield return WithdrawalEvent();
            gm.clock.running = false;
            stage = 4;
            AudioManager.Loop("tension_loop", false);
            yield return new WaitForSeconds(4.5f);
            yield return new WaitUntil(() => !gm.ui.ModalOpen);
            gm.EndScenario();
        }

        void SetSpeedForStage()
        {
            bool practice = gm.difficulty == Difficulty.Practica && !ff;
            gm.clock.running = !practice;
            float k = gm.difficulty == Difficulty.Rapido ? 1.6f : 1f;
            switch (stage)
            {
                case 0: case 1: gm.clock.minPerSec = ff ? 120f : 1.5f * k; break;
                case 2: gm.clock.minPerSec = ff ? 1500f : 180f; break;
                case 3: gm.clock.minPerSec = ff ? 25f : 1f * k; break;
                default: gm.clock.minPerSec = 0f; break;
            }
        }

        public void FastForward()
        {
            ff = true; SetSpeedForStage();
            gm.ui.Toast("Avanzando el tiempo…", 2f, ToastKind.Info);
        }

        // =====================================================================
        //  Hitos (los usan los dos modos)
        // =====================================================================
        IEnumerator CreditEvent()
        {
            string when = "Vie 05/02 · mañana";
            if (gm.mode == SimMode.Defensa && gm.IsDefenseApplied("kyc"))
            {
                creditRejected = true;
                gm.log.Add(when, "Crédito de 4 órdenes (SWIFT)", "US$ 81.000.000", "RECHAZADO · KYC", UIManager.Neon);
                gm.ui.Toast("CRÉDITO RECHAZADO · las 5 cuentas no pasaron la verificación de identidad", 6f, ToastKind.Good);
                AudioManager.Play("denied");
                yield break;
            }
            creditsArrived = true;
            gm.log.ApplyCredit("Sin alerta");
            gm.log.Add(when, "Crédito de 4 órdenes (SWIFT)", "US$ 81.000.000", "ACREDITADO", UIManager.Amber);
            AudioManager.Play("cash");
            Fx.Bills(this, w.terminal.transform.position + Vector3.up * 3f, 70, false, Vector3.zero);
            gm.ui.Toast("LLEGAN US$ 81 MILLONES a 4 cuentas que llevaban 9 meses dormidas", 6f, ToastKind.Warn);
            yield return new WaitForSeconds(0.3f);
            Reevaluate();
        }

        IEnumerator BoxEvent()
        {
            string when = "Vie 05/02 · tarde (hora ilustrativa)";
            // La demanda describe una entrega física a la sucursal para su posterior recogida.
            // La defensa puede retener la caja dentro de la sucursal, pero no borrar la entrega.
            if (w.caja != null) w.caja.gameObject.SetActive(true);
            boxDelivered = true;
            boxHeld = gm.mode == SimMode.Defensa && gm.IsDefenseApplied("hold");
            string status = boxHeld ? "RECIBIDA · RETENIDA PARA VERIFICACIÓN" : "RECIBIDA · PENDIENTE DE RECOGIDA";
            gm.log.Add(when, "Entrega a sucursal · caja de cartón", "20.000.000 pesos", status, boxHeld ? UIManager.Neon : UIManager.Amber);
            gm.ui.Toast(boxHeld
                ? "LA CAJA LLEGÓ A LA SUCURSAL, PERO QUEDA RETENIDA Y NO SE LIBERA PARA RECOGIDA"
                : "SE ENTREGA A LA SUCURSAL UNA CAJA CON 20 MILLONES DE PESOS · queda pendiente de recogida",
                6f, boxHeld ? ToastKind.Good : ToastKind.Warn);
            AudioManager.Play("box");
            StartCoroutine(DeliverCashBox());
            yield return null;
        }

        IEnumerator DeliverCashBox()
        {
            var m = w.mensajero;
            if (m == null || w.cajaMensajero == null) yield break;
            // En la reconstrucción, el personaje representa la entrega del efectivo a la sucursal.
            // La demanda indica que una caja quedó preparada para recogida; no fija la hora exacta.
            m.gameObject.SetActive(true); m.transform.position = new Vector3(6.2f, 0f, 16.2f);
            w.cajaMensajero.SetActive(true);
            yield return m.WalkTo(new Vector3(5.0f, 0f, 14.2f), 1.4f);
            yield return m.WalkPath(w.mensajeroPath, 1.6f);
            m.gameObject.SetActive(false);
        }

        IEnumerator WithdrawalEvent()
        {
            // La demanda de Bangladesh describe cinco transferencias, no un retiro único.
            // Importes en dólares; total exacto de las cinco operaciones: US$58.158.641,63.
            DateTime[] times =
            {
                new DateTime(2016, 2, 9, 10, 24, 0),
                new DateTime(2016, 2, 9, 11, 19, 0),
                new DateTime(2016, 2, 9, 11, 34, 0),
                new DateTime(2016, 2, 9, 11, 35, 0),
                new DateTime(2016, 2, 9, 11, 37, 0)
            };
            // El registro conserva cada importe exacto en centavos. El libro de cuentas trabaja
            // en dólares enteros, por lo que se redondea el acumulado y se debita la diferencia
            // de cada hito; así las cinco filas y el total no acumulan errores por redondeo.
            long[] amountCents = { 1_521_597_726, 1_995_150_213, 976_912_415, 723_615_462, 598_588_347 };
            string[] amountLabels = { "15.215.977,26", "19.951.502,13", "9.769.124,15", "7.236.154,62", "5.985.883,47" };
            string[] destinations = { "Philrem", "Go-Centurytex", "Go-Centurytex / Philrem", "Go-Centurytex", "Go-Centurytex" };
            if (gm.mode == SimMode.Defensa && creditRejected)
            {
                gm.log.Add("Mar 09/02 · 10:24–11:37", "Cinco transferencias solicitadas", "US$ 58.158.641,63", "SIN FONDOS · KYC", UIManager.Neon);
                gm.ui.Toast("Las cinco operaciones no encuentran saldo: el crédito fue rechazado antes de entrar.", 7f, ToastKind.Good);
                AudioManager.Play("denied"); withdrawalDone = true; yield break;
            }
            if (gm.mode == SimMode.Defensa && held)
            {
                gm.log.Add("Mar 09/02 · 10:24–11:37", "Cinco transferencias solicitadas", "US$ 58.158.641,63", "RECHAZADAS · RETENIDAS", UIManager.Neon);
                gm.ui.Toast("Las cinco operaciones quedan rechazadas: los fondos permanecen retenidos.", 7f, ToastKind.Good);
                AudioManager.Play("denied"); withdrawalDone = true; yield break;
            }

            lostUsd = 0;
            long cumulativeCents = 0, cumulativeDebitedUsd = 0;
            for (int i = 0; i < times.Length; i++)
            {
                if (gm.clock.now < times[i])
                    yield return new WaitUntil(() => gm.clock.now >= times[i]);
                gm.clock.now = times[i];
                cumulativeCents += amountCents[i];
                long nextRoundedTotal = (cumulativeCents + 50L) / 100L;
                long ledgerDebit = nextRoundedTotal - cumulativeDebitedUsd;
                cumulativeDebitedUsd = nextRoundedTotal;
                lostUsd += ledgerDebit;
                string when = "Mar 09/02 · " + times[i].ToString("HH:mm");
                string amount = "US$ " + amountLabels[i];
                gm.log.Add(when, "Transferencia a " + destinations[i], amount,
                    "APROBADA · SIN RETENCIÓN", UIManager.Red);
                ApplyProportionalDebit(ledgerDebit);
                AudioManager.Play(i == 0 ? "cash" : "keys", i == 0 ? 0.65f : 0.35f);
                if (i == 0)
                {
                    w.vault.Open(true);
                    Fx.Bills(this, w.vault.transform.position + new Vector3(1.1f, 0.5f, 0.6f), 36, true, new Vector3(0f, 0f, -1f));
                    gm.ui.Toast("10:24 · primera transferencia: US$15,22 millones", 5f, ToastKind.Bad);
                }
                else if (i == times.Length - 1)
                    gm.ui.Toast("11:37 · quinta operación. Total transferido: US$58,16 millones.", 6f, ToastKind.Bad);
            }
            gm.log.Add("Mar 09/02 · 11:37", "TOTAL · cinco transferencias", "US$ 58.158.641,63", "EJECUTADAS", UIManager.Red);
            gm.log.version++;
            withdrawalDone = true;
        }

        void ApplyProportionalDebit(long amount)
        {
            long remaining = amount;
            long available = 0;
            for (int i = 0; i < gm.log.accounts.Count; i++)
                if (gm.log.accounts[i].recibe) available += gm.log.accounts[i].saldoUsd;
            if (available <= 0) return;

            // Distribución ilustrativa entre los cuatro saldos del simulador; los importes por cuenta
            // originales no se presentan como una asignación histórica probada.
            long[] debits = new long[gm.log.accounts.Count];
            long assigned = 0;
            for (int i = 0; i < gm.log.accounts.Count; i++)
            {
                var account = gm.log.accounts[i];
                if (!account.recibe || account.saldoUsd <= 0) continue;
                debits[i] = (long)Math.Floor(amount * (account.saldoUsd / (double)available));
                assigned += debits[i];
            }
            remaining = amount - assigned;
            for (int i = 0; i < gm.log.accounts.Count && remaining > 0; i++)
            {
                var account = gm.log.accounts[i];
                if (!account.recibe || account.saldoUsd <= debits[i]) continue;
                long extra = Math.Min(account.saldoUsd - debits[i], remaining);
                debits[i] += extra;
                remaining -= extra;
            }
            for (int i = 0; i < gm.log.accounts.Count; i++)
            {
                var account = gm.log.accounts[i];
                if (!account.recibe) continue;
                account.saldoUsd = Math.Max(0L, account.saldoUsd - debits[i]);
                account.ultimoMov = "09/02/2016";
            }
            gm.log.version++;
        }

        // ---------------- reglas de defensa ----------------
        public void SetDormantRule(int i) { w.dormant.selected = i; }
        public void ReevaluateRule() { Reevaluate(); }

        public void OnDefenseApplied(string id)
        {
            if (id == "dormant") w.dormant.active = true;
            if (id == "kyc")
            {
                KycChecker.I.AutoVerify(gm.data);
                if (!creditsArrived && !creditRejected) gm.ui.Toast("Las 5 cuentas quedan marcadas como no verificadas: el crédito será rechazado.", 5f, ToastKind.Good);
            }
            if (id == "hold")
            {
                if (w.cctv != null) w.cctv.Show(w.cctv.selected);
                if (boxDelivered && !boxHeld)
                {
                    boxHeld = true;
                    gm.log.Add(gm.clock.Label, "Custodia de la caja entregada", "20.000.000 pesos", "RETENIDA · NO LIBERAR PARA RECOGIDA", UIManager.Neon);
                    gm.ui.Toast("La caja ya entregada queda ahora bajo retención; no se autoriza su recogida.", 5f, ToastKind.Good);
                }
            }
            Reevaluate();
            idleTimer = 0f; RefreshBeacons();
        }

        public void OnLedgerViewed(int tab)
        {
            if (gm.mode == SimMode.Defensa) { if (tab == 0) gm.RegisterEvidence("CUENTAS_DORMIDAS"); return; }
            if (tab == 0 && creditsArrived) gm.RegisterEvidence("CUENTAS_DORMIDAS");
            if (tab == 1 && withdrawalDone) gm.RegisterEvidence("RETIRO_1024");
            if (tab == 2) gm.RegisterEvidence("SWIFT_OFFLINE");
        }

        void Reevaluate()
        {
            if (gm.mode != SimMode.Defensa || !creditsArrived || creditRejected) return;
            bool effective = w.dormant.Effective;
            bool trigger = effective || gm.IsDefenseApplied("kyc");
            if (effective && !alarmOn)
            {
                alarmOn = true; w.dormant.Trigger(true); gm.ui.SetAlarm(true);
                foreach (var a in gm.log.accounts) if (a.recibe) a.alerta = "ALERTA";
                gm.log.Add(gm.clock.Label, "Alerta: cuentas dormidas con montos grandes", "US$ 81.000.000", "ALERTA", UIManager.Amber);
                gm.ui.Toast("ALERTA · cuentas dormidas reciben montos grandes. Falta la retención de fondos.", 6f, ToastKind.Warn);
            }
            if (!held && trigger && gm.IsDefenseApplied("hold"))
            {
                held = true; w.vault.SetLamp(new Color(1f, 0.7f, 0.1f));
                gm.log.Add(gm.clock.Label, "Retención preventiva de los fondos", "US$ 81.000.000", "RETENIDO", UIManager.Neon);
                gm.ui.Toast("FONDOS RETENIDOS · US$ 81 millones congelados para revisión", 6f, ToastKind.Good);
                if (alarmOn) { w.dormant.Trigger(false); gm.ui.SetAlarm(false); }
            }
            gm.ui.Rebuild();
        }

        // ---------------- estado para el HUD ----------------
        public string MoneyLine()
        {
            if (gm.mode == SimMode.Defensa)
            {
                string m;
                switch (stage)
                {
                    case 0: m = "10:30 llegan US$81 M"; break;
                    case 1: m = "Entrega de efectivo a la sucursal (hora ilustrativa)"; break;
                    case 2: m = "fin de semana"; break;
                    case 3: m = "¡Cinco transferencias entre 10:24 y 11:37!"; break;
                    default: m = held ? "FONDOS RETENIDOS" : (creditRejected ? "CRÉDITO RECHAZADO" : "cinco transferencias procesadas"); break;
                }
                return "Controles " + gm.defenses.Count + "/3 · " + m;
            }
            if (withdrawalDone) return "Se transfirieron US$ 58,16 M";
            if (creditsArrived) return "En cuentas: US$ 81 M";
            return "5 cuentas · US$ 500 c/u";
        }

        // ---------------- resultado ----------------
        public ResultData BuildResult()
        {
            var d = gm.data; var kyc = KycChecker.I; var r = new ResultData();
            int tot = 0; foreach (var f in d.fichas) foreach (var k in KycChecker.Kinds) if (KycChecker.Check(f, k, d).Flaggable) tot++;
            int mins = Mathf.FloorToInt(gm.playTime / 60f), secs = Mathf.FloorToInt(gm.playTime % 60f);
            string time = mins + ":" + secs.ToString("00");

            if (gm.mode == SimMode.Ataque)
            {
                r.exito = true; r.color = UIManager.Red;
                r.titulo = "RECONSTRUCCIÓN COMPLETA";
                r.subtitulo = "ATAQUE CONSUMADO · cinco transferencias por US$58,16 millones entre 10:24 y 11:37.";
                int score = gm.EvidenceCount * 6 + Mathf.RoundToInt(30f * Mathf.Min(tot, kyc.correctFlags) / Mathf.Max(1, tot)) - kyc.wrongFlags * 4 - gm.hintsUsed * 2;
                r.puntaje = Mathf.Clamp(score, 0, 100); r.estrellas = r.puntaje >= 85 ? 3 : (r.puntaje >= 60 ? 2 : 1);
                r.cifras.Add("Recibido en las cuentas: <b>US$ 81.000.000</b>");
                r.cifras.Add("Transferido el martes 10:24–11:37: <b>US$ 58.158.641,63</b> (cinco operaciones)");
                r.cifras.Add("Efectivo en caja de cartón: <b>20.000.000 pesos entregados a la sucursal para recogida</b>");
                r.cifras.Add("Cuentas falsas: <b>5</b> · depósito inicial US$ 500");
                r.cifras.Add("Pistas halladas: <b>" + gm.EvidenceCount + "/" + d.evidencias.Length + "</b>");
                r.cifras.Add("Datos falsos detectados: <b>" + kyc.correctFlags + "/" + tot + "</b> · falsas alarmas: " + kyc.wrongFlags);
                r.cifras.Add("Tiempo: " + time + " · pistas pedidas: " + gm.hintsUsed);
                for (int i = 0; i < 3; i++) r.lecciones.Add("<b>" + d.vulnerabilidades[i] + "</b>\n→ Control: " + d.defensas[i] + ".");
            }
            else
            {
                bool prevented = creditRejected, heldOk = held;
                bool alertOnly = w.dormant.Effective && !heldOk && !prevented;
                bool cashSecured = boxDelivered && boxHeld;
                // El escenario solo cuenta como contención completa si se frenan los créditos
                // digitales o las transferencias y, además, la caja queda bajo custodia.
                r.exito = (prevented || heldOk) && cashSecured;
                long recibido = prevented ? 0 : d.recibidoUsd; long retenido = heldOk ? d.recibidoUsd : 0; long salio = (prevented || heldOk) ? 0 : d.retiradoUsd;
                if (prevented && cashSecured) { r.titulo = "ATAQUE PREVENIDO · KYC Y CUSTODIA ACTIVOS"; r.subtitulo = "El KYC rechazó los créditos SWIFT y la caja entregada quedó retenida en la sucursal."; r.color = UIManager.Neon; }
                else if (prevented) { r.titulo = "CONTROL PARCIAL · CRÉDITO SWIFT RECHAZADO"; r.subtitulo = "El KYC impidió la entrada de los US$81 millones, pero la caja física llegó a la sucursal y sigue pendiente de recogida: falta asegurar su custodia."; r.color = UIManager.Amber; }
                else if (heldOk) { r.titulo = "ATAQUE FRUSTRADO · FONDOS Y CAJA RETENIDOS"; r.subtitulo = "Los US$81 millones y la caja de 20 millones de pesos quedaron bajo retención para revisión."; r.color = UIManager.Neon; }
                else { r.titulo = "EL DINERO SALIÓ IGUAL"; r.subtitulo = alertOnly ? "La alerta sonó, pero sin retención no se detuvieron las cinco transferencias. La caja quedó pendiente de recogida." : "Faltaron controles: US$58,16 M se transfirieron entre 10:24 y 11:37 y la caja quedó pendiente de recogida."; r.color = UIManager.Red; }
                int score = prevented ? (cashSecured ? 100 : 72) : (heldOk ? 92 : (alertOnly ? 40 : 20));
                if (w.dormant.selected == 0 && w.dormant.active) score -= 10;
                if (w.dormant.active && !w.dormant.Effective) score -= 15;
                score -= gm.hintsUsed * 2;
                r.puntaje = Mathf.Clamp(score, 0, 100); r.estrellas = r.puntaje >= 90 ? 3 : (r.puntaje >= 65 ? 2 : 1);
                r.cifras.Add("Entró a las cuentas: <b>US$ " + recibido.ToString("N0") + "</b>");
                r.cifras.Add("Retenido: <b>US$ " + retenido.ToString("N0") + "</b>");
                r.cifras.Add("Transferido el martes 10:24–11:37: <b>US$ " + salio.ToString("N0") + "</b>");
                r.cifras.Add("Caja de cartón: <b>" + (!boxDelivered ? "NO REGISTRADA" : (boxHeld ? "ENTREGADA · RETENIDA EN SUCURSAL (20.000.000 pesos)" : "ENTREGADA · PENDIENTE DE RECOGIDA (20.000.000 pesos)")) + "</b>");
                r.cifras.Add("Controles activos: <b>" + gm.defenses.Count + "/3</b>");
                r.cifras.Add("Tiempo: " + time + " · pistas pedidas: " + gm.hintsUsed);
                string[] ids = { "kyc", "dormant", "hold" };
                for (int i = 0; i < 3; i++)
                {
                    bool on = gm.IsDefenseApplied(ids[i]);
                    r.lecciones.Add("<b>" + d.defensas[i] + "</b>: " + (on ? "ACTIVO" : "NO activado") + "\n→ Cierra: " + d.vulnerabilidades[i] + ".");
                }
                if (prevented && !cashSecured) r.lecciones.Add("<b>Controlar el riesgo digital no cubre el efectivo físico</b>: KYC rechazó los créditos, pero la caja entregada necesita custodia y bloqueo de recogida.");
                if (alertOnly) r.lecciones.Add("<b>Detectar no basta</b>: una alerta sin responsable ni retención no detiene el dinero.");
                if (w.dormant.active && !w.dormant.Effective) r.lecciones.Add("<b>Una regla demasiado laxa</b> nunca se dispara.");
            }
            return r;
        }
    }
}
