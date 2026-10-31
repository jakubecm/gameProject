using System.Collections.Generic;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Illuminated emergency exit sign 360 x 160 mm, 50 mm deep: a white housing with the supplied design on its face (white
        /// "EXIT" on a green rounded panel with a white border), lit from inside (emissive). The face looks along +Z.
        /// </summary>
        [Model(Prop.ExitSign)]
        private static void BuildExitSign(Transform root)
        {
            const float width = 0.36f;
            const float height = 0.16f;
            const float depth = 0.05f;
            var body = new MeshBuilder();
            var face = new MeshBuilder();
            body.AddBox(new Vector3(-width / 2f, -height / 2f, 0f), new Vector3(width / 2f, height / 2f, depth - 0.0012f));
            float x = width / 2f - 0.004f;
            float y = height / 2f - 0.004f;
            float z = depth;
            // Viewed from +Z the left is +X, so u runs from +X to -X.
            face.AddPanel(new Vector3(-x, -y, z), new Vector3(x, -y, z), new Vector3(x, y, z), new Vector3(-x, y, z), Vector3.forward,
                new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f));
            body.Attach("Body", root, SurfaceMaterial.Trunking);
            face.Attach("Face", root, SurfaceMaterial.ExitSignFace);
        }
    }
}
