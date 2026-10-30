using System.Collections.Generic;
using UnityEngine;
using static ColdShutdown.Editor.GrayboxMeshBuilder;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Photo-based safety and access props: the red manual call point, the 6 kg ABC extinguisher, the wall hose-reel cabinet,
    /// the RFID reader and the four-way ceiling diffuser. Same method and conventions as <c>GrayboxPropsTool.Detail.cs</c>.
    /// </summary>
    public static partial class GrayboxPropsTool
    {
        /// <summary>
        /// Surface-mounted manual call point, 92 x 92 x 52 mm: a rounded red housing with a lid ridge, a raised frame round
        /// the recessed white window with the push symbol (a circle between two arrows), the fire pictogram above it and
        /// a status lens below. The pivot is the middle of its back, +Z away from the wall.
        /// </summary>
        private static void BuildFireCallPoint(Transform root)
        {
            const float width = 0.092f;
            const float depth = 0.052f;
            const float cy = -0.004f;
            var body = new GrayboxMeshBuilder();
            var panel = new GrayboxMeshBuilder();
            var black = new GrayboxMeshBuilder();
            var white = new GrayboxMeshBuilder();
            var lens = new GrayboxMeshBuilder();
            var toZ = Quaternion.Euler(90f, 0f, 0f);

            body.AddRoundedSlab(Vector3.zero, width, width, 0.010f, depth, 0.006f);
            body.AddBox(new Vector3(-width / 2f - 0.001f, 0.0305f, 0f), new Vector3(width / 2f + 0.001f, 0.0345f, depth - 0.004f), true, false);

            // Raised frame (outer 80 x 54, opening 66 x 40) round the recessed window.
            const float frame = 0.007f;
            body.AddBox(new Vector3(-0.040f, cy + 0.027f - frame, depth - 0.001f), new Vector3(0.040f, cy + 0.027f, depth + 0.004f));
            body.AddBox(new Vector3(-0.040f, cy - 0.027f, depth - 0.001f), new Vector3(0.040f, cy - 0.027f + frame, depth + 0.004f));
            body.AddBox(new Vector3(-0.040f, cy - 0.027f + frame, depth - 0.001f), new Vector3(-0.040f + frame, cy + 0.027f - frame, depth + 0.004f));
            body.AddBox(new Vector3(0.040f - frame, cy - 0.027f + frame, depth - 0.001f), new Vector3(0.040f, cy + 0.027f - frame, depth + 0.004f));
            panel.AddBox(new Vector3(-0.033f, cy - 0.020f, depth - 0.001f), new Vector3(0.033f, cy + 0.020f, depth + 0.0008f), true, false);

            // Symbol: a circle between two arrows pointing at it.
            float zs = depth + 0.0008f;
            black.AddLathe(new[] { P(0.0078f, 0.0003f, true), P(0f, 0.0003f, true) }, 32, true, new Vector3(0f, cy, zs), toZ);
            foreach (float side in new[] { -1f, 1f })
            {
                float x0 = side * 0.030f;
                float x1 = side * 0.016f;
                black.AddBox(new Vector3(Mathf.Min(x0, x1), cy - 0.0022f, zs), new Vector3(Mathf.Max(x0, x1), cy + 0.0022f, zs + 0.0003f), true, false);
                black.AddFlatTriangle(new Vector3(x1, cy + 0.0075f, zs + 0.0003f), new Vector3(side * 0.0075f, cy, zs + 0.0003f),
                    new Vector3(x1, cy - 0.0075f, zs + 0.0003f), Vector3.forward);
            }

            // Fire pictogram above the window: a house outline with a flame in it.
            float zp = depth;
            white.AddBox(new Vector3(-0.0112f, 0.0200f, zp), new Vector3(-0.0092f, 0.0292f, zp + 0.0008f), true, false);
            white.AddBox(new Vector3(0.0092f, 0.0200f, zp), new Vector3(0.0112f, 0.0292f, zp + 0.0008f), true, false);
            white.AddBox(new Vector3(-0.0112f, 0.0200f, zp), new Vector3(0.0112f, 0.0218f, zp + 0.0008f), true, false);
            white.AddStrip(new List<Vector3> { new Vector3(-0.0140f, 0.0285f, 0f), new Vector3(0f, 0.0415f, 0f), new Vector3(0.0140f, 0.0285f, 0f) },
                Vector3.forward, 0.0008f, 0.0020f, new Vector3(0f, 0f, zp + 0.0004f), Quaternion.identity);
            white.AddFlatTriangle(new Vector3(-0.0035f, 0.0222f, zp + 0.0008f), new Vector3(0f, 0.0310f, zp + 0.0008f),
                new Vector3(0.0035f, 0.0222f, zp + 0.0008f), Vector3.forward);

            lens.AddLathe(new[] { P(0.0035f, 0.0008f, true), P(0.0025f, 0.0013f), P(0f, 0.0014f) }, 20, true, new Vector3(0f, -0.0385f, depth), toZ);

            body.Attach("Body", root, SurfaceMaterial.SafetyRed);
            panel.Attach("Window", root, SurfaceMaterial.SignFace);
            black.Attach("Symbol", root, SurfaceMaterial.BlackHousing);
            white.Attach("Pictogram", root, SurfaceMaterial.DetectorWhite);
            lens.Attach("Lens", root, SurfaceMaterial.BulbClear);
        }

        /// <summary>A smooth path through <paramref name="points"/> (Catmull-Rom), <paramref name="perSegment"/> samples between each pair.</summary>
        private static List<Vector3> SmoothPath(IList<Vector3> points, int perSegment)
        {
            var path = new List<Vector3>();
            for (int i = 0; i < points.Count - 1; i++)
            {
                var p0 = points[Mathf.Max(i - 1, 0)];
                var p1 = points[i];
                var p2 = points[i + 1];
                var p3 = points[Mathf.Min(i + 2, points.Count - 1)];
                for (int k = 0; k < perSegment; k++)
                {
                    float t = k / (float)perSegment;
                    float t2 = t * t;
                    float t3 = t2 * t;
                    path.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
                }
            }
            path.Add(points[points.Count - 1]);
            return path;
        }

        /// <summary>
        /// Wall-mounted 6 kg ABC powder extinguisher (160 mm across, about 0.6 m high on its bracket), after a product photo:
        /// a glossy red cylinder with a rounded shoulder and rolled foot, a generic printed label on the front, the chrome valve
        /// head with a pressure gauge and the two red levers with a safety wire, and the thick black rubber hose that leaves
        /// the head to the left, sweeps out and down in one smooth curve and is clipped to the body above its black nozzle.
        /// A floor-based wall prop: the pivot is on the floor at the wall, the axis 100 mm out, +Z away from the wall.
        /// </summary>
        private static void BuildFireExtinguisher(Transform root)
        {
            var offset = new Vector3(0f, 0f, 0.10f);
            var none = Quaternion.identity;
            var toZ = Quaternion.Euler(90f, 0f, 0f);
            var red = new GrayboxMeshBuilder();
            var levers = new GrayboxMeshBuilder();
            var rubber = new GrayboxMeshBuilder();
            var label = new GrayboxMeshBuilder();
            var chrome = new GrayboxMeshBuilder();
            var gauge = new GrayboxMeshBuilder();
            var bracket = new GrayboxMeshBuilder();

            red.AddLathe(new[]
            {
                P(0f, 0.545f, true), P(0.0600f, 0.545f, true), P(0.0690f, 0.550f), P(0.0770f, 0.562f), P(0.0800f, 0.582f), P(0.0788f, 0.600f), P(0.0800f, 0.614f),
                P(0.0800f, 0.930f), P(0.0782f, 0.952f), P(0.0720f, 0.980f), P(0.0580f, 1.002f), P(0.0420f, 1.014f), P(0.0300f, 1.020f, true), P(0.0245f, 1.022f, true),
                P(0.0245f, 1.036f, true),
            }, 48, true, offset, none);
            // The label wraps 119 degrees of the straight part of the body, facing +Z, in the proportions of its texture (330 x 562).
            label.AddLathe(new[] { P(0.0806f, 0.630f, true), P(0.0806f, 0.915f, true) }, 36, true, offset, none, 30.6f, 118.8f, true);

            // Valve head: body, hose connector with its nut on the left, gauge on the front, rivet.
            chrome.AddLathe(new[]
            {
                P(0.0300f, 1.034f, true), P(0.0300f, 1.060f), P(0.0278f, 1.075f), P(0.0200f, 1.086f), P(0.0100f, 1.090f, true), P(0f, 1.090f, true),
            }, 32, true, offset, none);
            chrome.AddLathe(new[]
            {
                P(0.0125f, 0f, true), P(0.0125f, 0.004f), P(0.0158f, 0.007f, true), P(0.0158f, 0.021f, true), P(0.0128f, 0.024f, true), P(0.0125f, 0.036f),
                P(0.0108f, 0.040f, true), P(0f, 0.040f, true),
            }, 24, true, new Vector3(-0.020f, 1.060f, 0.10f), Quaternion.Euler(0f, 0f, 90f));
            chrome.AddLathe(new[] { P(0.0165f, 0f, true), P(0.0165f, 0.012f), P(0.0150f, 0.015f, true), P(0f, 0.015f, true) }, 28, true, new Vector3(0f, 1.056f, 0.12f), toZ);
            gauge.AddLathe(new[] { P(0.0135f, 0.0152f, true), P(0f, 0.0152f, true) }, 28, true, new Vector3(0f, 1.056f, 0.12f), toZ);
            chrome.AddLathe(new[] { P(0.0055f, -0.004f, true), P(0.0055f, 0.004f, true), P(0f, 0.004f, true) }, 12, true, new Vector3(0.002f, 1.088f, 0.10f), toZ);

            // Levers: the long top one rises to the right and bends down at its end, the short one below it curves down.
            levers.AddStrip(SmoothPath(new[]
            {
                new Vector3(0.002f, 1.086f, 0f), new Vector3(0.035f, 1.100f, 0f), new Vector3(0.080f, 1.124f, 0f), new Vector3(0.125f, 1.150f, 0f),
                new Vector3(0.146f, 1.156f, 0f), new Vector3(0.152f, 1.147f, 0f),
            }, 6), Vector3.forward, 0.024f, 0.005f, offset, none);
            levers.AddStrip(SmoothPath(new[]
            {
                new Vector3(-0.004f, 1.064f, 0f), new Vector3(0.035f, 1.080f, 0f), new Vector3(0.080f, 1.088f, 0f), new Vector3(0.118f, 1.074f, 0f),
                new Vector3(0.134f, 1.050f, 0f),
            }, 6), Vector3.forward, 0.022f, 0.005f, offset, none);
            // The safety wire between the levers.
            chrome.AddTube(new List<Vector3> { new Vector3(0.112f, 1.140f, 0f), new Vector3(0.126f, 1.108f, 0f), new Vector3(0.108f, 1.076f, 0f) }, 0.0013f, 6, offset, none);

            // The hose: out of the connector, a wide smooth sweep to the left and down, back in to the body above the nozzle.
            rubber.AddTube(SmoothPath(new[]
            {
                new Vector3(-0.058f, 1.060f, 0f), new Vector3(-0.088f, 1.058f, 0f), new Vector3(-0.116f, 1.034f, 0f), new Vector3(-0.130f, 0.985f, 0f),
                new Vector3(-0.126f, 0.910f, 0f), new Vector3(-0.112f, 0.820f, 0f), new Vector3(-0.098f, 0.730f, 0f), new Vector3(-0.090f, 0.660f, 0f),
                new Vector3(-0.0885f, 0.605f, 0f),
            }, 8), 0.0115f, 14, offset, none);
            // Nozzle: a chrome ferrule and the black rubber horn.
            chrome.AddLathe(new[] { P(0.0125f, 0.600f, true), P(0.0125f, 0.616f, true), P(0.0105f, 0.620f, true) }, 20, true, new Vector3(-0.0885f, 0f, 0.10f), none);
            rubber.AddLathe(new[]
            {
                P(0f, 0.540f, true), P(0.0110f, 0.540f, true), P(0.0132f, 0.548f), P(0.0128f, 0.600f, true), P(0f, 0.600f, true),
            }, 20, true, new Vector3(-0.0885f, 0f, 0.10f), none);
            // The clip that holds the hose to the body.
            rubber.AddBox(new Vector3(-0.108f, 0.655f, 0.085f), new Vector3(-0.072f, 0.700f, 0.115f));

            // Wall bracket: a plate, an arm round the neck and a lower cradle.
            bracket.AddBox(new Vector3(-0.025f, 0.955f, 0f), new Vector3(0.025f, 1.040f, 0.004f));
            bracket.AddBox(new Vector3(-0.018f, 1.030f, 0.004f), new Vector3(0.018f, 1.036f, 0.078f));
            bracket.AddBox(new Vector3(-0.030f, 0.690f, 0f), new Vector3(0.030f, 0.730f, 0.012f));

            red.Attach("Body", root, SurfaceMaterial.SafetyRed);
            label.Attach("Label", root, SurfaceMaterial.ExtinguisherLabel);
            chrome.Attach("Head", root, SurfaceMaterial.Chrome);
            gauge.Attach("GaugeFace", root, SurfaceMaterial.DetectorWhite);
            levers.Attach("Levers", root, SurfaceMaterial.SafetyRed);
            rubber.Attach("Hose", root, SurfaceMaterial.BlackHousing);
            bracket.Attach("Bracket", root, SurfaceMaterial.Metal);
        }

        /// <summary>
        /// Wall fire-hose cabinet, 0.65 x 0.65 x 0.24 m, after a product photo: a glossy red steel box with a flat red door
        /// that has a chrome lock plate on the left and hinges on the right, and a rounded-square window with a black rubber
        /// gasket. Behind the glass: the flat white hose rolled on its red reel (a cross bar on two legs). A wall prop: the
        /// pivot is on the floor at the wall, the cabinet's bottom 0.9 m up, +Z away from the wall.
        /// </summary>
        private static void BuildHydrantCabinet(Transform root)
        {
            const float size = 0.65f;
            const float depth = 0.24f;
            const float bottom = 0.9f;
            float top = bottom + size;
            float half = size / 2f;
            float centerY = bottom + size / 2f;
            var cabinet = new GrayboxMeshBuilder();
            var door = new GrayboxMeshBuilder();
            var glass = new GrayboxMeshBuilder();
            var reel = new GrayboxMeshBuilder();
            var coil = new GrayboxMeshBuilder();
            var gasket = new GrayboxMeshBuilder();
            var metal = new GrayboxMeshBuilder();
            var slot = new GrayboxMeshBuilder();
            var tail = new GrayboxMeshBuilder();
            var coupling = new GrayboxMeshBuilder();
            var toZ = Quaternion.Euler(90f, 0f, 0f);

            // The box: back and four walls.
            cabinet.AddBox(new Vector3(-half, bottom, 0f), new Vector3(half, top, 0.010f), true, false);
            cabinet.AddBox(new Vector3(-half, bottom, 0f), new Vector3(-half + 0.012f, top, depth), true, false);
            cabinet.AddBox(new Vector3(half - 0.012f, bottom, 0f), new Vector3(half, top, depth), true, false);
            cabinet.AddBox(new Vector3(-half, top - 0.012f, 0f), new Vector3(half, top, depth), true, false);
            cabinet.AddBox(new Vector3(-half, bottom, 0f), new Vector3(half, bottom + 0.012f, depth), true, false);

            // The door: a flat panel round a square window opening (0.40 m), flush with the front of the box.
            const float opening = 0.20f;
            float doorHalf = half - 0.012f;
            float z0 = depth - 0.014f;
            door.AddBox(new Vector3(-doorHalf, centerY + opening, z0), new Vector3(doorHalf, centerY + doorHalf, depth));
            door.AddBox(new Vector3(-doorHalf, centerY - doorHalf, z0), new Vector3(doorHalf, centerY - opening, depth));
            door.AddBox(new Vector3(-doorHalf, centerY - opening, z0), new Vector3(-opening, centerY + opening, depth));
            door.AddBox(new Vector3(opening, centerY - opening, z0), new Vector3(doorHalf, centerY + opening, depth));
            glass.AddBox(new Vector3(-opening, centerY - opening, z0 + 0.002f), new Vector3(opening, centerY + opening, z0 + 0.005f));

            // Black rubber gasket round the window: one closed ring of constant section along a rounded square, so it is
            // the same width everywhere (no narrowing at the corners or where the straights join the arcs).
            var loop = new List<Vector3>();
            const float gasketHalf = opening + 0.004f;
            const float cornerRadius = 0.045f;
            const int cornerSteps = 10;
            const int sideSteps = 6;
            foreach (var corner in new[] { new Vector2(1f, 1f), new Vector2(-1f, 1f), new Vector2(-1f, -1f), new Vector2(1f, -1f) })
            {
                float startAngle = Mathf.Atan2(corner.y, corner.x) * Mathf.Rad2Deg - 45f;
                for (int i = 0; i < cornerSteps; i++)
                {
                    float angle = (startAngle + 90f * i / cornerSteps) * Mathf.Deg2Rad;
                    loop.Add(new Vector3(corner.x * (gasketHalf - cornerRadius) + Mathf.Cos(angle) * cornerRadius,
                        centerY + corner.y * (gasketHalf - cornerRadius) + Mathf.Sin(angle) * cornerRadius, depth + 0.003f));
                }
                // The straight run to the next corner (the arc above ends on the side that leads to it).
                var arcEnd = new Vector3(corner.x * (gasketHalf - cornerRadius) + Mathf.Cos((startAngle + 90f) * Mathf.Deg2Rad) * cornerRadius,
                    centerY + corner.y * (gasketHalf - cornerRadius) + Mathf.Sin((startAngle + 90f) * Mathf.Deg2Rad) * cornerRadius, depth + 0.003f);
                loop.Add(arcEnd);
                var nextCorner = new Vector2(-corner.y, corner.x);
                float nextStart = Mathf.Atan2(nextCorner.y, nextCorner.x) * Mathf.Rad2Deg - 45f;
                var nextBegin = new Vector3(nextCorner.x * (gasketHalf - cornerRadius) + Mathf.Cos(nextStart * Mathf.Deg2Rad) * cornerRadius,
                    centerY + nextCorner.y * (gasketHalf - cornerRadius) + Mathf.Sin(nextStart * Mathf.Deg2Rad) * cornerRadius, depth + 0.003f);
                for (int i = 1; i < sideSteps; i++)
                {
                    loop.Add(Vector3.Lerp(arcEnd, nextBegin, i / (float)sideSteps));
                }
            }
            gasket.AddTube(loop, 0.0105f, 14, Vector3.zero, Quaternion.identity, true);

            // Hose reel: a red tripod frame (cross bar on two legs and a post) in front of the white coiled hose.
            var axis = new Vector3(-0.015f, centerY, 0.115f);
            var ridges = new List<ProfilePoint> { P(0.165f, -0.040f, true), P(0.165f, 0.040f, true) };
            for (float r = 0.165f; r > 0.052f; r -= 0.0085f)
            {
                ridges.Add(P(r, 0.040f, true));
                ridges.Add(P(r - 0.0042f, 0.0435f, true));
            }
            ridges.Add(P(0.045f, 0.040f, true));
            ridges.Add(P(0.045f, -0.040f, true));
            coil.AddLathe(ridges, 48, true, axis, toZ);
            metal.AddLathe(new[] { P(0.030f, 0.030f, true), P(0.030f, 0.060f), P(0.020f, 0.066f, true), P(0f, 0.066f, true) }, 20, true, axis, toZ);
            float zf = axis.z + 0.075f;
            reel.AddBox(new Vector3(-0.185f, centerY - 0.012f, zf), new Vector3(0.185f, centerY + 0.012f, zf + 0.012f));
            reel.AddBox(new Vector3(0.165f, centerY - 0.185f, zf), new Vector3(0.190f, centerY + 0.185f, zf + 0.020f));
            foreach (float side in new[] { -1f, 1f })
            {
                reel.AddStrip(new List<Vector3> { new Vector3(side * 0.035f, centerY, 0f), new Vector3(side * 0.095f, centerY - 0.195f, 0f) },
                    Vector3.forward, 0.012f, 0.018f, new Vector3(-0.015f, 0f, zf + 0.006f), Quaternion.identity);
            }

            // The reel itself: a core the hose is wound on, a rim ring on the front and a disc behind the coil, and on the axis
            // a brass swivel that leads through the back of the cabinet (the hose is fed from the hub through it).
            reel.AddLathe(new[] { P(0.040f, -0.046f, true), P(0.047f, -0.046f, true), P(0.047f, 0.046f, true), P(0.040f, 0.046f, true) }, 32, true, axis, toZ);
            reel.AddLathe(new[] { P(0.047f, -0.0475f, true), P(0.178f, -0.0475f, true), P(0.178f, -0.0435f, true), P(0.047f, -0.0435f, true) }, 48, true, axis, toZ);
            reel.AddLathe(new[] { P(0.166f, 0.0435f, true), P(0.178f, 0.0435f, true), P(0.178f, 0.0475f, true), P(0.166f, 0.0475f, true) }, 48, true, axis, toZ);
            coupling.AddLathe(new[]
            {
                P(0.034f, -axis.z, true), P(0.034f, -axis.z + 0.008f, true), P(0.022f, -axis.z + 0.008f, true), P(0.022f, -0.070f, true),
                P(0.030f, -0.070f, true), P(0.030f, -0.056f, true), P(0.022f, -0.056f, true), P(0.022f, -0.0475f, true),
            }, 24, true, axis, toZ);

            // The free end of the hose: it leaves the outermost wrap of the coil on its right side tangentially, hangs down and
            // finishes in a brass coupling and a chrome nozzle, so it is one continuous hose from the reel.
            float hoseZ = axis.z;
            const float wrapRadius = 0.155f;
            var tailPoints = new List<Vector3>();
            for (int degrees = 50; degrees >= 0; degrees -= 10)
            {
                float angle = degrees * Mathf.Deg2Rad;
                tailPoints.Add(new Vector3(axis.x + Mathf.Cos(angle) * wrapRadius, centerY + Mathf.Sin(angle) * wrapRadius, hoseZ));
            }
            tailPoints.Add(new Vector3(axis.x + wrapRadius + 0.004f, centerY - 0.060f, hoseZ));
            tailPoints.Add(new Vector3(axis.x + wrapRadius + 0.010f, centerY - 0.115f, hoseZ));
            tailPoints.Add(new Vector3(axis.x + wrapRadius + 0.006f, centerY - 0.165f, hoseZ));
            tailPoints.Add(new Vector3(axis.x + wrapRadius - 0.004f, centerY - 0.205f, hoseZ));
            var tailPath = SmoothPath(tailPoints, 4);
            tail.AddTube(tailPath, 0.0135f, 14, Vector3.zero, Quaternion.identity);
            var tailEnd = tailPath[tailPath.Count - 1];
            var tailDirection = (tailEnd - tailPath[tailPath.Count - 2]).normalized;
            var tailTurn = Quaternion.FromToRotation(Vector3.up, tailDirection);
            coupling.AddLathe(new[]
            {
                P(0.0135f, -0.004f, true), P(0.0165f, -0.004f, true), P(0.0165f, 0.026f, true), P(0.0135f, 0.028f, true), P(0f, 0.028f, true),
            }, 20, true, tailEnd, tailTurn);
            metal.AddLathe(new[]
            {
                P(0.0090f, 0.026f, true), P(0.0090f, 0.072f), P(0.0115f, 0.080f, true), P(0.0115f, 0.090f, true), P(0.0070f, 0.094f, true), P(0f, 0.094f, true),
            }, 20, true, tailEnd, tailTurn);

            // Chrome lock plate on the left of the door with its dark slot.
            metal.AddBox(new Vector3(-doorHalf + 0.010f, centerY - 0.055f, depth), new Vector3(-doorHalf + 0.038f, centerY + 0.055f, depth + 0.004f));
            slot.AddBox(new Vector3(-doorHalf + 0.019f, centerY - 0.016f, depth + 0.004f), new Vector3(-doorHalf + 0.029f, centerY + 0.034f, depth + 0.0045f), true, false);
            // Hinges on the right.
            foreach (float y in new[] { bottom + 0.09f, top - 0.14f })
            {
                door.AddBox(new Vector3(half - 0.010f, y, depth - 0.008f), new Vector3(half + 0.004f, y + 0.05f, depth + 0.004f));
            }

            cabinet.Attach("Cabinet", root, SurfaceMaterial.SafetyRed);
            door.Attach("Door", root, SurfaceMaterial.SafetyRed);
            glass.Attach("Pane", root, SurfaceMaterial.Glass);
            gasket.Attach("Gasket", root, SurfaceMaterial.BlackHousing);
            coil.Attach("HoseCoil", root, SurfaceMaterial.HoseWhite);
            tail.Attach("HoseEnd", root, SurfaceMaterial.HoseWhite);
            coupling.Attach("HoseCoupling", root, SurfaceMaterial.Brass);
            reel.Attach("ReelFrame", root, SurfaceMaterial.SafetyRed);
            metal.Attach("Chrome", root, SurfaceMaterial.Chrome);
            slot.Attach("LockSlot", root, SurfaceMaterial.BlackHousing);
            AddBoxCollider(root, new Vector3(0f, centerY, depth / 2f), new Vector3(size, size, depth));
        }

        /// <summary>
        /// RFID access reader, 55 x 125 x 20 mm: a black rounded housing with a bevelled edge and a vertical slot near the top
        /// whose light strip shows the lock state (green unlocked, red locked: the door swaps its material). The pivot is the
        /// middle of the back at <paramref name="position"/>, facing yaw.
        /// </summary>
        private static Renderer BuildReader(Transform root, string name, Vector3 position, float yaw)
        {
            const float width = 0.055f;
            const float height = 0.125f;
            const float depth = 0.020f;
            var reader = GrayboxUtility.CreateGroup(name, root);
            reader.localPosition = position;
            reader.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var body = new GrayboxMeshBuilder();
            var slot = new GrayboxMeshBuilder();
            var light = new GrayboxMeshBuilder();

            body.AddRoundedSlab(Vector3.zero, width, height, 0.007f, depth, 0.0035f, 4);
            slot.AddBox(new Vector3(-0.0036f, 0.014f, depth - 0.0002f), new Vector3(0.0036f, 0.056f, depth + 0.0003f), true, false);
            light.AddBox(new Vector3(-0.0022f, 0.017f, depth + 0.0003f), new Vector3(0.0022f, 0.053f, depth + 0.0007f), true, false);

            body.Attach($"{name}_Body", reader, SurfaceMaterial.ReaderBlack);
            slot.Attach($"{name}_Slot", reader, SurfaceMaterial.Screen);
            var led = light.Attach($"{name}_Led", reader, SurfaceMaterial.SafetyGreen);
            return led.GetComponent<MeshRenderer>();
        }

        /// <summary>
        /// Four-way ceiling air diffuser for one 600 mm tile, white, after a product photo: a flat frame in the grid with a
        /// dark throat and, stepping down from it, three nested inverted pyramids (sloped square bands, each narrower and
        /// lower, with a dark gap round each) and a small pyramid in the middle. The pivot is on the ceiling surface.
        /// </summary>
        private static void BuildCeilingDiffuser(Transform root)
        {
            var white = new GrayboxMeshBuilder();
            var dark = new GrayboxMeshBuilder();
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
