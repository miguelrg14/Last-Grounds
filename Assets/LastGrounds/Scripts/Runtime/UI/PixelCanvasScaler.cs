using UnityEngine;

namespace LastGrounds
{
    /// <summary>
    /// Makes a Screen Space - Camera canvas measure exactly 112x176 units inside the
    /// letterboxed camera viewport, so UI can be laid out in original room pixels.
    /// (Unity's CanvasScaler ignores the camera's viewport rect.)
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Canvas))]
    public class PixelCanvasScaler : MonoBehaviour
    {
        [SerializeField] float referenceHeight = PlayField.RoomHeight;

        Canvas canvas;

        void OnEnable()
        {
            canvas = GetComponent<Canvas>();
            Apply();
        }

        void Update()
        {
            Apply();
        }

        void Apply()
        {
            if (canvas == null) return;
            Camera cam = canvas.worldCamera;
            float height = cam != null ? cam.pixelHeight : Screen.height;
            if (height <= 0f) return;
            float scale = height / referenceHeight;
            if (!Mathf.Approximately(canvas.scaleFactor, scale)) canvas.scaleFactor = scale;
        }
    }
}
