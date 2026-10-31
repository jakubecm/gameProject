using System.Collections.Generic;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Four-way ceiling air diffuser for one 600 mm tile, white, after a product photo: a flat frame in the grid with a
        /// dark throat and, stepping down from it, three nested inverted pyramids (sloped square bands, each narrower and
        /// lower, with a dark gap round each) and a small pyramid in the middle. The pivot is on the ceiling surface.
        /// </summary>
        [Model(Prop.CeilingDiffuser)]
        private static void BuildCeilingDiffuser(Transform root)
        {
            var white = new MeshBuilder();
            var dark = new MeshBuilder();
            const float top = -0.0120f;
            const float flange = -0.0160f;
            const float half = 0.300f;
            const float border = 0.045f;
            var origin = Vector3.zero;
            white.AddBox(new Vector3(-half, flange, -half), new Vector3(half, top, -half + border), false);
            white.AddBox(new Vector3(-half, flange, half - border), new Vector3(half, top, half), false);
            white.AddBox(new Vector3(-half, flange, -half + border), new Vector3(-half + border, top, half - border), false);
            white.AddBox(new Vector3(half - border, flange, -half + border), new Vector3(half, top, half - border), false);
            // The opening's bevel, and the dark throat behind everything.
            white.AddSquareBand(flange, half - border, flange - 0.003f, half - border - 0.012f, origin, false);
            dark.AddBox(new Vector3(-0.255f, flange - 0.0005f, -0.255f), new Vector3(0.255f, flange, 0.255f), false);

            // Nested pyramids: wide edge at the top, narrow at the bottom, the next one starting inside that gap.
            white.AddSquareBand(-0.0170f, 0.240f, -0.0270f, 0.198f, origin);
            white.AddSquareBand(-0.0270f, 0.172f, -0.0380f, 0.132f, origin);
            white.AddSquareBand(-0.0380f, 0.104f, -0.0480f, 0.070f, origin);
            white.AddSquareBand(-0.0480f, 0.046f, -0.0570f, 0.000f, origin);
            // Thin cross struts that carry the cones, visible across the dark gaps.
            foreach (float angle in new[] { 0f, 90f })
            {
                var turn = Quaternion.Euler(0f, angle, 0f);
                white.AddStrip(new List<Vector3> { new Vector3(-0.245f, -0.0200f, 0f), new Vector3(0.245f, -0.0200f, 0f) }, Vector3.forward, 0.004f, 0.003f, origin, turn);
            }

            white.Attach("Diffuser", root, SurfaceMaterial.SpeakerWhite);
            dark.Attach("Throat", root, SurfaceMaterial.BlackHousing);
        }
    }
}
