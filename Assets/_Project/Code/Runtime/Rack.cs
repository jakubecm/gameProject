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

        /// <summary>
        /// The share of the screen's height the rack must fill for its filling to be drawn. A rack is about 2 m tall; with the quality level's LOD bias
        /// of 2 this is about 7 m from the camera at a field of view of 60 degrees. Further away the filling is hidden behind the closed door. The
        /// filling is what costs most when many racks are near: it is lit by every light, pixel by pixel (measured, see PERFORMANCE.md).
        /// </summary>
        public const float ContentsScreenHeight = 0.5f;

        private bool configured;

        public bool IsOpenable { get; private set; }

        public RackDoor Door => door;

        private void Awake()
        {
            AddLevelOfDetail();
            // A rack nobody configured is a closed one.
            if (!configured) Configure(false, 0);
        }

        /// <summary>
        /// Gives the rack a level-of-detail group in which only the filling (the 70 000 triangles of servers, cables and ports) has a detailed
        /// level and no level beyond it: the body and the door always stay. A hall has hundreds of racks and the filling was nine tenths of
        /// all the triangles of the world. Safe to call again: it sets the group up the same way each time (so a changed
        /// <see cref="ContentsScreenHeight"/> reaches racks whose prefab was built with the old one).
        /// </summary>
        public void AddLevelOfDetail()
        {
            if (contentsRenderer == null) return;

            var group = GetComponent<LODGroup>();
            if (group == null) group = gameObject.AddComponent<LODGroup>();
            group.fadeMode = LODFadeMode.None;
            group.SetLODs(new[]
            {
                new LOD(ContentsScreenHeight, new Renderer[] { contentsRenderer }),
                new LOD(0f, new Renderer[0]),
            });
            group.RecalculateBounds();
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
