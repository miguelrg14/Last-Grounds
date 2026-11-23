using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastGrounds
{
    /// <summary>
    /// Minimal per-prefab object pool. Used for projectiles: the MP7 alone fires
    /// 30 bullets per second, which would otherwise mean constant Instantiate/Destroy.
    /// </summary>
    public static class PrefabPool
    {
        static readonly Dictionary<GameObject, Stack<GameObject>> Pools = new Dictionary<GameObject, Stack<GameObject>>();
        static bool hooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Pools.Clear();
            hooked = false;
        }

        public static T Spawn<T>(T prefab, Vector3 position) where T : Component
        {
            Hook();
            GameObject key = prefab.gameObject;
            Stack<GameObject> stack;
            if (!Pools.TryGetValue(key, out stack))
            {
                stack = new Stack<GameObject>();
                Pools[key] = stack;
            }

            GameObject instance = null;
            while (stack.Count > 0 && instance == null) instance = stack.Pop();

            if (instance == null)
            {
                instance = Object.Instantiate(key, position, Quaternion.identity);
                PooledObject tag = instance.GetComponent<PooledObject>();
                if (tag == null) tag = instance.AddComponent<PooledObject>();
                tag.Source = key;
            }
            else
            {
                instance.transform.SetPositionAndRotation(position, Quaternion.identity);
                instance.SetActive(true);
            }

            return instance.GetComponent<T>();
        }

        public static void Despawn(GameObject instance)
        {
            if (instance == null) return;
            PooledObject tag = instance.GetComponent<PooledObject>();
            Stack<GameObject> stack;
            if (tag == null || tag.Source == null || !Pools.TryGetValue(tag.Source, out stack))
            {
                Object.Destroy(instance);
                return;
            }

            instance.SetActive(false);
            stack.Push(instance);
        }

        static void Hook()
        {
            if (hooked) return;
            hooked = true;
            // Pooled instances live in the scene, so forget them when it unloads.
            SceneManager.sceneUnloaded += scene => Pools.Clear();
        }
    }
}
