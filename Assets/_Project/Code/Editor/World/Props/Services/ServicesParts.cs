using System.Collections.Generic;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Louvered ventilation grille in a white painted steel frame, after a product photo: a stepped flat border, a dark
        /// recess and fixed slanted louvers that overlap like slats, sloping down and out. Wall props.
        /// </summary>
        private static void BuildLouveredGrille(Transform root, float width, float height, float centerY, float depth, float pitch)
        {
            float rim = height > 0.4f ? 0.04f : 0.028f;
            float halfW = width / 2f;
            float halfH = height / 2f;
            var offset = new Vector3(0f, centerY, 0f);
            var frame = new MeshBuilder();
            var back = new MeshBuilder();
            var slats = new MeshBuilder();

            // One flat frame, one depth: it stands only a centimetre proud of the wall.
            frame.AddBox(offset + new Vector3(-halfW, -halfH, 0f), offset + new Vector3(halfW, -halfH + rim, depth), true, false);
            frame.AddBox(offset + new Vector3(-halfW, halfH - rim, 0f), offset + new Vector3(halfW, halfH, depth), true, false);
            frame.AddBox(offset + new Vector3(-halfW, -halfH + rim, 0f), offset + new Vector3(-halfW + rim, halfH - rim, depth), true, false);
            frame.AddBox(offset + new Vector3(halfW - rim, -halfH + rim, 0f), offset + new Vector3(halfW, halfH - rim, depth), true, false);

            float openW = width - 2f * rim;
            float openH = height - 2f * rim;
            back.AddBox(offset + new Vector3(-openW / 2f, -openH / 2f, 0f), offset + new Vector3(openW / 2f, openH / 2f, 0.002f), true, false);

            // Slats: each slopes down towards the room, overlapping the next one down. Few and broad (10 in the large grille, 5 in the
            // small one), so the gaps between them stay clearly visible even without anti-aliasing.
            int louvers = height > 0.4f ? 10 : 5;
            float step = openH / louvers;
            float rise = step * 0.8f;
            for (int i = 0; i < louvers; i++)
            {
                float y = -openH / 2f + step * (i + 0.5f);
                var path = new List<Vector3> { new Vector3(0f, y + rise / 2f, 0.002f), new Vector3(0f, y - rise / 2f, depth - 0.001f) };
                slats.AddStrip(path, Vector3.right, openW, 0.004f, offset, Quaternion.identity);
            }

            frame.Attach("Frame", root, SurfaceMaterial.CeilingGrid);
            back.Attach("Recess", root, SurfaceMaterial.BlackHousing);
            slats.Attach("Louvers", root, SurfaceMaterial.CeilingGrid);
        }

        /// <summary>
        /// The joint where an outlet plate meets the plaster: a 1.5 mm dark line round the plate, so the plate reads as set into
        /// the wall. Lies on the wall surface.
        /// </summary>
        private static void WallJoint(Transform root)
        {
            var joint = new MeshBuilder();
            const float inner = 0.0400f;
            const float outer = 0.0415f;
            joint.AddBox(new Vector3(-outer, -outer, 0f), new Vector3(outer, -inner, 0.0004f), true, false);
            joint.AddBox(new Vector3(-outer, inner, 0f), new Vector3(outer, outer, 0.0004f), true, false);
            joint.AddBox(new Vector3(-outer, -inner, 0f), new Vector3(-inner, inner, 0.0004f), true, false);
            joint.AddBox(new Vector3(inner, -inner, 0f), new Vector3(outer, inner, 0.0004f), true, false);
            joint.Attach("WallJoint", root, SurfaceMaterial.Upholstery);
        }
    }
}
