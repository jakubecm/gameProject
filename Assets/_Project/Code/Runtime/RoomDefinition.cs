using ColdShutdown.Core;
using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Links a room type to its prefabs: <see cref="Prefab"/> is the first layout, <c>otherLayouts</c> the rest
    /// (rooms have <see cref="RoomRules.RoomLayouts"/> fixed layouts, corridors and hall tiles one). The level builder
    /// looks prefabs up here; rack and terminal slots are read from the prefab's RackSlot and TerminalSlot components.
    /// </summary>
    [CreateAssetMenu(fileName = "RoomDefinition", menuName = "Cold Shutdown/Room Definition")]
    public class RoomDefinition : ScriptableObject
    {
        [SerializeField] private RoomType type;
        [SerializeField] private GameObject prefab;
        [Tooltip("Layouts 2, 3, ... of the room; the prefab above is layout 1.")]
        [SerializeField] private GameObject[] otherLayouts = new GameObject[0];

        public RoomType Type => type;

        /// <summary>The first layout.</summary>
        public GameObject Prefab => prefab;

        /// <summary>Number of usable layouts (the first one plus every assigned other layout).</summary>
        public int LayoutCount
        {
            get
            {
                int count = 1;
                if (otherLayouts == null) return count;
                foreach (var layout in otherLayouts)
                {
                    if (layout != null) count++;
                }
                return count;
            }
        }

        /// <summary>
        /// The prefab of layout <paramref name="index"/> (0 is <see cref="Prefab"/>). A missing layout falls back to the
        /// first one, so a definition that has only one prefab still works for every layout index.
        /// </summary>
        public GameObject GetLayout(int index)
        {
            if (index <= 0 || otherLayouts == null || index - 1 >= otherLayouts.Length) return prefab;
            return otherLayouts[index - 1] != null ? otherLayouts[index - 1] : prefab;
        }

        private void OnValidate()
        {
            if (prefab == null)
            {
                Debug.LogWarning($"RoomDefinition '{name}' has no prefab assigned.", this);
            }
        }
    }
}
