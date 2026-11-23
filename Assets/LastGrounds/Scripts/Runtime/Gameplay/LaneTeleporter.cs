using UnityEngine;

namespace LastGrounds
{
    /// <summary>
    /// Scientist zombie: jumps to a random lane every few seconds while walking.
    /// The original intended this (Zombi_Cientifico Alarm_0) but the alarm was reset
    /// every step, so it never fired.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public class LaneTeleporter : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] float interval = 3f;
        [SerializeField, Min(0f)] float blinkTime = 0.25f;

        Enemy enemy;
        SpriteRenderer spriteRenderer;
        float timer;
        float blinkTimer;

        void Awake()
        {
            enemy = GetComponent<Enemy>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        void OnEnable()
        {
            timer = interval;
            blinkTimer = 0f;
        }

        void Update()
        {
            if (blinkTimer > 0f)
            {
                blinkTimer -= Time.deltaTime;
                spriteRenderer.enabled = blinkTimer <= 0f || Mathf.Repeat(blinkTimer, 0.1f) > 0.05f;
            }

            if (!enemy.IsWalking) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = interval;

            int current = PlayField.NearestLane(transform.position.x);
            int lane = Random.Range(0, PlayField.LaneCount - 1);
            if (lane >= current) lane++;
            enemy.TeleportToX(PlayField.LaneX(lane));
            blinkTimer = blinkTime;
        }

        void OnDisable()
        {
            if (spriteRenderer != null) spriteRenderer.enabled = true;
        }
    }
}
