using ColdShutdown.Core;
using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Links a room type to its prefab. The level builder looks prefabs up here; rack and terminal
    /// slots are read from the prefab's RackSlot and TerminalSlot components.
    /// </summary>
    [CreateAssetMenu(fileName = "RoomDefinition", menuName = "Cold Shutdown/Room Definition")]
    public class RoomDefinition : ScriptableObject
    {
        [SerializeField] private RoomType type;
        [SerializeField] private GameObject prefab;

        public RoomType Type => type;
        public GameObject Prefab => prefab;

        private void OnValidate()
        {
            if (prefab == null)
            {
                Debug.LogWarning($"RoomDefinition '{name}' has no prefab assigned.", this);
            }
        }
    }
}
