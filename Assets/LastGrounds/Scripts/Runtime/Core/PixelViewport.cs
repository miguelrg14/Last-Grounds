using UnityEngine;

namespace LastGrounds
{
    /// <summary>
    /// Keeps the camera at the original 112x176 aspect, letterboxed inside the screen's
    /// safe area (phone notches), and scaled by whole numbers when possible so the pixel
    /// art stays crisp. On phones the touch controls push the view to the top, leaving the
    /// space below for the buttons.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class PixelViewport : MonoBehaviour
    {
        [SerializeField] int referenceWidth = PlayField.RoomWidth;
        [SerializeField] int referenceHeight = PlayField.RoomHeight;
        [SerializeField] bool integerScaling = true;
        [Tooltip("0 = bottom, 0.5 = centred, 1 = top.")]
        [SerializeField, Range(0f, 1f)] float verticalAlignment = 0.5f;
        [Tooltip("Fraction of the safe area kept free at the bottom (used by the touch controls).")]
        [SerializeField, Range(0f, 0.5f)] float reservedBottom;

        Camera cam;
        int lastWidth = -1;
        int lastHeight = -1;
        Rect lastSafeArea;
        bool dirty = true;

        /// <summary>Screen pixels per art pixel.</summary>
        public float Scale { get; private set; }

        /// <summary>Screen-space rect the game is drawn in.</summary>
        public Rect PixelRect { get; private set; }

        void OnEnable()
        {
            cam = GetComponent<Camera>();
            dirty = true;
            Apply();
        }

        void Update()
        {
            if (dirty || Screen.width != lastWidth || Screen.height != lastHeight || Screen.safeArea != lastSafeArea) Apply();
        }

        /// <summary>Aligns the view and reserves space below it (touch controls).</summary>
        public void SetLayout(float alignment, float reservedBottomFraction)
        {
            verticalAlignment = Mathf.Clamp01(alignment);
            reservedBottom = Mathf.Clamp(reservedBottomFraction, 0f, 0.5f);
            dirty = true;
            Apply();
        }

        void Apply()
        {
            dirty = false;
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            lastSafeArea = Screen.safeArea;
            if (lastWidth <= 0 || lastHeight <= 0 || cam == null) return;

            Rect area = lastSafeArea;
            if (area.width <= 0f || area.height <= 0f) area = new Rect(0f, 0f, lastWidth, lastHeight);
            float reserved = area.height * reservedBottom;
            area.yMin += reserved;

            float scale = Mathf.Min(area.width / referenceWidth, area.height / referenceHeight);
            if (integerScaling && scale >= 1f) scale = Mathf.Floor(scale);
            Scale = scale;

            float pixelW = referenceWidth * scale;
            float pixelH = referenceHeight * scale;
            float x = Mathf.Round(area.x + (area.width - pixelW) * 0.5f);
            float y = Mathf.Round(area.y + (area.height - pixelH) * verticalAlignment);
            PixelRect = new Rect(x, y, pixelW, pixelH);
            cam.rect = new Rect(x / lastWidth, y / lastHeight, pixelW / lastWidth, pixelH / lastHeight);
            cam.orthographic = true;
            cam.orthographicSize = referenceHeight / PlayField.PixelsPerUnit / 2f;
        }
    }
}
