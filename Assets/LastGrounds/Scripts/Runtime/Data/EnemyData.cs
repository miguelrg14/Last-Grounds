using UnityEngine;

namespace LastGrounds
{
    /// <summary>Stats of one zombie type. Special behaviours live in extra components on the prefab.</summary>
    [CreateAssetMenu(fileName = "Enemy", menuName = "Last Grounds/Enemy")]
    public class EnemyData : ScriptableObject
    {
        public string displayName = "Zombie";

        [Min(1)] public int maxHealth = 125;

        [Tooltip("World units per second downwards (original: px per frame * 60 / 16).")]
        [Min(0f)] public float speed = 0.75f;

        [Tooltip("Points awarded on kill.")]
        [Min(0)] public int scoreValue = 2;

        public AudioClip spawnSound;
        public AudioClip deathSound;
        [Range(0f, 1f)] public float soundVolume = 0.8f;
    }
}
