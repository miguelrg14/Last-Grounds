using System;
using UnityEngine;

namespace LastGrounds
{
    /// <summary>Player options stored in PlayerPrefs.</summary>
    public static class GameSettings
    {
        const string MusicKey = "settings.musicVolume";
        const string SfxKey = "settings.sfxVolume";

        public static event Action Changed;

        public static float MusicVolume
        {
            get { return PlayerPrefs.GetFloat(MusicKey, 0.7f); }
            set { Store(MusicKey, value); }
        }

        public static float SfxVolume
        {
            get { return PlayerPrefs.GetFloat(SfxKey, 0.8f); }
            set { Store(SfxKey, value); }
        }

        public static bool Fullscreen
        {
            get { return Screen.fullScreen; }
            set
            {
                Screen.fullScreen = value;
                if (Changed != null) Changed();
            }
        }

        static void Store(string key, float value)
        {
            // Snap to steps of 10% so repeated +/- never drift.
            PlayerPrefs.SetFloat(key, Mathf.Round(Mathf.Clamp01(value) * 10f) / 10f);
            PlayerPrefs.Save();
            if (Changed != null) Changed();
        }
    }
}
