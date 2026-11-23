using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LastGrounds
{
    /// <summary>
    /// Spawns zombies at the top of a random lane following the current mode's phases.
    /// One component replaces oGeneradorFacil/Normal/Dificil/Hardcore/Escalada.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] Transform enemiesParent;

        GameModeData mode;
        Func<float> clock;
        Coroutine routine;

        public void Begin(GameModeData gameMode, Func<float> elapsedTime)
        {
            StopSpawning();
            mode = gameMode;
            clock = elapsedTime;
            routine = StartCoroutine(Run());
        }

        public void StopSpawning()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
        }

        IEnumerator Run()
        {
            if (mode.firstSpawnDelay > 0f) yield return new WaitForSeconds(mode.firstSpawnDelay);

            while (true)
            {
                int index;
                SpawnPhase phase = mode.GetPhase(clock(), out index);
                if (phase == null)
                {
                    yield return null;
                    continue;
                }

                Enemy prefab = phase.PickEnemy();
                if (prefab != null) Spawn(prefab);
                yield return new WaitForSeconds(phase.PickInterval());
            }
        }

        void Spawn(Enemy prefab)
        {
            int lane = Random.Range(0, PlayField.LaneCount);
            Vector3 position = new Vector3(PlayField.LaneX(lane), PlayField.SpawnY, 0f);
            Instantiate(prefab, position, Quaternion.identity, enemiesParent);
        }
    }
}
