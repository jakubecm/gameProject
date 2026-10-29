using System;
using ColdShutdown.Core;
using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// The pieces a hall tile is assembled from. The tile itself (<c>Room_Hall</c>: floor, roof, structure,
    /// pods, overhead services, the sign with the tile's code) comes from its room definition like every room; the kit adds one side
    /// piece per side and one corner piece per corner, chosen by <see cref="HallPlan"/>. Side pieces are
    /// authored for the north side and corner pieces for the north-east corner; the kit turns them into place.
    /// </summary>
    [CreateAssetMenu(fileName = "HallKit", menuName = "Cold Shutdown/Hall Kit")]
    public class HallKit : ScriptableObject
    {
        [Header("Sides (authored for North)")]
        [SerializeField] private GameObject sideOpen;
        [SerializeField] private GameObject sideClosed;
        [SerializeField] private GameObject sideDoor;
        [SerializeField] private GameObject sideWall;

        [Header("Corners (authored for North-East)")]
        [SerializeField] private GameObject cornerInner;
        [SerializeField] private GameObject cornerConcave;
        [SerializeField] private GameObject cornerEdgeFirst;
        [SerializeField] private GameObject cornerEdgeSecond;
        [SerializeField] private GameObject cornerOuter;

        public GameObject SidePrefab(HallSideKind kind)
        {
            switch (kind)
            {
                case HallSideKind.Open: return sideOpen;
                case HallSideKind.Closed: return sideClosed;
                case HallSideKind.Door: return sideDoor;
                default: return sideWall;
            }
        }

        public GameObject CornerPrefab(HallCornerKind kind)
        {
            switch (kind)
            {
                case HallCornerKind.Inner: return cornerInner;
                case HallCornerKind.Concave: return cornerConcave;
                case HallCornerKind.EdgeFirst: return cornerEdgeFirst;
                case HallCornerKind.EdgeSecond: return cornerEdgeSecond;
                default: return cornerOuter;
            }
        }

        /// <summary>True when every piece is assigned.</summary>
        public bool IsComplete
        {
            get
            {
                foreach (HallSideKind kind in Enum.GetValues(typeof(HallSideKind)))
                {
                    if (SidePrefab(kind) == null) return false;
                }
                foreach (HallCornerKind kind in Enum.GetValues(typeof(HallCornerKind)))
                {
                    if (CornerPrefab(kind) == null) return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Adds the side and corner pieces of one tile under <paramref name="cell"/> (the cell object at the
        /// tile's position, not turned). Pieces are named
        /// <c>HallSide_&lt;Direction&gt;</c> and <c>HallCorner_&lt;Corner&gt;</c>.
        /// </summary>
        /// <param name="instantiate">Creates an instance of a prefab under a parent; defaults to <see cref="UnityEngine.Object.Instantiate(UnityEngine.Object)"/>.
        /// Editor tools pass PrefabUtility.InstantiatePrefab to keep the prefab links.</param>
        public void AddPieces(Transform cell, HallCell plan, Func<GameObject, Transform, GameObject> instantiate = null)
        {
            if (cell == null) throw new ArgumentNullException(nameof(cell));
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            instantiate = instantiate ?? ((prefab, parent) => Instantiate(prefab, parent));

            for (int i = 0; i < DirectionExtensions.Count; i++)
            {
                var side = (Direction)i;
                Place(SidePrefab(plan.Side(side)), cell, $"HallSide_{side}", i, instantiate);
            }

            for (int i = 0; i < DirectionExtensions.Count; i++)
            {
                Place(CornerPrefab(plan.Corner((HallCorner)i)), cell, $"HallCorner_{(HallCorner)i}", i, instantiate);
            }
        }

        /// <summary>Writes the tile code on the signs of the tile itself (the <c>Room_Hall</c> instance).</summary>
        public static void LabelTile(GameObject tile, HallCell plan)
        {
            foreach (var sign in tile.GetComponentsInChildren<HallSign>(true))
            {
                sign.Code = plan.Code;
            }
        }

        private GameObject Place(GameObject prefab, Transform cell, string name, int quarterTurns,
            Func<GameObject, Transform, GameObject> instantiate)
        {
            if (prefab == null)
            {
                Debug.LogWarning($"HallKit '{this.name}' has no piece for {name}.", this);
                return null;
            }

            var piece = instantiate(prefab, cell);
            piece.name = name;
            piece.transform.SetLocalPositionAndRotation(Vector3.zero,
                Quaternion.Euler(0f, quarterTurns * RoomContract.RotationStepDegrees, 0f));
            return piece;
        }
    }
}
