using System.Collections.Generic;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEngine;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Low-profile LED luminaire 1.2 m long (along Z) under the containment roof of a cold aisle: a 70 mm wide tray only 10 mm deep with chamfered
        /// corners (8 mm, in plan) and a bevelled lower edge (a 2.5 mm step), in <see cref="SurfaceMaterial.RackBlack"/>, the colour of the containment roof it hangs
        /// from, and an opal diffuser with chamfered corners that is 2 mm proud of it (12 mm in all), no clips or brackets, glued to the roof panels. Two
        /// downward spot lights (one near each end) light the aisle evenly; pivot on the roof's underside.
        /// </summary>
        [Model(Prop.AisleLight)]
        private static void BuildAisleLight(Transform root)
        {
            const float length = 1.2f;
            var body = new MeshBuilder();
            var diffuser = new MeshBuilder();
            body.AddPrism(ChamferedOutline(0.035f, length / 2f, 0.008f), -0.0075f, 0f, Vector3.zero);
            body.AddPrism(ChamferedOutline(0.0325f, length / 2f - 0.0025f, 0.0065f), -0.010f, -0.0075f, Vector3.zero);
            diffuser.AddPrism(ChamferedOutline(0.026f, length / 2f - 0.014f, 0.005f), -0.0122f, -0.0098f, Vector3.zero);
            body.Attach("Body", root, SurfaceMaterial.RackBlack);
            diffuser.Attach("Diffuser", root, SurfaceMaterial.LightPanel);
            foreach (int end in new[] { -1, 1 })
            {
                AddSpotLight(root, new Vector3(0f, -0.03f, end * 0.3f), 150f, 100f, 4.5f, 2.4f, LedPanelColor);
            }
        }

        /// <summary>A rectangle (half sizes <paramref name="hx"/> x <paramref name="hz"/>) with its four corners cut off by <paramref name="chamfer"/>, counter-clockwise from above.</summary>
        private static Vector2[] ChamferedOutline(float hx, float hz, float chamfer) => new[]
        {
            new Vector2(-hx + chamfer, -hz), new Vector2(hx - chamfer, -hz), new Vector2(hx, -hz + chamfer), new Vector2(hx, hz - chamfer),
            new Vector2(hx - chamfer, hz), new Vector2(-hx + chamfer, hz), new Vector2(-hx, hz - chamfer), new Vector2(-hx, -hz + chamfer),
        };
    }
}
