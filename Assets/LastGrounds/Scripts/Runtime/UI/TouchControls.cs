using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LastGrounds
{
    /// <summary>
    /// On-screen controls for phones, under the game view:
    ///  - Move zone (left): drag along it and the player walks to the matching spot of the field.
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
        [SerializeField] RectTransform moveZone;
        [SerializeField] RectTransform moveKnob;
        [SerializeField] RectTransform fireButton;
        [SerializeField] Image fireImage;
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
        int moveFinger = -1;
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
                player.SetTouchInput(false, 0f, false);
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
                moveFinger = -1;
                fireFinger = -1;
                if (player != null) player.SetTouchInput(false, 0f, false);
            }
        }

        void Update()
        {
            if (!GameInput.TouchActive || player == null) return;
            LayoutPad();
            GameInput.GetTouches(touches);

            // A finger keeps its role (move or fire) from where it first touched until it lifts.
            TouchPoint? move = Find(moveFinger);
            TouchPoint? fire = Find(fireFinger);
            if (move == null) moveFinger = -1;
            if (fire == null) fireFinger = -1;

            for (int i = 0; i < touches.Count; i++)
            {
                TouchPoint t = touches[i];
                if (!t.began || t.id == moveFinger || t.id == fireFinger) continue;
                if (moveFinger < 0 && Contains(moveZone, t.position))
                {
                    moveFinger = t.id;
                    move = t;
                }
                else if (fireFinger < 0 && Contains(fireButton, t.position))
                {
                    fireFinger = t.id;
                    fire = t;
                }
            }

            bool firing = fire != null;
            if (fireImage != null) fireImage.color = firing ? firePressed : fireIdle;

            bool playing = game == null || game.IsPlaying;
            if (!playing)
            {
                player.SetTouchInput(false, 0f, false);
                return;
            }

            bool moving = move != null;
            float targetX = 0f;
            if (moving)
            {
                float t = ZoneFraction(move.Value.position);
                targetX = Mathf.Lerp(PlayField.LaneX(0), PlayField.LaneX(PlayField.LaneCount - 1), t);
                PlaceKnob(t);
            }
            player.SetTouchInput(moving, targetX, firing);
        }

        TouchPoint? Find(int id)
        {
            if (id < 0) return null;
            for (int i = 0; i < touches.Count; i++)
                if (touches[i].id == id) return touches[i];
            return null;
        }

        /// <summary>0 at the left end of the move zone, 1 at the right end.</summary>
        float ZoneFraction(Vector2 screenPosition)
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(moveZone, screenPosition, null, out local);
            Rect r = moveZone.rect;
            return Mathf.Clamp01((local.x - r.xMin) / Mathf.Max(1f, r.width));
        }

        void PlaceKnob(float fraction)
        {
            if (moveKnob == null) return;
            Vector2 min = moveKnob.anchorMin;
            Vector2 max = moveKnob.anchorMax;
            float half = (max.x - min.x) * 0.5f;
            moveKnob.anchorMin = new Vector2(fraction - half, min.y);
            moveKnob.anchorMax = new Vector2(fraction + half, max.y);
        }

        static bool Contains(RectTransform rect, Vector2 screenPosition)
        {
            return rect != null && rect.gameObject.activeInHierarchy
                && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, null);
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
                if (weaponButtons[i] != null) weaponButtons[i].color = i == index ? weaponSelected : weaponIdle;
        }
    }
}
