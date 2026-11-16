// ANDES-SIM · Escenario 07 · Agencia Las Palmeras
// ScenarioData (ScriptableObject): textos, fechas y cifras del escenario.
// Contrato común del caso: GameManager.data apunta a este asset.
using System;
using UnityEngine;

namespace LasPalmeras
{
    /// <summary>Modo de la simulación (contrato común de los 11 escenarios).</summary>
    public enum SimMode { Ataque, Defensa }

    [Serializable]
    public class FichaData
    {
        public string id;              // "01".."05"
        public string titular;         // ficticio
        public string documento;       // 8 dígitos + dígito verificador (regla del simulador, no real)
        public string domicilio;
        public string telefono;
        public string ocupacion;
        public int ingresoMensualUsd;  // ingreso declarado
        public int depositoUsd;        // depósito inicial
        public bool cartaDevuelta;     // carta de bienvenida devuelta por el correo
        public Color tinte = Color.white;   // color de ropa del "titular" holográfico
    }

    [Serializable] public class TimelineEntry { public string fecha; public string texto; }

    [Serializable]
    public class EvidenceDef
    {
        public string id;
        public string titulo;
        public string texto;
        public string vulnerabilidad;
    }

    [Serializable]
    public class DefenseDef
    {
        public string id;          // kyc | dormant | hold
        public string nombre;
        public string descripcion;
        public string vulnerabilidad;
        public string requisito;   // prerrequisito (qué revisar antes de activar)
    }

    [CreateAssetMenu(fileName = "ScenarioData_LasPalmeras", menuName = "ANDES-SIM/ScenarioData")]
    public class ScenarioData : ScriptableObject
    {
        [Header("Identificación")]
        public string escenario = "Escenario 07";
        public string titulo = "Agencia Las Palmeras";
        public string bancoReceptor = "Banco Pacífico Insular";
        public string bancoAtacado = "BANCO ANDINO";
        public string fechas = "15 may 2015 · 9 feb 2016";
        [TextArea(3, 8)] public string historia;

        [Header("Cifras históricas (verificadas)")]
        public int cuentas = 5;
        public int depositoInicialUsd = 500;
        public long recibidoUsd = 81_000_000;       // llegan el viernes 5 feb 2016
        public long retiradoUsd = 58_158_642;       // total exacto US$58.158.641,63 redondeado al dólar más cercano
        public long efectivoPesos = 20_000_000;     // caja de cartón, viernes por la tarde
        public string horaRetiro = "10:24–11:37";
        public string horaOrdenLeida = "09:11";
        public int cuentasQueRecibenFondos = 4;

        [Header("Contenido")]
        public string[] vulnerabilidades;
        public string[] defensas;
        public FichaData[] fichas;
        public string[] domiciliosVerificados;
        public TimelineEntry[] timeline;
        public EvidenceDef[] evidencias;
        public DefenseDef[] controles;
        public string[] epilogo;
        public string[] reglasEticas;
        public string[] creditos;
        public string[] fuentes;

        public EvidenceDef Evidence(string id)
        {
            if (evidencias != null) foreach (var e in evidencias) if (e.id == id) return e;
            return null;
        }
        public DefenseDef Control(string id)
        {
            if (controles != null) foreach (var c in controles) if (c.id == id) return c;
            return null;
        }

        /// <summary>Crea la versión por defecto con todos los datos del escenario 07.</summary>
        public static ScenarioData CreateDefault()
        {
            var d = CreateInstance<ScenarioData>();
            d.name = "ScenarioData_LasPalmeras";
            d.Fill();
            return d;
        }

        public void Fill()
        {
            historia =
                "El 15 de mayo de 2015 se abren cinco cuentas ficticias con US$500 cada una. " +
                "El 5 de febrero de 2016 llegan cerca de US$81 millones a cuatro cuentas; ese día se entrega " +
                "a la sucursal una caja con 20 millones de pesos. Las cámaras no graban del 4 al 9. " +
                "El 9 de febrero se ejecutan cinco transferencias por unos US$58,2 millones entre las 10:24 y las 11:37.";

            vulnerabilidades = new[]
            {
                "Verificación de identidad débil (KYC)",
                "Cuentas dormidas que reciben millones sin alerta",
                "Cámaras sin grabar y retiros sin retención"
            };
            defensas = new[]
            {
                "Verificación de identidad y domicilio",
                "Alerta por cuentas inactivas con montos grandes",
                "Retención de fondos y monitoreo de CCTV"
            };

            // Domicilios que SÍ existen en el registro del distrito (ficticios).
            domiciliosVerificados = new[]
            {
                "Calle Los Cedros 45, Dpto. 3",
                "Av. Las Gaviotas 1280",
                "Jr. Mercaderes 230, Of. 4",
                "Calle Las Orquídeas 112",
                "Av. Costa Verde 905, Piso 2"
            };

            // Cinco titulares ficticios creados para el ejercicio; todos los datos personales son inventados.
            // Documento: 8 dígitos + dígito verificador (regla propia del simulador, no es la real).
            fichas = new[]
            {
                new FichaData { id="01", titular="Eduardo Páez Linares",   documento=KycChecker.MakeDocument("45000000", false),
                                domicilio="Av. Los Imaginarios 000, Dpto. 0", telefono="999 999 999", ocupacion="Comerciante",
                                ingresoMensualUsd=900,   depositoUsd=500, cartaDevuelta=true,  tinte=new Color(0.35f,0.6f,0.9f) },
                new FichaData { id="02", titular="Luciano Santillán Vera", documento=KycChecker.MakeDocument("32147890", false),
                                domicilio="Calle Sin Nombre s/n", telefono="987 412 365", ocupacion="Comerciante",
                                ingresoMensualUsd=1100,  depositoUsd=500, cartaDevuelta=true,  tinte=new Color(0.9f,0.5f,0.55f) },
                new FichaData { id="03", titular="Hernán Cabrera Ocampo",  documento=KycChecker.MakeDocument("20458831", true),
                                domicilio="Pasaje Inexistente 77", telefono="000 000 000", ocupacion="Armador naviero",
                                ingresoMensualUsd=45000, depositoUsd=500, cartaDevuelta=true,  tinte=new Color(0.55f,0.85f,0.55f) },
                new FichaData { id="04", titular="Mauro Solano Quiroz",    documento=KycChecker.MakeDocument("28811230", false),
                                domicilio="Av. Fantasma 4040, Piso 9", telefono="954 118 702", ocupacion="Comerciante",
                                ingresoMensualUsd=800,   depositoUsd=500, cartaDevuelta=true,  tinte=new Color(0.95f,0.8f,0.4f) },
                new FichaData { id="05", titular="Ramiro Echeverría Dall", documento=KycChecker.MakeDocument("61903377", true),
                                domicilio="Calle Los Cedros 45, Dpto. 3", telefono="976 530 884", ocupacion="Comerciante",
                                ingresoMensualUsd=1000,  depositoUsd=500, cartaDevuelta=false, tinte=new Color(0.75f,0.6f,0.95f) }
            };

            timeline = new[]
            {
                new TimelineEntry { fecha="15 may 2015", texto="Se abren cinco cuentas en dólares con identidades ficticias y US$500 cada una. La demanda sostiene que eran cuentas ficticias." },
                new TimelineEntry { fecha="jun–ago 2015", texto="Cuatro de cinco cartas de bienvenida son devueltas como no entregables. La asignación de esas cartas a los titulares del simulador es ficticia." },
                new TimelineEntry { fecha="266 días", texto="Las cuentas permanecen inactivas antes de recibir fondos; el intervalo exacto se representa de forma simplificada en la simulación." },
                new TimelineEntry { fecha="Vie 5 feb 2016 · mañana", texto="Llegan alrededor de US$81 millones a cuatro de las cinco cuentas." },
                new TimelineEntry { fecha="Vie 5 feb 2016 · antes de 19:00", texto="Según la demanda, el centro de efectivo de Makati entrega 20 millones de pesos a la sucursal Jupiter. El efectivo queda en una caja de cartón para que William So Go la recoja; la hora exacta no se fija aquí." },
                new TimelineEntry { fecha="4–9 feb 2016", texto="La demanda alega que las cámaras de la sucursal Jupiter no funcionaban durante estos días." },
                new TimelineEntry { fecha="Dom 7 feb · 06:54", texto="Según la demanda, el servidor SWIFT de RCBC fue desconectado. Volvió a conectarse el martes 9 a las 09:11." },
                new TimelineEntry { fecha="Lun 8 feb · 17:05–17:12 (Bangladesh)", texto="Bangladesh Bank envía mensajes STOPPAY/solicitudes de detener pagos. La demanda alega que el servidor receptor aún no estaba conectado para procesarlos." },
                new TimelineEntry { fecha="Mar 9 feb · 09:11", texto="RCBC vuelve a iniciar sesión en el servidor SWIFT y los mensajes quedan disponibles para el personal." },
                new TimelineEntry { fecha="Mar 9 feb · 10:24–11:37", texto="Se registran cinco transferencias por un total aproximado de US$58,2 millones. La primera ocurre a las 10:24; la última, a las 11:37." }
            };

            evidencias = new[]
            {
                new EvidenceDef { id="FICHA_01", titulo="Ficha 01 revisada", vulnerabilidad="KYC débil", texto="La ficha 01 tiene documento, domicilio y teléfono que no resisten una verificación." },
                new EvidenceDef { id="FICHA_02", titulo="Ficha 02 revisada", vulnerabilidad="KYC débil", texto="La ficha 02 usa un documento con dígito verificador erróneo y un domicilio que no existe." },
                new EvidenceDef { id="FICHA_03", titulo="Ficha 03 revisada", vulnerabilidad="KYC débil", texto="La ficha 03 declara un ingreso de US$45.000 al mes y abre la cuenta con solo US$500." },
                new EvidenceDef { id="FICHA_04", titulo="Ficha 04 revisada", vulnerabilidad="KYC débil", texto="La ficha 04 tiene documento inválido, un domicilio que no existe y su carta de bienvenida volvió devuelta." },
                new EvidenceDef { id="FICHA_05", titulo="Ficha 05 revisada", vulnerabilidad="KYC débil", texto="La ficha 05 parece la más limpia, pero comparte el mismo depósito de US$500 que las otras cuatro." },
                new EvidenceDef { id="CUENTAS_DORMIDAS", titulo="Cuentas dormidas", vulnerabilidad="Sin alerta",
                    texto="Cuatro cuentas sin movimiento durante nueve meses reciben US$81 millones en un día. El sistema no emite ninguna alerta." },
                new EvidenceDef { id="CAJA_CARTON", titulo="La caja de cartón", vulnerabilidad="Sin retención",
                    texto="La demanda alega que el centro de efectivo de Makati entregó 20 millones de pesos a la sucursal Jupiter antes de las 19:00. El efectivo quedó en una caja de cartón para que un tercero la recogiera; la hora exacta no se fija en el simulador." },
                new EvidenceDef { id="CCTV_SIN_SENAL", titulo="Cámaras sin grabar", vulnerabilidad="CCTV apagado",
                    texto="Las cámaras de la agencia no graban nada del 4 al 9 de febrero. Nadie lo nota ni lo reporta." },
                new EvidenceDef { id="SWIFT_OFFLINE", titulo="Registro de sesión SWIFT", vulnerabilidad="Continuidad y escalamiento",
                    texto="La demanda alega que el servidor SWIFT de RCBC quedó desconectado el 7 de febrero a las 06:54 y volvió a conectarse el 9 a las 09:11. Bangladesh Bank había enviado solicitudes STOPPAY el 8 de febrero." },
                new EvidenceDef { id="RETIRO_1024", titulo="Cinco transferencias · 10:24–11:37", vulnerabilidad="Sin retención",
                    texto="La demanda describe cinco transferencias entre las 10:24 y las 11:37 del 9 de febrero, por un total de US$58.158.641,63. No fue un único retiro a las 10:24." },
                new EvidenceDef { id="PANEL_SIN_REGLAS", titulo="Panel de alertas vacío", vulnerabilidad="Sin controles",
                    texto="En la reconstrucción, el panel de alertas de la agencia no tiene ninguna regla activa para cuentas inactivas ni retenciones." }
            };

            controles = new[]
            {
                new DefenseDef { id="kyc",     nombre="Verificación de identidad y domicilio",
                    descripcion="Valida documento, domicilio, teléfono y perfil, y rechaza las cuentas que no pasan.",
                    vulnerabilidad="Verificación de identidad débil (KYC)",
                    requisito="Revisa las fichas de la ventanilla y encuentra al menos 3 datos falsos." },
                new DefenseDef { id="dormant", nombre="Alerta por cuentas inactivas con montos grandes",
                    descripcion="Avisa cuando una cuenta sin movimiento recibe un monto fuera de lo normal.",
                    vulnerabilidad="Cuentas dormidas que reciben millones sin alerta",
                    requisito="Revisa el registro de cuentas en el terminal de la bóveda." },
                new DefenseDef { id="hold",     nombre="Retención de fondos y monitoreo de CCTV",
                    descripcion="Retiene los fondos que disparan una alerta y obliga a las cámaras a grabar.",
                    vulnerabilidad="Cámaras sin grabar y retiros sin retención",
                    requisito="Revisa las cámaras de la sala de seguridad (4 al 9 de febrero)." }
            };

            epilogo = new[]
            {
                "De los US$81 millones que llegaron a Las Palmeras se recuperaron unos US$15 millones; siguen perdidos unos US$66 millones. (Los US$20 millones pagados a Sri Lanka sí se devolvieron.)",
                "El caso real tuvo investigaciones, procesos judiciales y sanciones posteriores. Marta Vidal y los demás empleados del BANCO ANDINO son personajes ficticios: no representan a personas reales ni se les atribuyen condenas.",
                "Como consecuencia posterior del caso, Filipinas amplió en 2017 el alcance de su legislación contra el lavado de activos para incluir casinos. Es una evolución normativa posterior, no un control que existiera en febrero de 2016.",
                "Pista para el cómplice interno del BANCO ANDINO: este escenario NO la contiene. Ocurre fuera de tu banco y ninguna ficha revela al cómplice."
            };

            reglasEticas = new[]
            {
                "Solo defensa y solo simulación: los ataques existen únicamente dentro de la app.",
                "Solo datos inventados: nombres, documentos, cuentas y teléfonos son ficticios.",
                "Entidades ficticias: BANCO ANDINO, Banco Pacífico Insular y Banco Corresponsal Internacional.",
                "Personas ficticias: ningún personaje se parece a docentes, autoridades ni a las personas reales del caso.",
                "La atribución del ataque a actores conocidos como Lazarus procede de evaluaciones de autoridades externas; el juego no presenta esa atribución como una conclusión judicial sobre cada personaje."
            };

            creditos = new[]
            {
                "ANDES-SIM · Caso BANCO ANDINO: La transferencia fantasma (Semana 10)",
                "Escenario 07 · Agencia Las Palmeras · VR (Cardboard) y PC",
                "Motor: Unity 6.5 (6000.5.2f1)",
                "Plugin: Google Cardboard XR Plugin 1.36.0 · Apache License 2.0 · Google LLC",
                "Paquetes de Unity: Input System, XR Plug-in Management, uGUI",
                "Arte 3D, texturas, shaders y audio: generados por código (procedurales). Sin recursos de terceros.",
                "Herramientas de IA usadas: Claude (Anthropic) para generar el código base. El equipo debe revisarlo y declararlo en su ficha.",
                "Fuentes: demanda civil del Bangladesh Bank contra RCBC (2019; alegaciones), materiales públicos de autoridades estadounidenses y legislación filipina posterior."
            };

            fuentes = new[]
            {
                "Denuncia penal del FBI contra Park Jin Hyok (2018)",
                "Demanda del Banco de Bangladesh contra RCBC (2019; alegaciones, no una sentencia)",
                "Departamento de Justicia de EE. UU. (material público sobre el caso, 2018)",
                "Ley filipina RA 10927 (2017; evolución normativa posterior)"
            };
        }
    }
}
