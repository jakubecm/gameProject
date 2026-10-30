using System.Collections.Generic;
using UnityEngine;
using static ColdShutdown.Editor.GrayboxMeshBuilder;

namespace ColdShutdown.Editor
{
    /// <summary>The office swivel chair, modelled in detail after a product photo (a black chair with a high two-part backrest).</summary>
    public static partial class GrayboxPropsTool
    {
        /// <summary>
        /// Office swivel chair, after a product photo: a five-star base of flat tapering legs with twin-wheel casters, a gas
        /// lift with a dust cover and the height knob under the seat, a rounded black leatherette seat with a quilted rear
        /// part, a high backrest (smooth upper third, quilted lower part) on a spine bracket from under the seat, and two
        /// armrests that are closed rounded-rectangle loops of thick black band fixed to the sides of the backrest. 0.58 m across
        /// the arms, 0.66 m across the base, seat 0.47 m high, 1.05 m tall. A seated person faces +Z.
        /// </summary>
        private static void BuildOfficeChair(Transform root)
        {
            var plastic = new GrayboxMeshBuilder();
            var matt = new GrayboxMeshBuilder();
            var chrome = new GrayboxMeshBuilder();
            var leather = new GrayboxMeshBuilder();
            var quilt = new GrayboxMeshBuilder();
            const float quiltTile = 0.13f;

            // Star base: five flat legs rising a little from the rim to the hub, each ending in a swivel fork with two wheels.
            for (int i = 0; i < 5; i++)
            {
                var turn = Quaternion.Euler(0f, i * 72f, 0f);
                matt.AddStrip(new List<Vector3> { new Vector3(0f, 0.082f, 0.02f), new Vector3(0f, 0.070f, 0.14f), new Vector3(0f, 0.062f, 0.30f) },
                    Vector3.right, 0.040f, 0.026f, Vector3.zero, turn);
                var caster = turn * new Vector3(0f, 0f, 0.29f);
                plastic.AddLathe(new[] { P(0f, 0.052f, true), P(0.014f, 0.052f, true), P(0.014f, 0.066f, true), P(0f, 0.066f, true) }, 16, true, caster, Quaternion.identity);
                foreach (int side in new[] { -1, 1 })
                {
                    var wheelAt = caster + turn * new Vector3(side * 0.0115f, 0.025f, 0.0f);
                    plastic.AddLathe(new[] { P(0f, -0.0085f, true), P(0.0235f, -0.0085f, true), P(0.0250f, -0.0060f), P(0.0250f, 0.0060f), P(0.0235f, 0.0085f, true), P(0f, 0.0085f, true) },
                        28, true, wheelAt, turn * Quaternion.Euler(0f, 0f, 90f));
                }
                // The fork: two thin cheeks round the wheels and a bridge over them.
                foreach (int side in new[] { -1, 1 })
                {
                    var cheek = caster + turn * new Vector3(side * 0.0215f, 0.0f, 0f);
                    matt.AddStrip(new List<Vector3> { cheek + new Vector3(0f, 0.012f, 0f), cheek + new Vector3(0f, 0.052f, 0f) }, turn * Vector3.forward, 0.030f, 0.004f, Vector3.zero, Quaternion.identity);
                }
            }

            // Hub and gas lift: a cone over the leg roots, the black outer sleeve, the slim inner column, the bellows cover and the knob.
            matt.AddLathe(new[]
            {
                P(0f, 0.045f, true), P(0.062f, 0.050f), P(0.056f, 0.078f), P(0.034f, 0.100f), P(0.027f, 0.110f, true), P(0f, 0.110f, true),
            }, 40, true, Vector3.zero, Quaternion.identity);
            plastic.AddLathe(new[] { P(0.0265f, 0.100f, true), P(0.0265f, 0.255f, true), P(0.0245f, 0.262f, true), P(0f, 0.262f, true) }, 36, true, Vector3.zero, Quaternion.identity);
            chrome.AddLathe(new[] { P(0.0195f, 0.255f, true), P(0.0195f, 0.352f, true), P(0f, 0.352f, true) }, 32, true, Vector3.zero, Quaternion.identity);
            var bellows = new List<ProfilePoint> { P(0.036f, 0.325f, true) };
            for (float y = 0.325f; y < 0.383f; y += 0.012f)
            {
                bellows.Add(P(0.044f, y + 0.006f));
                bellows.Add(P(0.036f, y + 0.012f));
            }
            bellows.Add(P(0f, 0.385f, true));
            plastic.AddLathe(bellows, 36, true, Vector3.zero, Quaternion.identity);
            // Height adjustment knob to the side under the seat (ribbed black cylinder on a short stem).
            plastic.AddLathe(new[] { P(0.012f, -0.004f, true), P(0.012f, 0.050f, true), P(0.014f, 0.052f, true), P(0.014f, 0.074f, true), P(0.012f, 0.076f, true), P(0f, 0.076f, true) },
                24, true, new Vector3(0.020f, 0.340f, 0.04f), Quaternion.Euler(0f, 0f, -90f));

            // Seat: rounded pad, a metal-grey mounting plate under it, and a quilted panel on its rear part.
            matt.AddRoundedBlock(new Vector3(0f, 0.375f, 0.015f), 0.40f, 0.38f, 0.04f, 0.020f, 0.004f, 5);
            leather.AddRoundedBlock(new Vector3(0f, 0.392f, 0.02f), 0.480f, 0.470f, 0.075f, 0.078f, 0.030f, 8);
            leather.AddPanel(new Vector3(-0.21f, 0.392f, -0.185f), new Vector3(0.21f, 0.392f, -0.185f), new Vector3(0.21f, 0.392f, 0.225f), new Vector3(-0.21f, 0.392f, 0.225f), Vector3.down,
                Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            float seatTop = 0.392f + 0.078f + 0.0006f;
            float qx = 0.190f, qz0 = -0.190f, qz1 = 0.020f;
            quilt.AddPanel(new Vector3(-qx, seatTop, qz0), new Vector3(qx, seatTop, qz0), new Vector3(qx, seatTop, qz1), new Vector3(-qx, seatTop, qz1), Vector3.up,
                new Vector2(-qx, qz0) / quiltTile, new Vector2(qx, qz0) / quiltTile, new Vector2(qx, qz1) / quiltTile, new Vector2(-qx, qz1) / quiltTile);

            // Backrest: tall rounded pad (smooth top), quilted over its lower 60 %, hung from the spine bracket.
            const float backBottom = 0.540f;
            const float backHeight = 0.510f;
            const float backZ = -0.275f;
            const float backDepth = 0.065f;
            leather.AddRoundedSlab(new Vector3(0f, backBottom + backHeight / 2f, backZ), 0.470f, backHeight, 0.060f, backDepth, 0.022f, 8, true);
            float faceZ = backZ + backDepth + 0.0006f;
            float panelTop = backBottom + backHeight * 0.60f;
            float px = 0.225f;
            float py0 = backBottom + 0.012f;
            quilt.AddPanel(new Vector3(px, py0, faceZ), new Vector3(-px, py0, faceZ), new Vector3(-px, panelTop, faceZ), new Vector3(px, panelTop, faceZ), Vector3.forward,
                new Vector2(-px, py0) / quiltTile, new Vector2(px, py0) / quiltTile, new Vector2(px, panelTop) / quiltTile, new Vector2(-px, panelTop) / quiltTile);
            // The spine bracket stays inside the backrest: its rear face never goes behind the back of the pad (z = backZ).
            matt.AddStrip(new List<Vector3>
            {
                new Vector3(0f, 0.368f, 0.00f), new Vector3(0f, 0.368f, -0.170f), new Vector3(0f, 0.400f, -0.215f), new Vector3(0f, 0.520f, -0.242f), new Vector3(0f, 0.600f, -0.246f),
            }, Vector3.right, 0.060f, 0.016f, Vector3.zero, Quaternion.identity);

            // Armrests: a closed rounded-rectangle loop of thick black band (30 mm in the plane, 40 mm across) on each side, standing in the
            // YZ plane against the side of the seat pad and the backrest: tall, its lower run lying along the seat's side, so it is fixed
            // to the seat as well as the backrest (no separate strut).
            foreach (int side in new[] { -1, 1 })
            {
                float x = side * 0.258f;
                var loop = new List<Vector3>();
                const float zFront = 0.205f;
                const float zBack = -0.250f;
                const float yBottom = 0.415f;
                const float yTop = 0.735f;
                const float r = 0.050f;
                var centres = new[] { new Vector2(zFront - r, yTop - r), new Vector2(zBack + r, yTop - r), new Vector2(zBack + r, yBottom + r), new Vector2(zFront - r, yBottom + r) };
                for (int corner = 0; corner < 4; corner++)
                {
                    for (int step = 0; step <= 8; step++)
                    {
                        float angle = (90f * corner + 90f * step / 8f) * Mathf.Deg2Rad;
                        loop.Add(new Vector3(x, centres[corner].y + Mathf.Sin(angle) * r, centres[corner].x + Mathf.Cos(angle) * r));
                    }
                }
                loop.Add(loop[0]);
                plastic.AddStrip(loop, Vector3.right, 0.040f, 0.030f, Vector3.zero, Quaternion.identity);
                // Mounting plate: a flat plate on the backrest side, behind the loop's rear bar.
                plastic.AddBox(new Vector3(Mathf.Min(side * 0.232f, x), 0.560f, -0.275f), new Vector3(Mathf.Max(side * 0.232f, x), 0.720f, -0.205f));
            }

            matt.Attach("Chair_Base", root, SurfaceMaterial.MouseBlack);
            plastic.Attach("Chair_Plastic", root, SurfaceMaterial.BlackHousing);
            chrome.Attach("Chair_Piston", root, SurfaceMaterial.Chrome);
            leather.Attach("Chair_Pads", root, SurfaceMaterial.ChairLeather);
            quilt.Attach("Chair_Quilting", root, SurfaceMaterial.ChairQuilt);
            AddBoxCollider(root, new Vector3(0f, 0.53f, 0f), new Vector3(0.6f, 1.06f, 0.6f));
        }
    }
}
