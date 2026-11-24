using System;
using System.IO;
using UnityEngine;

namespace LastGrounds
{
    /// <summary>
    /// Helpers for screenshots and trailer footage. Only active in the editor and in
    /// development builds, never in release builds.
    ///   F12  save a screenshot to &lt;project&gt;/Screenshots
    ///   F10  hide / show the UI for clean shots
    ///   F9   toggle an invincible barricade, to record long gameplay takes
    /// </summary>
    public class CaptureTools : MonoBehaviour
    {
        /// <summary>Read by <see cref="Barricade"/>.</summary>
        public static bool InvincibleBarricade { get; private set; }

        bool uiHidden;

        public static void SetInvincibleBarricade(bool on)
        {
            InvincibleBarricade = on;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            InvincibleBarricade = false;
            if (!Application.isEditor && !Debug.isDebugBuild) return;
            if (Application.platform == RuntimePlatform.WebGLPlayer) return;
            GameObject go = new GameObject("[CaptureTools]");
            DontDestroyOnLoad(go);
            go.AddComponent<CaptureTools>();
        }

        void Update()
        {
            if (GameInput.ScreenshotPressed()) TakeScreenshot();

            bool toggled = GameInput.ToggleUiPressed();
            if (toggled)
            {
                uiHidden = !uiHidden;
                Debug.Log("[Capture] UI " + (uiHidden ? "hidden" : "visible"));
            }
            // While hidden, re-apply every frame so canvases of newly loaded scenes are hidden too.
            if (toggled || uiHidden)
            {
                Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                for (int i = 0; i < canvases.Length; i++) canvases[i].enabled = !uiHidden;
            }

            if (GameInput.ToggleInvinciblePressed())
            {
                InvincibleBarricade = !InvincibleBarricade;
                Debug.Log("[Capture] Invincible barricade " + (InvincibleBarricade ? "ON" : "OFF"));
            }
        }

        static void TakeScreenshot()
        {
            string folder = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Screenshots");
            Directory.CreateDirectory(folder);
            string file = Path.Combine(folder, "LastGrounds_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
            ScreenCapture.CaptureScreenshot(file);
            Debug.Log("[Capture] Screenshot saved to " + file);
        }
    }
}
