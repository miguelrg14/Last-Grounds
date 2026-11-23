using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastGrounds
{
    /// <summary>
    /// Runs one game: score, timer, phases, pause, and the end of the run.
    /// The mode comes from <see cref="GameSession"/>, so a single Game scene
    /// replaces the five HordaDificultadX rooms and Level1.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        enum State { Playing, Paused, Ended }

        public static GameManager Instance { get; private set; }

        [Tooltip("Used when the Game scene is played directly from the editor.")]
        [SerializeField] GameModeData defaultMode;
        [SerializeField] Barricade barricade;
        [SerializeField] EnemySpawner spawner;
        [SerializeField] PlayerController player;
        [SerializeField] HUD hud;
        [SerializeField] GameObject pausePanel;
        [SerializeField] Selectable pauseFirstSelected;
        [SerializeField] AudioClip music;
        [SerializeField, Min(0f)] float defeatDelay = 1.5f;
        [SerializeField, Min(0f)] float victoryDelay = 3f;

        public event Action<int> ScoreChanged;
        public event Action<SpawnPhase> PhaseStarted;

        State state;
        int phaseIndex = -1;

        public GameModeData Mode { get; private set; }
        public int Score { get; private set; }
        public float Elapsed { get; private set; }
        public bool IsPlaying { get { return state == State.Playing; } }

        void Awake()
        {
            Instance = this;
            Mode = GameSession.CurrentMode != null ? GameSession.CurrentMode : defaultMode;
            GameSession.CurrentMode = Mode;
        }

        void OnEnable()
        {
            Enemy.Killed += OnEnemyKilled;
            if (barricade != null) barricade.Destroyed += OnBarricadeDestroyed;
        }

        void OnDisable()
        {
            Enemy.Killed -= OnEnemyKilled;
            if (barricade != null) barricade.Destroyed -= OnBarricadeDestroyed;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }

        void Start()
        {
            Time.timeScale = 1f;
            state = State.Playing;
            if (pausePanel != null) pausePanel.SetActive(false);

            barricade.Configure(Mode);
            if (hud != null) hud.Bind(this, barricade, player);
            spawner.Begin(Mode, () => Elapsed);
            AudioManager.PlayMusic(music);
            if (ScoreChanged != null) ScoreChanged(Score);
            CheckPhase();
        }

        void Update()
        {
            if (state != State.Ended && GameInput.PausePressed())
            {
                if (state == State.Paused) Resume();
                else Pause();
            }

            if (state == State.Paused)
            {
                UISelection.Ensure(pauseFirstSelected);
                return;
            }
            if (state != State.Playing) return;

            Elapsed += Time.deltaTime;
            CheckPhase();

            if (!Mode.IsEndless && Elapsed >= Mode.survivalTime) EndRun(true);
        }

        void CheckPhase()
        {
            int index;
            SpawnPhase phase = Mode.GetPhase(Elapsed, out index);
            if (index == phaseIndex || phase == null) return;
            phaseIndex = index;
            if (PhaseStarted != null) PhaseStarted(phase);
        }

        void OnEnemyKilled(Enemy enemy)
        {
            if (state == State.Ended || enemy.Data == null) return;
            Score += enemy.Data.scoreValue;
            if (ScoreChanged != null) ScoreChanged(Score);
        }

        void OnBarricadeDestroyed()
        {
            if (state != State.Ended) EndRun(false);
        }

        public void Pause()
        {
            if (state != State.Playing) return;
            state = State.Paused;
            Time.timeScale = 0f;
            if (pausePanel != null) pausePanel.SetActive(true);
            UISelection.SelectNextFrame(this, pauseFirstSelected);
        }

        public void Resume()
        {
            if (state != State.Paused) return;
            state = State.Playing;
            Time.timeScale = 1f;
            if (pausePanel != null) pausePanel.SetActive(false);
            UISelection.Clear();
        }

        public void QuitToMenu()
        {
            Time.timeScale = 1f;
            GameSession.SkipTitle = true;
            SceneManager.LoadScene(SceneNames.Menu);
        }

        void EndRun(bool victory)
        {
            state = State.Ended;
            spawner.StopSpawning();

            GameSession.LastResult = new RunResult
            {
                mode = Mode,
                score = Score,
                time = Elapsed,
                victory = victory,
            };

            if (victory)
            {
                // Rescue arrives: clear the field.
                for (int i = Enemy.AliveEnemies.Count - 1; i >= 0; i--) Enemy.AliveEnemies[i].Vanish();
                if (hud != null) hud.ShowBanner(Mode.victoryMessage, victoryDelay);
            }

            StartCoroutine(LoadGameOver(victory ? victoryDelay : defeatDelay));
        }

        IEnumerator LoadGameOver(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            SceneManager.LoadScene(SceneNames.GameOver);
        }
    }
}
