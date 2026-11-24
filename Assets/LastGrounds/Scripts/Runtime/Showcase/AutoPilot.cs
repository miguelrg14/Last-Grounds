using UnityEngine;

namespace LastGrounds
{
    /// <summary>
    /// Plays the game on its own for showcase recordings: chases the zombie closest to
    /// the barricade, picks the shotgun for fat zombies and the MP7 when the screen fills up.
    /// </summary>
    public class AutoPilot : MonoBehaviour, IGameInputSource
    {
        const int Pistol = 0, Shotgun = 1, Mp7 = 2;

        [Tooltip("When false the pilot keeps moving but stops shooting (to let the barricade fall).")]
        public bool fireEnabled = true;

        [SerializeField, Min(0f)] float aimTolerance = 0.3f;
        [SerializeField, Min(0f)] float deadZone = 0.08f;
        [SerializeField, Min(1)] int crowdForMp7 = 5;
        [SerializeField, Min(1)] int tankHealth = 150;

        float horizontal;
        bool fire;
        bool pulse;
        int slot = -1;

        void OnEnable()
        {
            GameInput.Override = this;
        }

        void OnDisable()
        {
            if (GameInput.Override == this) GameInput.Override = null;
        }

        void Update()
        {
            horizontal = 0f;
            fire = false;
            slot = -1;

            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;
            float playerX = player.transform.position.x;

            Enemy target = null;
            float lowest = float.MaxValue;
            int onScreen = 0;
            for (int i = 0; i < Enemy.AliveEnemies.Count; i++)
            {
                Enemy e = Enemy.AliveEnemies[i];
                if (e == null || !e.IsAlive) continue;
                float y = e.transform.position.y;
                if (y > PlayField.HalfHeight - 0.3f) continue;
                onScreen++;
                if (y < lowest)
                {
                    lowest = y;
                    target = e;
                }
            }

            float goalX = target != null ? target.transform.position.x : 0f;
            float dx = goalX - playerX;
            if (Mathf.Abs(dx) > deadZone) horizontal = Mathf.Sign(dx);
            if (target == null) return;

            int wanted = Pistol;
            if (target.MaxHealth >= tankHealth) wanted = Shotgun;
            else if (onScreen >= crowdForMp7) wanted = Mp7;
            if (wanted != player.WeaponIndex && wanted < player.WeaponCount) slot = wanted;

            if (fireEnabled && Mathf.Abs(dx) < aimTolerance)
            {
                fire = true;
                pulse = !pulse; // semi-automatic weapons need a fresh press
            }
        }

        public float Horizontal() { return horizontal; }
        public bool FirePressed() { return fire && pulse; }
        public bool FireHeld() { return fire; }
        public int WeaponSlot() { return slot; }
    }
}
