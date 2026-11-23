using UnityEngine;

namespace LastGrounds
{
    /// <summary>Marks an instance as coming from <see cref="PrefabPool"/>.</summary>
    public class PooledObject : MonoBehaviour
    {
        public GameObject Source { get; set; }
    }
}
