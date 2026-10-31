using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Air handling unit (CRAH, fan coil) 0.7 x 0.6 m and 1.9 m high with a supply duct of 0.5 x 0.3 m rising to the ceiling, on a dark
        /// plinth: grey sheet-metal body with rounded corners, an air inlet grille with slots, a service hatch with a lever handle, a blue
        /// stripe, a unit tag and a warning sticker centred on the front; the duct has flanged joints and a damper with a quadrant lever.
        /// Two flanged water connections on top at the back (flow at -X, return at +X, 0.2 m off the axis, z = -0.23 m, 1.9 m high) for the
        /// room's wall pipes. The service side faces +Z.
        /// </summary>
        /// <summary>A slab on a front face (+Z) with 45 degree chamfered edges: its base on the face at <paramref name="z"/>, <paramref name="depth"/> proud, the top face inset by <paramref name="inset"/> on every side.</summary>
        private static void AddChamferedSlab(MeshBuilder b, float x0, float x1, float y0, float y1, float z, float depth, float inset)
        {
            var a0 = new Vector3(x0, y0, z);
            var a1 = new Vector3(x1, y0, z);
            var a2 = new Vector3(x1, y1, z);
            var a3 = new Vector3(x0, y1, z);
            float zt = z + depth;
            var t0 = new Vector3(x0 + inset, y0 + inset, zt);
            var t1 = new Vector3(x1 - inset, y0 + inset, zt);
            var t2 = new Vector3(x1 - inset, y1 - inset, zt);
            var t3 = new Vector3(x0 + inset, y1 - inset, zt);
            var uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            b.AddPanel(t0, t1, t2, t3, Vector3.forward, uv[0], uv[1], uv[2], uv[3]);
            b.AddPanel(a0, a1, t1, t0, new Vector3(0f, -1f, 1f), uv[0], uv[1], uv[2], uv[3]);
            b.AddPanel(a1, a2, t2, t1, new Vector3(1f, 0f, 1f), uv[0], uv[1], uv[2], uv[3]);
            b.AddPanel(a2, a3, t3, t2, new Vector3(0f, 1f, 1f), uv[0], uv[1], uv[2], uv[3]);
            b.AddPanel(a3, a0, t0, t3, new Vector3(-1f, 0f, 1f), uv[0], uv[1], uv[2], uv[3]);
        }

        [Model(Prop.AirHandler)]
        private static void BuildAirHandler(Transform root)
        {
            const float halfX = 0.35f;
            const float halfZ = 0.30f;
            const float plinth = 0.08f;
            const float bodyTop = 1.9f;
            float ceiling = RoomContract.CeilingHeight;
            var toZ = Quaternion.Euler(90f, 0f, 0f);

            var body = new MeshBuilder();
            body.AddExtrusion(RoundedRectLoop(-halfX, halfX, -halfZ, halfZ, 0.025f, 3), new Vector3(0f, plinth, 0f), new Vector3(0f, bodyTop, 0f),
                Vector3.right, Vector3.forward, Vector3.down, Vector3.up);
            body.AddExtrusion(RoundedRectLoop(-halfX - 0.012f, halfX + 0.012f, -halfZ - 0.012f, halfZ + 0.012f, 0.03f, 3), new Vector3(0f, bodyTop - 0.02f, 0f), new Vector3(0f, bodyTop + 0.01f, 0f),
                Vector3.right, Vector3.forward, Vector3.down, Vector3.up);
            body.Attach("Body", root, SurfaceMaterial.CabinetGrey);
            var dark = new MeshBuilder();
            dark.AddBox(new Vector3(-halfX + 0.015f, 0f, -halfZ + 0.015f), new Vector3(halfX - 0.015f, plinth, halfZ - 0.015f));
            // Two doors, the upper one (louvered) over the lower one (service hatch), each a slab with 45 degree chamfered edges, touching: the edges show as V grooves, no gap.
            const float doorDepth = 0.010f;
            var doors = new MeshBuilder();
            AddChamferedSlab(doors, -0.30f, 0.30f, 0.98f, 1.82f, halfZ, doorDepth, doorDepth);
            AddChamferedSlab(doors, -0.30f, 0.30f, 0.20f, 0.98f, halfZ, doorDepth, doorDepth);
            doors.Attach("Doors", root, SurfaceMaterial.CabinetGrey);
            float front = halfZ + doorDepth + 0.0005f;
            for (int i = 0; i < 12; i++)
            {
                float y = 1.12f + i * 0.055f;
                dark.AddPanel(new Vector3(-0.24f, y, front), new Vector3(0.24f, y, front), new Vector3(0.24f, y + 0.016f, front), new Vector3(-0.24f, y + 0.016f, front), Vector3.forward,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            }
            dark.Attach("Slots", root, SurfaceMaterial.BlackHousing);
            var metal = new MeshBuilder();
            // Lever handle of the hatch: a plate, a stand-off and a bar.
            metal.AddBox(new Vector3(0.16f, 0.57f, halfZ + doorDepth), new Vector3(0.20f, 0.61f, halfZ + doorDepth + 0.014f));
            metal.AddBox(new Vector3(0.17f, 0.53f, halfZ + doorDepth + 0.014f), new Vector3(0.19f, 0.65f, halfZ + doorDepth + 0.03f));
            metal.AddLathe(new[] { P(0.016f, 0f, true), P(0.016f, 0.01f, true), P(0.0001f, 0.01f, true) }, 20, true, new Vector3(0.18f, 0.59f, halfZ + doorDepth + 0.03f), toZ);
            metal.Attach("Handle", root, SurfaceMaterial.Chrome);

            // Water connections on top at the back, flanged, short insulated stubs.
            var nozzles = new MeshBuilder();
            var bolts = new MeshBuilder();
            foreach (float x in new[] { -0.2f, 0.2f })
            {
                nozzles.AddLathe(new[] { P(RoomPipes.WideRadius, bodyTop, true), P(RoomPipes.WideRadius, bodyTop + 0.07f, true), P(0.058f, bodyTop + 0.07f, true),
                    P(0.058f, bodyTop + 0.088f, true), P(RoomPipes.WideRadius, bodyTop + 0.088f, true) }, 32, true, new Vector3(x, 0f, -0.23f), Quaternion.identity);
                for (int i = 0; i < 4; i++)
                {
                    float angle = (45f + 90f * i) * Mathf.Deg2Rad;
                    bolts.AddLathe(new[] { P(0.008f, 0f, true), P(0.008f, 0.008f, true), P(0.0001f, 0.008f, true) }, 6, false,
                        new Vector3(x + Mathf.Cos(angle) * 0.045f, bodyTop + 0.088f, -0.23f + Mathf.Sin(angle) * 0.045f), Quaternion.identity);
                }
            }
            nozzles.Attach("WaterNozzles", root, SurfaceMaterial.AluminiumClad);
            bolts.Attach("NozzleBolts", root, SurfaceMaterial.Chrome);

            // Supply duct to the ceiling with flanged joints and a damper.
            var duct = new MeshBuilder();
            var ductLoop = RoundedRectLoop(-0.25f, 0.25f, -0.15f, 0.15f, 0.012f, 2);
            duct.AddExtrusion(ductLoop, new Vector3(0f, bodyTop + 0.01f, 0f), new Vector3(0f, ceiling, 0f), Vector3.right, Vector3.forward, Vector3.down, Vector3.up);
            foreach (float y in new[] { 2.30f, 2.70f })
            {
                duct.AddExtrusion(RoundedRectLoop(-0.275f, 0.275f, -0.175f, 0.175f, 0.014f, 2), new Vector3(0f, y, 0f), new Vector3(0f, y + 0.035f, 0f), Vector3.right, Vector3.forward, Vector3.down, Vector3.up);
            }
            duct.Attach("Duct", root, SurfaceMaterial.AluminiumClad);
            var damper = new MeshBuilder();
            damper.AddLathe(new[] { P(0.05f, 0f, true), P(0.05f, 0.006f, true), P(0.0001f, 0.006f, true) }, 28, true, new Vector3(0f, 2.05f, 0.15f), toZ);
            damper.AddBox(new Vector3(-0.012f, 2.05f - 0.01f, 0.156f), new Vector3(0.012f, 2.05f + 0.085f, 0.164f));
            damper.AddLathe(new[] { P(0.012f, 0.164f, true), P(0.012f, 0.172f, true), P(0.0001f, 0.172f, true) }, 16, true, new Vector3(0f, 2.05f + 0.07f, 0f), toZ);
            damper.Attach("Damper", root, SurfaceMaterial.Chrome);

            var labels = new MeshBuilder();
            AddLabel(labels, new Vector3(0f, 1.50f, front), Vector3.left, Vector3.up, 0.30f, 0.15f, PlantLabel.UnitTag);
            AddLabel(labels, new Vector3(-0.0f, 0.40f, front), Vector3.left, Vector3.up, 0.24f, 0.12f, PlantLabel.Warning);
            labels.Attach("Labels", root, SurfaceMaterial.PlantLabels);
            AddBoxCollider(root, new Vector3(0f, (bodyTop + 0.02f) / 2f, 0f), new Vector3(2f * halfX, bodyTop + 0.02f, 2f * halfZ));
        }
    }
}
