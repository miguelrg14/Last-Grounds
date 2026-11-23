using UnityEngine;

namespace LastGrounds
{
    /// <summary>
    /// Bullet fired by the player (hits zombies) or by the police zombie (hits the barricade).
    /// Uses a swept box cast each frame so fast bullets never tunnel through a zombie.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        public enum Team { Player, Enemy }

        [SerializeField] Team team = Team.Player;
        [SerializeField] Vector2 hitboxSize = new Vector2(0.125f, 0.125f);
        [SerializeField] Vector2 hitboxOffset = Vector2.zero;

        Vector2 direction = Vector2.up;
        float speed;
        int damage;
        bool live;

        public void Launch(Vector2 dir, float projectileSpeed, int projectileDamage)
        {
            direction = dir.normalized;
            speed = projectileSpeed;
            damage = projectileDamage;
            live = true;
        }

        void OnDisable()
        {
            live = false;
        }

        void Update()
        {
            if (!live) return;

            float distance = speed * Time.deltaTime;
            Vector2 position = transform.position;
            RaycastHit2D[] hits = Physics2D.BoxCastAll(position + hitboxOffset, hitboxSize, 0f, direction, distance);

            for (int i = 0; i < hits.Length; i++)
            {
                if (TryHit(hits[i].collider))
                {
                    Despawn();
                    return;
                }
            }

            position += direction * distance;
            transform.position = position;
            if (Mathf.Abs(position.y) > PlayField.OutOfBoundsY) Despawn();
        }

        bool TryHit(Collider2D other)
        {
            if (team == Team.Player)
            {
                Enemy enemy = other.GetComponentInParent<Enemy>();
                if (enemy == null || !enemy.IsAlive) return false;
                enemy.TakeDamage(damage);
                return true;
            }

            Barricade barricade = other.GetComponentInParent<Barricade>();
            if (barricade == null || barricade.IsDestroyed) return false;
            barricade.TakeDamage(damage);
            return true;
        }

        void Despawn()
        {
            live = false;
            PrefabPool.Despawn(gameObject);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = team == Team.Player ? Color.yellow : Color.red;
            Gizmos.DrawWireCube((Vector2)transform.position + hitboxOffset, hitboxSize);
        }
    }
}
