using System.IO;
using ColdShutdown.Core;
using UnityEditor;
using UnityEngine;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Creates the room shell prefab (floor, ceiling, four walls with an opening in each) and the
    /// wall plug prefab that fills one opening. All dimensions come from <see cref="RoomContract"/>.
    /// Existing prefabs are never replaced, so hand edits are safe; delete a prefab to regenerate it.
    /// </summary>
    public static class RoomShellTool
    {
        public const string RoomsFolder = "Assets/_Project/Prefabs/Rooms";
        public const string ShellPath = RoomsFolder + "/RoomShell.prefab";
        public const string WallPlugPath = RoomsFolder + "/WallPlug.prefab";

        private const float SlabThickness = 0.1f;

        public static void CreateRoomShell()
        {
            // The room prefabs are variants of the shell: after regenerating the shell, delete them too and
            // run Create Preview Scene again, otherwise they keep pointing at the deleted shell.
            if (!SkipExisting(ShellPath))
            {
                SaveAsPrefab(BuildShell(), ShellPath);
                Debug.Log($"Room shell saved to {ShellPath}.");
            }

            if (!SkipExisting(WallPlugPath))
            {
                SaveAsPrefab(BuildWallPlug(), WallPlugPath);
                Debug.Log($"Wall plug saved to {WallPlugPath}.");
            }
        }

        private static GameObject BuildShell()
        {
            var root = new GameObject("RoomShell");
            var shell = GrayboxUtility.CreateGroup("Shell", root.transform);

            var slabSize = new Vector3(RoomContract.CellSize, SlabThickness, RoomContract.CellSize);
            GrayboxUtility.CreateBox("Floor", shell, new Vector3(0f, -SlabThickness / 2f, 0f), slabSize, SurfaceMaterial.Floor);
            GrayboxUtility.CreateBox("Ceiling", shell, new Vector3(0f, RoomContract.CeilingHeight + SlabThickness / 2f, 0f), slabSize, SurfaceMaterial.Ceiling);

            for (int i = 0; i < DirectionExtensions.Count; i++)
            {
                BuildWall((Direction)i, shell);
            }

            // Every wall spans only the interior, so the corners get their own pillars and the shell
            // stays identical under 90° rotations.
            var pillarSize = new Vector3(RoomContract.WallThickness, RoomContract.CeilingHeight, RoomContract.WallThickness);
            float corner = RoomContract.WallCenterOffset;
            foreach (int x in new[] { -1, 1 })
            {
                foreach (int z in new[] { -1, 1 })
                {
                    string name = $"Corner_{(z > 0 ? "N" : "S")}{(x > 0 ? "E" : "W")}";
                    GrayboxUtility.CreateStandingBox(name, shell, new Vector3(x * corner, 0f, z * corner), pillarSize);
                }
            }

            return root;
        }

        /// <summary>
        /// One wall: two full-height segments beside the opening and a lintel above it. The wall group
        /// stands on the opening pose, so local +Z points into the room and local X runs along the wall.
        /// </summary>
        private static void BuildWall(Direction side, Transform parent)
        {
            var wall = GrayboxUtility.CreateGroup($"Wall_{side}", parent);
            ApplyPose(wall, RoomContract.GetOpeningPose(side));

            float halfOpening = RoomContract.OpeningWidth / 2f;
            float segmentLength = RoomContract.InteriorHalfSize - halfOpening;
            float segmentCenter = halfOpening + segmentLength / 2f;
            var segmentSize = new Vector3(segmentLength, RoomContract.CeilingHeight, RoomContract.WallThickness);
            GrayboxUtility.CreateStandingBox("Segment_Left", wall, new Vector3(-segmentCenter, 0f, 0f), segmentSize);
            GrayboxUtility.CreateStandingBox("Segment_Right", wall, new Vector3(segmentCenter, 0f, 0f), segmentSize);

            float lintelHeight = RoomContract.CeilingHeight - RoomContract.OpeningHeight;
            var lintelSize = new Vector3(RoomContract.OpeningWidth, lintelHeight, RoomContract.WallThickness);
            GrayboxUtility.CreateStandingBox("Lintel", wall, new Vector3(0f, RoomContract.OpeningHeight, 0f), lintelSize);
        }

        /// <summary>The plug pivot is the opening pose: place it with <see cref="RoomContract.GetOpeningPose"/>.</summary>
        private static GameObject BuildWallPlug()
        {
            var root = new GameObject("WallPlug");
            var size = new Vector3(RoomContract.OpeningWidth, RoomContract.OpeningHeight, RoomContract.WallThickness);
            GrayboxUtility.CreateStandingBox("Plug", root.transform, Vector3.zero, size);
            return root;
        }

        internal static void ApplyPose(Transform transform, SlotPose pose)
        {
            transform.localPosition = new Vector3(pose.X, 0f, pose.Z);
            transform.localRotation = Quaternion.Euler(0f, pose.YawDegrees, 0f);
        }

        /// <summary>True (and logs why) when the asset already exists and must not be replaced.</summary>
        internal static bool SkipExisting(string path)
        {
            if (!File.Exists(path))
            {
                return false;
            }
            Debug.Log($"{path} already exists, skipped. Delete it to regenerate.");
            return true;
        }

        /// <summary>Saves the object as a prefab (a variant if it is a prefab instance) and destroys the scene copy.</summary>
        internal static GameObject SaveAsPrefab(GameObject root, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }
    }
}
