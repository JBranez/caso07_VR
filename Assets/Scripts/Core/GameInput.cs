// Entrada unificada: funciona con el Input System nuevo, con el antiguo o con ambos.
// (La guía de Cardboard pide "Input System Package (New)"; el plugin lo necesita.)
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LasPalmeras
{
    public static class GameInput
    {
        public static Vector2 Move;            // WASD / flechas
        public static Vector2 Look;            // delta de ratón o arrastre táctil (grados)
        public static bool Run;
        public static bool Interact;           // E / clic izquierdo / Intro
        public static bool Click;              // clic izquierdo
        public static bool Pause, Help, Hint, Journal, GazeToggle;
        public static bool TouchBegan;         // se tocó la pantalla (disparador Cardboard)
        public static bool TouchTap;           // toque corto soltado
        public static int NumberKey;           // 1..5 pulsada este frame (0 = ninguna)
        public static Vector2 PointerPos;      // posición del ratón/toque en píxeles
        public static bool PointerValid;
        static int lastFrame = -1;
        static float touchStartTime; static Vector2 touchStartPos; static bool touching;

        public static void Poll()
        {
            if (lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            Move = Vector2.zero; Look = Vector2.zero; Run = false;
            Interact = Click = Pause = Help = Hint = Journal = GazeToggle = false;
            TouchBegan = TouchTap = false; NumberKey = 0; PointerValid = false;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current; var ms = Mouse.current; var ts = Touchscreen.current;
            if (kb != null)
            {
                float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
                float y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
                Move = new Vector2(x, y);
                Run = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                Interact = kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;
                Pause = kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame;
                Help = kb.f1Key.wasPressedThisFrame;
                Hint = kb.hKey.wasPressedThisFrame;
                Journal = kb.tabKey.wasPressedThisFrame;
                GazeToggle = kb.f2Key.wasPressedThisFrame;
                for (int i = 0; i < 5; i++)
                    if (kb[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame) { NumberKey = i + 1; break; }
            }
            if (ms != null)
            {
                Look += ms.delta.ReadValue() * 0.08f;
                if (ms.leftButton.wasPressedThisFrame) { Click = true; Interact = true; }
                PointerPos = ms.position.ReadValue(); PointerValid = true;
            }
            if (ts != null)
            {
                var pt = ts.primaryTouch;
                if (pt.press.wasPressedThisFrame)
                {
                    TouchBegan = true; touching = true; touchStartTime = Time.unscaledTime; touchStartPos = pt.position.ReadValue();
                }
                if (pt.press.isPressed) { Look -= pt.delta.ReadValue() * 0.12f; PointerPos = pt.position.ReadValue(); PointerValid = true; }
                if (pt.press.wasReleasedThisFrame && touching)
                {
                    touching = false;
                    if (Time.unscaledTime - touchStartTime < 0.4f && (pt.position.ReadValue() - touchStartPos).magnitude < 24f) TouchTap = true;
                }
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            float lx = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            float ly = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
            Move = new Vector2(lx, ly);
            Run = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            Interact = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
            Click = Input.GetMouseButtonDown(0);
            Pause = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
            Help = Input.GetKeyDown(KeyCode.F1);
            Hint = Input.GetKeyDown(KeyCode.H);
            Journal = Input.GetKeyDown(KeyCode.Tab);
            GazeToggle = Input.GetKeyDown(KeyCode.F2);
            for (int i = 0; i < 5; i++) if (Input.GetKeyDown(KeyCode.Alpha1 + i)) { NumberKey = i + 1; break; }
            Look += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 2.2f;
            PointerPos = Input.mousePosition; PointerValid = true;
            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Began) { TouchBegan = true; touching = true; touchStartTime = Time.unscaledTime; touchStartPos = t.position; }
                if (t.phase == TouchPhase.Moved) Look -= t.deltaPosition * 0.12f;
                PointerPos = t.position; PointerValid = true;
                if (t.phase == TouchPhase.Ended && touching)
                {
                    touching = false;
                    if (Time.unscaledTime - touchStartTime < 0.4f && (t.position - touchStartPos).magnitude < 24f) TouchTap = true;
                }
            }
#endif
        }
    }
}
