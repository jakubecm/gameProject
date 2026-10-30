using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// A stack of 16 sheets of A4 paper (210 x 297 mm, 0.1 mm thick each), every sheet a little askew like a loose pile. The top
        /// sheet carries the printed page texture (<c>T_PaperPage</c>, text inside 25 mm margins). Lies on a desk; the page reads
        /// upright for a person standing at +Z.
        /// </summary>
        [Model(Prop.PaperStack)]
        private static void BuildPaperStack(Transform root)
        {
            const int sheets = 16;
            const float thickness = 0.0001f;
            const float halfX = 0.105f;
            const float halfZ = 0.1485f;
            var edges = new MeshBuilder();
            var top = new MeshBuilder();
            for (int i = 0; i < sheets; i++)
            {
                // Deterministic scatter: each sheet is shifted a few millimetres and turned a degree or two.
                float yaw = Mathf.Sin(i * 2.7f) * 2.2f * Mathf.Deg2Rad;
                var shift = new Vector3(Mathf.Sin(i * 1.9f + 0.4f) * 0.006f, 0f, Mathf.Cos(i * 2.3f) * 0.006f);
                float y0 = i * thickness;
                float y1 = y0 + thickness;
                Vector3 Corner(float x, float z, float y) => shift + new Vector3(x * Mathf.Cos(yaw) + z * Mathf.Sin(yaw), y, -x * Mathf.Sin(yaw) + z * Mathf.Cos(yaw));
                var c0 = new[] { new Vector2(-halfX, -halfZ), new Vector2(halfX, -halfZ), new Vector2(halfX, halfZ), new Vector2(-halfX, halfZ) };
                // Edges of the sheet (the faces under the top one are never seen).
                for (int k = 0; k < 4; k++)
                {
                    var a = c0[k];
                    var b = c0[(k + 1) % 4];
                    var outward = Corner((a.x + b.x), (a.y + b.y), 0f) - shift;
                    edges.AddPanel(Corner(b.x, b.y, y0), Corner(a.x, a.y, y0), Corner(a.x, a.y, y1), Corner(b.x, b.y, y1), outward, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
                }
                // Top face: the printed page on the last sheet; the others are plain paper (a blank sheet shows through the edges' material).
                var plain = i == sheets - 1 ? top : edges;
                // Viewed from +Z looking down: u runs from +X (left) to -X, v from +Z (bottom of the page) to -Z (head of the page).
                plain.AddPanel(Corner(-halfX, halfZ, y1), Corner(halfX, halfZ, y1), Corner(halfX, -halfZ, y1), Corner(-halfX, -halfZ, y1), Vector3.up,
                    new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f));
            }
            edges.Attach("Paper", root, SurfaceMaterial.Whiteboard);
            top.Attach("TopSheet", root, SurfaceMaterial.PaperPage);
        }
    }
}
