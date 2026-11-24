using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastGrounds
{
    /// <summary>
    /// Scripted showcase run for screenshots and trailer footage: walks through the menus,
    /// plays a demo round with the <see cref="AutoPilot"/>, lets the barricade fall and
    /// ends on the game over screen. Renders every frame at 672x1056 (6x the original
    /// resolution) into a RenderTexture; the editor tool encodes them into a video.
    /// Started by "Last Grounds > Capture Showcase" in the editor.
    /// </summary>
    public class ShowcaseDirector : MonoBehaviour
    {
        public const int Width = PlayField.RoomWidth * 6;
        public const int Height = PlayField.RoomHeight * 6;

        /// <summary>Raised once per rendered frame while recording.</summary>
        public static event Action<Texture2D> FrameCaptured;
        public static event Action Finished;

        GameModeData mode;
        string outputFolder;
        int fps = 60;

        RenderTexture target;
        Texture2D frame;
        AutoPilot pilot;
        Camera boundCamera;
        bool cameraRenderedLastFrame;
        int framesEmitted;
        string pendingShot;
        readonly List<KeyValuePair<string, float>> markers = new List<KeyValuePair<string, float>>();

        public static ShowcaseDirector Begin(GameModeData showcaseMode, string folder, int framesPerSecond)
        {
            GameObject go = new GameObject("[ShowcaseDirector]");
            DontDestroyOnLoad(go);
            ShowcaseDirector director = go.AddComponent<ShowcaseDirector>();
            director.mode = showcaseMode;
            director.outputFolder = folder;
            director.fps = Mathf.Max(1, framesPerSecond);
            // Fixed timestep: the game advances exactly 1/fps per frame however slow capturing is.
            Time.captureFramerate = director.fps;
            return director;
        }

        void Awake()
        {
            target = new RenderTexture(Width, Height, 24) { filterMode = FilterMode.Point, name = "ShowcaseTarget" };
            frame = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            pilot = gameObject.AddComponent<AutoPilot>();
            pilot.enabled = false;
        }

        void Start()
        {
            StartCoroutine(Run());
        }

        void OnDestroy()
        {
            Time.captureFramerate = 0;
            CaptureTools.SetInvincibleBarricade(false);
            if (boundCamera != null) boundCamera.targetTexture = null;
            if (target != null) target.Release();
        }

        void Update()
        {
            BindCamera();
            if (!cameraRenderedLastFrame)
            {
                cameraRenderedLastFrame = boundCamera != null;
                return;
            }

            // The RenderTexture holds what the camera rendered last frame.
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            frame.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            frame.Apply();
            RenderTexture.active = previous;

            framesEmitted++;
            if (FrameCaptured != null) FrameCaptured(frame);

            if (pendingShot != null)
            {
                File.WriteAllBytes(Path.Combine(outputFolder, pendingShot + ".png"), frame.EncodeToPNG());
                pendingShot = null;
            }
        }

        /// <summary>Every scene has its own camera: point the current one at the RenderTexture.</summary>
        void BindCamera()
        {
            Camera cam = Camera.main;
            if (cam == boundCamera) return;
            boundCamera = cam;
            cameraRenderedLastFrame = false;
            if (cam == null) return;

            PixelViewport viewport = cam.GetComponent<PixelViewport>();
            if (viewport != null) viewport.enabled = false;
            cam.rect = new Rect(0f, 0f, 1f, 1f);
            cam.targetTexture = target;
        }

        IEnumerator Run()
        {
            // --- Menus
            GameSession.SkipTitle = false;
            SceneManager.LoadScene(SceneNames.Menu);
            yield return null;
            yield return null;
            Mark("title");
            yield return Wait(1.5f);
            yield return Shot("01_title");

            MenuController menu = FindFirstObjectByType<MenuController>();
            menu.ShowPanel("Main");
            Mark("main_menu");
            yield return Wait(1.2f);
            yield return Shot("02_main_menu");

            menu.ShowPanel("Horde");
            Mark("horde_menu");
            yield return Wait(1.2f);
            yield return Shot("03_horde_modes");

            menu.ShowPanel("StoryIntro");
            Mark("story_intro");
            yield return Wait(1.5f);
            yield return Shot("04_story_intro");

            // --- Demo round
            CaptureTools.SetInvincibleBarricade(true);
            pilot.fireEnabled = true;
            pilot.enabled = true;
            menu.StartMode(mode);
            yield return null;
            Mark("gameplay");

            yield return Wait(14f);
            yield return Shot("05_gameplay");
            yield return Wait(10f);
            yield return Shot("06_gameplay");
            yield return Wait(10f);
            yield return Shot("07_gameplay");
            yield return Wait(4f);

            // --- Let the barricade fall
            Mark("barricade_falls");
            pilot.fireEnabled = false;
            CaptureTools.SetInvincibleBarricade(false);
            float waited = 0f;
            while (SceneManager.GetActiveScene().name != SceneNames.GameOver && waited < 30f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            pilot.enabled = false;

            // --- Game over: type a name and save it
            Mark("game_over");
            yield return Wait(1.2f);
            GameOverController gameOver = FindFirstObjectByType<GameOverController>();
            if (gameOver != null && gameOver.NameInput != null)
            {
                InputField input = gameOver.NameInput;
                const string playerName = "MIGUEL";
                for (int i = 1; i <= playerName.Length; i++)
                {
                    input.text = playerName.Substring(0, i);
                    yield return Wait(0.12f);
                }
                yield return Wait(0.5f);
                gameOver.SaveEntry();
            }
            yield return Wait(2f);
            yield return Shot("08_game_over");
            yield return Wait(1f);
            Mark("end");

            WriteMarkers();
            if (Finished != null) Finished();
            Destroy(gameObject);
        }

        WaitForSeconds Wait(float seconds)
        {
            return new WaitForSeconds(seconds);
        }

        IEnumerator Shot(string name)
        {
            pendingShot = name;
            while (pendingShot != null) yield return null;
        }

        void Mark(string name)
        {
            markers.Add(new KeyValuePair<string, float>(name, framesEmitted / (float)fps));
        }

        void WriteMarkers()
        {
            StringBuilder json = new StringBuilder("{\n");
            for (int i = 0; i < markers.Count; i++)
            {
                json.Append("  \"").Append(markers[i].Key).Append("\": ")
                    .Append(markers[i].Value.ToString("0.000", CultureInfo.InvariantCulture))
                    .Append(i < markers.Count - 1 ? ",\n" : "\n");
            }
            json.Append("}\n");
            File.WriteAllText(Path.Combine(outputFolder, "markers.json"), json.ToString());
        }
    }
}
