using System.Collections.Generic;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Photo-based models of the overhead building services, shared by every room and the hall: the perforated cable tray (after a product
    /// photo: roll-formed galvanised steel with slotted sides and floor, beaded top edges and bolted splice plates, with its cables in it), the
    /// hangers (threaded rods with nuts and washers from a ceiling plate down to a strut trapeze, or a ring clamp round a pipe), the flanged
    /// aluminium-clad rectangular duct (like the cooling plant's air handler), the black ribbed flexible connector, the bolted collar frame, and
    /// the louvred duct grille. Every mesh part is saved under a name that starts with the run's name, so two runs in one prefab never share an asset.
    /// </summary>
    internal static class InfrastructureModels
    {
        public const float TraySideHeight = 0.08f;
        private const float SlotTileAlong = 0.09f;
        private const float SlotTileAcross = 0.04f;

        // ---------------------------------------------------------------- helpers

        private static void Box(MeshBuilder b, Vector3 a, Vector3 c) => b.AddBox(Vector3.Min(a, c), Vector3.Max(a, c));

        /// <summary>A box given in the frame of a run: ranges along <paramref name="along"/> and <paramref name="across"/> (unit axes) from <paramref name="origin"/>, and heights.</summary>
        private static void Slab(MeshBuilder b, Vector3 origin, Vector3 along, Vector3 across, float a0, float a1, float c0, float c1, float y0, float y1) =>
            Box(b, origin + along * a0 + across * c0 + Vector3.up * y0, origin + along * a1 + across * c1 + Vector3.up * y1);

        /// <summary>Section axes <paramref name="u"/>, <paramref name="v"/> for a run along <paramref name="d"/> (the extrusion wants Cross(u, v) = -d).</summary>
        private static void SectionAxes(Vector3 d, out Vector3 u, out Vector3 v)
        {
            if (Mathf.Abs(d.y) > 0.9f)
            {
                u = Vector3.right;
                v = Vector3.forward;
            }
            else
            {
                v = Vector3.up;
                u = Vector3.Cross(Vector3.up, d).normalized;
            }
            if (Vector3.Dot(Vector3.Cross(u, v), d) > 0f) u = -u;
        }

        // ---------------------------------------------------------------- cable tray

        /// <summary>
        /// A perforated cable tray hung from <paramref name="ceiling"/> at the heights of its hangers (<paramref name="rodsAt"/>, positions along its run).
        /// <paramref name="from"/> and <paramref name="to"/> are the ends of its centre line (only X and Z are used), <paramref name="bottom"/> the height
        /// of its floor. Slots are an alpha-cut texture (<see cref="SurfaceMaterial.TrayPerforated"/>); the beaded edges, splice plates (every 3 m) and
        /// hangers are geometry.
        /// </summary>
        public static void CableTray(Transform parent, string name, Vector3 from, Vector3 to, float width, float bottom, SurfaceMaterial cables,
            IEnumerable<float> rodsAt, float ceiling)
        {
            bool alongZ = Mathf.Abs(to.z - from.z) > Mathf.Abs(to.x - from.x);
            var along = alongZ ? Vector3.forward : Vector3.right;
            var across = alongZ ? Vector3.right : Vector3.forward;
            float centre = alongZ ? from.x : from.z;
            float a0 = alongZ ? Mathf.Min(from.z, to.z) : Mathf.Min(from.x, to.x);
            float a1 = alongZ ? Mathf.Max(from.z, to.z) : Mathf.Max(from.x, to.x);
            float h = width / 2f;
            float side = TraySideHeight;
            float length = a1 - a0;
            Vector3 V(float a, float c, float y) => along * a + across * (centre + c) + Vector3.up * y;

            var perforated = new MeshBuilder();
            var steel = new MeshBuilder();
            var cable = new MeshBuilder();
            var hangers = new MeshBuilder();

            // The floor and the two sides: slotted sheet, drawn on both faces.
            float bottomTile = 0.07f;
            perforated.AddPanel(V(a0, -h, bottom + 0.001f), V(a1, -h, bottom + 0.001f), V(a1, h, bottom + 0.001f), V(a0, h, bottom + 0.001f), Vector3.up,
                Vector2.zero, new Vector2(length / SlotTileAlong, 0f), new Vector2(length / SlotTileAlong, width / bottomTile), new Vector2(0f, width / bottomTile));
            foreach (int s in new[] { -1, 1 })
            {
                perforated.AddPanel(V(a0, s * h, bottom), V(a1, s * h, bottom), V(a1, s * h, bottom + side), V(a0, s * h, bottom + side), across * s,
                    Vector2.zero, new Vector2(length / SlotTileAlong, 0f), new Vector2(length / SlotTileAlong, side / SlotTileAcross), new Vector2(0f, side / SlotTileAcross));
                // The rolled bead along the top edge with its inward flange, and the folded lip along the bottom edge.
                steel.AddTube(new[] { V(a0, s * (h - 0.002f), bottom + side), V(a1, s * (h - 0.002f), bottom + side) }, 0.0045f, 8, Vector3.zero, Quaternion.identity);
                Box(steel, V(a0, s * (h - 0.012f), bottom + side - 0.0025f), V(a1, s * h, bottom + side));
                Box(steel, V(a0, s * (h - 0.010f), bottom), V(a1, s * h, bottom + 0.003f));
            }

            // Splice plates every 3 m on both sides, four bolt heads on each.
            for (float at = a0 + 3f; at < a1 - 0.2f; at += 3f)
            {
                foreach (int s in new[] { -1, 1 })
                {
                    Box(steel, V(at - 0.09f, s * h, bottom + 0.012f), V(at + 0.09f, s * (h + 0.003f), bottom + side - 0.012f));
                    foreach (float da in new[] { -0.05f, 0.05f })
                    {
                        foreach (float y in new[] { bottom + 0.026f, bottom + side - 0.026f })
                        {
                            steel.AddLathe(new[] { P(0.0055f, 0f, true), P(0.0055f, 0.004f, true), P(0.0001f, 0.004f, true) }, 6, false, V(at + da, s * (h + 0.003f), y),
                                Quaternion.FromToRotation(Vector3.up, across * s));
                        }
                    }
                }
            }

            // The cables: a layer of five and two on top, along the whole run.
            const float cableRadius = 0.011f;
            for (int i = 0; i < 5; i++)
            {
                float c = -h + 0.04f + i * (width - 0.08f) / 4f;
                cable.AddTube(new[] { V(a0 + 0.02f, c, bottom + 0.002f + cableRadius), V(a1 - 0.02f, c, bottom + 0.002f + cableRadius) }, cableRadius, 8, Vector3.zero, Quaternion.identity);
            }
            for (int i = 0; i < 2; i++)
            {
                float c = -width * 0.18f + i * width * 0.36f;
                cable.AddTube(new[] { V(a0 + 0.02f, c, bottom + 0.002f + 2.7f * cableRadius), V(a1 - 0.02f, c, bottom + 0.002f + 2.7f * cableRadius) }, cableRadius, 8, Vector3.zero, Quaternion.identity);
            }

            foreach (float at in rodsAt)
            {
                Hanger(hangers, V(at, 0f, bottom), across, along, h, ceiling);
            }

            var group = ModelUtility.CreateGroup(name, parent);
            perforated.Attach($"{name}_Perforated", group, SurfaceMaterial.TrayPerforated);
            steel.Attach($"{name}_Steel", group, SurfaceMaterial.GalvanisedSteel);
            cable.Attach($"{name}_Cables", group, cables);
            hangers.Attach($"{name}_Hangers", group, SurfaceMaterial.GalvanisedSteel);
        }

        /// <summary>
        /// A hanger for something that is <paramref name="halfSpan"/> wide either side of <paramref name="underside"/> (the point under its middle):
        /// two M10 threaded rods 3 cm outside it, up to a round ceiling plate with a nut at <paramref name="ceiling"/>, down to a 41 x 41 mm strut that
        /// carries it, each rod with a nut and a washer above and below the strut.
        /// </summary>
        public static void Hanger(MeshBuilder steel, Vector3 underside, Vector3 across, Vector3 along, float halfSpan, float ceiling)
        {
            float reach = halfSpan + 0.05f;
            float y = underside.y;
            // The strut: a web under the load, two flanges and inward lips (a channel open at the bottom).
            Slab(steel, underside, along, across, -0.0205f, 0.0205f, -reach, reach, y - 0.0025f, y);
            foreach (int s in new[] { -1, 1 })
            {
                Slab(steel, underside, along, across, s < 0 ? -0.0205f : 0.0180f, s < 0 ? -0.0180f : 0.0205f, -reach, reach, y - 0.041f, y - 0.0025f);
                Slab(steel, underside, along, across, s < 0 ? -0.0205f : 0.0090f, s < 0 ? -0.0090f : 0.0205f, -reach, reach, y - 0.041f, y - 0.0385f);
            }
            foreach (int s in new[] { -1, 1 })
            {
                var rod = underside + across * (s * (halfSpan + 0.03f));
                steel.AddTube(new[] { new Vector3(rod.x, y - 0.056f, rod.z), new Vector3(rod.x, ceiling - 0.004f, rod.z) }, 0.005f, 8, Vector3.zero, Quaternion.identity);
                // Washer and hex nut under the strut and on top of it.
                foreach (float level in new[] { y - 0.041f, y })
                {
                    float washer = level < y ? level - 0.002f : level;
                    steel.AddLathe(new[] { P(0.019f, 0f, true), P(0.019f, 0.002f, true), P(0.0001f, 0.002f, true) }, 16, true, new Vector3(rod.x, washer, rod.z), Quaternion.identity);
                    float nut = level < y ? level - 0.002f - 0.008f : level + 0.002f;
                    steel.AddLathe(new[] { P(0.0095f, 0f, true), P(0.0095f, 0.008f, true), P(0.0001f, 0.008f, true) }, 6, false, new Vector3(rod.x, nut, rod.z), Quaternion.identity);
                }
                // Ceiling plate and nut.
                steel.AddLathe(new[] { P(0.032f, -0.004f, true), P(0.032f, 0f, true), P(0.0001f, 0f, true) }, 20, true, new Vector3(rod.x, ceiling, rod.z), Quaternion.identity);
                steel.AddLathe(new[] { P(0.0095f, -0.012f, true), P(0.0095f, -0.004f, true), P(0.0001f, -0.004f, true) }, 6, false, new Vector3(rod.x, ceiling, rod.z), Quaternion.identity);
            }
        }

        /// <summary>A ring clamp round a pipe (axis <paramref name="along"/>) hung from the ceiling on one threaded rod.</summary>
        public static void PipeHanger(MeshBuilder steel, Vector3 pipeCentre, float pipeRadius, Vector3 along, float ceiling)
        {
            var across = Vector3.Cross(Vector3.up, along).normalized;
            var ring = new List<Vector3>();
            for (int i = 0; i < 20; i++)
            {
                float angle = i / 20f * 2f * Mathf.PI;
                ring.Add(pipeCentre + across * Mathf.Cos(angle) * (pipeRadius + 0.004f) + Vector3.up * Mathf.Sin(angle) * (pipeRadius + 0.004f));
            }
            steel.AddTube(ring, 0.0028f, 6, Vector3.zero, Quaternion.identity, true);
            float top = pipeCentre.y + pipeRadius + 0.008f;
            steel.AddLathe(new[] { P(0.0075f, 0f, true), P(0.0075f, 0.012f, true), P(0.0001f, 0.012f, true) }, 6, false, new Vector3(pipeCentre.x, top - 0.012f, pipeCentre.z), Quaternion.identity);
            steel.AddTube(new[] { new Vector3(pipeCentre.x, top, pipeCentre.z), new Vector3(pipeCentre.x, ceiling - 0.004f, pipeCentre.z) }, 0.004f, 8, Vector3.zero, Quaternion.identity);
            steel.AddLathe(new[] { P(0.028f, -0.004f, true), P(0.028f, 0f, true), P(0.0001f, 0f, true) }, 20, true, new Vector3(pipeCentre.x, ceiling, pipeCentre.z), Quaternion.identity);
        }

        // ---------------------------------------------------------------- ducts

        /// <summary>
        /// A rectangular duct (centre line from <paramref name="start"/> to <paramref name="end"/>, <paramref name="halfU"/> across and <paramref name="halfV"/> high for a
        /// horizontal run) clad in aluminium like the cooling plant's air handler: rounded edges, two standing seams along the top and bottom, and a galvanised
        /// angle-frame flange at both ends and every <paramref name="joint"/> metres. Horizontal runs hang from <paramref name="ceiling"/> at <paramref name="hangersAt"/>
        /// (positions along their axis).
        /// </summary>
        public static void RectDuct(Transform parent, string name, Vector3 start, Vector3 end, float halfU, float halfV, float joint, IEnumerable<float> hangersAt, float ceiling,
            SurfaceMaterial paint = SurfaceMaterial.AluminiumClad)
        {
            var d = (end - start).normalized;
            SectionAxes(d, out var u, out var v);
            bool horizontal = Mathf.Abs(d.y) < 0.1f;
            float length = (end - start).magnitude;
            var skin = new MeshBuilder();
            var flange = new MeshBuilder();
            var hangers = new MeshBuilder();

            skin.AddExtrusion(RoundedRectLoop(-halfU, halfU, -halfV, halfV, 0.014f, 2), start, end, u, v, -d, d);
            if (horizontal)
            {
                foreach (float off in new[] { -0.45f, 0.45f })
                {
                    foreach (float s in new[] { -1f, 1f })
                    {
                        Box(skin, start + u * (off * halfU - 0.006f) + v * (s * halfV), end + u * (off * halfU + 0.006f) + v * (s * (halfV + 0.004f)));
                    }
                }
            }
            int joints = Mathf.Max(1, Mathf.RoundToInt(length / joint));
            for (int i = 0; i <= joints; i++)
            {
                float at = Mathf.Clamp(length * i / joints, 0.02f, length - 0.02f);
                var p = start + d * at;
                flange.AddExtrusion(RoundedRectLoop(-halfU - 0.022f, halfU + 0.022f, -halfV - 0.022f, halfV + 0.022f, 0.004f, 1), p - d * 0.02f, p + d * 0.02f, u, v, -d, d);
            }
            if (horizontal && hangersAt != null)
            {
                bool alongX = Mathf.Abs(d.x) > 0.5f;
                foreach (float at in hangersAt)
                {
                    var p = alongX ? new Vector3(at, start.y - halfV, start.z) : new Vector3(start.x, start.y - halfV, at);
                    Hanger(hangers, p, u, d, halfU, ceiling);
                }
            }

            var group = ModelUtility.CreateGroup(name, parent);
            skin.Attach($"{name}_Skin", group, paint);
            flange.Attach($"{name}_Flanges", group, SurfaceMaterial.GalvanisedSteel);
            if (horizontal) hangers.Attach($"{name}_Hangers", group, SurfaceMaterial.GalvanisedSteel);
        }

        /// <summary>A black flexible connector between two flanges: ribbed bellows (alternating wide and narrow rings), vertical.</summary>
        public static void Bellows(Transform parent, string name, Vector3 top, Vector3 bottom, float halfU, float halfV)
        {
            var d = (bottom - top).normalized;
            SectionAxes(d, out var u, out var v);
            var bellows = new MeshBuilder();
            float length = (bottom - top).magnitude;
            int rings = Mathf.Max(2, Mathf.RoundToInt(length / 0.03f));
            float step = length / rings;
            for (int i = 0; i < rings; i++)
            {
                float grow = i % 2 == 0 ? 0.012f : 0f;
                bellows.AddExtrusion(RoundedRectLoop(-halfU - grow, halfU + grow, -halfV - grow, halfV + grow, 0.01f, 1), top + d * (i * step), top + d * ((i + 1) * step), u, v, -d, d);
            }
            bellows.Attach($"{name}_Bellows", ModelUtility.CreateGroup(name, parent), SurfaceMaterial.BlackHousing);
        }

        /// <summary>A flat bolted frame (a mounting collar) round a rectangular opening: four plates and eight hex bolts. Lies with its underside at <paramref name="underside"/>.</summary>
        public static void CollarFrame(Transform parent, string name, Vector3 underside, float halfX, float halfZ, float thickness)
        {
            var steel = new MeshBuilder();
            const float rim = 0.05f;
            float y1 = underside.y + thickness;
            Box(steel, underside + new Vector3(-halfX - rim, 0f, -halfZ - rim), new Vector3(underside.x + halfX + rim, y1, underside.z - halfZ));
            Box(steel, underside + new Vector3(-halfX - rim, 0f, halfZ), new Vector3(underside.x + halfX + rim, y1, underside.z + halfZ + rim));
            Box(steel, underside + new Vector3(-halfX - rim, 0f, -halfZ), new Vector3(underside.x - halfX, y1, underside.z + halfZ));
            Box(steel, underside + new Vector3(halfX, 0f, -halfZ), new Vector3(underside.x + halfX + rim, y1, underside.z + halfZ));
            foreach (float fx in new[] { -0.75f, 0f, 0.75f })
            {
                foreach (float sz in new[] { -1f, 1f })
                {
                    steel.AddLathe(new[] { P(0.0075f, 0f, true), P(0.0075f, 0.007f, true), P(0.0001f, 0.007f, true) }, 6, false,
                        new Vector3(underside.x + fx * halfX, y1, underside.z + sz * (halfZ + rim / 2f)), Quaternion.identity);
                }
            }
            foreach (float sx in new[] { -1f, 1f })
            {
                steel.AddLathe(new[] { P(0.0075f, 0f, true), P(0.0075f, 0.007f, true), P(0.0001f, 0.007f, true) }, 6, false,
                    new Vector3(underside.x + sx * (halfX + rim / 2f), y1, underside.z), Quaternion.identity);
            }
            steel.Attach($"{name}_Frame", ModelUtility.CreateGroup(name, parent), SurfaceMaterial.GalvanisedSteel);
        }

        /// <summary>A return-air grille 0.6 x 0.3 m in the underside of a duct: a white frame with seven angled louvres, facing down.</summary>
        public static void DuctGrille(MeshBuilder white, Vector3 centreOnUnderside)
        {
            const float halfX = 0.3f;
            const float halfZ = 0.15f;
            const float frame = 0.02f;
            float y = centreOnUnderside.y;
            var c = centreOnUnderside;
            Box(white, new Vector3(c.x - halfX, y - 0.012f, c.z - halfZ), new Vector3(c.x + halfX, y, c.z - halfZ + frame));
            Box(white, new Vector3(c.x - halfX, y - 0.012f, c.z + halfZ - frame), new Vector3(c.x + halfX, y, c.z + halfZ));
            Box(white, new Vector3(c.x - halfX, y - 0.012f, c.z - halfZ), new Vector3(c.x - halfX + frame, y, c.z + halfZ));
            Box(white, new Vector3(c.x + halfX - frame, y - 0.012f, c.z - halfZ), new Vector3(c.x + halfX, y, c.z + halfZ));
            for (int i = 0; i < 7; i++)
            {
                float z = c.z - halfZ + frame + (i + 0.5f) * (2f * (halfZ - frame)) / 7f;
                Box(white, new Vector3(c.x - halfX + frame, y - 0.016f, z - 0.006f), new Vector3(c.x + halfX - frame, y - 0.012f, z + 0.006f));
            }
        }
    }
}
