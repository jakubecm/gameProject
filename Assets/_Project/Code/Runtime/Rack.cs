using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// The one server rack. A rack is either closed (a fixed door, a flush handle: nothing to interact with) or openable (the attackable
    /// one: a door that opens with E, see <see cref="RackDoor"/>, and a pulled-out lever handle that stays visible even when the door is
    /// shut). <see cref="Configure"/> sets that flag and picks the random filling; <see cref="RackPlacement"/> decides which racks are openable.
    /// Nothing inside a rack can be used, so its filling is a shared mesh from a <see cref="RackContentLibrary"/>.
    /// </summary>
    public class Rack : MonoBehaviour
    {
        [SerializeField] private RackContentLibrary library;
        [SerializeField] private MeshFilter contentsFilter;
        [SerializeField] private MeshRenderer contentsRenderer;
        [SerializeField] private RackDoor door;
        [SerializeField] private Collider doorCollider;
        [SerializeField] private GameObject flushHandle;
        [SerializeField] private GameObject leverHandle;
        [SerializeField] private GameObject glow;

        private bool configured;

        public bool IsOpenable { get; private set; }

        public RackDoor Door => door;

        private void Awake()
        {
            // A rack nobody configured is a closed one.
            if (!configured) Configure(false, 0);
        }

        /// <summary>Makes the rack openable or closed and gives it the random filling for <paramref name="seed"/>.</summary>
        public void Configure(bool openable, int seed)
        {
            configured = true;
            IsOpenable = openable;

            if (library != null && contentsFilter != null && contentsRenderer != null && library.Count > 0)
            {
                var variant = library.Pick(seed);
                contentsFilter.sharedMesh = variant.mesh;
                contentsRenderer.sharedMaterials = variant.materials;
            }

            if (flushHandle != null) flushHandle.SetActive(!openable);
            if (leverHandle != null) leverHandle.SetActive(openable);
            if (glow != null) glow.SetActive(openable);
            if (doorCollider != null) doorCollider.enabled = openable;
            if (door == null) return;

            door.enabled = openable;
            if (openable) door.ResetToAjar();
            else door.CloseImmediately();
        }
    }
}
