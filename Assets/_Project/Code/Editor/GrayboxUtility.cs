using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace ColdShutdown.Editor
{
    /// <summary>Builds editable ProBuilder boxes for room shells and grayboxes.</summary>
    internal static class GrayboxUtility
    {
        /// <summary>
        /// Creates a ProBuilder box with a mesh collider. The size is baked into the mesh (the
        /// transform scale stays 1) so the box can be edited in ProBuilder without surprises.
        /// </summary>
        public static ProBuilderMesh CreateBox(string name, Transform parent, Vector3 localCenter, Vector3 size)
        {
            var mesh = ShapeGenerator.GenerateCube(PivotLocation.Center, size);
            mesh.gameObject.name = name;
            mesh.transform.SetParent(parent, false);
            mesh.transform.localPosition = localCenter;
            mesh.SetMaterial(mesh.faces, BuiltinMaterials.defaultMaterial);
            mesh.ToMesh();
            mesh.Refresh();

            var collider = mesh.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh.GetComponent<MeshFilter>().sharedMesh;
            return mesh;
        }

        /// <summary>Creates a box that stands on the floor: <paramref name="floorPosition"/> is the center of its bottom face.</summary>
        public static ProBuilderMesh CreateStandingBox(string name, Transform parent, Vector3 floorPosition, Vector3 size)
        {
            return CreateBox(name, parent, floorPosition + new Vector3(0f, size.y / 2f, 0f), size);
        }

        public static Transform CreateGroup(string name, Transform parent)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }
    }
}
