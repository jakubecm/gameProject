using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace ColdShutdown.Editor
{
    /// <summary>Builds editable ProBuilder boxes and cylinders for room shells, grayboxes and props.</summary>
    internal static class GrayboxUtility
    {
        /// <summary>
        /// Creates a ProBuilder box. The size is baked into the mesh (the transform scale stays 1) so the
        /// box can be edited in ProBuilder without surprises. Pass <paramref name="collider"/> false for
        /// parts the player can never touch (ceiling fittings) or that a prop covers with one box collider.
        /// </summary>
        public static ProBuilderMesh CreateBox(string name, Transform parent, Vector3 localCenter, Vector3 size,
            SurfaceMaterial surface = SurfaceMaterial.Wall, bool collider = true)
        {
            var mesh = ShapeGenerator.GenerateCube(PivotLocation.Center, size);
            return Finish(mesh, name, parent, localCenter, surface, collider);
        }

        /// <summary>Creates a box that stands on the floor: <paramref name="floorPosition"/> is the center of its bottom face.</summary>
        public static ProBuilderMesh CreateStandingBox(string name, Transform parent, Vector3 floorPosition, Vector3 size,
            SurfaceMaterial surface = SurfaceMaterial.Wall, bool collider = true)
        {
            return CreateBox(name, parent, floorPosition + new Vector3(0f, size.y / 2f, 0f), size, surface, collider);
        }

        /// <summary>
        /// Creates a ProBuilder cylinder centered on <paramref name="localCenter"/>. Its axis is local +Y;
        /// rotate the returned transform for horizontal pipes.
        /// </summary>
        public static ProBuilderMesh CreateCylinder(string name, Transform parent, Vector3 localCenter, float radius, float height,
            SurfaceMaterial surface, bool collider = false)
        {
            var mesh = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 12, radius, height, 0, 1);
            return Finish(mesh, name, parent, localCenter, surface, collider);
        }

        /// <summary>Creates a box spanning two opposite corners (room-local), for runs of trunking, trays and rails.</summary>
        public static ProBuilderMesh CreateBoxBetween(string name, Transform parent, Vector3 cornerA, Vector3 cornerB,
            SurfaceMaterial surface, bool collider = false)
        {
            var min = Vector3.Min(cornerA, cornerB);
            var max = Vector3.Max(cornerA, cornerB);
            return CreateBox(name, parent, (min + max) / 2f, max - min, surface, collider);
        }

        /// <summary>Creates a straight pipe between two points that differ along exactly one axis.</summary>
        public static ProBuilderMesh CreatePipe(string name, Transform parent, Vector3 from, Vector3 to, float radius, SurfaceMaterial surface)
        {
            var delta = to - from;
            var pipe = CreateCylinder(name, parent, (from + to) / 2f, radius, delta.magnitude, surface);
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y) && Mathf.Abs(delta.x) > Mathf.Abs(delta.z))
            {
                pipe.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            else if (Mathf.Abs(delta.z) > Mathf.Abs(delta.y))
            {
                pipe.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            return pipe;
        }

        public static Transform CreateGroup(string name, Transform parent)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        /// <summary>Gives the mesh one surface material on every face and rebuilds it.</summary>
        public static void SetSurface(ProBuilderMesh mesh, SurfaceMaterial surface)
        {
            mesh.SetMaterial(mesh.faces, GrayboxMaterials.Get(surface));
            mesh.ToMesh();
            mesh.Refresh();
        }

        private static ProBuilderMesh Finish(ProBuilderMesh mesh, string name, Transform parent, Vector3 localCenter,
            SurfaceMaterial surface, bool collider)
        {
            mesh.gameObject.name = name;
            mesh.transform.SetParent(parent, false);
            mesh.transform.localPosition = localCenter;
            SetSurface(mesh, surface);

            if (collider)
            {
                var meshCollider = mesh.gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh.GetComponent<MeshFilter>().sharedMesh;
            }
            return mesh;
        }
    }
}
