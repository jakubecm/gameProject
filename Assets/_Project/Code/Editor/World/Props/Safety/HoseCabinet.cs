using System.Collections.Generic;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Wall fire-hose cabinet, 0.65 x 0.65 x 0.24 m, after a product photo: a glossy red steel box with a flat red door
        /// that has a chrome lock plate on the left and hinges on the right, and a rounded-square window with a black rubber
        /// gasket. Behind the glass: the flat white hose rolled on its red reel (a cross bar on two legs). A wall prop: the
        /// pivot is on the floor at the wall, the cabinet's bottom 0.9 m up, +Z away from the wall.
        /// </summary>
        [Model(Prop.HoseCabinet)]
        private static void BuildHoseCabinet(Transform root)
        {
            const float size = 0.65f;
            const float depth = 0.24f;
            const float bottom = 0.9f;
            float top = bottom + size;
            float half = size / 2f;
            float centerY = bottom + size / 2f;
            var cabinet = new MeshBuilder();
            var door = new MeshBuilder();
            var glass = new MeshBuilder();
            var reel = new MeshBuilder();
            var coil = new MeshBuilder();
            var gasket = new MeshBuilder();
            var metal = new MeshBuilder();
            var slot = new MeshBuilder();
            var tail = new MeshBuilder();
            var coupling = new MeshBuilder();
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
    }
}
