// DormantAlert: regla de «cuenta inactiva con monto grande».
using System;
using UnityEngine;

namespace LasPalmeras
{
    public class DormantAlert : MonoBehaviour
    {
        public struct Rule { public string nombre; public int dias; public long montoUsd; public string nota; public bool ruidosa; }

        public static readonly Rule[] Presets =
        {
            new Rule { nombre = "A · 30 días y ≥ US$10.000",  dias = 30,  montoUsd = 10_000,      ruidosa = true,
                       nota = "Detecta el caso, pero también marca cuentas normales: 38 falsas alarmas por día." },
            new Rule { nombre = "B · 90 días y ≥ US$100.000", dias = 90,  montoUsd = 100_000,     ruidosa = false,
                       nota = "Equilibrada: detecta cuentas dormidas con montos fuera de lo normal." },
            new Rule { nombre = "C · 365 días y ≥ US$100 M",  dias = 365, montoUsd = 100_000_000, ruidosa = false,
                       nota = "Demasiado laxa: nunca se activaría con cuentas de 9 meses y créditos de unos 20 millones." }
        };

        /// <summary>Días entre la apertura (15 may 2015) y la llegada del dinero (5 feb 2016).</summary>
        public static int DiasDormidas { get { return (new DateTime(2016, 2, 5) - new DateTime(2015, 5, 15)).Days; } }

        public static bool Detects(Rule r, int dias, long monto) { return dias >= r.dias && monto >= r.montoUsd; }

        public int selected = -1;
        public bool active;
        public Renderer lampRenderer; public Light lampLight; public Transform lampSpin;
        bool alarm;
        Material lampMat;

        public void SetupLamp(Renderer r, Light l, Transform spin)
        {
            lampRenderer = r; lampLight = l; lampSpin = spin;
            lampMat = new Material(r.sharedMaterial); r.sharedMaterial = lampMat;
            Trigger(false);
        }

        public bool Effective
        {
            get { return active && selected >= 0 && Detects(Presets[selected], DiasDormidas, 20_000_000); }
        }

        public void Trigger(bool on)
        {
            alarm = on;
            if (lampLight != null) lampLight.enabled = on;
            if (lampMat != null) lampMat.color = on ? new Color(1f, 0.15f, 0.1f) : new Color(0.25f, 0.05f, 0.04f);
            AudioManager.Loop("alarm_loop", on, 0.35f);
        }

        void Update()
        {
            if (alarm && lampSpin != null) lampSpin.Rotate(0f, 260f * Time.deltaTime, 0f);
        }
    }
}
