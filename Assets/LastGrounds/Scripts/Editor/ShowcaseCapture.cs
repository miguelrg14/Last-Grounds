using System;
using System.IO;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Media;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastGrounds.EditorTools
{
    /// <summary>
    /// "Last Grounds > Capture Showcase": enters Play mode, runs <see cref="ShowcaseDirector"/>
    /// and encodes every frame (plus the game audio) into Recordings/Showcase_*/showcase.mp4,
    /// next to the screenshots and a markers.json with the timestamp of each section.
    /// </summary>
    [InitializeOnLoad]
    public static class ShowcaseCapture
    {
        const string RequestKey = "LastGrounds.ShowcaseRequested";
        const string FolderKey = "LastGrounds.ShowcaseFolder";
        const string MenuScene = "Assets/LastGrounds/Scenes/Menu.unity";
        const string AscentMode = "Assets/LastGrounds/Data/Mode_Ascent.asset";
        const int Fps = 60;

        static MediaEncoder encoder;
        static bool recordAudio;
        static int audioChannels;
        static string outputFolder;
        static int frames;

        static ShowcaseCapture()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        static string ProjectRoot { get { return Path.GetDirectoryName(Application.dataPath); } }

        [MenuItem("Last Grounds/Capture Showcase (screenshots + video)", priority = 20)]
        static void Capture()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Showcase] Stop Play mode first.");
                return;
            }
            if (!File.Exists(MenuScene))
            {
                Debug.LogError("[Showcase] Generate the project first (Last Grounds > Rebuild Project).");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string folder = Path.Combine(ProjectRoot, "Recordings", "Showcase_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(folder);
            SessionState.SetBool(RequestKey, true);
            SessionState.SetString(FolderKey, folder);

            EditorSceneManager.OpenScene(MenuScene);
            // Cameras only render while the Game view is open.
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            EditorApplication.isPlaying = true;
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(RequestKey, false))
            {
                SessionState.SetBool(RequestKey, false);
                StartCapture(SessionState.GetString(FolderKey, Path.Combine(ProjectRoot, "Recordings")));
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                StopEncoder();
            }
        }

        static void StartCapture(string folder)
        {
            GameModeData mode = BuildShowcaseMode();
            if (mode == null)
            {
                Debug.LogError("[Showcase] Could not read the zombie prefabs from " + AscentMode + ". Rebuild the project.");
                EditorApplication.isPlaying = false;
                return;
            }

            outputFolder = folder;
            frames = 0;
            if (!OpenEncoder())
            {
                EditorApplication.isPlaying = false;
                return;
            }

            ShowcaseDirector.FrameCaptured += OnFrame;
            ShowcaseDirector.Finished += OnFinished;
            ShowcaseDirector.Begin(mode, folder, Fps);
            Debug.Log("[Showcase] Recording to " + folder + " ... (about a minute of game time)");
        }

        static bool OpenEncoder()
        {
            VideoTrackAttributes video = new VideoTrackAttributes
            {
                frameRate = new MediaRational(Fps),
                width = (uint)ShowcaseDirector.Width,
                height = (uint)ShowcaseDirector.Height,
                includeAlpha = false,
                bitRateMode = VideoBitrateMode.High,
            };

            audioChannels = AudioSettings.speakerMode == AudioSpeakerMode.Mono ? 1 : 2;
            AudioTrackAttributes audio = new AudioTrackAttributes
            {
                sampleRate = new MediaRational(AudioSettings.outputSampleRate),
                channelCount = (ushort)audioChannels,
                language = "en",
            };

            // H.264 is available in the Windows and macOS editors; VP8/WebM everywhere.
            string[] files = { "showcase.mp4", "showcase.webm" };
            foreach (string file in files)
            {
                string path = Path.Combine(outputFolder, file);
                try
                {
                    recordAudio = AudioRenderer.Start();
                    encoder = recordAudio ? new MediaEncoder(path, video, audio) : new MediaEncoder(path, video);
                    return true;
                }
                catch (Exception e)
                {
                    if (recordAudio) AudioRenderer.Stop();
                    recordAudio = false;
                    Debug.LogWarning("[Showcase] Could not create " + file + ": " + e.Message);
                }
            }
            Debug.LogError("[Showcase] No video encoder available. Screenshots are still saved.");
            return true; // keep going for the screenshots
        }

        static void OnFrame(Texture2D frame)
        {
            if (encoder == null) return;
            encoder.AddFrame(frame);
            frames++;
            if (!recordAudio) return;

            int samples = AudioRenderer.GetSampleCountForCaptureFrame();
            if (samples <= 0) return;
            NativeArray<float> buffer = new NativeArray<float>(samples * audioChannels, Allocator.Temp);
            AudioRenderer.Render(buffer);
            encoder.AddSamples(buffer);
            buffer.Dispose();
        }

        static void OnFinished()
        {
            StopEncoder();
            Debug.Log("[Showcase] Done: " + frames + " frames. Files in " + outputFolder);
            EditorApplication.isPlaying = false;
            EditorUtility.RevealInFinder(Path.Combine(outputFolder, "markers.json"));
        }

        static void StopEncoder()
        {
            ShowcaseDirector.FrameCaptured -= OnFrame;
            ShowcaseDirector.Finished -= OnFinished;
            if (recordAudio) AudioRenderer.Stop();
            recordAudio = false;
            if (encoder != null) encoder.Dispose();
            encoder = null;
        }

        /// <summary>A short, escalating round with every zombie type (not saved as an asset).</summary>
        static GameModeData BuildShowcaseMode()
        {
            GameModeData ascent = AssetDatabase.LoadAssetAtPath<GameModeData>(AscentMode);
            if (ascent == null || ascent.phases == null || ascent.phases.Length == 0) return null;

            Enemy normal = null, fast = null, fat = null, scientist = null, police = null;
            foreach (SpawnPhase phase in ascent.phases)
            {
                foreach (SpawnEntry entry in phase.enemies)
                {
                    if (entry.prefab == null) continue;
                    string n = entry.prefab.name;
                    if (n == "Zombie_Normal") normal = entry.prefab;
                    else if (n == "Zombie_Fast") fast = entry.prefab;
                    else if (n == "Zombie_Fat") fat = entry.prefab;
                    else if (n == "Zombie_Scientist") scientist = entry.prefab;
                    else if (n == "Zombie_Police") police = entry.prefab;
                }
            }
            if (normal == null || fast == null || fat == null || scientist == null || police == null) return null;

            GameModeData mode = ScriptableObject.CreateInstance<GameModeData>();
            mode.id = "showcase";
            mode.displayName = "HORDE";
            mode.firstSpawnDelay = 0.5f;
            mode.barricadeHealth = 100;
            mode.biteDamage = 25;
            mode.biteCooldown = 0.4f; // falls quickly at the end of the recording
            mode.survivalTime = 0f;
            mode.phases = new[]
            {
                Phase("SURVIVE", 0f, new[] { 0.9f, 1.3f }, E(normal, 3), E(fast, 2)),
                Phase("WAVE 2", 10f, new[] { 0.7f, 1f }, E(normal, 2), E(fast, 2), E(fat, 2), E(scientist, 2)),
                Phase("WAVE 3", 20f, new[] { 0.5f, 0.8f }, E(normal, 2), E(fast, 3), E(fat, 1), E(scientist, 1), E(police, 2)),
                Phase("FINAL WAVE", 30f, new[] { 0.3f, 0.45f }, E(normal, 2), E(fast, 4), E(fat, 2), E(scientist, 2), E(police, 1)),
            };
            return mode;
        }

        static SpawnPhase Phase(string label, float start, float[] intervals, params SpawnEntry[] enemies)
        {
            return new SpawnPhase { label = label, startTime = start, intervals = intervals, enemies = enemies };
        }

        static SpawnEntry E(Enemy prefab, float weight)
        {
            return new SpawnEntry { prefab = prefab, weight = weight };
        }
    }
}
