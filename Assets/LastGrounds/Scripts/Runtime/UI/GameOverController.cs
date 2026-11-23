using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastGrounds
{
    /// <summary>
    /// Game over / victory screen: enter a name, see the mode's top 10, C clears it,
    /// Esc returns to the menu. One scene replaces the five MenuGameOverX rooms.
    /// </summary>
    public class GameOverController : MonoBehaviour
    {
        const string NameKey = "player.lastName";

        [SerializeField] GameModeData fallbackMode;
        [SerializeField] Image background;
        [SerializeField] GameObject victoryHeader;
        [SerializeField] Text resultText;
        [SerializeField] GameObject entryRoot;
        [SerializeField] InputField nameInput;
        [SerializeField] Text namesColumn;
        [SerializeField] Text scoresColumn;
        [SerializeField] Selectable menuButton;
        [SerializeField] Color highlightColor = new Color(0.83f, 0.94f, 0f, 1f);
        [SerializeField, Min(1)] int maxNameLength = 10;

        [Header("Audio")]
        [SerializeField] AudioClip defeatMusic;
        [SerializeField] AudioClip defeatSting;
        [SerializeField] AudioClip victoryMusic;

        RunResult result;
        GameModeData mode;
        int highlightRank = -1;

        bool EntryOpen { get { return entryRoot != null && entryRoot.activeSelf; } }

        void Start()
        {
            Time.timeScale = 1f;
            result = GameSession.LastResult;
            mode = result != null && result.mode != null ? result.mode
                : (GameSession.CurrentMode != null ? GameSession.CurrentMode : fallbackMode);

            bool victory = result != null && result.victory;
            if (victoryHeader != null) victoryHeader.SetActive(victory);
            if (victory && background != null)
            {
                // The game-over art has "GAME OVER" painted in; use plain black instead.
                background.sprite = null;
                background.color = Color.black;
            }

            if (victory) AudioManager.PlayMusic(victoryMusic);
            else
            {
                AudioManager.PlayMusic(defeatMusic);
                if (result != null) AudioManager.PlaySfx(defeatSting);
            }

            if (resultText != null)
            {
                string modeName = mode != null ? mode.displayName : string.Empty;
                resultText.text = result != null
                    ? modeName + "   " + result.score + " PTS   " + GameSession.FormatTime(result.time)
                    : modeName;
            }

            bool canRecord = result != null && !result.recorded && mode != null;
            if (entryRoot != null) entryRoot.SetActive(canRecord);
            if (nameInput != null)
            {
                nameInput.characterLimit = maxNameLength;
                nameInput.text = PlayerPrefs.GetString(NameKey, string.Empty);
                nameInput.onValidateInput = ValidateChar;
            }

            Refresh();
            UISelection.SelectNextFrame(this, canRecord ? (Selectable)nameInput : menuButton);
        }

        void Update()
        {
            if (EntryOpen)
            {
                // Enter ends editing in the InputField and saves.
                if (GameInput.SubmitPressed()) SaveEntry();
                else if (GameInput.BackPressed() && !nameInput.isFocused) BackToMenu();
                return;
            }

            if (GameInput.BackPressed())
            {
                BackToMenu();
                return;
            }

            if (GameInput.ClearPressed() && mode != null)
            {
                Leaderboard.Clear(mode.id);
                highlightRank = -1;
                Refresh();
            }

            UISelection.Ensure(menuButton);
        }

        static char ValidateChar(string text, int index, char c)
        {
            if (char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_') return char.ToUpperInvariant(c);
            return '\0';
        }

        public void SaveEntry()
        {
            if (!EntryOpen || result == null || mode == null) return;

            string playerName = nameInput != null ? nameInput.text.Trim() : string.Empty;
            if (playerName.Length == 0) playerName = "SURVIVOR";
            PlayerPrefs.SetString(NameKey, playerName);

            highlightRank = Leaderboard.Add(mode.id, playerName, result.score, result.time);
            result.recorded = true;
            entryRoot.SetActive(false);
            Refresh();
            UISelection.SelectNextFrame(this, menuButton);
        }

        public void BackToMenu()
        {
            GameSession.SkipTitle = true;
            SceneManager.LoadScene(SceneNames.Menu);
        }

        void Refresh()
        {
            if (namesColumn == null || scoresColumn == null) return;
            List<LeaderboardEntry> entries = mode != null ? Leaderboard.Load(mode.id) : new List<LeaderboardEntry>();

            if (entries.Count == 0)
            {
                namesColumn.text = "NO SCORES YET";
                scoresColumn.text = string.Empty;
                return;
            }

            string hex = ColorUtility.ToHtmlStringRGB(highlightColor);
            StringBuilder names = new StringBuilder();
            StringBuilder scores = new StringBuilder();
            for (int i = 0; i < entries.Count; i++)
            {
                string open = i == highlightRank ? "<color=#" + hex + ">" : string.Empty;
                string close = i == highlightRank ? "</color>" : string.Empty;
                names.Append(open).Append(i + 1).Append(". ").Append(entries[i].name).Append(close).Append('\n');
                scores.Append(open).Append(entries[i].score).Append(close).Append('\n');
            }
            namesColumn.text = names.ToString();
            scoresColumn.text = scores.ToString();
        }
    }
}
