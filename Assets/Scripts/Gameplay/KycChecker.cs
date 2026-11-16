// KycChecker: compara los datos de la ficha de apertura contra reglas de verificación.
// Reglas del SIMULADOR (no son las oficiales de ningún país):
//  - Documento: 8 dígitos + dígito verificador (módulo 11 con pesos 3,2,7,6,5,4,3,2).
//  - Domicilio: debe existir en el registro de domicilios verificados.
//  - Teléfono: 9 dígitos, empieza con 9 y no es un patrón repetido.
//  - Carta de bienvenida devuelta = domicilio no verificable.
//  - Perfil: ingreso declarado ≥ 20 × depósito inicial es incoherente.
//  - Patrón: cuentas abiertas en serie con el mismo depósito exacto.
using System.Collections.Generic;
using UnityEngine;

namespace LasPalmeras
{
    public enum FieldKind { Titular, Documento, Domicilio, Telefono, Ocupacion, Deposito, Carta }
    public enum FieldStatus { Ok, Falso, Sospechoso }

    public struct FieldCheck
    {
        public FieldStatus status;
        public string razon;
        public bool Flaggable { get { return status != FieldStatus.Ok; } }
    }

    public class KycChecker : MonoBehaviour
    {
        public static KycChecker I;
        public int correctFlags, wrongFlags;
        readonly HashSet<string> flagged = new HashSet<string>();   // "01:Documento"
        public static readonly FieldKind[] Kinds = { FieldKind.Titular, FieldKind.Documento, FieldKind.Domicilio, FieldKind.Telefono, FieldKind.Ocupacion, FieldKind.Deposito, FieldKind.Carta };

        void Awake() { I = this; }

        // ---------- reglas ----------
        static char CheckChar(string d8)
        {
            int[] w = { 3, 2, 7, 6, 5, 4, 3, 2 };
            int s = 0;
            for (int i = 0; i < 8; i++) s += (d8[i] - '0') * w[i];
            int r = (11 - (s % 11)) % 11;
            return r == 10 ? 'K' : (char)('0' + r);
        }

        public static string MakeDocument(string d8, bool valid)
        {
            char c = CheckChar(d8);
            if (!valid) c = (c == '3') ? '5' : '3';
            return d8 + "-" + c;
        }

        public static bool DocumentValid(string doc)
        {
            if (string.IsNullOrEmpty(doc) || doc.Length != 10 || doc[8] != '-') return false;
            for (int i = 0; i < 8; i++) if (doc[i] < '0' || doc[i] > '9') return false;
            return CheckChar(doc.Substring(0, 8)) == doc[9];
        }

        public static bool PhoneValid(string tel)
        {
            if (string.IsNullOrEmpty(tel)) return false;
            string d = tel.Replace(" ", "");
            if (d.Length != 9 || d[0] != '9') return false;
            bool allSame = true;
            for (int i = 1; i < d.Length; i++) if (d[i] != d[0]) { allSame = false; break; }
            return !allSame;
        }

        public static bool AddressVerified(string dom, ScenarioData d)
        {
            if (ReferenceEquals(d, null) || d.domiciliosVerificados == null) return false;
            foreach (var a in d.domiciliosVerificados) if (a == dom) return true;
            return false;
        }

        public static FieldCheck Check(FichaData f, FieldKind k, ScenarioData d)
        {
            var r = new FieldCheck { status = FieldStatus.Ok, razon = "Sin señales de alerta con los datos disponibles." };
            switch (k)
            {
                case FieldKind.Titular:
                    r.razon = "Un nombre por sí solo no se puede verificar: no hay nada falso que marcar aquí."; break;
                case FieldKind.Documento:
                    if (!DocumentValid(f.documento)) { r.status = FieldStatus.Falso; r.razon = "El dígito verificador no coincide: el documento no es válido."; }
                    else r.razon = "El documento cumple el formato y el dígito verificador."; break;
                case FieldKind.Domicilio:
                    if (!AddressVerified(f.domicilio, d)) { r.status = FieldStatus.Falso; r.razon = "Ese domicilio no existe en el registro de direcciones verificadas."; }
                    else r.razon = "El domicilio existe en el registro."; break;
                case FieldKind.Telefono:
                    if (!PhoneValid(f.telefono)) { r.status = FieldStatus.Falso; r.razon = "El teléfono es un patrón repetido: no es un número real."; }
                    else r.razon = "El teléfono tiene un formato normal."; break;
                case FieldKind.Ocupacion:
                    if (f.depositoUsd > 0 && f.ingresoMensualUsd >= 20 * f.depositoUsd)
                    { r.status = FieldStatus.Sospechoso; r.razon = "Perfil incoherente: declara US$" + f.ingresoMensualUsd.ToString("N0") + " al mes y abre con solo US$" + f.depositoUsd + "."; }
                    else r.razon = "El ingreso declarado es coherente con un depósito pequeño."; break;
                case FieldKind.Deposito:
                    if (SameDepositCount(f, d) >= 4)
                    { r.status = FieldStatus.Sospechoso; r.razon = "Las 5 cuentas abren con exactamente US$" + f.depositoUsd + ": patrón de cuentas creadas en serie."; }
                    else r.razon = "Un depósito inicial normal."; break;
                case FieldKind.Carta:
                    if (f.cartaDevuelta) { r.status = FieldStatus.Falso; r.razon = "La carta de bienvenida volvió devuelta: nadie vive en ese domicilio."; }
                    else r.razon = "La carta de bienvenida fue entregada."; break;
            }
            return r;
        }

        static int SameDepositCount(FichaData f, ScenarioData d)
        {
            int n = 0;
            if (!ReferenceEquals(d, null) && d.fichas != null) foreach (var o in d.fichas) if (o.depositoUsd == f.depositoUsd) n++;
            return n;
        }

        public static string ValueOf(FichaData f, FieldKind k)
        {
            switch (k)
            {
                case FieldKind.Titular: return f.titular;
                case FieldKind.Documento: return f.documento;
                case FieldKind.Domicilio: return f.domicilio;
                case FieldKind.Telefono: return f.telefono;
                case FieldKind.Ocupacion: return f.ocupacion + "  (ingreso declarado US$" + f.ingresoMensualUsd.ToString("N0") + "/mes)";
                case FieldKind.Deposito: return "US$ " + f.depositoUsd.ToString("N2");
                default: return f.cartaDevuelta ? "DEVUELTA por el correo" : "Entregada";
            }
        }

        public static string LabelOf(FieldKind k)
        {
            switch (k)
            {
                case FieldKind.Titular: return "Titular";
                case FieldKind.Documento: return "Documento";
                case FieldKind.Domicilio: return "Domicilio";
                case FieldKind.Telefono: return "Teléfono";
                case FieldKind.Ocupacion: return "Ocupación";
                case FieldKind.Deposito: return "Depósito inicial";
                default: return "Carta de bienvenida";
            }
        }

        // ---------- estado de las marcas del jugador ----------
        static string Key(FichaData f, FieldKind k) { return f.id + ":" + k; }
        public bool IsFlagged(FichaData f, FieldKind k) { return flagged.Contains(Key(f, k)); }
        public int FakeFound { get { return correctFlags; } }

        /// <summary>El jugador marca un dato. Devuelve el resultado para mostrarlo al instante.</summary>
        public FieldCheck Flag(FichaData f, FieldKind k)
        {
            var gm = GameManager.I;
            var res = Check(f, k, gm != null ? gm.data : null);
            if (!flagged.Add(Key(f, k))) return res;
            if (res.Flaggable)
            {
                correctFlags++;
                if (gm != null) gm.RegisterEvidence("FICHA_" + f.id);
                AudioManager.Play("stamp");
            }
            else { wrongFlags++; AudioManager.Play("wrong"); }
            return res;
        }

        /// <summary>Modo defensa: el control KYC revisa todo automáticamente.</summary>
        public int AutoVerify(ScenarioData d)
        {
            int n = 0;
            foreach (var f in d.fichas) foreach (var k in Kinds) if (Check(f, k, d).Flaggable) { flagged.Add(Key(f, k)); n++; }
            return n;
        }

        public int FlagsInFicha(FichaData f)
        {
            int n = 0; foreach (var k in Kinds) if (IsFlagged(f, k)) n++; return n;
        }
    }
}
