using System;
using System.Collections.Generic;
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
    /// <summary>A finger on the screen, in screen pixels.</summary>
    public struct TouchPoint
    {
        public int id;
        public Vector2 position;
        public bool began;
    }

    public static class GameInput
    {
        /// <summary>
        /// True on phones/tablets, or after the screen is touched; turns off again when the
        /// keyboard is used on a non-mobile device. Touch controls and texts follow it.
        /// </summary>
        public static bool TouchActive { get; private set; }
        public static event Action TouchModeChanged;

        public static void SetTouchActive(bool active)
        {
            if (TouchActive == active) return;
            TouchActive = active;
            if (TouchModeChanged != null) TouchModeChanged();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            TouchActive = false;
            TouchModeChanged = null;
        }

        /// <summary>Fills <paramref name="results"/> with the fingers currently on the screen.</summary>
        public static void GetTouches(List<TouchPoint> results)
        {
            results.Clear();
#if ENABLE_INPUT_SYSTEM
            Touchscreen screen = Touchscreen.current;
            if (screen == null) return;
            foreach (UnityEngine.InputSystem.Controls.TouchControl touch in screen.touches)
            {
                if (!touch.isInProgress) continue;
                results.Add(new TouchPoint
                {
                    id = touch.touchId.ReadValue(),
                    position = touch.position.ReadValue(),
                    began = touch.press.wasPressedThisFrame,
                });
            }
#else
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) continue;
                results.Add(new TouchPoint { id = touch.fingerId, position = touch.position, began = touch.phase == TouchPhase.Began });
            }
#endif
        }

        public static bool TouchBegan()
        {
#if ENABLE_INPUT_SYSTEM
            Touchscreen screen = Touchscreen.current;
            return screen != null && screen.primaryTouch.press.wasPressedThisFrame;
#else
            for (int i = 0; i < Input.touchCount; i++)
                if (Input.GetTouch(i).phase == TouchPhase.Began) return true;
            return false;
#endif
        }

        /// <summary>Keyboard gameplay keys (used to leave touch mode on desktops).</summary>
        public static bool KeyboardUsed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && kb.anyKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow)
                || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.Escape);
#endif
        }

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
                || TouchBegan()
                || (pad != null && (pad.startButton.wasPressedThisFrame || pad.buttonSouth.wasPressedThisFrame));
#else
            return Input.anyKeyDown || TouchBegan();
#endif
        }
    }
}
