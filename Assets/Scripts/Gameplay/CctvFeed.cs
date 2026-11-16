// CctvFeed: el monitor de cámaras. Alterna ruido («SIN SEÑAL») o grabación según el modo.
using UnityEngine;

namespace LasPalmeras
{
    public class CctvFeed : MonoBehaviour
    {
        public static readonly string[] Days = { "JUE 04/02/2016", "VIE 05/02/2016", "SÁB 06/02/2016", "DOM 07/02/2016", "LUN 08/02/2016", "MAR 09/02/2016" };
        public static readonly string[] DaysShort = { "Jue 4", "Vie 5", "Sáb 6", "Dom 7", "Lun 8", "Mar 9" };
        public static readonly System.DateTime[] DayDates = {
            new System.DateTime(2016,2,4), new System.DateTime(2016,2,5), new System.DateTime(2016,2,6),
            new System.DateTime(2016,2,7), new System.DateTime(2016,2,8), new System.DateTime(2016,2,9) };

        public Renderer screen;
        public TextMesh title, sub, rec;
        public bool[] visited = new bool[6];
        public int selected = -1;
        Material mat;
        public System.Collections.Generic.List<Material> others;

        public int VisitedCount { get { int n = 0; foreach (bool b in visited) if (b) n++; return n; } }
        public bool AllVisited { get { return VisitedCount >= 6; } }

        public void Setup(Renderer r, TextMesh t, TextMesh s, TextMesh recLabel)
        {
            screen = r; title = t; sub = s; rec = recLabel;
            mat = Mats.Cctv(); screen.sharedMaterial = mat;
            Show(-1);
        }

        /// <summary>En defensa, las cámaras graban desde que se activa el control «hold».</summary>
        bool Recording
        {
            get { var gm = GameManager.I; return gm != null && gm.mode == SimMode.Defensa && gm.IsDefenseApplied("hold"); }
        }

        public void Show(int day)
        {
            selected = day;
            var gm = GameManager.I;
            float modeVal = 0f; string t = "SELECCIONA UNA FECHA", s = ""; bool recOn = false;
            if (day >= 0)
            {
                visited[day] = true;
                t = Days[day];
                bool future = gm != null && gm.mode == SimMode.Defensa && gm.clock != null && gm.clock.now.Date < DayDates[day];
                if (Recording && day >= 1 && !future)
                {
                    bool box = day == 1 && gm.story != null && gm.story.boxDelivered;
                    modeVal = box ? 2f : 1f; s = "CAM-01 · GRABANDO"; recOn = true;
                }
                else if (future) { modeVal = 0f; s = "AÚN SIN DATOS"; }
                else { modeVal = 0f; s = "SIN SEÑAL"; }
            }
            else modeVal = 0f;
            if (mat.HasProperty("_Mode")) mat.SetFloat("_Mode", modeVal);
            title.text = t; sub.text = s;
            sub.color = recOn ? new Color(0.4f, 1f, 0.6f) : new Color(1f, 0.35f, 0.3f);
            rec.gameObject.SetActive(recOn);
            if (day >= 0 && gm != null && gm.mode == SimMode.Ataque && AllVisited) gm.RegisterEvidence("CCTV_SIN_SENAL");
            if (day >= 0 && gm != null && gm.mode == SimMode.Defensa && day >= 0) { /* la evidencia de defensa se registra al abrir el selector */ }
        }

        void Update()
        {
            if (others != null) { float mv = Recording ? 1f : 0f; foreach (var m in others) if (m.HasProperty("_Mode")) m.SetFloat("_Mode", mv); }
            if (rec != null && rec.gameObject.activeSelf) rec.color = new Color(1f, 0.2f, 0.2f, Mathf.PingPong(Time.time * 1.5f, 1f) > 0.5f ? 1f : 0.2f);
        }
    }
}
