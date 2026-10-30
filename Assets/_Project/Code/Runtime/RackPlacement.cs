using System;
using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Puts a <see cref="Rack"/> on every <see cref="RackSlot"/> and decides which racks are openable (attackable): exactly one of the
    /// two racks of the security office, and in every server aisle (the two rows of racks facing each other) exactly one rack in each
    /// row, so two per aisle, one on each side. The choice and the random fillings depend only on the seed and the slot positions, so
    /// the same seed builds the same racks.
    ///
    /// A slot's room is its parent's parent (slots sit in a <c>RackSlots</c> group directly under the room); the rows of a hall tile are
    /// the slots of one room that share the same local Z.
    /// </summary>
    public static class RackPlacement
    {
        /// <summary>Spawns a rack from <paramref name="prefab"/> on every slot under <paramref name="root"/> and configures them.</summary>
        public static List<Rack> Fill(Transform root, GameObject prefab, Transform parent, int seed) =>
            Assign(root.GetComponentsInChildren<RackSlot>(), slot =>
            {
                var instance = UnityEngine.Object.Instantiate(prefab, slot.transform.position, slot.transform.rotation, parent);
                return instance.GetComponent<Rack>();
            }, seed);

        /// <summary>Creates a rack for every slot with <paramref name="create"/> and configures it (openable or not, random filling).</summary>
        public static List<Rack> Assign(IEnumerable<RackSlot> slots, Func<RackSlot, Rack> create, int seed)
        {
            var all = new List<RackSlot>(slots);
            var openable = new HashSet<RackSlot>();
            var groups = new Dictionary<(Transform room, int row), List<RackSlot>>();
            foreach (var slot in all)
            {
                var room = slot.transform.parent != null ? slot.transform.parent.parent : null;
                bool security = room != null && room.name.StartsWith($"Room_{RoomType.SecurityOffice}");
                // The security office is one group; a hall tile has a group per row of racks.
                int row = security ? 0 : Mathf.RoundToInt((room != null ? room.InverseTransformPoint(slot.transform.position).z : slot.transform.position.z) * 10f);
                var key = (room, row);
                if (!groups.TryGetValue(key, out var list))
                {
                    groups[key] = list = new List<RackSlot>();
                }
                list.Add(slot);
            }

            foreach (var pair in groups)
            {
                var list = pair.Value;
                var first = list[0].transform.position;
                int hash = Hash(first, seed);
                openable.Add(list[new System.Random(hash).Next(list.Count)]);
            }

            var racks = new List<Rack>();
            foreach (var slot in all)
            {
                var rack = create(slot);
                if (rack == null) continue;
                rack.Configure(openable.Contains(slot), Hash(slot.transform.position, seed ^ 0x5bd1e995));
                racks.Add(rack);
            }
            return racks;
        }

        private static int Hash(Vector3 position, int seed)
        {
            unchecked
            {
                int h = seed;
                h = h * 73856093 ^ Mathf.RoundToInt(position.x * 10f);
                h = h * 19349663 ^ Mathf.RoundToInt(position.z * 10f);
                h = h * 83492791 ^ Mathf.RoundToInt(position.y * 10f);
                return h & int.MaxValue;
            }
        }
    }
}
