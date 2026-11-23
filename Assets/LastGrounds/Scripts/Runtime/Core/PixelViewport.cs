using UnityEngine;

namespace LastGrounds
{
    /// <summary>
    /// Keeps the camera at the original 112x176 aspect, letterboxed, and scaled by
    /// whole numbers when possible so the pixel art stays crisp.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class PixelViewport : MonoBehaviour
    {
        [SerializeField] int referenceWidth = PlayField.RoomWidth;
        [SerializeField] int referenceHeight = PlayField.RoomHeight;
        [SerializeField] bool integerScaling = true;

        Camera cam;
        int lastWidth = -1;
        int lastHeight = -1;

        /// <summary>Screen pixels per art pixel.</summary>
        public float Scale { get; private set; }

        void OnEnable()
        {
            cam = GetComponent<Camera>();
            lastWidth = -1;
            Apply();
        }

        void Update()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight) Apply();
        }

        void Apply()
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            if (lastWidth <= 0 || lastHeight <= 0 || cam == null) return;

            float scale = Mathf.Min((float)lastWidth / referenceWidth, (float)lastHeight / referenceHeight);
            if (integerScaling && scale >= 1f) scale = Mathf.Floor(scale);
            Scale = scale;

            float pixelW = referenceWidth * scale;
            float pixelH = referenceHeight * scale;
            float x = Mathf.Round((lastWidth - pixelW) * 0.5f);
            float y = Mathf.Round((lastHeight - pixelH) * 0.5f);
            cam.rect = new Rect(x / lastWidth, y / lastHeight, pixelW / lastWidth, pixelH / lastHeight);
            cam.orthographic = true;
            cam.orthographicSize = referenceHeight / PlayField.PixelsPerUnit / 2f;
        }
    }
}
