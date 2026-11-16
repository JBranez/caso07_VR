// WithdrawalLog: registro de cuentas y de retiros con hora.
using System.Collections.Generic;
using UnityEngine;

namespace LasPalmeras
{
    public class LogRow { public string cuando, concepto, monto, estado; public Color color; }
    public class AccountRow { public string numero, titular, apertura, ultimoMov, alerta; public long saldoUsd; public bool recibe; }

    public class WithdrawalLog : MonoBehaviour
    {
        public static WithdrawalLog I;
        public readonly List<LogRow> rows = new List<LogRow>();
        public readonly List<AccountRow> accounts = new List<AccountRow>();
        /// <summary>Montos por cuenta: ilustrativos. El TOTAL (US$81 M) es el dato histórico.</summary>
        public static readonly long[] Credits = { 20_000_000, 20_000_000, 20_000_000, 21_000_000 };
        public bool credited;
        public int version;

        void Awake() { I = this; }

        public void Init(ScenarioData d)
        {
            accounts.Clear(); rows.Clear(); credited = false;
            for (int i = 0; i < d.fichas.Length; i++)
            {
                accounts.Add(new AccountRow
                {
                    numero = "PI-5501-" + (101 + i), titular = d.fichas[i].titular,
                    apertura = "15/05/2015", ultimoMov = "15/05/2015", saldoUsd = d.depositoInicialUsd,
                    alerta = "—", recibe = i < d.cuentasQueRecibenFondos
                });
            }
        }

        public void Add(string cuando, string concepto, string monto, string estado, Color c)
        {
            rows.Add(new LogRow { cuando = cuando, concepto = concepto, monto = monto, estado = estado, color = c });
            version++;
        }

        public void ApplyCredit(string alerta)
        {
            credited = true; version++;
            for (int i = 0; i < accounts.Count; i++)
                if (accounts[i].recibe) { accounts[i].saldoUsd += Credits[i]; accounts[i].ultimoMov = "05/02/2016"; accounts[i].alerta = alerta; }
        }

        public long TotalBalance { get { long t = 0; foreach (var a in accounts) t += a.saldoUsd; return t; } }
    }
}
