using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastGrounds
{
    /// <summary>
    /// All menu screens live as panels in one scene (the original used a room per screen:
    /// Presentacion, Menu, MenuSinMusica, MenuSeleccionJuego, MenuSeleccionHorda, ...).
    /// </summary>
    public class MenuController : MonoBehaviour
    {
        [Serializable]
        public class Panel
        {
            public string id;
            public GameObject root;
            public Selectable firstSelected;
            [Tooltip("Panel opened by Esc. Empty = Esc does nothing.")]
            public string backTo;
        }

        [SerializeField] Panel[] panels = new Panel[0];
        [SerializeField] string titlePanel = "Title";
        [SerializeField] string mainPanel = "Main";
        [SerializeField] AudioClip menuMusic;
        [SerializeField] GameModeData storyMode;

        Panel current;

        void Start()
        {
            Time.timeScale = 1f;
            string first = GameSession.SkipTitle ? mainPanel : titlePanel;
            GameSession.SkipTitle = false;
            ShowPanel(first);
        }

        void Update()
        {
            if (current == null) return;

            if (current.id == titlePanel)
            {
                if (GameInput.AnyPressed()) ShowPanel(mainPanel);
                return;
            }

            if (GameInput.BackPressed() && !string.IsNullOrEmpty(current.backTo))
            {
                ShowPanel(current.backTo);
                return;
            }

            UISelection.Ensure(current.firstSelected);
        }

        public void ShowPanel(string id)
        {
            Panel target = null;
            for (int i = 0; i < panels.Length; i++)
            {
                bool match = panels[i].id == id;
                if (panels[i].root != null) panels[i].root.SetActive(match);
                if (match) target = panels[i];
            }

            if (target == null)
            {
                Debug.LogWarning("[MenuController] Unknown panel '" + id + "'.", this);
                return;
            }

            current = target;
            // The title screen is silent, like the original Presentacion room.
            if (id != titlePanel) AudioManager.PlayMusic(menuMusic);
            UISelection.Clear();
            UISelection.SelectNextFrame(this, target.firstSelected);
        }

        public void StartMode(GameModeData mode)
        {
            if (mode == null)
            {
                Debug.LogError("[Last Grounds] This button has no GameModeData. Run 'Last Grounds > Rebuild Project'.", this);
                return;
            }
            GameSession.CurrentMode = mode;
            GameSession.LastResult = null;
            SceneManager.LoadScene(SceneNames.Game);
        }

        public void StartStory()
        {
            StartMode(storyMode);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
