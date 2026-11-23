using UnityEngine;
using UnityEngine.UI;

namespace LastGrounds
{
    /// <summary>Music / SFX volume and fullscreen. The original Options room was empty.</summary>
    public class OptionsPanel : MonoBehaviour
    {
        [SerializeField] Text musicValue;
        [SerializeField] Text sfxValue;
        [SerializeField] Text fullscreenLabel;
        [SerializeField] AudioClip sfxPreview;

        void OnEnable()
        {
            Refresh();
        }

        public void ChangeMusic(int steps)
        {
            GameSettings.MusicVolume += steps * 0.1f;
            Refresh();
        }

        public void ChangeSfx(int steps)
        {
            GameSettings.SfxVolume += steps * 0.1f;
            if (sfxPreview != null) AudioManager.PlaySfx(sfxPreview);
            Refresh();
        }

        public void ToggleFullscreen()
        {
            GameSettings.Fullscreen = !GameSettings.Fullscreen;
            // Screen.fullScreen applies at the end of the frame, so show the requested state.
            if (fullscreenLabel != null) fullscreenLabel.text = "FULLSCREEN " + (!Screen.fullScreen ? "ON" : "OFF");
        }

        void Refresh()
        {
            if (musicValue != null) musicValue.text = Mathf.RoundToInt(GameSettings.MusicVolume * 10f).ToString();
            if (sfxValue != null) sfxValue.text = Mathf.RoundToInt(GameSettings.SfxVolume * 10f).ToString();
            if (fullscreenLabel != null) fullscreenLabel.text = "FULLSCREEN " + (Screen.fullScreen ? "ON" : "OFF");
        }
    }
}
