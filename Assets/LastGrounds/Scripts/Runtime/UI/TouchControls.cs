using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LastGrounds
{
    /// <summary>
    /// On-screen controls for phones: a touch pad under the game (drag to move, hold to
    /// shoot), three weapon buttons and a pause button. The game view moves to the top of
    /// the screen so the pad uses the space a tall phone leaves free. Shown only while
    /// <see cref="GameInput.TouchActive"/>; keyboard and gamepad keep working regardless.
    /// </summary>
    public class TouchControls : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] RectTransform pad;
        [SerializeField] Canvas canvas;
        [Tooltip("Touches that start on these rects do not move the player (the buttons).")]
        [SerializeField] RectTransform[] blockers = new RectTransform[0];
        [SerializeField] Image[] weaponButtons = new Image[0];
        [SerializeField] Color weaponIdle = new Color(0.15f, 0.15f, 0.15f, 0.85f);
        [SerializeField] Color weaponSelected = new Color(0.83f, 0.94f, 0f, 0.95f);
        [SerializeField] PlayerController player;
        [SerializeField] GameManager game;
        [SerializeField] PixelViewport viewport;
        [Tooltip("Part of the screen kept for the pad below the game view.")]
        [SerializeField, Range(0.1f, 0.4f)] float padFraction = 0.22f;

        readonly List<TouchPoint> touches = new List<TouchPoint>();
        Camera gameCamera;
        int trackedFinger = -1;

        void OnEnable()
        {
            GameInput.TouchModeChanged += ApplyMode;
            if (player != null) player.WeaponChanged += OnWeaponChanged;
            if (viewport != null) gameCamera = viewport.GetComponent<Camera>();
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
                trackedFinger = -1;
                if (player != null) player.SetTouchInput(false, 0f, false);
            }
        }

        void Update()
        {
            if (!GameInput.TouchActive || player == null) return;
            LayoutPad();

            GameInput.GetTouches(touches);
            TouchPoint? tracked = null;
            for (int i = 0; i < touches.Count; i++)
            {
                if (touches[i].id == trackedFinger)
                {
                    tracked = touches[i];
                    break;
                }
            }

            if (tracked == null)
            {
                trackedFinger = -1;
                for (int i = 0; i < touches.Count; i++)
                {
                    if (!touches[i].began || IsOnBlocker(touches[i].position)) continue;
                    trackedFinger = touches[i].id;
                    tracked = touches[i];
                    break;
                }
            }

            bool playing = game == null || game.IsPlaying;
            if (tracked == null || !playing || gameCamera == null)
            {
                player.SetTouchInput(false, 0f, false);
                return;
            }

            // Screen X maps straight to world X (the camera never moves).
            Vector3 world = gameCamera.ScreenToWorldPoint(new Vector3(tracked.Value.position.x, gameCamera.pixelRect.center.y, 10f));
            player.SetTouchInput(true, world.x, true);
        }

        /// <summary>The pad fills whatever is left below the game view (at least padFraction of the screen).</summary>
        void LayoutPad()
        {
            if (pad == null || canvas == null || viewport == null) return;
            float freePixels = Mathf.Max(viewport.PixelRect.yMin, Screen.height * padFraction * 0.5f);
            float scale = canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
            Vector2 size = pad.sizeDelta;
            size.y = freePixels / scale;
            pad.sizeDelta = size;
        }

        bool IsOnBlocker(Vector2 screenPosition)
        {
            for (int i = 0; i < blockers.Length; i++)
            {
                if (blockers[i] != null && blockers[i].gameObject.activeInHierarchy
                    && RectTransformUtility.RectangleContainsScreenPoint(blockers[i], screenPosition, null))
                    return true;
            }
            return false;
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
