using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Kitchen fridge-freezer 0.6 x 1.8 x 0.65 m after a product photo: a white body on small feet, a tall upper door and a lower
        /// freezer door (a 10 mm joint between them), each with a vertical silver D-handle on the left edge held by two bracket
        /// stand-offs; the doors face +Z.
        /// </summary>
        [Model(Prop.Fridge)]
        private static void BuildFridge(Transform root)
        {
            const float width = 0.6f;
            const float height = 1.8f;
            const float depth = 0.65f;
            const float doorThickness = 0.022f;
            const float split = 0.85f;
            float bodyFront = depth / 2f - 0.003f - doorThickness;
            var white = new MeshBuilder();
            var silver = new MeshBuilder();
            var feet = new MeshBuilder();

            white.AddRoundedBlock(new Vector3(0f, 0.03f, (bodyFront - depth / 2f) / 2f), width, bodyFront + depth / 2f, 0.02f, height - 0.03f, 0.006f, 5);
            // Lower freezer door and upper fridge door: gloss slabs with a rounded edge.
            white.AddRoundedSlab(new Vector3(0f, (0.035f + split - 0.005f) / 2f, bodyFront), width - 0.01f, split - 0.04f, 0.012f, doorThickness, 0.005f, 4, true);
            white.AddRoundedSlab(new Vector3(0f, (split + 0.005f + height - 0.005f) / 2f, bodyFront), width - 0.01f, height - split - 0.01f, 0.012f, doorThickness, 0.005f, 4, true);
            foreach (var corner in new[] { new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, 1f) })
            {
                feet.AddLathe(new[] { P(0f, 0f, true), P(0.016f, 0f, true), P(0.016f, 0.03f, true), P(0f, 0.03f, true) }, 16, true,
                    new Vector3(corner.x * (width / 2f - 0.05f), 0f, corner.y * (depth / 2f - 0.07f)), Quaternion.identity);
            }
            // D-handles on the viewer's left (+X): a vertical bar and a stand-off at each end.
            foreach (var (y0, y1) in new[] { (0.52f, 0.80f), (0.94f, 1.22f) })
            {
                float x = width / 2f - 0.045f;
                silver.AddRoundedBlock(new Vector3(x, y0, depth / 2f + 0.026f), 0.022f, 0.016f, 0.004f, y1 - y0, 0.003f, 2);
                foreach (float y in new[] { y0 + 0.012f, y1 - 0.034f })
                {
                    silver.AddBox(new Vector3(x - 0.011f, y, depth / 2f - 0.004f), new Vector3(x + 0.011f, y + 0.022f, depth / 2f + 0.030f));
                }
            }
            white.Attach("Fridge_Body", root, SurfaceMaterial.DeskWhite);
            silver.Attach("Fridge_Handles", root, SurfaceMaterial.Metal);
            feet.Attach("Fridge_Feet", root, SurfaceMaterial.MouseBlack);
            AddBoxCollider(root, new Vector3(0f, height / 2f, 0.02f), new Vector3(width, height, depth + 0.04f));
        }
    }
}
