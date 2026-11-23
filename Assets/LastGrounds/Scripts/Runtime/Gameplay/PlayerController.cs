using System;
using UnityEngine;

namespace LastGrounds
{
    /// <summary>
    /// Moves the survivor left/right behind the barricade and fires upwards.
    /// Weapon switching existed in the original only as commented-out code; it works here.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] WeaponData[] weapons = new WeaponData[0];

        [Tooltip("World units per second (original: 2.5 px per frame).")]
        [SerializeField, Min(0f)] float moveSpeed = 9.375f;
        [SerializeField] float minX = -3f;
        [SerializeField] float maxX = 3f;
        [SerializeField] Vector2 muzzleOffset = new Vector2(0f, 0.45f);
        [SerializeField] AudioClip switchSound;

        public event Action<WeaponData, int> WeaponChanged;

        SpriteRenderer spriteRenderer;
        int weaponIndex;
        float nextShotTime;

        public WeaponData CurrentWeapon { get { return weapons.Length > 0 ? weapons[weaponIndex] : null; } }
        public int WeaponIndex { get { return weaponIndex; } }
        public int WeaponCount { get { return weapons.Length; } }

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        void Start()
        {
            Equip(0, false);
        }

        void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && !gm.IsPlaying) return;

            Move();
            HandleWeaponSwitch();
            HandleFire();
        }

        void Move()
        {
            float input = GameInput.Horizontal();
            if (input == 0f) return;
            Vector3 position = transform.position;
            position.x = Mathf.Clamp(position.x + input * moveSpeed * Time.deltaTime, minX, maxX);
            transform.position = position;
        }

        void HandleWeaponSwitch()
        {
            if (weapons.Length < 2) return;
            int slot = GameInput.WeaponSlot();
            if (slot >= 0 && slot < weapons.Length) Equip(slot, true);
            else if (GameInput.NextWeapon()) Equip((weaponIndex + 1) % weapons.Length, true);
            else if (GameInput.PreviousWeapon()) Equip((weaponIndex - 1 + weapons.Length) % weapons.Length, true);
        }

        void HandleFire()
        {
            WeaponData weapon = CurrentWeapon;
            if (weapon == null || weapon.projectilePrefab == null) return;

            bool wantsToFire = weapon.automatic ? GameInput.FireHeld() : GameInput.FirePressed();
            if (!wantsToFire || Time.time < nextShotTime) return;
            nextShotTime = Time.time + weapon.cooldown;

            Vector3 origin = transform.position + (Vector3)muzzleOffset;
            Projectile bullet = PrefabPool.Spawn(weapon.projectilePrefab, origin);
            bullet.Launch(Vector2.up, weapon.projectileSpeed, weapon.damage);
            if (weapon.shotSound != null) AudioManager.PlaySfx(weapon.shotSound, weapon.shotVolume, weapon.shotPitch);
        }

        void Equip(int index, bool playSound)
        {
            if (weapons.Length == 0) return;
            if (playSound && index == weaponIndex) return;
            weaponIndex = Mathf.Clamp(index, 0, weapons.Length - 1);
            WeaponData weapon = weapons[weaponIndex];
            if (weapon.playerSprite != null) spriteRenderer.sprite = weapon.playerSprite;
            // Small grace so switching is not a way to bypass the shotgun's fire rate.
            nextShotTime = Mathf.Max(nextShotTime, Time.time + 0.1f);
            if (playSound && switchSound != null) AudioManager.PlaySfx(switchSound, 0.4f, 1.6f);
            if (WeaponChanged != null) WeaponChanged(weapon, weaponIndex);
        }
    }
}
