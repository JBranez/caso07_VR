# Fidelidad histórica y límites de la reconstrucción

## Fuente primaria y cautela

La referencia principal para la cronología es la demanda civil de Bangladesh Bank contra Rizal Commercial Banking Corporation (RCBC), presentada el 31 de enero de 2019. Es un escrito de parte: sus descripciones se expresan en el juego como **alegaciones de la demanda**, no como constataciones judiciales automáticas. El documento público está disponible en el National Security Archive: https://nsarchive.gwu.edu/sites/default/files/documents/5736655/National-Security-Archive-Bangladesh-Bank-vs.pdf

## Cronología reproducida

| Fecha / hora local indicada en el expediente | Descripción que utiliza la simulación | Grado de representación |
|---|---|---|
| 15 mayo 2015 | Se abren cinco cuentas con depósito inicial de US$500 cada una; la demanda las describe como cuentas con identidades ficticias. | Basado en la demanda. Los nombres e identificadores del simulador son inventados. |
| Junio–agosto 2015 | Cuatro de cinco cartas de bienvenida son devueltas como no entregables. | Basado en la demanda. El juego asigna las cuatro devoluciones a cuatro fichas inventadas solo para permitir el ejercicio KYC; la demanda no valida esa asignación de nombres ficticios. |
| 5 febrero 2016 | Cerca de US$81 millones llegan a cuatro de las cinco cuentas. | Basado en la demanda a escala agregada. Los saldos exactos asignados por el juego a cada ficha son didácticos. |
| 5 febrero 2016, antes de las 19:00 | El centro de efectivo de Makati entrega 20 millones de pesos a la sucursal RCBC Jupiter. El efectivo queda en una caja de cartón para que William So Go la recoja. | La demanda ubica la entrega antes de las 19:00. La hora 15:30 que usa el reloj de juego es **solo ilustrativa**, no un dato histórico confirmado. |
| 4–9 febrero 2016 | La demanda alega que el CCTV de la sucursal Jupiter no estaba operativo. | Alegación de la demanda; se reproduce como fallo de control. |
| 7 febrero 2016, 06:54 | El registro del servidor SWIFT aparece desconectado. | Basado en el registro descrito en la demanda. |
| 8 febrero 2016, 17:05–17:12 (hora de Bangladesh) | Bangladesh Bank envía mensajes/órdenes de solicitud de detener pagos (STOPPAY). | Basado en la cronología alegada. La hora está rotulada explícitamente como hora de Bangladesh y no se compara visualmente como si fuera la hora de Manila. |
| 9 febrero 2016, 09:11 | La sesión del servidor SWIFT vuelve a iniciar. | Basado en la demanda. |
| 9 febrero 2016, 10:24–11:37 | Se describen cinco transferencias por un total exacto de US$58.158.641,63. | Basado en la demanda. El juego conserva horas, montos y el total; sus saldos internos trabajan con dólares enteros; el acumulado se redondea al dólar más cercano en cada hito y el débito de ese hito es la diferencia acumulada. Las filas y el total documental conservan los centavos exactos. |

## Las cinco transferencias (no un único retiro)

| Hora | Monto | Destino descrito |
|---|---:|---|
| 10:24 | US$15.215.977,26 | Philrem |
| 11:19 | US$19.951.502,13 | Go-Centurytex |
| 11:34 | US$9.769.124,15 | Go-Centurytex y Philrem |
| 11:35 | US$7.236.154,62 | Go-Centurytex |
| 11:37 | US$5.985.883,47 | Go-Centurytex |
| **Total** | **US$58.158.641,63** | **Cinco operaciones** |

La primera orden se registra a las 10:24; no se debe afirmar que los US$58,16 millones salieron en un solo retiro a esa hora. El juego ahora presenta cinco filas en la bitácora, anima la primera orden al abrir la bóveda y actualiza los saldos por tramos. Para no inventar una distribución histórica por titular, la aplicación reparte los débitos proporcionalmente entre los cuatro saldos del simulador y lo identifica como una abstracción didáctica.

## Elementos deliberadamente ficticios o simplificados

- BANCO ANDINO, Banco Pacífico Insular, Banco Corresponsal Internacional y Agencia Las Palmeras son entidades inventadas.
- Marta Vidal, los cinco titulares, empleados, documentos, domicilios y teléfonos del juego son inventados. Ningún personaje debe describirse como representación literal de una persona investigada o condenada.
- La correspondencia entre cuatro cartas devueltas y cuatro fichas concretas es una asignación del ejercicio, no una identificación probada por la demanda.
- El reparto de US$81 millones entre las cuentas es ilustrativo. La cifra agregada y el número de cuentas receptoras son los datos que se busca enseñar.
- El reloj de 15:30 para la entrega de efectivo, la ruta del mensajero y las animaciones son recursos escénicos. La fuente indica que el efectivo se entregó antes de las 19:00 y quedó esperando recogida; no fija el minuto representado. En DEFENSA, el control puede retener la caja después de su llegada, pero no cambia el hecho de que la entrega física ocurrió.
- Las reglas de documento, teléfono, domicilio, perfil y patrón de depósitos de `KycChecker` son reglas del simulador. No constituyen un estándar legal KYC/AML de Filipinas, Perú ni otro país.
- La simulación simplifica la logística internacional y no convierte en prueba los argumentos de ninguna parte litigante.

## Cambios hechos durante esta revisión

1. Se sustituyó la referencia imprecisa a un retiro de US$58 millones a las 10:24 por una secuencia de cinco transferencias con horas, montos y destinos.
2. Se agregó la evidencia `SWIFT_OFFLINE` y una pestaña específica para relacionar desconexión, STOPPAY, reconexión y transferencias.
3. Se corrigió el estado de la caja: la entrega física siempre se representa como llegada a la sucursal; en DEFENSA puede quedar retenida allí y no liberarse para recogida. No se representa como una entrega que desaparece por arte de magia.
4. Se separó al personaje ficticio Marta Vidal de cualquier persona real y se eliminó la afirmación no sustentada de que ella fue condenada.
5. Se aclara que las medidas legales posteriores de Filipinas son posteriores a febrero de 2016 y no se muestran como controles que ya existían durante el evento.
