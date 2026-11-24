using UnityEngine;
using UnityEngine.UI;

namespace LastGrounds
{
    /// <summary>
    /// Adapts a UI element to touch play: hide keyboard hints, show touch-only buttons, or
    /// swap a text ("PRESS ANY KEY" becomes "TAP TO START"). Follows <see cref="GameInput.TouchActive"/>.
    /// </summary>
    public class PlatformVariant : MonoBehaviour
    {
        public enum Mode { HideOnTouch, ShowOnTouch, SwapText }

        [SerializeField] Mode mode = Mode.HideOnTouch;
        [SerializeField] string touchText = "";

        Text label;
        string keyboardText;

        void Awake()
        {
            label = GetComponent<Text>();
            if (label != null) keyboardText = label.text;
            GameInput.TouchModeChanged += Apply;
            Apply();
        }

        void OnDestroy()
        {
            GameInput.TouchModeChanged -= Apply;
        }

        void Apply()
        {
            bool touch = GameInput.TouchActive;
            switch (mode)
            {
                case Mode.HideOnTouch:
                    gameObject.SetActive(!touch);
                    break;
                case Mode.ShowOnTouch:
                    gameObject.SetActive(touch);
                    break;
                case Mode.SwapText:
                    if (label != null) label.text = touch ? touchText : keyboardText;
                    break;
            }
        }
    }
}
