using UnityEngine;

namespace LastGrounds
{
    /// <summary>
    /// Police zombie: shoots at the barricade. In the original the shooting code
    /// was commented out and the zombie was never spawned; it is now enabled.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public class EnemyShooter : MonoBehaviour
    {
        [SerializeField] Projectile projectilePrefab;
        [SerializeField, Min(0)] int damage = 5;
        [SerializeField, Min(0f)] float projectileSpeed = 12f;
        [SerializeField, Min(0.1f)] float minInterval = 1.5f;
        [SerializeField, Min(0.1f)] float maxInterval = 3f;
        [SerializeField] Vector2 muzzleOffset = new Vector2(0f, -0.4f);
        [SerializeField] AudioClip shotSound;
        [SerializeField, Range(0f, 1f)] float shotVolume = 0.5f;

        Enemy enemy;
        float timer;

        void Awake()
        {
            enemy = GetComponent<Enemy>();
        }

        void OnEnable()
        {
            timer = Random.Range(minInterval, maxInterval);
        }

        void Update()
        {
            if (!enemy.IsAlive || projectilePrefab == null) return;
            // Only shoot once on screen.
            if (transform.position.y > PlayField.HalfHeight - 0.5f) return;

            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = Random.Range(minInterval, Mathf.Max(minInterval, maxInterval));

            Vector3 origin = transform.position + (Vector3)muzzleOffset;
            Projectile bullet = PrefabPool.Spawn(projectilePrefab, origin);
            bullet.Launch(Vector2.down, projectileSpeed, damage);
            if (shotSound != null) AudioManager.PlaySfx(shotSound, shotVolume, 0.75f);
        }
    }
}
