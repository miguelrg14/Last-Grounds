using UnityEngine;

namespace LastGrounds
{
    public static class SceneNames
    {
        public const string Menu = "Menu";
        public const string Game = "Game";
        public const string GameOver = "GameOver";
    }

    /// <summary>Result of a finished run, handed from the Game scene to the GameOver scene.</summary>
    public class RunResult
    {
        public GameModeData mode;
        public int score;
        public float time;
        public bool victory;
        public bool recorded;
    }

    /// <summary>State that survives scene loads (replaces GameMaker's globals and room_goto chain).</summary>
    public static class GameSession
    {
        public static GameModeData CurrentMode;
        public static RunResult LastResult;

        /// <summary>When true the menu opens on the main panel instead of the title screen.</summary>
        public static bool SkipTitle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            CurrentMode = null;
            LastResult = null;
            SkipTitle = false;
        }

        public static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return string.Format("{0:00}:{1:00}", total / 60, total % 60);
        }
    }
}
