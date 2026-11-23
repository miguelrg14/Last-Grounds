using UnityEngine;
using UnityEngine.UI;

namespace LastGrounds
{
    /// <summary>Score, time, weapon and barricade health, plus the phase banner.</summary>
    public class HUD : MonoBehaviour
    {
        [SerializeField] Text scoreText;
        [SerializeField] Text timeText;
        [SerializeField] Text weaponText;
        [SerializeField] Text bannerText;
        [SerializeField] Image barricadeFill;
        [Tooltip("Original barrier bar went from olive (empty) to aqua (full).")]
        [SerializeField] Color barricadeLow = new Color(0.5f, 0.5f, 0f, 1f);
        [SerializeField] Color barricadeHigh = new Color(0f, 1f, 1f, 1f);
        [SerializeField, Min(0.1f)] float bannerDuration = 2.5f;

        GameManager game;
        PlayerController player;
        float bannerTimer;
        float bannerLength = 1f;

        public void Bind(GameManager gameManager, Barricade barricade, PlayerController playerController)
        {
            game = gameManager;
            player = playerController;

            game.ScoreChanged += OnScoreChanged;
            game.PhaseStarted += OnPhaseStarted;
            barricade.HealthChanged += OnBarricadeChanged;
            if (player != null)
            {
                player.WeaponChanged += OnWeaponChanged;
                if (player.CurrentWeapon != null) OnWeaponChanged(player.CurrentWeapon, player.WeaponIndex);
            }

            OnScoreChanged(game.Score);
            OnBarricadeChanged(barricade.Health, barricade.MaxHealth);
            if (bannerText != null) bannerText.enabled = false;
        }

        void OnDestroy()
        {
            if (game != null)
            {
                game.ScoreChanged -= OnScoreChanged;
                game.PhaseStarted -= OnPhaseStarted;
            }
            if (player != null) player.WeaponChanged -= OnWeaponChanged;
            if (Barricade.Instance != null) Barricade.Instance.HealthChanged -= OnBarricadeChanged;
        }

        void Update()
        {
            if (game != null && timeText != null)
            {
                timeText.text = game.Mode.IsEndless
                    ? "TIME " + GameSession.FormatTime(game.Elapsed)
                    : "RESCUE " + GameSession.FormatTime(game.Mode.survivalTime - game.Elapsed);
            }

            if (bannerText != null && bannerTimer > 0f)
            {
                bannerTimer -= Time.unscaledDeltaTime;
                Color c = bannerText.color;
                // Fade out during the last third.
                c.a = Mathf.Clamp01(bannerTimer / (bannerLength / 3f));
                bannerText.color = c;
                if (bannerTimer <= 0f) bannerText.enabled = false;
            }
        }

        public void ShowBanner(string message, float duration)
        {
            if (bannerText == null || string.IsNullOrEmpty(message)) return;
            bannerText.text = message;
            bannerText.enabled = true;
            bannerLength = Mathf.Max(0.1f, duration);
            bannerTimer = bannerLength;
            Color c = bannerText.color;
            c.a = 1f;
            bannerText.color = c;
        }

        void OnPhaseStarted(SpawnPhase phase)
        {
            ShowBanner(phase.label, bannerDuration);
        }

        void OnScoreChanged(int score)
        {
            if (scoreText != null) scoreText.text = "SCORE " + score;
        }

        void OnWeaponChanged(WeaponData weapon, int index)
        {
            if (weaponText != null) weaponText.text = (index + 1) + " " + weapon.displayName;
        }

        void OnBarricadeChanged(int current, int max)
        {
            if (barricadeFill == null) return;
            float ratio = max > 0 ? (float)current / max : 0f;
            barricadeFill.fillAmount = ratio;
            barricadeFill.color = Color.Lerp(barricadeLow, barricadeHigh, ratio);
        }
    }
}
