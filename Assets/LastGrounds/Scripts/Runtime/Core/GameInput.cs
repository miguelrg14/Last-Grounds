using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LastGrounds
{
    /// <summary>
    /// Thin input layer. Works with the Input System package (keyboard + gamepad)
    /// and falls back to the legacy Input Manager when that is the active backend.
    /// </summary>
    public static class GameInput
    {
#if ENABLE_INPUT_SYSTEM
        static bool Held(Key key)
        {
            Keyboard kb = Keyboard.current;
            return kb != null && kb[key].isPressed;
        }

        static bool Down(Key key)
        {
            Keyboard kb = Keyboard.current;
            return kb != null && kb[key].wasPressedThisFrame;
        }

        static Gamepad Pad { get { return Gamepad.current; } }
#endif

        public static float Horizontal()
        {
            float h = 0f;
#if ENABLE_INPUT_SYSTEM
            if (Held(Key.LeftArrow) || Held(Key.A)) h -= 1f;
            if (Held(Key.RightArrow) || Held(Key.D)) h += 1f;
            Gamepad pad = Pad;
            if (pad != null)
            {
                float stick = pad.leftStick.x.ReadValue() + pad.dpad.x.ReadValue();
                if (Mathf.Abs(stick) > 0.3f) h = Mathf.Sign(stick);
            }
#else
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) h -= 1f;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) h += 1f;
#endif
            return Mathf.Clamp(h, -1f, 1f);
        }

        public static bool FirePressed()
        {
#if ENABLE_INPUT_SYSTEM
            Gamepad pad = Pad;
            return Down(Key.Space) || (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame));
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }

        public static bool FireHeld()
        {
#if ENABLE_INPUT_SYSTEM
            Gamepad pad = Pad;
            return Held(Key.Space) || (pad != null && (pad.buttonSouth.isPressed || pad.rightTrigger.isPressed));
#else
            return Input.GetKey(KeyCode.Space);
#endif
        }

        /// <summary>E, right shoulder, or Ctrl (Ctrl was the weapon key in the original).</summary>
        public static bool NextWeapon()
        {
#if ENABLE_INPUT_SYSTEM
            Gamepad pad = Pad;
            return Down(Key.E) || Down(Key.LeftCtrl) || Down(Key.RightCtrl) || (pad != null && pad.rightShoulder.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl);
#endif
        }

        public static bool PreviousWeapon()
        {
#if ENABLE_INPUT_SYSTEM
            Gamepad pad = Pad;
            return Down(Key.Q) || (pad != null && pad.leftShoulder.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Q);
#endif
        }

        /// <summary>Returns 0..2 when a number key 1..3 was pressed this frame, otherwise -1.</summary>
        public static int WeaponSlot()
        {
#if ENABLE_INPUT_SYSTEM
            if (Down(Key.Digit1) || Down(Key.Numpad1)) return 0;
            if (Down(Key.Digit2) || Down(Key.Numpad2)) return 1;
            if (Down(Key.Digit3) || Down(Key.Numpad3)) return 2;
#else
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) return 0;
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) return 1;
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) return 2;
#endif
            return -1;
        }

        /// <summary>Esc / Start: pause in game, go back in menus.</summary>
        public static bool BackPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Gamepad pad = Pad;
            return Down(Key.Escape) || (pad != null && (pad.startButton.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame));
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        public static bool PausePressed()
        {
#if ENABLE_INPUT_SYSTEM
            Gamepad pad = Pad;
            return Down(Key.Escape) || Down(Key.P) || (pad != null && pad.startButton.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
#endif
        }

        public static bool SubmitPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Down(Key.Enter) || Down(Key.NumpadEnter);
#else
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
        }

        public static bool ClearPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Down(Key.C);
#else
            return Input.GetKeyDown(KeyCode.C);
#endif
        }

        public static bool NavigatePressed()
        {
#if ENABLE_INPUT_SYSTEM
            Gamepad pad = Pad;
            return Down(Key.UpArrow) || Down(Key.DownArrow) || Down(Key.LeftArrow) || Down(Key.RightArrow)
                || Down(Key.W) || Down(Key.S) || Down(Key.Tab)
                || (pad != null && (pad.dpad.up.wasPressedThisFrame || pad.dpad.down.wasPressedThisFrame));
#else
            return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow)
                || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow)
                || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.Tab);
#endif
        }

        public static bool ScreenshotPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Down(Key.F12);
#else
            return Input.GetKeyDown(KeyCode.F12);
#endif
        }

        public static bool ToggleUiPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Down(Key.F10);
#else
            return Input.GetKeyDown(KeyCode.F10);
#endif
        }

        public static bool ToggleInvinciblePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Down(Key.F9);
#else
            return Input.GetKeyDown(KeyCode.F9);
#endif
        }

        public static bool AnyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;
            Gamepad pad = Pad;
            return (kb != null && kb.anyKey.wasPressedThisFrame)
                || (mouse != null && mouse.leftButton.wasPressedThisFrame)
                || (pad != null && (pad.startButton.wasPressedThisFrame || pad.buttonSouth.wasPressedThisFrame));
#else
            return Input.anyKeyDown;
#endif
        }
    }
}
