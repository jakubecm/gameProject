using System.Collections.Generic;
using UnityEngine;
using static ColdShutdown.Editor.GrayboxMeshBuilder;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// The photo-realistic detail props: the pendent glass-bulb sprinkler, the black dome camera and the recessed ceiling
    /// loudspeaker, modelled to their real sizes (metres) from product photos. Smooth surfaces of revolution and bent strips
    /// from <see cref="GrayboxMeshBuilder"/>, one mesh per material, no colliders (all out of reach). Rebuilt in place
    /// (same GUIDs) with every other prop by <see cref="RoomPreviewSceneTool.CreatePreviewScene"/>.
    /// </summary>
    public static partial class GrayboxPropsTool
    {
        /// <summary>How far the face of a drop-ceiling tile is below the ceiling surface (the grid depth less 0.8 mm).</summary>
        private const float TileDrop = 0.0112f;

        private static IEnumerable<Vector3> Arc(float centerR, float centerY, float radiusR, float radiusY, float fromDegrees, float toDegrees, int steps)
        {
            for (int i = 0; i <= steps; i++)
            {
                float angle = Mathf.Lerp(fromDegrees, toDegrees, i / (float)steps) * Mathf.Deg2Rad;
                yield return new Vector3(centerR + radiusR * Mathf.Cos(angle), centerY + radiusY * Mathf.Sin(angle), 0f);
            }
        }

        private static List<ProfilePoint> ArcProfile(float centerR, float centerY, float radiusR, float radiusY, float fromDegrees, float toDegrees, int steps)
        {
            var points = new List<ProfilePoint>();
            foreach (var v in Arc(centerR, centerY, radiusR, radiusY, fromDegrees, toDegrees, steps)) points.Add(P(Mathf.Max(0f, v.x), v.y));
            return points;
        }

        /// <summary>
        /// Quick-response pendent sprinkler, 15 mm thread, glass bulb: a chrome escutcheon (72 mm across, a rounded rim
        /// round a recess) flush with the ceiling, the hex body and the brass sealing ring under it, the 3 mm red
        /// glycerine bulb with its clear seal, the two bent chrome frame arms that carry it down to the hub, and the
        /// slotted deflector (16 tines) 63 mm below the ceiling. The pivot is on the ceiling surface, hanging down.
        /// </summary>
        private static void BuildSprinkler(Transform root)
        {
            const int round = 40;
            var chrome = new GrayboxMeshBuilder();
            var brass = new GrayboxMeshBuilder();
            var red = new GrayboxMeshBuilder();
            var clear = new GrayboxMeshBuilder();
            // The pivot is on the ceiling surface, but a drop ceiling's tile face is TileDrop lower: the sprinkler hangs from there,
            // and its escutcheon is deep enough to reach up to the surface.
            var origin = new Vector3(0f, -TileDrop, 0f);
            var none = Quaternion.identity;

            // Escutcheon: the inner wall of the recess (walking down faces the axis), the underside, the rounded rim.
            chrome.AddLathe(new[]
            {
                P(0.0200f, 0.0100f + TileDrop), P(0.0200f, -0.0035f), P(0.0215f, -0.0055f, true), P(0.0260f, -0.0062f), P(0.0310f, -0.0062f), P(0.0345f, -0.0052f),
                P(0.0360f, -0.0035f), P(0.0362f, -0.0015f), P(0.0350f, 0f, true), P(0.0350f, TileDrop, true),
            }, round, true, origin, none);

            // Hex body in the recess and the frame's boss; the brass ring below the body.
            chrome.AddLathe(new[]
            {
                P(0f, -0.0090f), P(0.0115f, -0.0090f, true), P(0.0115f, 0.0020f, true), P(0f, 0.0020f, true),
            }, 6, false, origin, none);
            brass.AddLathe(new[]
            {
                P(0f, -0.0130f), P(0.0100f, -0.0130f, true), P(0.0100f, -0.0090f, true), P(0.0085f, -0.0090f, true),
            }, round, true, origin, none);

            // Frame arms: flat bands bent from the body down and in to the hub, one each side.
            foreach (float side in new[] { -1f, 1f })
            {
                var path = new List<Vector3>();
                foreach (var xy in new[]
                {
                    (0.0105f, -0.0090f), (0.0113f, -0.0145f), (0.0119f, -0.0220f), (0.0118f, -0.0310f), (0.0106f, -0.0400f), (0.0080f, -0.0475f), (0.0045f, -0.0535f),
                })
                {
                    path.Add(new Vector3(side * xy.Item1, xy.Item2, 0f));
                }
                chrome.AddStrip(path, Vector3.forward, 0.0050f, 0.0018f, origin, none);
            }

            // Hub (a tapered boss under the arms) with the deflector's solid centre.
            chrome.AddLathe(new[]
            {
                P(0f, -0.0625f), P(0.0030f, -0.0625f, true), P(0.0045f, -0.0600f), P(0.0058f, -0.0560f), P(0.0068f, -0.0525f), P(0.0070f, -0.0510f, true), P(0f, -0.0510f, true),
            }, 24, true, origin, none);
            chrome.AddLathe(new[]
            {
                P(0f, -0.0634f), P(0.0090f, -0.0634f, true), P(0.0092f, -0.0630f, true), P(0.0090f, -0.0626f, true), P(0f, -0.0626f, true),
            }, 32, true, origin, none);

            // Deflector tines: 16 slightly cupped radial fingers.
            for (int i = 0; i < 16; i++)
            {
                var path = new List<Vector3>
                {
                    new Vector3(0.0070f, -0.0630f, 0f), new Vector3(0.0110f, -0.0631f, 0f), new Vector3(0.0145f, -0.0626f, 0f), new Vector3(0.0172f, -0.0617f, 0f),
                };
                chrome.AddStrip(path, Vector3.forward, 0.0036f, 0.0008f, origin, Quaternion.Euler(0f, i * 22.5f, 0f));
            }

            // Bulb: red glycerine above, the clear seal and rounded tip below.
            var liquid = new List<ProfilePoint> { P(0f, -0.0385f, true), P(0.0026f, -0.0385f, true), P(0.0026f, -0.0135f) };
            liquid.AddRange(ArcProfile(0f, -0.0135f, 0.0026f, 0.0026f, 0f, 90f, 8));
            red.AddLathe(liquid, 20, true, origin, none);
            var seal = new List<ProfilePoint>();
            seal.AddRange(ArcProfile(0f, -0.0475f, 0.0026f, 0.0026f, -90f, 0f, 8));
            seal.Add(P(0.0026f, -0.0385f, true));
            clear.AddLathe(seal, 20, true, origin, none);

            chrome.Attach("Chrome", root, SurfaceMaterial.Chrome);
            brass.Attach("Brass", root, SurfaceMaterial.Brass);
            red.Attach("BulbLiquid", root, SurfaceMaterial.BulbRed);
            clear.Attach("BulbSeal", root, SurfaceMaterial.BulbClear);
        }

        /// <summary>
        /// A black dome camera housing (111 mm across, 80 mm high) hanging from the ceiling surface: the satin black shell
        /// with its rounded top edge, the seam and the flared lower bezel with a small screw port, a clear glass dome
        /// under it and, inside the glass with a gap of about 1.5 cm all round, a smaller black dome (the camera
        /// is not modelled). The pivot is the middle of the top face; the model hangs along -Y.
        /// </summary>
        private static void BuildDomeCameraModel(Transform parent)
        {
            var model = GrayboxUtility.CreateGroup("Model", parent);
            var housing = new GrayboxMeshBuilder();
            var dome = new GrayboxMeshBuilder();
            var inner = new GrayboxMeshBuilder();
            var port = new GrayboxMeshBuilder();
            var origin = new Vector3(0f, -TileDrop, 0f);
            var none = Quaternion.identity;

            // Outside, from the bezel's underside up over the seam to the top face (which touches the ceiling).
            housing.AddLathe(new[]
            {
                P(0.0405f, -0.0462f, true), P(0.0460f, -0.0462f), P(0.0488f, -0.0452f), P(0.0512f, -0.0432f), P(0.0536f, -0.0392f), P(0.0551f, -0.0356f),
                P(0.0556f, -0.0340f, true), P(0.0546f, -0.0338f, true), P(0.0546f, -0.0325f, true), P(0.0556f, -0.0323f, true),
                P(0.0557f, -0.0220f), P(0.0553f, 0.0030f), P(0.0540f, 0.0100f), P(0.0522f, TileDrop, true), P(0f, TileDrop, true),
            }, 64, true, origin, none);
            // The black plate under the shell, inside the dome.
            housing.AddLathe(new[] { P(0f, -0.0460f, true), P(0.0405f, -0.0460f, true) }, 48, true, origin, none);

            // The clear dome: an ellipsoid cap, 81 mm across, walking from the tip up to the rim (outside faces out and down).
            dome.AddLathe(ArcProfile(0f, -0.0455f, 0.0405f, 0.0345f, -90f, 0f, 16), 48, true, origin, none);

            // The smaller black dome on the plate, free of the glass.
            inner.AddLathe(ArcProfile(0f, -0.0460f, 0.0340f, 0.0280f, -90f, 0f, 16), 40, true, origin, none);

            // A small screw port on the bezel, 30 degrees round from the front.
            float angle = 30f * Mathf.Deg2Rad;
            var radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            port.AddLathe(new[] { P(0.0034f, 0f, true), P(0.0034f, 0.0004f, true), P(0f, 0.0004f, true) }, 16, true,
                radial * 0.0536f + new Vector3(0f, -0.0392f - TileDrop, 0f), Quaternion.FromToRotation(Vector3.up, radial));

            housing.Attach("Housing", model, SurfaceMaterial.BlackHousing);
            inner.Attach("InnerDome", model, SurfaceMaterial.BlackHousing);
            port.Attach("Port", model, SurfaceMaterial.Screen);
            dome.Attach("Dome", model, SurfaceMaterial.DomeClear);
        }

        /// <summary>Ceiling dome camera: hangs from the ceiling, the feed looking along +Z and 30 degrees down.</summary>
        private static void BuildSecurityDome(Transform root)
        {
            BuildDomeCameraModel(root);
            AddSecurityCamera(root, new Vector3(0f, -0.06f, 0f), 30f);
        }

        /// <summary>
        /// Wall dome camera: the same housing lying on its top face against the wall, bulging along +Z, the feed looking
        /// along +Z and 25 degrees down. The pivot is the middle of the back face; no cable.
        /// </summary>
        private static void BuildSecurityWallDome(Transform root)
        {
            var mount = GrayboxUtility.CreateGroup("Mount", root);
            mount.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            BuildDomeCameraModel(mount);
            AddSecurityCamera(root, new Vector3(0f, -0.01f, 0.06f), 25f);
        }

        /// <summary>Pendant dome camera for the hall: a junction box clamped to a beam, a pole with the cable inside and the dome camera at its end.</summary>
        private static void BuildSecurityPendant(Transform root)
        {
            float bottom = -SecurityPendantDrop;
            GrayboxUtility.CreateBox("JunctionBox", root, new Vector3(0f, -0.04f, 0f), new Vector3(0.15f, 0.08f, 0.15f), SurfaceMaterial.Trunking, false);
            GrayboxUtility.CreatePipe("Pole", root, new Vector3(0f, -0.08f, 0f), new Vector3(0f, bottom, 0f), 0.025f, SurfaceMaterial.BlackHousing);
            var camera = GrayboxUtility.CreateGroup("Camera", root);
            camera.localPosition = new Vector3(0f, bottom, 0f);
            BuildDomeCameraModel(camera);
            AddSecurityCamera(root, new Vector3(0f, bottom - 0.065f, 0f), 35f);
        }

        /// <summary>
        /// Recessed ceiling loudspeaker (public address and voice alarm), 216 mm across: a flat white trim ring with a
        /// rounded rim, flush with the tile, and the metal grille in front of it: a slightly domed disc 185 mm across
        /// that covers the ring's inner edge and stands 3 mm proud of it. It sits in the middle of a tile.
        /// </summary>
        private static void BuildCeilingSpeaker(Transform root)
        {
            var trim = new GrayboxMeshBuilder();
            var grille = new GrayboxMeshBuilder();
            var origin = Vector3.zero;
            var none = Quaternion.identity;

            trim.AddLathe(new[]
            {
                P(0.0800f, -0.0145f, true), P(0.0990f, -0.0148f), P(0.1040f, -0.0144f), P(0.1068f, -0.0132f), P(0.1078f, -0.0122f), P(0.1074f, -0.0116f, true),
            }, 64, true, origin, none);
            grille.AddLathe(new[]
            {
                P(0f, -0.0185f), P(0.0400f, -0.0181f), P(0.0800f, -0.0170f), P(0.0905f, -0.0162f), P(0.0925f, -0.0153f, true), P(0.0925f, -0.0146f, true), P(0f, -0.0146f, true),
            }, 64, true, origin, none);

            trim.Attach("Trim", root, SurfaceMaterial.SpeakerWhite);
            grille.Attach("Grille", root, SurfaceMaterial.SpeakerGrille);
        }

        /// <summary>
        /// Smoke detector, 100 mm across and 46 mm high, glossy white: the round base with a rounded top edge and the seam,
        /// the stepped lower ring with its status lens, and under it the sensing chamber: a dark insect mesh between four
        /// white posts, closed by a domed white cap. The pivot is on the ceiling surface, hanging down.
        /// </summary>
        private static void BuildSmokeDetector(Transform root)
        {
            var white = new GrayboxMeshBuilder();
            var mesh = new GrayboxMeshBuilder();
            var lens = new GrayboxMeshBuilder();
            var origin = new Vector3(0f, -TileDrop, 0f);
            var none = Quaternion.identity;

            // Base and stepped ring, from the underside next to the chamber outward, up the side and over the top edge.
            white.AddLathe(new[]
            {
                P(0.0300f, -0.0290f, true), P(0.0365f, -0.0290f), P(0.0392f, -0.0283f), P(0.0420f, -0.0268f), P(0.0452f, -0.0262f), P(0.0470f, -0.0250f),
                P(0.0478f, -0.0232f), P(0.0480f, -0.0206f, true), P(0.0500f, -0.0203f, true), P(0.0503f, -0.0190f), P(0.0503f, 0.0040f), P(0.0497f, 0.0090f),
                P(0.0482f, TileDrop, true), P(0f, TileDrop, true),
            }, 64, true, origin, none);

            // The sensing chamber: mesh between four posts, then the cap.
            mesh.AddLathe(new[] { P(0.0290f, -0.0400f, true), P(0.0290f, -0.0292f, true) }, 48, true, origin, none);
            for (int i = 0; i < 4; i++)
            {
                var path = new List<Vector3> { new Vector3(0.0305f, -0.0292f, 0f), new Vector3(0.0305f, -0.0410f, 0f) };
                white.AddStrip(path, Vector3.forward, 0.0080f, 0.0045f, origin, Quaternion.Euler(0f, 45f + i * 90f, 0f));
            }
            white.AddLathe(new[]
            {
                P(0f, -0.0462f), P(0.0200f, -0.0458f), P(0.0300f, -0.0446f), P(0.0338f, -0.0425f), P(0.0345f, -0.0410f), P(0.0338f, -0.0400f, true), P(0.0285f, -0.0400f, true),
            }, 48, true, origin, none);

            // Status lens on the lower ring, facing down.
            lens.AddLathe(new[] { P(0f, -0.0277f), P(0.0028f, -0.0277f, true), P(0.0030f, -0.0272f, true) }, 16, true, new Vector3(0f, -TileDrop, 0.0432f), none);

            white.Attach("Body", root, SurfaceMaterial.DetectorWhite);
            mesh.Attach("Mesh", root, SurfaceMaterial.DetectorMesh);
            lens.Attach("Lens", root, SurfaceMaterial.BulbClear);
        }

        /// <summary>
        /// Lay-in LED panel for the 600 mm ceiling grid: 0.6 m wide, <paramref name="length"/> long (along Z), a white frame
        /// 18 mm wide that covers the T-bars under it, the opal diffuser set back 1 mm into it. The light is a wide downward
        /// spot with no shadows.
        /// </summary>
        private static void BuildLedPanel(Transform root, float length, float intensity)
        {
            const float width = 0.6f;
            const float rim = 0.018f;
            const float top = -0.0118f;
            const float bottom = -0.0158f;
            float halfX = width / 2f;
            float halfZ = length / 2f;
            var frame = new GrayboxMeshBuilder();
            var diffuser = new GrayboxMeshBuilder();

            // Four frame bars: the long sides along Z, the short ones between them.
            frame.AddBox(new Vector3(-halfX, bottom, -halfZ), new Vector3(-halfX + rim, top, halfZ), false);
            frame.AddBox(new Vector3(halfX - rim, bottom, -halfZ), new Vector3(halfX, top, halfZ), false);
            frame.AddBox(new Vector3(-halfX + rim, bottom, -halfZ), new Vector3(halfX - rim, top, -halfZ + rim), false);
            frame.AddBox(new Vector3(-halfX + rim, bottom, halfZ - rim), new Vector3(halfX - rim, top, halfZ), false);
            diffuser.AddBox(new Vector3(-halfX + rim, bottom + 0.0008f, -halfZ + rim), new Vector3(halfX - rim, top, halfZ - rim), false);

            frame.Attach("Frame", root, SurfaceMaterial.DetectorWhite);
            diffuser.Attach("Diffuser", root, SurfaceMaterial.LightPanel);
            AddSpotLight(root, new Vector3(0f, bottom - 0.03f, 0f), 150f, 100f, 6.5f, intensity, LedPanelColor);
        }
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
            var frame = new GrayboxMeshBuilder();
            var back = new GrayboxMeshBuilder();
            var slats = new GrayboxMeshBuilder();

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

        /// <summary>Small wall ventilation grille 600 x 300 mm mounted high under the ceiling; the pivot is the middle of its back.</summary>
        private static void BuildWallVentGrille(Transform root) => BuildLouveredGrille(root, 0.6f, WallGrilleHeight, 0f, 0.010f, 0.016f);

        /// <summary>Large square ventilation grille 600 x 600 mm at the foot of a wall, <see cref="GrilleGap"/> above the floor; the pivot is on the floor at the wall.</summary>
        private static void BuildFloorVentGrille(Transform root) =>
            BuildLouveredGrille(root, FloorGrilleSize, FloorGrilleSize, GrilleGap + FloorGrilleSize / 2f, 0.010f, 0.016f);

        /// <summary>
        /// Single-pole light switch on a flush-mounted 80 x 80 mm plate (rounded corners, bevelled edge, 6.5 mm proud of the
        /// wall) with a 56 mm square rocker in a dark square gap, 11 mm proud in all; the pivot is the middle of its back, +Z
        /// away from the wall.
        /// </summary>
        private static void BuildLightSwitch(Transform root)
        {
            var plate = new GrayboxMeshBuilder();
            var gap = new GrayboxMeshBuilder();
            var rocker = new GrayboxMeshBuilder();
            plate.AddRoundedSlab(Vector3.zero, 0.080f, 0.080f, 0.006f, 0.0065f, 0.0018f);
            gap.AddBox(new Vector3(-0.0295f, -0.0295f, 0.0063f), new Vector3(0.0295f, 0.0295f, 0.0070f), true, false);
            rocker.AddRoundedSlab(new Vector3(0f, 0f, 0.0067f), 0.056f, 0.056f, 0.0025f, 0.0042f, 0.0012f);
            plate.Attach("Plate", root, SurfaceMaterial.DetectorWhite);
            gap.Attach("Gap", root, SurfaceMaterial.BlackHousing);
            rocker.Attach("Rocker", root, SurfaceMaterial.DetectorWhite);
        }

        /// <summary>
        /// The joint where an outlet plate meets the plaster: a 1.5 mm dark line round the plate, so the plate reads as set into
        /// the wall. Lies on the wall surface.
        /// </summary>
        private static void WallJoint(Transform root)
        {
            var joint = new GrayboxMeshBuilder();
            const float inner = 0.0400f;
            const float outer = 0.0415f;
            joint.AddBox(new Vector3(-outer, -outer, 0f), new Vector3(outer, -inner, 0.0004f), true, false);
            joint.AddBox(new Vector3(-outer, inner, 0f), new Vector3(outer, outer, 0.0004f), true, false);
            joint.AddBox(new Vector3(-outer, -inner, 0f), new Vector3(-inner, inner, 0.0004f), true, false);
            joint.AddBox(new Vector3(inner, -inner, 0f), new Vector3(outer, inner, 0.0004f), true, false);
            joint.Attach("WallJoint", root, SurfaceMaterial.Upholstery);
        }

        /// <summary>
        /// Flush-mounted 230 V socket (type E: round recess, two shuttered holes and the earth pin) on an 80 x 80 mm plate,
        /// 10 mm proud of the wall; the pivot is the middle of its back, +Z away from the wall.
        /// </summary>
        private static void BuildPowerSocket(Transform root)
        {
            var plate = new GrayboxMeshBuilder();
            var ring = new GrayboxMeshBuilder();
            var holes = new GrayboxMeshBuilder();
            var pin = new GrayboxMeshBuilder();
            var toZ = Quaternion.Euler(90f, 0f, 0f);
            const float top = 0.0035f;
            WallJoint(root);
            plate.AddRoundedSlab(Vector3.zero, 0.080f, 0.080f, 0.006f, top, 0.0012f);
            // Round recess: a raised collar, then the recess floor (set 1.5 mm back from the collar's top).
            ring.AddLathe(new[]
            {
                P(0.0300f, 0f), P(0.0300f, 0.0018f), P(0.0288f, 0.0024f), P(0.0262f, 0.0022f), P(0.0255f, 0.0016f, true), P(0.0255f, 0.0004f, true), P(0f, 0.0004f, true),
            }, 48, true, new Vector3(0f, 0f, top), toZ);
            // The two plug holes (4 mm, dark) and the earth pin between them.
            foreach (float x in new[] { -0.0105f, 0.0105f })
            {
                holes.AddLathe(new[] { P(0.0042f, 0.0006f, true), P(0f, 0.0006f, true) }, 20, true, new Vector3(x, -0.0060f, top), toZ);
            }
            pin.AddLathe(new[] { P(0.0023f, 0.0006f, true), P(0.0023f, 0.0040f), P(0.0016f, 0.0052f), P(0f, 0.0054f) }, 16, true, new Vector3(0f, 0.0115f, top), toZ);

            plate.Attach("Plate", root, SurfaceMaterial.DetectorWhite);
            ring.Attach("Recess", root, SurfaceMaterial.DetectorWhite);
            holes.Attach("Holes", root, SurfaceMaterial.BlackHousing);
            pin.Attach("EarthPin", root, SurfaceMaterial.Chrome);
        }

        /// <summary>
        /// Double RJ45 data outlet on an 80 x 80 mm plate, flush-mounted, 8 mm proud of the wall: a rounded plate, the
        /// inner cover and two port surrounds with dark openings; the pivot is the middle of its back, +Z away from the wall.
        /// </summary>
        private static void BuildDataSocket(Transform root)
        {
            var plate = new GrayboxMeshBuilder();
            var openings = new GrayboxMeshBuilder();
            WallJoint(root);
            plate.AddRoundedSlab(Vector3.zero, 0.080f, 0.080f, 0.008f, 0.0035f, 0.0014f);
            plate.AddBox(new Vector3(-0.030f, -0.036f, 0.0032f), new Vector3(0.030f, 0.036f, 0.0037f), true, false);
            foreach (float x in new[] { -0.0175f, 0.0175f })
            {
                plate.AddRoundedSlab(new Vector3(x, 0f, 0.0037f), 0.022f, 0.022f, 0.005f, 0.0013f, 0.0004f, 3);
                openings.AddBox(new Vector3(x - 0.0065f, -0.0050f, 0.0048f), new Vector3(x + 0.0065f, 0.0050f, 0.0052f), true, false);
            }
            plate.Attach("Plate", root, SurfaceMaterial.DetectorWhite);
            openings.Attach("Ports", root, SurfaceMaterial.BlackHousing);
        }
    }
}
