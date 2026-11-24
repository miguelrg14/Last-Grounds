using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LastGrounds
{
    /// <summary>
    /// On-screen controls for phones, under the game view:
    ///  - &lt; and &gt; buttons (left): move while held; the thumb can slide from one to the other.
    ///  - FIRE button (right): shoots while held.
    ///  - Weapon buttons 1-2-3 and pause.
    /// Moving and shooting are independent, so both thumbs can be used at once.
    /// Shown only while <see cref="GameInput.TouchActive"/>; keyboard and gamepad keep working.
    /// </summary>
    public class TouchControls : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] RectTransform pad;
        [SerializeField] Canvas canvas;
        [SerializeField] Image leftButton;
        [SerializeField] Image rightButton;
        [SerializeField] Image fireButton;
        [SerializeField] Color moveIdle = new Color(0.2f, 0.2f, 0.2f, 0.85f);
        [SerializeField] Color movePressed = new Color(0.83f, 0.94f, 0f, 0.95f);
        [SerializeField] Color fireIdle = new Color(0.55f, 0.08f, 0.08f, 0.9f);
        [SerializeField] Color firePressed = new Color(0.95f, 0.2f, 0.15f, 1f);
        [SerializeField] Image[] weaponButtons = new Image[0];
        [SerializeField] Color weaponIdle = new Color(0.15f, 0.15f, 0.15f, 0.85f);
        [SerializeField] Color weaponSelected = new Color(0.83f, 0.94f, 0f, 0.95f);
        [SerializeField] PlayerController player;
        [SerializeField] GameManager game;
        [SerializeField] PixelViewport viewport;
        [Tooltip("Part of the screen kept for the controls below the game view.")]
        [SerializeField, Range(0.1f, 0.4f)] float padFraction = 0.24f;

        readonly List<TouchPoint> touches = new List<TouchPoint>();
        int fireFinger = -1;

        void OnEnable()
        {
            GameInput.TouchModeChanged += ApplyMode;
            if (player != null) player.WeaponChanged += OnWeaponChanged;
            ApplyMode();
        }

        void OnDisable()
        {
            GameInput.TouchModeChanged -= ApplyMode;
            if (player != null)
            {
                player.WeaponChanged -= OnWeaponChanged;
                player.SetTouchInput(0f, false);
            }
        }

        void Start()
        {
            if (player != null && player.CurrentWeapon != null) OnWeaponChanged(player.CurrentWeapon, player.WeaponIndex);
        }

        void ApplyMode()
        {
            bool on = GameInput.TouchActive;
            if (root != null) root.SetActive(on);
            if (viewport != null) viewport.SetLayout(on ? 1f : 0.5f, on ? padFraction : 0f);
            if (!on)
            {
                fireFinger = -1;
                if (player != null) player.SetTouchInput(0f, false);
            }
        }

        void Update()
        {
            if (!GameInput.TouchActive || player == null) return;
            LayoutPad();
            GameInput.GetTouches(touches);

            // FIRE keeps the finger that pressed it until it lifts, even if it slides off.
            bool fireStillDown = false;
            for (int i = 0; i < touches.Count; i++)
                if (touches[i].id == fireFinger) fireStillDown = true;
            if (!fireStillDown) fireFinger = -1;

            bool left = false;
            bool right = false;
            for (int i = 0; i < touches.Count; i++)
            {
                TouchPoint t = touches[i];
                if (t.id == fireFinger) continue;
                if (fireFinger < 0 && t.began && Contains(fireButton, t.position))
                {
                    fireFinger = t.id;
                    continue;
                }
                // Arrows react to where the finger is now, so the thumb can slide between them.
                if (Contains(leftButton, t.position)) left = true;
                else if (Contains(rightButton, t.position)) right = true;
            }

            bool firing = fireFinger >= 0;
            Tint(fireButton, firing ? firePressed : fireIdle);
            Tint(leftButton, left ? movePressed : moveIdle);
            Tint(rightButton, right ? movePressed : moveIdle);

            bool playing = game == null || game.IsPlaying;
            float horizontal = left == right ? 0f : (left ? -1f : 1f);
            player.SetTouchInput(playing ? horizontal : 0f, playing && firing);
        }

        static void Tint(Image image, Color color)
        {
            if (image != null) image.color = color;
        }

        static bool Contains(Image image, Vector2 screenPosition)
        {
            return image != null && image.gameObject.activeInHierarchy
                && RectTransformUtility.RectangleContainsScreenPoint(image.rectTransform, screenPosition, null);
        }

        /// <summary>The pad fills whatever is left below the game view.</summary>
        void LayoutPad()
        {
            if (pad == null || canvas == null || viewport == null) return;
            float freePixels = Mathf.Max(viewport.PixelRect.yMin, Screen.height * padFraction * 0.5f);
            float scale = canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
            Vector2 size = pad.sizeDelta;
            size.y = freePixels / scale;
            pad.sizeDelta = size;
        }

        public void SelectWeapon(int index)
        {
            if (player != null) player.SelectWeapon(index);
        }

        public void Pause()
        {
            if (game != null) game.Pause();
        }

        void OnWeaponChanged(WeaponData weapon, int index)
        {
            for (int i = 0; i < weaponButtons.Length; i++)
                Tint(weaponButtons[i], i == index ? weaponSelected : weaponIdle);
        }
    }
}
