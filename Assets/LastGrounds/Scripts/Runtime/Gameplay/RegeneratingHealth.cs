using UnityEngine;

namespace LastGrounds
{
    /// <summary>Scientist zombie: heals over time (original: +1 HP every frame).</summary>
    [RequireComponent(typeof(Enemy))]
    public class RegeneratingHealth : MonoBehaviour
    {
        [SerializeField, Min(0f)] float healthPerSecond = 60f;

        Enemy enemy;

        void Awake()
        {
            enemy = GetComponent<Enemy>();
        }

        void Update()
        {
            if (enemy.IsAlive) enemy.Heal(healthPerSecond * Time.deltaTime);
        }
    }
}
