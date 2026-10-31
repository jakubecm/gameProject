using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    /// <summary>Dial faces (thermometers, kW meters) shared by the plant props.</summary>
    public static partial class Props
    {
        /// <summary>The meshes of any number of dial thermometers (one mesh per material); each dial faces +Z.</summary>
        private sealed class DialMeshes
        {
            private readonly MeshBuilder chrome = new MeshBuilder();
            private readonly MeshBuilder face = new MeshBuilder();
            private readonly MeshBuilder glass = new MeshBuilder();
            private readonly MeshBuilder needle = new MeshBuilder();
            private readonly MeshBuilder brass = new MeshBuilder();
            private readonly SurfaceMaterial faceMaterial;

            public DialMeshes(SurfaceMaterial faceMaterial = SurfaceMaterial.DialFace) => this.faceMaterial = faceMaterial;

            /// <summary>
            /// A bimetal dial thermometer (0 to 120 degrees C, photo): chrome rolled bezel, white face with the scale texture, a flat
            /// glass, a black needle with a paddle tail on a round hub, and a brass stem with a hex collar running back for
            /// <paramref name="stemLength"/>. <paramref name="origin"/> is the middle of the back of the case, <paramref name="radius"/>
            /// the outer radius, <paramref name="reading"/> between 0 and 1 the needle position.
            /// </summary>
            public void AddDial(Vector3 origin, float radius, float stemLength, float reading, bool needleMesh = true)
            {
                float r = radius;
                var toZ = Quaternion.Euler(90f, 0f, 0f);
                chrome.AddLathe(new[]
                {
                    P(0.98f * r, 0f, true), P(1.0f * r, 0.05f * r), P(1.0f * r, 0.30f * r), P(0.96f * r, 0.37f * r), P(0.90f * r, 0.38f * r),
                    P(0.87f * r, 0.35f * r, true), P(0.87f * r, 0.20f * r, true),
                }, 48, true, origin, toZ);
                chrome.AddDisc(origin, Vector3.back, 0.98f * r, 48);
                // Seen from +Z the viewer's right is -X, so the texture's u runs along -X.
                face.AddDiscUv(origin + Vector3.forward * 0.20f * r, Vector3.left, Vector3.up, Vector3.forward, 0.87f * r, 48);
                glass.AddDisc(origin + Vector3.forward * 0.36f * r, Vector3.forward, 0.88f * r, 48);

                if (needleMesh)
                {
                    float angle = Mathf.Lerp(-135f, 135f, reading) * Mathf.Deg2Rad;
                    var pointer = Turned(new[] { new Vector2(-0.03f * r, -0.02f * r), new Vector2(0.03f * r, -0.02f * r), new Vector2(0.008f * r, 0.74f * r), new Vector2(-0.008f * r, 0.74f * r) }, angle);
                    var tail = Turned(new[] { new Vector2(-0.03f * r, 0f), new Vector2(-0.055f * r, -0.24f * r), new Vector2(0.055f * r, -0.24f * r), new Vector2(0.03f * r, 0f) }, angle);
                    var from = origin + Vector3.forward * 0.22f * r;
                    var to = origin + Vector3.forward * 0.235f * r;
                    needle.AddExtrusion(pointer, from, to, Vector3.left, Vector3.up, Vector3.back, Vector3.forward);
                    needle.AddExtrusion(tail, from, to, Vector3.left, Vector3.up, Vector3.back, Vector3.forward);
                }
                needle.AddLathe(new[] { P(0.08f * r, 0.22f * r, true), P(0.08f * r, 0.25f * r), P(0.05f * r, 0.275f * r), P(0.0001f, 0.28f * r, true) }, 20, true, origin, toZ);

                brass.AddLathe(new[] { P(0.12f * r, -stemLength, true), P(0.12f * r, 0f, true) }, 16, true, origin, toZ);
                brass.AddLathe(new[] { P(0.19f * r, -0.30f * r, true), P(0.19f * r, -0.15f * r, true), P(0.10f * r, -0.15f * r, true) }, 6, false, origin, toZ);
            }

            public void Attach(string prefix, Transform parent)
            {
                chrome.Attach($"{prefix}_Chrome", parent, SurfaceMaterial.Chrome);
                face.Attach($"{prefix}_Face", parent, faceMaterial);
                glass.Attach($"{prefix}_Glass", parent, SurfaceMaterial.DomeClear);
                needle.Attach($"{prefix}_Needle", parent, SurfaceMaterial.BlackHousing);
                brass.Attach($"{prefix}_Stem", parent, SurfaceMaterial.Brass);
            }

            /// <summary>Turns a convex outline clockwise (seen from the front) by <paramref name="angle"/> radians.</summary>
            private static List<Vector2> Turned(IList<Vector2> outline, float angle)
            {
                var result = new List<Vector2>();
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                foreach (var point in outline) result.Add(new Vector2(point.x * cos + point.y * sin, -point.x * sin + point.y * cos));
                return result;
            }
        }
    }
}
