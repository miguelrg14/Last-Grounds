using System;
using UnityEngine;

namespace LastGrounds
{
    /// <summary>
    /// The barricade zombies attack. When it falls, the run is over.
    /// Bites share one cooldown, exactly like the original barrier objects.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
    public class Barricade : MonoBehaviour
    {
        public static Barricade Instance { get; private set; }

        [SerializeField] AudioClip biteSound;
        [SerializeField, Range(0f, 1f)] float biteVolume = 1f;
        [SerializeField] Color damagedTint = new Color(0.55f, 0.45f, 0.45f, 1f);
        [SerializeField] Color hitColor = new Color(1f, 0.4f, 0.4f, 1f);
        [SerializeField, Min(0f)] float hitFlashTime = 0.1f;

        /// <summary>(current, max)</summary>
        public event Action<int, int> HealthChanged;
        public event Action Destroyed;

        SpriteRenderer spriteRenderer;
        BoxCollider2D box;
        int biteDamage = 20;
        float biteCooldown = 100f / 60f;
        float nextBiteTime;
        float flashTimer;

        public int Health { get; private set; }
        public int MaxHealth { get; private set; }
        public bool IsDestroyed { get { return Health <= 0; } }

        /// <summary>World Y of the top edge, where zombies stop.</summary>
        public float TopY { get { return transform.position.y + box.offset.y + box.size.y * 0.5f; } }

        void Awake()
        {
            Instance = this;
            spriteRenderer = GetComponent<SpriteRenderer>();
            box = GetComponent<BoxCollider2D>();
            MaxHealth = Health = 100;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Configure(GameModeData mode)
        {
            MaxHealth = Health = Mathf.Max(1, mode.barricadeHealth);
            biteDamage = mode.biteDamage;
            biteCooldown = mode.biteCooldown;
            nextBiteTime = 0f;
            RefreshTint();
            if (HealthChanged != null) HealthChanged(Health, MaxHealth);
        }

        /// <summary>Called every frame by each zombie touching the barricade.</summary>
        public void TryBite()
        {
            if (IsDestroyed || Time.time < nextBiteTime) return;
            nextBiteTime = Time.time + biteCooldown;
            if (biteSound != null) AudioManager.PlaySfx(biteSound, biteVolume, UnityEngine.Random.Range(0.9f, 1.1f));
            TakeDamage(biteDamage);
        }

        public void TakeDamage(int amount)
        {
            if (IsDestroyed || amount <= 0) return;
            if (CaptureTools.InvincibleBarricade) amount = 0;
            Health = Mathf.Max(0, Health - amount);
            flashTimer = hitFlashTime;
            spriteRenderer.color = hitColor;
            if (HealthChanged != null) HealthChanged(Health, MaxHealth);
            if (IsDestroyed && Destroyed != null) Destroyed();
        }

        void Update()
        {
            if (flashTimer <= 0f) return;
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f) RefreshTint();
        }

        void RefreshTint()
        {
            float ratio = MaxHealth > 0 ? (float)Health / MaxHealth : 0f;
            spriteRenderer.color = Color.Lerp(damagedTint, Color.white, ratio);
        }
    }
}
