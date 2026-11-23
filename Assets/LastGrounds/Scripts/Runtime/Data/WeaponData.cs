using UnityEngine;

namespace LastGrounds
{
    /// <summary>
    /// One player weapon. Replaces the three PersonajeXObj objects of the original,
    /// which duplicated the same movement and shooting code per weapon.
    /// </summary>
    [CreateAssetMenu(fileName = "Weapon", menuName = "Last Grounds/Weapon")]
    public class WeaponData : ScriptableObject
    {
        public string displayName = "Pistol";

        [Tooltip("Player sprite while this weapon is equipped.")]
        public Sprite playerSprite;

        public Projectile projectilePrefab;

        [Min(0)] public int damage = 25;

        [Tooltip("Seconds between shots (original: frames / 60).")]
        [Min(0f)] public float cooldown = 10f / 60f;

        [Tooltip("Keeps firing while the button is held.")]
        public bool automatic;

        [Tooltip("World units per second (original bullets moved 10 px per frame).")]
        [Min(0f)] public float projectileSpeed = 37.5f;

        public AudioClip shotSound;
        [Range(0f, 1f)] public float shotVolume = 1f;
        [Range(0.3f, 3f)] public float shotPitch = 1f;
    }
}
