using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LastGrounds
{
    [Serializable]
    public class SpawnEntry
    {
        public Enemy prefab;
        [Min(0f)] public float weight = 1f;
    }

    /// <summary>
    /// A stretch of a run with its own spawn rate and enemy mix.
    /// The original used one alarm per phase in oGeneradorEscalada; here they are data.
    /// </summary>
    [Serializable]
    public class SpawnPhase
    {
        [Tooltip("Banner shown when the phase starts. Leave empty for none.")]
        public string label;

        [Tooltip("Seconds since the run started.")]
        [Min(0f)] public float startTime;

        [Tooltip("One of these values (seconds) is picked after each spawn, like GameMaker's choose().")]
        public float[] intervals = { 1f, 2f };

        public SpawnEntry[] enemies = new SpawnEntry[0];

        public float PickInterval()
        {
            if (intervals == null || intervals.Length == 0) return 1f;
            return Mathf.Max(0.05f, intervals[Random.Range(0, intervals.Length)]);
        }

        public Enemy PickEnemy()
        {
            if (enemies == null || enemies.Length == 0) return null;
            float total = 0f;
            for (int i = 0; i < enemies.Length; i++)
                if (enemies[i].prefab != null) total += enemies[i].weight;
            if (total <= 0f) return null;

            float roll = Random.value * total;
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i].prefab == null) continue;
                roll -= enemies[i].weight;
                if (roll <= 0f) return enemies[i].prefab;
            }
            return enemies[enemies.Length - 1].prefab;
        }
    }

    /// <summary>
    /// A game mode / difficulty. Replaces the five copies of generator, barrier,
    /// highscore and game-over objects that the GameMaker version had per difficulty.
    /// </summary>
    [CreateAssetMenu(fileName = "GameMode", menuName = "Last Grounds/Game Mode")]
    public class GameModeData : ScriptableObject
    {
        [Tooltip("Stable id used as the leaderboard key.")]
        public string id = "normal";
        public string displayName = "NORMAL";

        [Header("Spawning")]
        [Min(0f)] public float firstSpawnDelay = 5f;
        public SpawnPhase[] phases = new SpawnPhase[0];

        [Header("Barricade")]
        [Min(1)] public int barricadeHealth = 100;
        [Tooltip("Damage of one zombie bite on the barricade.")]
        [Min(0)] public int biteDamage = 20;
        [Tooltip("The barricade can only be bitten once per this many seconds (original: 100 frames).")]
        [Min(0f)] public float biteCooldown = 100f / 60f;

        [Header("Win condition")]
        [Tooltip("Seconds to survive to win. 0 = endless horde.")]
        [Min(0f)] public float survivalTime;
        public string victoryMessage = "RESCUED!";

        public bool IsEndless { get { return survivalTime <= 0f; } }

        /// <summary>The phase active at <paramref name="time"/>, or null before the first one starts.</summary>
        public SpawnPhase GetPhase(float time, out int index)
        {
            index = -1;
            SpawnPhase current = null;
            if (phases == null) return null;
            for (int i = 0; i < phases.Length; i++)
            {
                if (phases[i].startTime <= time)
                {
                    current = phases[i];
                    index = i;
                }
            }
            return current;
        }
    }
}
