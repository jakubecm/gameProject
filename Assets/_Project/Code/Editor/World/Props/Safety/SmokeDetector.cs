using System.Collections.Generic;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Smoke detector, 100 mm across and 46 mm high, glossy white: the round base with a rounded top edge and the seam,
        /// the stepped lower ring with its status lens, and under it the sensing chamber: a dark insect mesh between four
        /// white posts, closed by a domed white cap. The pivot is on the ceiling surface, hanging down.
        /// </summary>
        [Model(Prop.SmokeDetector)]
        private static void BuildSmokeDetector(Transform root)
        {
            var white = new MeshBuilder();
            var mesh = new MeshBuilder();
            var lens = new MeshBuilder();
            var origin = new Vector3(0f, -TileDrop, 0f);
            var none = Quaternion.identity;

            // Base and stepped ring, from the underside next to the chamber outward, up the side and over the top edge.
            white.AddLathe(new[]
            {
                P(0.0300f, -0.0290f, true), P(0.0365f, -0.0290f), P(0.0392f, -0.0283f), P(0.0420f, -0.0268f), P(0.0452f, -0.0262f), P(0.0470f, -0.0250f),
                P(0.0478f, -0.0232f), P(0.0480f, -0.0206f, true), P(0.0500f, -0.0203f, true), P(0.0503f, -0.0190f), P(0.0503f, 0.0040f), P(0.0497f, 0.0090f),
                P(0.0482f, TileDrop, true), P(0f, TileDrop, true),
            }, 64, true, origin, none);

            // The sensing chamber: mesh between four posts, then the cap.
            mesh.AddLathe(new[] { P(0.0290f, -0.0400f, true), P(0.0290f, -0.0292f, true) }, 48, true, origin, none);
            for (int i = 0; i < 4; i++)
            {
                var path = new List<Vector3> { new Vector3(0.0305f, -0.0292f, 0f), new Vector3(0.0305f, -0.0410f, 0f) };
                white.AddStrip(path, Vector3.forward, 0.0080f, 0.0045f, origin, Quaternion.Euler(0f, 45f + i * 90f, 0f));
            }
            white.AddLathe(new[]
            {
                P(0f, -0.0462f), P(0.0200f, -0.0458f), P(0.0300f, -0.0446f), P(0.0338f, -0.0425f), P(0.0345f, -0.0410f), P(0.0338f, -0.0400f, true), P(0.0285f, -0.0400f, true),
            }, 48, true, origin, none);

            // Status lens on the lower ring, facing down.
            lens.AddLathe(new[] { P(0f, -0.0277f), P(0.0028f, -0.0277f, true), P(0.0030f, -0.0272f, true) }, 16, true, new Vector3(0f, -TileDrop, 0.0432f), none);

            white.Attach("Body", root, SurfaceMaterial.DetectorWhite);
            mesh.Attach("Mesh", root, SurfaceMaterial.DetectorMesh);
            lens.Attach("Lens", root, SurfaceMaterial.BulbClear);
        }
    }
}
