using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using Object = UnityEngine.Object;

namespace LastGrounds.EditorTools
{
    /// <summary>
    /// Generates the whole game from the imported art and audio: data assets, prefabs,
    /// scenes and build settings. Runs automatically the first time the project is opened
    /// (when Scenes/Menu.unity does not exist yet) and from the menu "Last Grounds/Rebuild".
    ///
    /// Positions are written in the original GameMaker room pixels (112x176, Y down)
    /// so the layout matches the GameMaker rooms one to one.
    /// </summary>
    [InitializeOnLoad]
    public static class LastGroundsBuilder
    {
        const string Root = ImportRules.Root;
        const string SpritesDir = ImportRules.SpritesRoot;
        const string AudioDir = Root + "/Audio";
        const string FontPath = ImportRules.FontsRoot + "/Silkscreen-Regular.ttf";
        const string DataDir = Root + "/Data";
        const string PrefabDir = Root + "/Prefabs";
        const string SceneDir = Root + "/Scenes";
        const string AutoBuildKey = "LastGrounds.AutoBuildTried";

        /// <summary>
        /// Bump when the builder changes what it generates: projects built with an older
        /// version are rebuilt automatically the next time scripts reload.
        /// </summary>
        const int BuildVersion = 7;

        static string BuildVersionKey { get { return "LastGrounds.BuildVersion." + Application.dataPath; } }

        static readonly Color Yellow = new Color(0.83f, 0.94f, 0f, 1f);
        static readonly Color Grey = new Color(0.7f, 0.7f, 0.7f, 1f);

        static Font font;
        static GameObject exitButton;
        static Sprite pixel;

        // Prefabs needed by the game modes.
        static Enemy normal, fast, fat, scientist, police;

        static LastGroundsBuilder()
        {
            EditorApplication.delayCall += AutoBuild;
        }

        static string ScenePath(string name) { return SceneDir + "/" + name + ".unity"; }

        static void AutoBuild()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
            bool upToDate = File.Exists(ScenePath(SceneNames.Menu))
                && EditorPrefs.GetInt(BuildVersionKey, 0) >= BuildVersion;
            if (upToDate) return;
            string triedKey = AutoBuildKey + BuildVersion;
            if (SessionState.GetBool(triedKey, false)) return;
            SessionState.SetBool(triedKey, true);
            Debug.Log("[Last Grounds] Generating project (builder version " + BuildVersion + ")...");
            Build();
        }

        [MenuItem("Last Grounds/Rebuild Project (data, prefabs, scenes)", priority = 0)]
        static void RebuildFromMenu()
        {
            bool ok = EditorUtility.DisplayDialog("Rebuild Last Grounds",
                "This regenerates the Data, Prefabs and Scenes folders under Assets/LastGrounds. " +
                "Manual changes to those assets will be overwritten. Continue?", "Rebuild", "Cancel");
            if (ok) Build();
        }

        /// <summary>Generates everything. Returns false if it failed (also used by CI).</summary>
        public static bool Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
            try
            {
                EditorUtility.DisplayProgressBar("Last Grounds", "Configuring imports...", 0.1f);
                ConfigureImports();
                EnsureFolder(DataDir);
                EnsureFolder(PrefabDir);
                EnsureFolder(SceneDir);

                font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
                pixel = SpriteAt("UI/Pixel");
                if (font == null) Debug.LogWarning("[Last Grounds] Font not found at " + FontPath + "; using Unity's default.");
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

                EditorUtility.DisplayProgressBar("Last Grounds", "Creating prefabs and data...", 0.3f);
                BuildWeapons();
                BuildEnemies();
                BuildModes();
                // Write the data to disk now: opening a new scene unloads unused assets,
                // so each scene builder reloads what it needs from disk (see Data<T>).
                AssetDatabase.SaveAssets();

                EditorUtility.DisplayProgressBar("Last Grounds", "Building scenes...", 0.6f);
                BuildGameScene();
                BuildGameOverScene();
                BuildMenuScene();

                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(ScenePath(SceneNames.Menu), true),
                    new EditorBuildSettingsScene(ScenePath(SceneNames.Game), true),
                    new EditorBuildSettingsScene(ScenePath(SceneNames.GameOver), true),
                };
                ConfigureProject();

                AssetDatabase.SaveAssets();
                EditorSceneManager.OpenScene(ScenePath(SceneNames.Menu));
                ValidateMenuScene();
                EditorPrefs.SetInt(BuildVersionKey, BuildVersion);
                Debug.Log("[Last Grounds] Project built. Press Play in the Menu scene.");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[Last Grounds] Build failed: " + e);
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Last Grounds", "Building the project failed:\n\n" + e.Message +
                        "\n\nSee the Console for details.", "OK");
                }
                return false;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>Checks the saved Menu scene really points at the game modes.</summary>
        static void ValidateMenuScene()
        {
            MenuController menu = Object.FindFirstObjectByType<MenuController>();
            if (menu == null) throw new InvalidOperationException("MenuController not found in the Menu scene.");
            SerializedObject so = new SerializedObject(menu);
            if (so.FindProperty("storyMode").objectReferenceValue == null)
                throw new InvalidOperationException("The Menu scene has no Story mode assigned after building.");
            SerializedProperty horde = so.FindProperty("hordeModes");
            if (horde.arraySize == 0) throw new InvalidOperationException("The Menu scene has no horde modes assigned.");
            for (int i = 0; i < horde.arraySize; i++)
                if (horde.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    throw new InvalidOperationException("Horde mode " + i + " is missing in the Menu scene.");
        }

        // ------------------------------------------------------------------ imports

        static void ConfigureImports()
        {
            List<AssetImporter> changed = new List<AssetImporter>();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SpritesDir }))
            {
                TextureImporter ti = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter;
                if (ti != null && ImportRules.ApplySprite(ti)) changed.Add(ti);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioImporter ai = AssetImporter.GetAtPath(path) as AudioImporter;
                if (ai != null && ImportRules.ApplyAudio(ai, path.StartsWith(ImportRules.MusicRoot))) changed.Add(ai);
            }
            TrueTypeFontImporter fi = AssetImporter.GetAtPath(FontPath) as TrueTypeFontImporter;
            if (fi != null && ImportRules.ApplyFont(fi)) changed.Add(fi);

            if (changed.Count == 0) return;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (AssetImporter importer in changed) importer.SaveAndReimport();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        static void ConfigureProject()
        {
            PlayerSettings.productName = "Last Grounds";
            PlayerSettings.companyName = "Miguel Rodriguez Gallego";
            PlayerSettings.defaultScreenWidth = PlayField.RoomWidth * 6;
            PlayerSettings.defaultScreenHeight = PlayField.RoomHeight * 6;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;

            // Web: 4x the original resolution fits most screens (itch.io embed uses the same size).
            // Gzip + decompression fallback works on any host, including GitHub Pages and itch.io.
            PlayerSettings.defaultWebScreenWidth = PlayField.RoomWidth * 4;
            PlayerSettings.defaultWebScreenHeight = PlayField.RoomHeight * 4;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;

            // Android: portrait only, 64-bit (required by Google Play).
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.miguelrg14.lastgrounds");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        }

        // ------------------------------------------------------------------ data + prefabs

        static void BuildWeapons()
        {
            AudioClip shot = Clip("SFX/SFX_Shot.mp3");

            // Hitboxes from the GameMaker sprite bounding boxes (left, top, right, bottom in px).
            Projectile pistolBullet = BuildProjectile("Bullet_Pistol", Projectile.Team.Player, 7, 3, 8, 4);
            Projectile shotgunBlast = BuildProjectile("Bullet_Shotgun", Projectile.Team.Player, 1, 2, 15, 14);
            Projectile mp7Bullet = BuildProjectile("Bullet_MP7", Projectile.Team.Player, 9, 2, 9, 3);

            Asset<WeaponData>("Weapon_Pistol", w =>
            {
                w.displayName = "PISTOL";
                w.playerSprite = SpriteAt("Player/Player_Pistol");
                w.projectilePrefab = pistolBullet;
                w.damage = 25;
                w.cooldown = 10f / 60f;
                w.automatic = false;
                w.projectileSpeed = 37.5f;
                w.shotSound = shot;
                w.shotVolume = 0.8f;
                w.shotPitch = 1f;
            });
            Asset<WeaponData>("Weapon_Shotgun", w =>
            {
                w.displayName = "SHOTGUN";
                w.playerSprite = SpriteAt("Player/Player_Shotgun");
                w.projectilePrefab = shotgunBlast;
                w.damage = 50;
                w.cooldown = 20f / 60f;
                w.automatic = false;
                w.projectileSpeed = 37.5f;
                w.shotSound = shot;
                w.shotVolume = 1f;
                w.shotPitch = 0.65f;
            });
            Asset<WeaponData>("Weapon_MP7", w =>
            {
                w.displayName = "MP7";
                w.playerSprite = SpriteAt("Player/Player_MP7");
                w.projectilePrefab = mp7Bullet;
                w.damage = 5;
                w.cooldown = 2f / 60f;
                w.automatic = true;
                w.projectileSpeed = 37.5f;
                w.shotSound = shot;
                w.shotVolume = 0.3f;
                w.shotPitch = 1.5f;
            });
        }

        static void BuildEnemies()
        {
            AudioClip death = Clip("SFX/SFX_ZombieDeath.mp3");
            AudioClip walk = Clip("SFX/SFX_ZombieSpawn.mp3");

            // Speeds: original px/frame * 60 fps / 16 px per unit.
            EnemyData normalData = EnemyAsset("Enemy_Normal", "ZOMBIE", 125, 0.2f, 2, walk, death);
            EnemyData fastData = EnemyAsset("Enemy_Fast", "FAST ZOMBIE", 75, 0.5f, 2, Clip("SFX/SFX_FastZombieSpawn.mp3"), death);
            EnemyData fatData = EnemyAsset("Enemy_Fat", "FAT ZOMBIE", 175, 0.1f, 3, Clip("SFX/SFX_FatZombieSpawn.mp3"), death);
            EnemyData scientistData = EnemyAsset("Enemy_Scientist", "SCIENTIST", 50, 0.4f, 4, walk, death);
            EnemyData policeData = EnemyAsset("Enemy_Police", "POLICE ZOMBIE", 100, 0.15f, 5, walk, death);

            Projectile policeBullet = BuildProjectile("Bullet_Police", Projectile.Team.Enemy, 10, 0, 10, 1);

            normal = BuildEnemy("Zombie_Normal", normalData, 8f, 1, 1, 14, 14, null);
            fast = BuildEnemy("Zombie_Fast", fastData, 16f, 1, 0, 14, 15, null);
            fat = BuildEnemy("Zombie_Fat", fatData, 8f, 1, 0, 14, 14, null);
            scientist = BuildEnemy("Zombie_Scientist", scientistData, 12f, 2, 0, 13, 12, go =>
            {
                Set(go.AddComponent<RegeneratingHealth>(), "healthPerSecond", 60f);
                Set(go.AddComponent<LaneTeleporter>(), "interval", 3f);
            });
            police = BuildEnemy("Zombie_Police", policeData, 0f, 2, 0, 13, 14, go =>
            {
                EnemyShooter shooter = go.AddComponent<EnemyShooter>();
                Set(shooter, "projectilePrefab", policeBullet);
                Set(shooter, "shotSound", Clip("SFX/SFX_Shot.mp3"));
            });
        }

        static EnemyData EnemyAsset(string file, string displayName, int hp, float pxPerFrame, int score, AudioClip spawn, AudioClip death)
        {
            return Asset<EnemyData>(file, d =>
            {
                d.displayName = displayName;
                d.maxHealth = hp;
                d.speed = pxPerFrame * 60f / PlayField.PixelsPerUnit;
                d.scoreValue = score;
                d.spawnSound = spawn;
                d.deathSound = death;
                d.soundVolume = 0.6f;
            });
        }

        static void BuildModes()
        {
            Dictionary<string, GameModeData> modes = new Dictionary<string, GameModeData>();
            float[] oneOrTwo = { 1f, 2f };

            // Weights come from the random(10) thresholds of each oGeneradorX alarm.
            modes["easy"] = Mode("Mode_Easy", "easy", "EASY", 5f, 0f,
                Phase("EASY", 0f, new[] { 1f, 2f, 3f }, 6, 3, 0, 1, 0));
            modes["normal"] = Mode("Mode_Normal", "normal", "NORMAL", 5f, 0f,
                Phase("NORMAL", 0f, oneOrTwo, 4, 4, 1, 1, 0));
            modes["hard"] = Mode("Mode_Hard", "hard", "HARD", 5f, 0f,
                Phase("HARD", 0f, oneOrTwo, 2, 6, 1, 1, 0));
            modes["hardcore"] = Mode("Mode_Hardcore", "hardcore", "HARDCORE", 5f, 0f,
                Phase("HARDCORE", 0f, new[] { 0.9f }, 1, 6, 2, 1, 0));

            // Ascent (Escalada): the 8 alarms of oGeneradorEscalada as timed waves.
            // The police zombie joins from wave 6 on (new).
            modes["ascent"] = Mode("Mode_Ascent", "ascent", "ASCENT", 5f, 0f,
                Phase("WAVE 1", 5f, new[] { 5f }, 7, 3, 0, 1, 0),
                Phase("WAVE 2", 30f, new[] { 4f }, 5, 4, 1, 1, 0),
                Phase("WAVE 3", 50f, new[] { 3f }, 4, 5, 1, 1, 0),
                Phase("WAVE 4", 65f, new[] { 2f }, 3, 5, 2, 1, 0),
                Phase("WAVE 5", 75f, new[] { 1f }, 2, 6, 2, 1, 0),
                Phase("WAVE 6", 80f, new[] { 1f }, 2, 5, 2, 2, 1),
                Phase("WAVE 7", 85f, new[] { 1f }, 1, 6, 2, 2, 1),
                Phase("FINAL WAVE", 90f, new[] { 1f }, 1, 5, 2, 3, 1));

            // Story: the intro promises a rescue in 5 days. Survive 5 "days" of 60 s.
            modes["story"] = Mode("Mode_Story", "story", "STORY", 3f, 300f,
                Phase("DAY 1", 0f, new[] { 2f, 3f }, 6, 3, 0, 1, 0),
                Phase("DAY 2", 60f, new[] { 1f, 2f, 3f }, 4, 4, 1, 1, 0),
                Phase("DAY 3", 120f, oneOrTwo, 4, 4, 1, 1, 1),
                Phase("DAY 4", 180f, oneOrTwo, 2, 6, 1, 1, 1),
                Phase("DAY 5", 240f, new[] { 1f }, 1, 6, 2, 1, 1));
        }

        static GameModeData Mode(string file, string id, string displayName, float firstDelay, float survival, params SpawnPhase[] phases)
        {
            return Asset<GameModeData>(file, m =>
            {
                m.id = id;
                m.displayName = displayName;
                m.firstSpawnDelay = firstDelay;
                m.phases = phases;
                m.barricadeHealth = 100;
                m.biteDamage = 20;
                m.biteCooldown = 100f / 60f;
                m.survivalTime = survival;
                m.victoryMessage = "RESCUED!";
            });
        }

        static SpawnPhase Phase(string label, float start, float[] intervals, float wNormal, float wFast, float wScientist, float wFat, float wPolice)
        {
            List<SpawnEntry> entries = new List<SpawnEntry>();
            AddEntry(entries, normal, wNormal);
            AddEntry(entries, fast, wFast);
            AddEntry(entries, scientist, wScientist);
            AddEntry(entries, fat, wFat);
            AddEntry(entries, police, wPolice);
            return new SpawnPhase { label = label, startTime = start, intervals = intervals, enemies = entries.ToArray() };
        }

        static void AddEntry(List<SpawnEntry> list, Enemy prefab, float weight)
        {
            if (weight > 0f && prefab != null) list.Add(new SpawnEntry { prefab = prefab, weight = weight });
        }

        static Projectile BuildProjectile(string name, Projectile.Team team, int left, int top, int right, int bottom)
        {
            GameObject go = new GameObject(name);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteAt("Projectiles/" + name);
            sr.sortingOrder = 15;

            Projectile p = go.AddComponent<Projectile>();
            Rect box = PixelBox(left, top, right, bottom);
            Set(p, "team", (int)team);
            Set(p, "hitboxSize", new Vector2(Mathf.Max(box.width, 0.125f), Mathf.Max(box.height, 0.125f)));
            Set(p, "hitboxOffset", box.center);
            return SavePrefab(go, name).GetComponent<Projectile>();
        }

        static Enemy BuildEnemy(string name, EnemyData data, float fps, int left, int top, int right, int bottom, Action<GameObject> extras)
        {
            Sprite[] frames = FramesIn("Enemies/" + name);
            GameObject go = new GameObject(name);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = frames.Length > 0 ? frames[0] : null;
            sr.sortingOrder = 10;

            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            Rect r = PixelBox(left, top, right, bottom);
            box.size = r.size;
            box.offset = r.center;

            Rigidbody2D body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;

            Enemy enemy = go.AddComponent<Enemy>();
            Set(enemy, "data", data);

            if (frames.Length > 1)
            {
                SpriteAnimator anim = go.AddComponent<SpriteAnimator>();
                Set(anim, "frames", frames);
                Set(anim, "framesPerSecond", fps);
            }

            // Health bar: 1 px above the sprite, like draw_healthbar(x+6, y-1, ...).
            GameObject bar = new GameObject("HealthBar");
            bar.transform.SetParent(go.transform, false);
            bar.transform.localPosition = new Vector3(0f, 9f / PlayField.PixelsPerUnit, 0f);
            SpriteRenderer back = PixelRenderer("Back", bar.transform, new Vector2(10f, 3f), new Color(0f, 0f, 0f, 0.8f), 11);
            SpriteRenderer fill = PixelRenderer("Fill", bar.transform, new Vector2(8f, 1f), Color.green, 12);
            back.transform.localPosition = Vector3.zero;
            EnemyHealthBar healthBar = go.AddComponent<EnemyHealthBar>();
            Set(healthBar, "fill", fill);
            Set(healthBar, "widthPixels", 8f);

            if (extras != null) extras(go);
            return SavePrefab(go, name).GetComponent<Enemy>();
        }

        static SpriteRenderer PixelRenderer(string name, Transform parent, Vector2 sizePixels, Color color, int order)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(sizePixels.x, sizePixels.y, 1f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = pixel;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>GameMaker bbox (inclusive px, 16x16 sprite, Y down) to a local rect in units around a centred pivot.</summary>
        static Rect PixelBox(int left, int top, int right, int bottom)
        {
            float ppu = PlayField.PixelsPerUnit;
            float w = (right - left + 1) / ppu;
            float h = (bottom - top + 1) / ppu;
            float cx = ((left + right + 1) * 0.5f - 8f) / ppu;
            float cy = (8f - (top + bottom + 1) * 0.5f) / ppu;
            return new Rect(cx - w * 0.5f, cy - h * 0.5f, w, h);
        }

        // ------------------------------------------------------------------ scenes

        static void BuildGameScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            WeaponData[] weapons =
            {
                Data<WeaponData>("Weapon_Pistol"),
                Data<WeaponData>("Weapon_Shotgun"),
                Data<WeaponData>("Weapon_MP7"),
            };
            GameModeData defaultMode = Data<GameModeData>("Mode_Normal");
            Camera cam = CreateCameras();
            CreateEventSystem();

            WorldSprite("Background", SpriteAt("Backgrounds/BG_Game"), 0f, 0f, PlayField.RoomWidth, PlayField.RoomHeight, -100);

            // Barricade: GameMaker instance at (0, 112), sprite 112x16.
            GameObject barricadeGO = WorldSprite("Barricade", SpriteAt("Environment/Barricade"), 0f, 112f, 112f, 16f, 0);
            BoxCollider2D barricadeBox = barricadeGO.AddComponent<BoxCollider2D>();
            barricadeBox.size = new Vector2(7f, 1f);
            Barricade barricade = barricadeGO.AddComponent<Barricade>();
            Set(barricade, "biteSound", Clip("SFX/SFX_Bite.mp3"));

            // Player: GameMaker instance at (48, 128), sprite 16x16.
            GameObject playerGO = WorldSprite("Player", weapons[0].playerSprite, 48f, 128f, 16f, 16f, 20);
            PlayerController player = playerGO.AddComponent<PlayerController>();
            Set(player, "weapons", weapons);
            Set(player, "minX", PlayField.LaneX(0));
            Set(player, "maxX", PlayField.LaneX(PlayField.LaneCount - 1));

            Transform enemies = new GameObject("Enemies").transform;

            GameObject managerGO = new GameObject("GameManager");
            EnemySpawner spawner = managerGO.AddComponent<EnemySpawner>();
            Set(spawner, "enemiesParent", enemies);
            GameManager gm = managerGO.AddComponent<GameManager>();

            // ---- HUD
            RectTransform canvas = CreateCanvas("HUD", cam, 10);
            HUD hud = canvas.gameObject.AddComponent<HUD>();
            Text score = Label(canvas, "Score", "SCORE 0", 3, 2, 70, 9, 8, TextAnchor.UpperLeft, Color.white);
            Text weapon = Label(canvas, "Weapon", "1 PISTOL", 50, 2, 59, 9, 8, TextAnchor.UpperRight, Yellow);
            Text time = Label(canvas, "Time", "TIME 00:00", 3, 165, 75, 9, 8, TextAnchor.UpperLeft, Color.white);
            Text banner = Label(canvas, "Banner", "WAVE 1", 0, 64, 112, 16, 12, TextAnchor.MiddleCenter, Yellow);

            // Barricade bar where BarreraX drew it: (82..110, 162..174).
            Image barBack = ImageTL(canvas, "BarricadeBar", pixel, 80, 165, 29, 7);
            barBack.color = new Color(0f, 0f, 0f, 0.85f);
            Image barFill = ImageTL(barBack.rectTransform, "Fill", pixel, 0, 0, 0, 0);
            Stretch(barFill.rectTransform, 1f);
            barFill.type = Image.Type.Filled;
            barFill.fillMethod = Image.FillMethod.Horizontal;
            barFill.fillOrigin = (int)Image.OriginHorizontal.Left;

            Set(hud, "scoreText", score);
            Set(hud, "timeText", time);
            Set(hud, "weaponText", weapon);
            Set(hud, "bannerText", banner);
            Set(hud, "barricadeFill", barFill);

            // ---- Pause panel
            RectTransform pause = UI("Pause", canvas);
            Stretch(pause, 0f);
            Image dim = pause.gameObject.AddComponent<Image>();
            dim.sprite = pixel;
            dim.color = new Color(0f, 0f, 0f, 0.75f);
            Label(pause, "Title", "PAUSED", 0, 60, 112, 16, 16, TextAnchor.MiddleCenter, Yellow);
            Button resume = MakeButton(pause, "Resume", "RESUME", "Button_Teal", 56, 100);
            Button quit = MakeButton(pause, "Menu", "MENU", "Button_Red", 56, 118);
            UnityEventTools.AddPersistentListener(resume.onClick, gm.Resume);
            UnityEventTools.AddPersistentListener(quit.onClick, gm.QuitToMenu);
            Text pauseHint = Label(pause, "Hint", "ESC TO RESUME", 0, 140, 112, 8, 6, TextAnchor.MiddleCenter, Grey);
            Variant(pauseHint.gameObject, PlatformVariant.Mode.HideOnTouch);

            BuildTouchControls(cam, gm, player);

            Set(gm, "defaultMode", defaultMode);
            Set(gm, "barricade", barricade);
            Set(gm, "spawner", spawner);
            Set(gm, "player", player);
            Set(gm, "hud", hud);
            Set(gm, "pausePanel", pause.gameObject);
            Set(gm, "pauseFirstSelected", resume);
            Set(gm, "music", Clip("Music/Music_Game.ogg"));
            pause.gameObject.SetActive(false);

            EditorSceneManager.SaveScene(scene, ScenePath(SceneNames.Game));
        }

        static void BuildGameOverScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameModeData fallbackMode = Data<GameModeData>("Mode_Normal");
            Camera cam = CreateCameras();
            CreateEventSystem();
            RectTransform canvas = CreateCanvas("UI", cam, 0);

            Image background = ImageTL(canvas, "Background", SpriteAt("Backgrounds/BG_GameOver"), 0, 0, 112, 176);
            GameOverController ctrl = canvas.gameObject.AddComponent<GameOverController>();

            RectTransform victory = UI("VictoryHeader", canvas);
            Stretch(victory, 0f);
            Label(victory, "Title", "RESCUED!", 0, 8, 112, 20, 16, TextAnchor.MiddleCenter, Yellow);
            Label(victory, "Subtitle", "HIGHSCORE", 0, 32, 112, 10, 8, TextAnchor.MiddleCenter, Color.white);
            victory.gameObject.SetActive(false);

            Text result = Label(canvas, "Result", "NORMAL   0 PTS   00:00", 0, 46, 112, 8, 6, TextAnchor.MiddleCenter, Yellow);

            // Name entry (replaces get_string's popup).
            RectTransform entry = UI("Entry", canvas);
            PlaceTL(entry, 0, 56, 112, 12);
            Label(entry, "Label", "NAME", 4, 2, 24, 8, 6, TextAnchor.MiddleLeft, Color.white);
            InputField input = MakeInputField(entry, "NameInput", 26, 1, 54, 10);
            Button save = MakeButton(entry, "Save", "OK", "Button_Green", 96, 6, 22, 10, 6);
            UnityEventTools.AddPersistentListener(save.onClick, ctrl.SaveEntry);

            Text names = Label(canvas, "Names", "", 8, 72, 72, 66, 6, TextAnchor.UpperLeft, Color.white);
            Text scores = Label(canvas, "Scores", "", 72, 72, 32, 66, 6, TextAnchor.UpperRight, Color.white);
            names.lineSpacing = 1.1f;
            scores.lineSpacing = 1.1f;

            Text clearHint = Label(canvas, "Hint", "PRESS C TO CLEAR LIST", 0, 140, 112, 8, 6, TextAnchor.MiddleCenter, Grey);
            Variant(clearHint.gameObject, PlatformVariant.Mode.HideOnTouch);
            Button clear = MakeButton(canvas, "Clear", "CLEAR LIST", "Button_Purple", 56, 142, 60, 10, 6);
            UnityEventTools.AddPersistentListener(clear.onClick, ctrl.ClearList);
            Variant(clear.gameObject, PlatformVariant.Mode.ShowOnTouch);
            Button menu = MakeButton(canvas, "Menu", "MENU", "Button_Teal", 56, 160);
            UnityEventTools.AddPersistentListener(menu.onClick, ctrl.BackToMenu);

            Set(ctrl, "fallbackMode", fallbackMode);
            Set(ctrl, "background", background);
            Set(ctrl, "victoryHeader", victory.gameObject);
            Set(ctrl, "resultText", result);
            Set(ctrl, "entryRoot", entry.gameObject);
            Set(ctrl, "nameInput", input);
            Set(ctrl, "namesColumn", names);
            Set(ctrl, "scoresColumn", scores);
            Set(ctrl, "menuButton", menu);
            Set(ctrl, "defeatMusic", Clip("Music/Music_GameOver.ogg"));
            Set(ctrl, "defeatSting", Clip("SFX/SFX_GameOver.mp3"));
            Set(ctrl, "victoryMusic", Clip("Music/Music_Menu.ogg"));

            EditorSceneManager.SaveScene(scene, ScenePath(SceneNames.GameOver));
        }

        struct PanelDef
        {
            public string id;
            public GameObject root;
            public Selectable first;
            public string backTo;
        }

        static void BuildMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Dictionary<string, GameModeData> modes = new Dictionary<string, GameModeData>();
            foreach (string id in new[] { "Easy", "Normal", "Hard", "Hardcore", "Ascent", "Story" })
                modes[id.ToLowerInvariant()] = Data<GameModeData>("Mode_" + id);
            Camera cam = CreateCameras();
            CreateEventSystem();
            RectTransform canvas = CreateCanvas("UI", cam, 0);
            ImageTL(canvas, "Background", SpriteAt("Backgrounds/BG_Title"), 0, 0, 112, 176);

            MenuController ctrl = canvas.gameObject.AddComponent<MenuController>();
            List<PanelDef> panels = new List<PanelDef>();

            // Title (Presentacion room).
            RectTransform title = Panel(canvas, "Title");
            Text press = Label(title, "PressAnyKey", "PRESS ANY KEY", 0, 154, 112, 12, 8, TextAnchor.MiddleCenter, Color.white);
            press.gameObject.AddComponent<UITextBlink>();
            Variant(press.gameObject, PlatformVariant.Mode.SwapText, "TAP TO START");
            panels.Add(new PanelDef { id = "Title", root = title.gameObject });

            // Main menu (Menu room). Buttons were scaled 1.2 in X.
            RectTransform main = Panel(canvas, "Main");
            Button play = MakeButton(main, "Play", "PLAY", "Button_Red", 56, 96, 96);
            Button controls = MakeButton(main, "Controls", "CONTROLS", "Button_Green", 56, 112, 96);
            Button options = MakeButton(main, "Options", "OPTIONS", "Button_Yellow", 56, 128, 96);
            Button credits = MakeButton(main, "Credits", "CREDITS", "Button_Blue", 56, 144, 96);
            Button exit = MakeButton(main, "Exit", "EXIT", "Button_Purple", 56, 160, 96);
            UnityEventTools.AddStringPersistentListener(play.onClick, ctrl.ShowPanel, "Play");
            UnityEventTools.AddStringPersistentListener(controls.onClick, ctrl.ShowPanel, "Controls");
            UnityEventTools.AddStringPersistentListener(options.onClick, ctrl.ShowPanel, "Options");
            UnityEventTools.AddStringPersistentListener(credits.onClick, ctrl.ShowPanel, "Credits");
            UnityEventTools.AddPersistentListener(exit.onClick, ctrl.QuitGame);
            exitButton = exit.gameObject;
            panels.Add(new PanelDef { id = "Main", root = main.gameObject, first = play });

            // Game type (MenuSeleccionJuego). Story was unreachable in the original.
            RectTransform playPanel = Panel(canvas, "Play");
            Button story = MakeButton(playPanel, "Story", "STORY", "Button_Blue", 56, 112);
            Button horde = MakeButton(playPanel, "Horde", "HORDE MODE", "Button_Red", 56, 128);
            Button playBack = MakeButton(playPanel, "Back", "BACK", "Button_Teal", 56, 160);
            UnityEventTools.AddStringPersistentListener(story.onClick, ctrl.ShowPanel, "StoryIntro");
            UnityEventTools.AddStringPersistentListener(horde.onClick, ctrl.ShowPanel, "Horde");
            UnityEventTools.AddStringPersistentListener(playBack.onClick, ctrl.ShowPanel, "Main");
            panels.Add(new PanelDef { id = "Play", root = playPanel.gameObject, first = story, backTo = "Main" });

            // Difficulty (MenuSeleccionHorda).
            RectTransform hordePanel = Panel(canvas, "Horde");
            Button ascent = MakeButton(hordePanel, "Ascent", "ASCENT", "Button_Teal", 56, 64);
            Button easy = MakeButton(hordePanel, "Easy", "EASY", "Button_Blue", 56, 96);
            Button normalB = MakeButton(hordePanel, "Normal", "NORMAL", "Button_Green", 56, 112);
            Button hard = MakeButton(hordePanel, "Hard", "HARD", "Button_Yellow", 56, 128);
            Button hardcore = MakeButton(hordePanel, "Hardcore", "HARDCORE", "Button_Red", 56, 144);
            Button hordeBack = MakeButton(hordePanel, "Back", "BACK", "Button_Teal", 56, 160);
            UnityEventTools.AddStringPersistentListener(ascent.onClick, ctrl.StartHorde, "ascent");
            UnityEventTools.AddStringPersistentListener(easy.onClick, ctrl.StartHorde, "easy");
            UnityEventTools.AddStringPersistentListener(normalB.onClick, ctrl.StartHorde, "normal");
            UnityEventTools.AddStringPersistentListener(hard.onClick, ctrl.StartHorde, "hard");
            UnityEventTools.AddStringPersistentListener(hardcore.onClick, ctrl.StartHorde, "hardcore");
            UnityEventTools.AddStringPersistentListener(hordeBack.onClick, ctrl.ShowPanel, "Play");
            panels.Add(new PanelDef { id = "Horde", root = hordePanel.gameObject, first = normalB, backTo = "Play" });

            // Controls (MenuControles) + the new weapon keys.
            RectTransform controlsPanel = Panel(canvas, "Controls");
            Image keysFrame = ImageTL(controlsPanel, "Frame", SpriteAt("UI/Panel_Controls"), 16, 80, 80, 64);
            Text weaponKeys = Label(controlsPanel, "Weapons", "1-3 / Q E = WEAPON", 0, 145, 112, 8, 6, TextAnchor.MiddleCenter, Yellow);
            Variant(keysFrame.gameObject, PlatformVariant.Mode.HideOnTouch);
            Variant(weaponKeys.gameObject, PlatformVariant.Mode.HideOnTouch);
            Image touchHelp = ImageTL(controlsPanel, "TouchHelp", pixel, 12, 84, 88, 58);
            touchHelp.color = new Color(0f, 0f, 0f, 0.75f);
            Label(touchHelp.rectTransform, "Text", "< > = MOVE\nFIRE = SHOOT\n1 2 3 = WEAPON\nII = PAUSE", 0, 0, 88, 58, 8,
                TextAnchor.MiddleCenter, Color.white).lineSpacing = 1.3f;
            Variant(touchHelp.gameObject, PlatformVariant.Mode.ShowOnTouch);
            Button controlsBack = MakeButton(controlsPanel, "Back", "BACK", "Button_Teal", 56, 160);
            UnityEventTools.AddStringPersistentListener(controlsBack.onClick, ctrl.ShowPanel, "Main");
            panels.Add(new PanelDef { id = "Controls", root = controlsPanel.gameObject, first = controlsBack, backTo = "Main" });

            // Credits (MenuCreditos).
            RectTransform creditsPanel = Panel(canvas, "Credits");
            ImageTL(creditsPanel, "Main", SpriteAt("UI/Panel_Credits_Main"), 16, 16, 80, 64);
            ImageTL(creditsPanel, "Effects", SpriteAt("UI/Panel_Credits_Effects"), 16, 80, 80, 64);
            Button creditsBack = MakeButton(creditsPanel, "Back", "BACK", "Button_Teal", 56, 160);
            UnityEventTools.AddStringPersistentListener(creditsBack.onClick, ctrl.ShowPanel, "Main");
            panels.Add(new PanelDef { id = "Credits", root = creditsPanel.gameObject, first = creditsBack, backTo = "Main" });

            // Options (MenuOpciones was empty).
            RectTransform optionsPanel = Panel(canvas, "Options");
            OptionsPanel opts = optionsPanel.gameObject.AddComponent<OptionsPanel>();
            Image optionsBack = ImageTL(optionsPanel, "Frame", pixel, 12, 86, 88, 62);
            optionsBack.color = new Color(0f, 0f, 0f, 0.7f);
            Label(optionsPanel, "Title", "OPTIONS", 0, 88, 112, 10, 8, TextAnchor.MiddleCenter, Yellow);
            Button musicDown, musicUp, sfxDown, sfxUp;
            Text musicValue = OptionRow(optionsPanel, "Music", "MUSIC", 106, out musicDown, out musicUp);
            Text sfxValue = OptionRow(optionsPanel, "Sfx", "SFX", 120, out sfxDown, out sfxUp);
            UnityEventTools.AddIntPersistentListener(musicDown.onClick, opts.ChangeMusic, -1);
            UnityEventTools.AddIntPersistentListener(musicUp.onClick, opts.ChangeMusic, 1);
            UnityEventTools.AddIntPersistentListener(sfxDown.onClick, opts.ChangeSfx, -1);
            UnityEventTools.AddIntPersistentListener(sfxUp.onClick, opts.ChangeSfx, 1);
            Button fullscreen = MakeButton(optionsPanel, "Fullscreen", "FULLSCREEN OFF", "Button_Yellow", 56, 138, 84, 12, 6);
            UnityEventTools.AddPersistentListener(fullscreen.onClick, opts.ToggleFullscreen);
            Button optionsBackButton = MakeButton(optionsPanel, "Back", "BACK", "Button_Teal", 56, 160);
            UnityEventTools.AddStringPersistentListener(optionsBackButton.onClick, ctrl.ShowPanel, "Main");
            Set(opts, "musicValue", musicValue);
            Set(opts, "sfxValue", sfxValue);
            Set(opts, "fullscreenLabel", fullscreen.GetComponentInChildren<Text>());
            Set(opts, "sfxPreview", Clip("SFX/SFX_Shot.mp3"));
            panels.Add(new PanelDef { id = "Options", root = optionsPanel.gameObject, first = musicUp, backTo = "Main" });

            // Story intro (IntroHistoria room).
            RectTransform intro = Panel(canvas, "StoryIntro");
            ImageTL(intro, "Story", SpriteAt("Backgrounds/BG_StoryIntro"), 0, 0, 112, 176);
            Label(intro, "Goal", "SURVIVE 5 DAYS", 0, 146, 112, 8, 6, TextAnchor.MiddleCenter, Yellow);
            Button cont = MakeButton(intro, "Continue", "CONTINUE", "Button_Teal", 56, 160);
            UnityEventTools.AddPersistentListener(cont.onClick, ctrl.StartStory);
            panels.Add(new PanelDef { id = "StoryIntro", root = intro.gameObject, first = cont, backTo = "Play" });

            SerializedObject so = new SerializedObject(ctrl);
            SerializedProperty list = so.FindProperty("panels");
            list.arraySize = panels.Count;
            for (int i = 0; i < panels.Count; i++)
            {
                SerializedProperty el = list.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("id").stringValue = panels[i].id;
                el.FindPropertyRelative("root").objectReferenceValue = panels[i].root;
                el.FindPropertyRelative("firstSelected").objectReferenceValue = panels[i].first;
                el.FindPropertyRelative("backTo").stringValue = panels[i].backTo ?? string.Empty;
            }
            so.FindProperty("menuMusic").objectReferenceValue = Clip("Music/Music_Menu.ogg");
            so.FindProperty("storyMode").objectReferenceValue = modes["story"];
            SerializedProperty hiddenOnWeb = so.FindProperty("hiddenOnWeb");
            hiddenOnWeb.arraySize = 1;
            hiddenOnWeb.GetArrayElementAtIndex(0).objectReferenceValue = exitButton;
            string[] hordeIds = { "ascent", "easy", "normal", "hard", "hardcore" };
            SerializedProperty hordeList = so.FindProperty("hordeModes");
            hordeList.arraySize = hordeIds.Length;
            for (int i = 0; i < hordeIds.Length; i++)
                hordeList.GetArrayElementAtIndex(i).objectReferenceValue = modes[hordeIds[i]];
            so.ApplyModifiedPropertiesWithoutUndo();

            for (int i = 1; i < panels.Count; i++) panels[i].root.SetActive(false);
            EditorSceneManager.SaveScene(scene, ScenePath(SceneNames.Menu));
        }

        static Text OptionRow(Transform parent, string name, string label, float y, out Button minus, out Button plus)
        {
            Label(parent, name + "Label", label, 18, y - 4, 40, 8, 8, TextAnchor.MiddleLeft, Color.white);
            minus = MakeButton(parent, name + "Down", "-", "Button_Teal", 66, y, 12, 10, 8);
            Text value = Label(parent, name + "Value", "7", 72, y - 4, 12, 8, 8, TextAnchor.MiddleCenter, Yellow);
            plus = MakeButton(parent, name + "Up", "+", "Button_Teal", 90, y, 12, 10, 8);
            return value;
        }

        // ------------------------------------------------------------------ scene helpers

        static Camera CreateCameras()
        {
            // Clears the letterbox bars around the game viewport.
            GameObject letterbox = new GameObject("Letterbox Camera");
            Camera lb = letterbox.AddComponent<Camera>();
            lb.clearFlags = CameraClearFlags.SolidColor;
            lb.backgroundColor = Color.black;
            lb.cullingMask = 0;
            lb.depth = -100;
            lb.orthographic = true;

            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0f, 0f, -10f);
            Camera cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = PlayField.HalfHeight;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;
            cam.depth = 0;
            go.AddComponent<AudioListener>();
            go.AddComponent<PixelViewport>();
            return cam;
        }

        static void CreateEventSystem()
        {
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            InputSystemUIInputModule module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        static RectTransform CreateCanvas(string name, Camera cam, int sortingOrder)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 5f;
            canvas.sortingOrder = 100 + sortingOrder;
            go.AddComponent<PixelCanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            return (RectTransform)go.transform;
        }

        /// <summary>A sprite in the world, positioned by its GameMaker top-left corner in room pixels.</summary>
        static GameObject WorldSprite(string name, Sprite sprite, float x, float y, float w, float h, int order)
        {
            GameObject go = new GameObject(name);
            go.transform.position = PlayField.FromRoom(x + w * 0.5f, y + h * 0.5f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return go;
        }

        static RectTransform UI(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static RectTransform Panel(Transform parent, string name)
        {
            RectTransform rt = UI(name, parent);
            Stretch(rt, 0f);
            return rt;
        }

        static void Stretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        /// <summary>Places a rect by its top-left corner in room pixels (parent must be room-sized or top-left anchored).</summary>
        static void PlaceTL(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x + w * 0.5f, -(y + h * 0.5f));
            rt.sizeDelta = new Vector2(w, h);
        }

        static Image ImageTL(Transform parent, string name, Sprite sprite, float x, float y, float w, float h)
        {
            RectTransform rt = UI(name, parent);
            PlaceTL(rt, x, y, w, h);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        static Text Label(Transform parent, string name, string text, float x, float y, float w, float h, int size, TextAnchor anchor, Color color)
        {
            RectTransform rt = UI(name, parent);
            PlaceTL(rt, x, y, w, h);
            return ConfigureText(rt.gameObject.AddComponent<Text>(), text, size, anchor, color, true);
        }

        static Text ConfigureText(Text t, string text, int size, TextAnchor anchor, Color color, bool shadow)
        {
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            t.raycastTarget = false;
            if (shadow)
            {
                Shadow s = t.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0f, 0f, 0f, 0.9f);
                s.effectDistance = new Vector2(0.5f, -0.5f);
            }
            return t;
        }

        /// <summary>Button centred at (cx, cy) in room pixels, like GameMaker objects with a centred origin.</summary>
        /// <summary>Overlay canvas with the touch pad, weapon buttons and pause button (phones).</summary>
        static void BuildTouchControls(Camera cam, GameManager gm, PlayerController player)
        {
            GameObject go = new GameObject("TouchControls", typeof(RectTransform));
            go.layer = 5;
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            go.AddComponent<GraphicRaycaster>();
            TouchControls touch = go.AddComponent<TouchControls>();

            RectTransform root = UI("Root", go.transform);
            Stretch(root, 0f);

            RectTransform pad = UI("Pad", root);
            pad.anchorMin = new Vector2(0f, 0f);
            pad.anchorMax = new Vector2(1f, 0f);
            pad.pivot = new Vector2(0.5f, 0f);
            pad.anchoredPosition = Vector2.zero;
            pad.sizeDelta = new Vector2(0f, 420f);
            Image padBack = pad.gameObject.AddComponent<Image>();
            padBack.sprite = pixel;
            padBack.color = new Color(0f, 0f, 0f, 0.35f);
            padBack.raycastTarget = false;

            // Top row: weapons 1-2-3 and pause.
            string[] labels = { "1 PISTOL", "2 SHOTGUN", "3 MP7" };
            Image[] weaponImages = new Image[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                Button b = TouchButton(pad, "Weapon" + (i + 1), labels[i], 0.03f + 0.2f * i, 0.72f, 0.21f + 0.2f * i, 0.95f, 26);
                UnityEventTools.AddIntPersistentListener(b.onClick, touch.SelectWeapon, i);
                weaponImages[i] = (Image)b.targetGraphic;
            }
            Button pause = TouchButton(pad, "Pause", "II", 0.84f, 0.72f, 0.97f, 0.95f, 34);
            UnityEventTools.AddPersistentListener(pause.onClick, touch.Pause);

            // Left: < and > buttons (held).
            Image left = PadImage(pad, "Left", "<", 0.03f, 0.06f, 0.30f, 0.64f, new Color(0.2f, 0.2f, 0.2f, 0.85f), 64);
            Image right = PadImage(pad, "Right", ">", 0.33f, 0.06f, 0.60f, 0.64f, new Color(0.2f, 0.2f, 0.2f, 0.85f), 64);

            // Right: FIRE (held).
            Image fire = PadImage(pad, "Fire", "FIRE", 0.66f, 0.06f, 0.97f, 0.64f, new Color(0.55f, 0.08f, 0.08f, 0.9f), 44);

            Set(touch, "leftButton", left);
            Set(touch, "rightButton", right);
            Set(touch, "fireButton", fire);
            Set(touch, "root", root.gameObject);
            Set(touch, "pad", pad);
            Set(touch, "canvas", canvas);
            Set(touch, "weaponButtons", weaponImages);
            Set(touch, "player", player);
            Set(touch, "game", gm);
            Set(touch, "viewport", cam.GetComponent<PixelViewport>());
        }

        static Button TouchButton(Transform parent, string name, string label, float xMin, float yMin, float xMax, float yMax, int fontSize)
        {
            RectTransform rt = UI(name, parent);
            Anchor(rt, xMin, yMin, xMax, yMax);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = pixel;
            img.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            button.transition = Selectable.Transition.None;
            Navigation nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;

            RectTransform textRT = UI("Label", rt);
            Stretch(textRT, 0f);
            ConfigureText(textRT.gameObject.AddComponent<Text>(), label, fontSize, TextAnchor.MiddleCenter, Color.white, false);
            return button;
        }

        /// <summary>A held touch area (not a Button): TouchControls reads the fingers directly.</summary>
        static Image PadImage(Transform parent, string name, string label, float xMin, float yMin, float xMax, float yMax, Color color, int fontSize)
        {
            RectTransform rt = UI(name, parent);
            Anchor(rt, xMin, yMin, xMax, yMax);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = pixel;
            img.color = color;
            img.raycastTarget = false;
            RectTransform textRT = UI("Label", rt);
            Stretch(textRT, 0f);
            ConfigureText(textRT.gameObject.AddComponent<Text>(), label, fontSize, TextAnchor.MiddleCenter, Color.white, false);
            return img;
        }

        static void Anchor(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
        {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void Variant(GameObject go, PlatformVariant.Mode mode, string touchText = "")
        {
            PlatformVariant v = go.AddComponent<PlatformVariant>();
            Set(v, "mode", (int)mode);
            Set(v, "touchText", touchText);
        }

        static Button MakeButton(Transform parent, string name, string label, string spriteName, float cx, float cy,
            float width = 80f, float height = 16f, int fontSize = 8)
        {
            RectTransform rt = UI(name, parent);
            PlaceTL(rt, cx - width * 0.5f, cy - height * 0.5f, width, height);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = SpriteAt("UI/" + spriteName);

            Button button = rt.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = img;
            rt.gameObject.AddComponent<UIButtonFeedback>();

            RectTransform textRT = UI("Label", rt);
            Stretch(textRT, 0f);
            ConfigureText(textRT.gameObject.AddComponent<Text>(), label, fontSize, TextAnchor.MiddleCenter, Color.white, true);
            return button;
        }

        static InputField MakeInputField(Transform parent, string name, float x, float y, float w, float h)
        {
            RectTransform rt = UI(name, parent);
            PlaceTL(rt, x, y, w, h);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = pixel;
            img.color = new Color(0f, 0f, 0f, 0.75f);

            RectTransform textRT = UI("Text", rt);
            Stretch(textRT, 1.5f);
            Text text = ConfigureText(textRT.gameObject.AddComponent<Text>(), string.Empty, 6, TextAnchor.MiddleLeft, Yellow, false);
            text.supportRichText = false;

            RectTransform phRT = UI("Placeholder", rt);
            Stretch(phRT, 1.5f);
            Text placeholder = ConfigureText(phRT.gameObject.AddComponent<Text>(), "TYPE NAME", 6, TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.35f), false);

            InputField input = rt.gameObject.AddComponent<InputField>();
            input.targetGraphic = img;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.lineType = InputField.LineType.SingleLine;
            input.characterLimit = 10;
            input.customCaretColor = true;
            input.caretColor = Color.white;
            input.selectionColor = new Color(0.83f, 0.94f, 0f, 0.4f);
            return input;
        }

        // ------------------------------------------------------------------ asset helpers

        static Sprite SpriteAt(string relativePath)
        {
            string path = SpritesDir + "/" + relativePath + ".png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogWarning("[Last Grounds] Sprite not found: " + path);
            return sprite;
        }

        static Sprite[] FramesIn(string relativeFolder)
        {
            string folder = SpritesDir + "/" + relativeFolder;
            List<string> paths = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { folder }))
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            paths.Sort(StringComparer.Ordinal);

            List<Sprite> sprites = new List<Sprite>();
            foreach (string p in paths)
            {
                Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                if (s != null) sprites.Add(s);
            }
            return sprites.ToArray();
        }

        static AudioClip Clip(string relativePath)
        {
            string path = AudioDir + "/" + relativePath;
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) Debug.LogWarning("[Last Grounds] Audio not found: " + path);
            return clip;
        }

        /// <summary>
        /// Loads a data asset from disk. Always reload after EditorSceneManager.NewScene:
        /// references held from before point to unloaded objects and serialize as null.
        /// </summary>
        static T Data<T>(string fileName) where T : ScriptableObject
        {
            string path = DataDir + "/" + fileName + ".asset";
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException("Data asset missing or unreadable: " + path);
            return asset;
        }

        /// <summary>Loads or creates a ScriptableObject under Data/, keeping its GUID across rebuilds.</summary>
        static T Asset<T>(string fileName, Action<T> init) where T : ScriptableObject
        {
            string path = DataDir + "/" + fileName + ".asset";
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                init(asset);
                AssetDatabase.CreateAsset(asset, path);
            }
            else
            {
                init(asset);
                EditorUtility.SetDirty(asset);
            }
            return asset;
        }

        static GameObject SavePrefab(GameObject go, string name)
        {
            string path = PrefabDir + "/" + name + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        /// <summary>Sets a (possibly private) serialized field.</summary>
        static void Set(Object target, string field, object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogError("[Last Grounds] " + target.GetType().Name + " has no serialized field '" + field + "'.");
                return;
            }

            if (value == null) p.objectReferenceValue = null;
            else if (value is Object) p.objectReferenceValue = (Object)value;
            else if (value is int) p.intValue = (int)value;
            else if (value is float) p.floatValue = (float)value;
            else if (value is bool) p.boolValue = (bool)value;
            else if (value is string) p.stringValue = (string)value;
            else if (value is Color) p.colorValue = (Color)value;
            else if (value is Vector2) p.vector2Value = (Vector2)value;
            else if (value is Vector3) p.vector3Value = (Vector3)value;
            else if (value is Object[])
            {
                Object[] items = (Object[])value;
                p.arraySize = items.Length;
                for (int i = 0; i < items.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
            else Debug.LogError("[Last Grounds] Unsupported value type for '" + field + "': " + value.GetType());

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
