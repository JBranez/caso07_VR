# Arquitectura y cumplimiento del escenario · Unity 6.5

## Contrato de clases

| Subsistema | Clases / responsabilidad |
|---|---|
| Ciclo de juego | `GameManager`, `Bootstrap`, `GameInput`, `ScenarioData`, `StoryDirector` y `SimClock` |
| Mundo | `AgencyBuilder`, `Npc`, `Mats`, `Procedural`, `AssistantOrb`, `VaultDoor` |
| Interacción | `Interactable`, `EvidenceItem`, `FichaItem`, `DefenseControl`, `TeleportPoint`, `PortalRing`, `UiButton3D` |
| Mecánicas | `KycChecker`, `CctvFeed`, `DormantAlert`, `WithdrawalLog` |
| Jugador | `PlayerRig`, `GazePointer`, `VrMode`, `ScreenFader` |
| UI | `UIManager` (HUD, menú, terminal, pausa, resultado), `WorldPanel` (paneles 3D) |
| Audio | `AudioManager` y `Synth` (efectos/ambientes), `AudioGuide` (TTS y subtítulos) |
| Editor/build | `ProjectBootstrap`, `XRSetup`, `AndroidGradlePostprocess`, `BuildScripts` |

## Requisitos y ubicación

| Requisito | Implementación |
|---|---|
| Escena Agencia_VR y cuatro zonas | `Assets/Scenes/Agencia_VR.unity` es la escena serializada incluida en el proyecto; `GameBootstrap` inicia la simulación y `AgencyBuilder` construye ventanilla, seguridad, bóveda y oficina al entrar en Play |
| Cinco fichas y KYC | `FichaItem` + `KycChecker`; validación ficticia de documento, domicilio, teléfono, carta, perfil y patrón de depósitos |
| CCTV 4–9 febrero | `CctvFeed` y shader de ruido; seis fechas revisables |
| Cuentas y movimientos | `WithdrawalLog` + terminal; cuentas iniciales, cuatro receptoras y cinco registros de transferencia |
| SWIFT/STOPPAY | Pista `SWIFT_OFFLINE` y tercera pestaña del terminal; cronología de desconexión, órdenes y reconexión |
| Alertas y retención | `DormantAlert` ofrece tres presets; `StoryDirector.Reevaluate()` calcula si la regla se dispara y si los fondos quedan retenidos |
| Modo ATAQUE / DEFENSA | `StoryDirector.RunAttack/RunDefense` y controles de `GameManager` |
| HUD responsivo | `CanvasScaler`, `Screen.safeArea`, anchors, reajuste por resolución/aspecto |
| Paneles 3D adaptables | `WorldPanel.LateUpdate()` ajusta escala con distancia, FOV y aspecto de cámara |
| Instrucciones y audio | `AudioGuide` (Android TextToSpeech / Windows SAPI + subtítulos), `AudioManager` y ambientes procedurales por zona |
| Cardboard Android | Plugin embebido Google Cardboard 1.36.0 y XR Plug-in Management 4.6.1 |

## Flujo de información

```text
Input (teclado, ratón, mirada o toque)
    ↓
PlayerRig / GazePointer / Interactable
    ↓
FichaItem / EvidenceItem / DefenseControl / TeleportPoint
    ↓
GameManager (estado, evidencias y defensas)
    ↔ StoryDirector + SimClock
    ↔ KycChecker / CctvFeed / DormantAlert / WithdrawalLog
    ↓
UIManager + WorldPanel (HUD, terminal, controles y resultados)
    ↔ AudioManager + AudioGuide
```

## Observaciones

- El mundo y los objetos 3D de la agencia se construyen por código; el ZIP original no contenía la escena principal del juego en `Assets/Scenes` ni prefabs propios de la agencia. Sí incluye escenas/prefabs de muestra que vienen dentro del paquete Cardboard.
- Los paneles holográficos usan botones con collider para la ruta de interacción por mirada/ratón; los controles de HUD son Canvas de pantalla.
- Las comprobaciones KYC son reglas pedagógicas inventadas y no equivalen a normativa de identificación ni a un motor AML bancario real.
- El proyecto es Cardboard-first. No contiene una integración OpenXR Standalone completa.
- La validación de compilación y VR depende del Editor y dispositivo. Consultar `Verificacion.md` y `Auditoria_Unity65.md`.
