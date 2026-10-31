using System;
using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Office desk 1.6 x 0.8 m after a product photo (a white panel desk): a 25 mm top, two full-depth side panels (18 mm, set
        /// in 20 mm from the ends and from the front and back edges of the top) and a modesty panel between them at the back (-Z),
        /// hanging 0.30 m below the top.
        /// </summary>
        [Model(Prop.Desk)]
        private static void BuildDesk(Transform root)
        {
            const float width = 1.6f;
            const float depth = 0.8f;
            const float top = 0.025f;
            const float panel = 0.018f;
            const float inset = 0.02f;
            float legHeight = DeskHeight - top;

            var topMesh = new MeshBuilder();
            var panels = new MeshBuilder();
            topMesh.AddRoundedBlock(new Vector3(0f, legHeight, 0f), width, depth, 0.006f, top, 0.002f, 2);
            float sideDepth = depth - 2f * inset;
            foreach (int side in new[] { -1, 1 })
            {
                float x = side * (width / 2f - inset - panel / 2f);
                panels.AddBox(new Vector3(x - panel / 2f, 0f, -sideDepth / 2f), new Vector3(x + panel / 2f, legHeight, sideDepth / 2f));
            }
            float innerHalf = width / 2f - inset - panel;
            panels.AddBox(new Vector3(-innerHalf, legHeight - 0.30f, -depth / 2f + 0.08f), new Vector3(innerHalf, legHeight, -depth / 2f + 0.08f + panel));
            topMesh.Attach("Top", root, SurfaceMaterial.DeskWhite);
            panels.Attach("Panels", root, SurfaceMaterial.DeskWhite);
            AddBoxCollider(root, new Vector3(0f, DeskHeight / 2f, 0f), new Vector3(width, DeskHeight, depth));
        }
    }
}
