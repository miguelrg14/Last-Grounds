using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastGrounds
{
    /// <summary>
    /// A zombie: walks down its lane until it reaches the barricade, then bites it.
    /// One class for every zombie type; the original duplicated the movement and
    /// damage code in every Zombi_X object and every bullet collision event.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
    public class Enemy : MonoBehaviour
    {
        enum State { Walking, Attacking, Dead }

        /// <summary>Raised when any zombie dies from damage.</summary>
        public static event Action<Enemy> Killed;

        static readonly List<Enemy> Alive = new List<Enemy>();
        public static IReadOnlyList<Enemy> AliveEnemies { get { return Alive; } }

        [SerializeField] EnemyData data;
        [SerializeField] Color hitColor = new Color(1f, 0.35f, 0.35f, 1f);
        [SerializeField, Min(0f)] float hitFlashTime = 0.07f;

        public event Action<Enemy> HealthChanged;

        SpriteRenderer spriteRenderer;
        BoxCollider2D box;
        State state;
        float flashTimer;

        public EnemyData Data { get { return data; } }
        public float Health { get; private set; }
        public float MaxHealth { get { return data != null ? data.maxHealth : 1f; } }
        public bool IsAlive { get { return state != State.Dead; } }
        public bool IsWalking { get { return state == State.Walking; } }
        public bool IsAttacking { get { return state == State.Attacking; } }

        /// <summary>Distance from the pivot to the bottom of the hitbox (negative).</summary>
        float BottomOffset { get { return box.offset.y - box.size.y * 0.5f; } }

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            box = GetComponent<BoxCollider2D>();
        }

        void OnEnable()
        {
            Health = MaxHealth;
            state = State.Walking;
            Alive.Add(this);
            if (data != null && data.spawnSound != null) AudioManager.PlaySfx(data.spawnSound, data.soundVolume);
            if (HealthChanged != null) HealthChanged(this);
        }

        void OnDisable()
        {
            Alive.Remove(this);
        }

        void Update()
        {
            if (state == State.Walking) Walk();
            else if (state == State.Attacking) Attack();

            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                if (flashTimer <= 0f) spriteRenderer.color = Color.white;
            }
        }

        void Walk()
        {
            Vector3 position = transform.position;
            position.y -= data.speed * Time.deltaTime;

            Barricade barricade = Barricade.Instance;
            if (barricade != null && !barricade.IsDestroyed)
            {
                float stopY = barricade.TopY - BottomOffset;
                if (position.y <= stopY)
                {
                    position.y = stopY;
                    state = State.Attacking;
                }
            }

            transform.position = position;
            if (position.y < -PlayField.OutOfBoundsY) Destroy(gameObject);
        }

        void Attack()
        {
            Barricade barricade = Barricade.Instance;
            if (barricade != null) barricade.TryBite();
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f) return;
            Health = Mathf.Max(0f, Health - amount);
            spriteRenderer.color = hitColor;
            flashTimer = hitFlashTime;
            if (HealthChanged != null) HealthChanged(this);
            if (Health <= 0f) Die();
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f || Health >= MaxHealth) return;
            Health = Mathf.Min(MaxHealth, Health + amount);
            if (HealthChanged != null) HealthChanged(this);
        }

        public void TeleportToX(float x)
        {
            Vector3 position = transform.position;
            position.x = x;
            transform.position = position;
        }

        void Die()
        {
            state = State.Dead;
            if (data != null && data.deathSound != null) AudioManager.PlaySfx(data.deathSound, data.soundVolume);
            if (Killed != null) Killed(this);
            Destroy(gameObject);
        }

        /// <summary>Removes the zombie without awarding points (used when a run ends).</summary>
        public void Vanish()
        {
            state = State.Dead;
            Destroy(gameObject);
        }
    }
}
