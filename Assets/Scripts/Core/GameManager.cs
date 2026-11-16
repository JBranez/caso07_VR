// GameManager (contrato común): estado de la simulación, evidencias, defensas y fin del escenario.
//   public ScenarioData data;  public SimMode mode;
//   public void RegisterEvidence(string id);  public void ApplyDefense(string id);  public void EndScenario();
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace LasPalmeras
{
    public enum Difficulty { Practica, Normal, Rapido }

    public class Objective
    {
        public string id;
        public Func<string> text;
        public Func<bool> done;
        public Interactable[] targets;
        public string hint;
        public bool optional;
    }

    public class ResultData
    {
        public bool exito;
        public string titulo, subtitulo;
        public Color color = Color.white;
        public List<string> cifras = new List<string>();
        public List<string> lecciones = new List<string>();
        public int puntaje, estrellas;
    }

    public class SimClock
    {
        public DateTime now = new DateTime(2015, 5, 15);
        public float minPerSec;
        public bool running, paused, showTime = true;
        static readonly string[] Dias = { "DOM", "LUN", "MAR", "MIÉ", "JUE", "VIE", "SÁB" };

        public void Tick(float dt) { if (running && !paused && minPerSec > 0f) now = now.AddMinutes(minPerSec * dt); }

        public string Label
        {
            get
            {
                var inv = CultureInfo.InvariantCulture;
                string d = Dias[(int)now.DayOfWeek] + " " + now.ToString("dd/MM/yyyy", inv);
                return showTime ? d + "  " + now.ToString("HH:mm", inv) : d;
            }
        }
    }

    public class GameManager : MonoBehaviour
    {
        public enum Phase { Loading, Menu, Playing, Result }

        public static GameManager I;
        public ScenarioData data;
        public SimMode mode = SimMode.Ataque;
        public Difficulty difficulty = Difficulty.Normal;
        public Phase phase = Phase.Loading;

        public PlayerRig player; public UIManager ui; public AgencyWorld world; public StoryDirector story;
        public WithdrawalLog log; public readonly SimClock clock = new SimClock();

        public readonly HashSet<string> evidences = new HashSet<string>();
        public readonly HashSet<string> defenses = new HashSet<string>();
        public readonly List<Objective> objectives = new List<Objective>();
        public event Action<string> EvidenceRegistered;
        public event Action<string> DefenseApplied;
        public int hintsUsed; public bool ledgerViewed; public float playTime;
        string lastBoard = "";

        public bool CctvViewed { get { return world != null && world.cctv != null && world.cctv.VisitedCount >= 2; } }
        public bool IsDefenseApplied(string id) { return defenses.Contains(id); }
        public int EvidenceCount { get { return evidences.Count; } }

        void Awake()
        {
            I = this;
            data = Resources.Load<ScenarioData>("ScenarioData_LasPalmeras");
            if (data == null) data = ScenarioData.CreateDefault();
        }

        void OnDestroy()
        {
            if (I == this) I = null;
            KycChecker.I = null; WithdrawalLog.I = null; AudioManager.I = null;
            Interactable.All.Clear(); TeleportPoint.Current = null;
        }

        IEnumerator Start()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = Application.isMobilePlatform ? 0 : 1;
            QualitySettings.pixelLightCount = Application.isMobilePlatform ? 3 : 6;
            if (Application.isMobilePlatform) QualitySettings.shadows = ShadowQuality.Disable;
#if UNITY_ANDROID && !UNITY_EDITOR
            Screen.orientation = ScreenOrientation.LandscapeLeft;
#endif
            CleanDefaults();
            AudioManager.Create(transform);
            AudioGuide.Create(transform);
            world = AgencyBuilder.Build(transform);
            yield return null;
            log = gameObject.AddComponent<WithdrawalLog>(); log.Init(data);
            player = PlayerRig.Create(transform, world.spawn.position, 0f);
            ui = UIManager.Create(this);
            story = StoryDirector.Create(this);
            AssistantOrb.Create(this);
            VrMode.Refresh();
            if (VrMode.Active) player.SetControlMode(PlayerRig.ControlMode.Gaze);
            else if (Application.isMobilePlatform) player.SetControlMode(PlayerRig.ControlMode.Gaze);
            AudioManager.SetEnvironment("lobby");
            yield return null;

            phase = Phase.Menu;
            if (Bootstrap.pendingStart)
            {
                Bootstrap.pendingStart = false;
                mode = Bootstrap.pendingMode; difficulty = Bootstrap.pendingDifficulty;
                ui.OpenBriefing();
            }
            else ui.OpenMenu();
        }

        void CleanDefaults()
        {
            foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None)) if (c.transform.root != transform) c.gameObject.SetActive(false);
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.transform.root != transform) l.enabled = false;
            foreach (var a in FindObjectsByType<AudioListener>(FindObjectsSortMode.None)) if (a.transform.root != transform) a.enabled = false;
        }

        void Update()
        {
            if (phase == Phase.Playing) playTime += Time.deltaTime;
            clock.paused = ui != null && ui.ModalOpen;
            clock.Tick(Time.deltaTime);
            if (world != null && world.clockBoard != null)
            {
                string lbl = phase == Phase.Playing ? clock.Label : "—";
                if (lbl != lastBoard) { lastBoard = lbl; world.clockBoard.text = lbl; }
            }
        }

        // ---------------- flujo ----------------
        public void StartMode(SimMode m, Difficulty d)
        {
            mode = m; difficulty = d;
            ui.OpenBriefing();
        }

        public void BeginPlay()
        {
            ui.CloseModal(false);
            phase = Phase.Playing;
            story.Begin();
        }

        public void ReturnToMenu() { Bootstrap.Restart(false, mode, difficulty); }
        public void Restart() { Bootstrap.Restart(true, mode, difficulty); }

        // ---------------- evidencias y defensas ----------------
        public void RegisterEvidence(string id)
        {
            if (string.IsNullOrEmpty(id) || !evidences.Add(id)) return;
            var def = data.Evidence(id);
            AudioManager.Play("ding");
            if (ui != null && def != null) ui.Toast("PISTA ENCONTRADA · " + def.titulo + "  (" + evidences.Count + "/" + data.evidencias.Length + ")", 3.5f, ToastKind.Good);
            if (EvidenceRegistered != null) EvidenceRegistered(id);
        }

        public bool Unlocked(string id)
        {
            switch (id)
            {
                case "kyc": return KycChecker.I != null && KycChecker.I.correctFlags >= 3;
                case "dormant": return ledgerViewed;
                case "hold": return CctvViewed;
            }
            return false;
        }

        public void TryApply(string id)
        {
            if (mode != SimMode.Defensa || phase != Phase.Playing || IsDefenseApplied(id)) return;
            if (!Unlocked(id))
            {
                var d = data.Control(id);
                ui.Toast("Bloqueado · " + (d != null ? d.requisito : ""), 3.5f, ToastKind.Warn);
                AudioManager.Play("wrong");
                return;
            }
            ApplyDefense(id);
        }

        public void ApplyDefense(string id)
        {
            if (string.IsNullOrEmpty(id) || !defenses.Add(id)) return;
            var d = data.Control(id);
            AudioManager.Play("success", 0.7f);
            if (ui != null && d != null) ui.Toast("CONTROL ACTIVADO · " + d.nombre, 3.5f, ToastKind.Good);
            if (DefenseApplied != null) DefenseApplied(id);
            if (story != null) story.OnDefenseApplied(id);
        }

        public void EndScenario()
        {
            if (phase == Phase.Result) return;
            phase = Phase.Result;
            var r = story.BuildResult();
            AudioManager.Loop("tension_loop", false);
            AudioManager.Loop("alarm_loop", false);
            AudioManager.Play(r.exito ? "success" : "fail");
            ui.OpenResult(r);
        }
    }
}
