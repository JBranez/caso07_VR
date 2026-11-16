# Guía de juego · Las Palmeras VR

## Objetivo

Investigar las debilidades de una agencia bancaria en dos rutas jugables: reconstruir el incidente (ATAQUE) o intentar evitarlo con controles (DEFENSA). El escenario no es un simulador de banca real: las entidades, personas y reglas KYC internas son ficticias.

## Zonas

1. **Ventanilla:** revisa las cinco fichas de apertura. Marca documentos, direcciones, teléfonos, cartas y perfiles que no superen las comprobaciones del simulador.
2. **Sala de seguridad:** recorre las seis fechas del monitor CCTV y registra los días sin señal.
3. **Bóveda / terminal:** compara las cuentas inactivas, los saldos y las cinco operaciones registradas. La nueva pestaña **SWIFT / órdenes** permite reconstruir el estado de la sesión, los mensajes STOPPAY y la hora de reconexión.
4. **Oficina:** inspecciona la caja de cartón y registra la entrega de efectivo. En DEFENSA, activa la retención para que la caja quede bajo custodia después de llegar a la sucursal. La hora visual del mensajero se declara ilustrativa.
5. **Panel de alertas:** en ATAQUE documenta los controles ausentes. En DEFENSA activa las medidas cuando hayas satisfecho sus prerrequisitos.

## Secuencia histórica del juego

- 15/05/2015: apertura de cinco cuentas con US$500 iniciales.
- Jun–ago 2015: cuatro cartas de bienvenida devueltas.
- 05/02/2016: cerca de US$81 M llegan a cuatro cuentas; se representa una entrega de 20 M de pesos antes de las 19:00 (la animación de 15:30 es ilustrativa).
- 04–09/02/2016: periodo de CCTV sin funcionamiento según la demanda.
- 07/02/2016 06:54: desconexión SWIFT alegada.
- 08/02/2016 17:05–17:12, hora Bangladesh: mensajes STOPPAY.
- 09/02/2016 09:11: reconexión SWIFT.
- 09/02/2016 10:24, 11:19, 11:34, 11:35 y 11:37: cinco transferencias por US$58.158.641,63.

## Modo DEFENSA: decisiones

- **KYC:** desbloquéalo al detectar al menos tres datos incorrectos o sospechosos en las fichas.
- **Cuenta inactiva:** revisa el terminal para desbloquear la regla. Compara los presets A/B/C: el umbral A detecta pero genera falsas alarmas; B es el compromiso diseñado para la simulación; C es demasiado laxo para el evento.
- **Retención y CCTV:** revisa el monitor de seguridad en las fechas requeridas para desbloquear el control. Una alerta sin retención no equivale a detener el dinero.

## Controles

| PC | Cardboard |
|---|---|
| WASD/flechas: moverse; Shift: correr; ratón: mirar | Gira la cabeza |
| E o clic: interactuar | Mira el elemento hasta completar el anillo o toca la pantalla |
| 1–5: teletransporte; H: pista; Tab: bitácora; Esc: pausa | Mira los anillos verdes del suelo; esfera azul = pista |
| F2: simular mirada VR | El modo VR se activa en Android/Cardboard |

## Audio y accesibilidad

El HUD presenta objetivos, reloj, avisos y subtítulos. La narración se puede activar/desactivar en **Menú → Audio y accesibilidad**. Volumen, narración y subtítulos se ajustan por separado. Android usa el motor TextToSpeech del sistema; Windows utiliza SAPI si está disponible. La voz y el idioma instalados pueden variar. El proyecto crea ambientes procedurales por zona (vestíbulo, ventanilla, bóveda, seguridad y oficina) y efectos de interacción.

## Resultado

El panel final resume controles utilizados, pistas, tiempo, dinero simulado que entró, se retuvo o se transfirió y lecciones aprendidas. Las cifras de las cuentas son un modelo del juego, no un ledger real del banco.
