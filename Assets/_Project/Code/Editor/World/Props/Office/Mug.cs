using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        [Model(Prop.Mug)]
        private static void BuildMug(Transform root) => BuildMug(root, Vector3.zero);

        /// <summary>
        /// A white ceramic mug standing on <paramref name="position"/>, after a product photo: 83 mm tall, 85 mm across the rim and
        /// narrowing to the base, rounded at the bottom edge, thin walls with a rolled rim and the open inside, and a handle
        /// like a rounded triangle on +X that joins the wall near the rim and again lower down.
        /// </summary>
        private static void BuildMug(Transform root, Vector3 position)
        {
            var ceramic = new MeshBuilder();
            ceramic.AddLathe(new[]
            {
                P(0f, 0f, true), P(0.0255f, 0f, true), P(0.0300f, 0.0016f), P(0.0330f, 0.0065f), P(0.0342f, 0.0125f), P(0.0426f, 0.0814f, true),
                P(0.0420f, 0.0829f), P(0.0405f, 0.0834f), P(0.0392f, 0.0829f), P(0.0386f, 0.0814f, true),
                P(0.0307f, 0.0125f, true), P(0.0292f, 0.0085f), P(0.0262f, 0.0064f, true), P(0f, 0.0064f, true),
            }, 48, true, position, Quaternion.identity);
            ceramic.AddTube(SmoothPath(new[]
            {
                new Vector3(0.0395f, 0.0670f, 0f), new Vector3(0.0500f, 0.0700f, 0f), new Vector3(0.0615f, 0.0660f, 0f), new Vector3(0.0680f, 0.0560f, 0f),
                new Vector3(0.0690f, 0.0480f, 0f), new Vector3(0.0640f, 0.0390f, 0f), new Vector3(0.0545f, 0.0335f, 0f), new Vector3(0.0330f, 0.0245f, 0f),
            }, 6), 0.0043f, 14, position, Quaternion.identity);
            ceramic.Attach("Mug", root, SurfaceMaterial.MugCeramic);
        }
    }
}
