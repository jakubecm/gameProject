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
        /// Perforated raised-floor tile 0.6 x 0.6 m (cold aisle air supply), lying on the floor, after a product photo: a steel panel powder-coated in the grey of the hall floor,
        /// perforated with staggered round holes at a 25 mm pitch (a photographic-style texture with chamfered rims, paint grain and a dark plenum in the holes)
        /// in a 20 mm rolled frame of the same colour, over the dark plenum. Its top is 12 mm above the floor.
        /// </summary>
        [Model(Prop.PerforatedFloorTile)]
        private static void BuildPerforatedFloorTile(Transform root)
        {
            const float half = 0.3f;
            const float top = 0.012f;
            const float rim = 0.02f;
            var panel = new MeshBuilder();
            var frame = new MeshBuilder();
            var plenum = new MeshBuilder();
            plenum.AddBox(new Vector3(-half + 0.004f, 0f, -half + 0.004f), new Vector3(half - 0.004f, 0.002f, half - 0.004f));
            // The texture holds 4 x 4 holes at a 25 mm pitch, 0.1 m square.
            float uv = 2f * (half - rim) / 0.1f;
            panel.AddPanel(new Vector3(-half + rim, top - 0.001f, -half + rim), new Vector3(half - rim, top - 0.001f, -half + rim), new Vector3(half - rim, top - 0.001f, half - rim),
                new Vector3(-half + rim, top - 0.001f, half - rim), Vector3.up, Vector2.zero, new Vector2(uv, 0f), new Vector2(uv, uv), new Vector2(0f, uv));
            // The frame: four rolled edges, with a small step down to the panel.
            frame.AddBox(new Vector3(-half, 0.002f, -half), new Vector3(half, top, -half + rim));
            frame.AddBox(new Vector3(-half, 0.002f, half - rim), new Vector3(half, top, half));
            frame.AddBox(new Vector3(-half, 0.002f, -half + rim), new Vector3(-half + rim, top, half - rim));
            frame.AddBox(new Vector3(half - rim, 0.002f, -half + rim), new Vector3(half, top, half - rim));
            plenum.Attach("Plenum", root, SurfaceMaterial.Upholstery);
            panel.Attach("Panel", root, SurfaceMaterial.FloorTilePerforated);
            frame.Attach("Frame", root, SurfaceMaterial.HallFloor);
        }
    }
}
