using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;

namespace ColdShutdown.Editor
{
    /// <summary>The meet-me room: the fibre entry point, optical distribution frames, copper patch racks, cross-connect cabinets and the cable trays that lead to the vertical ducts.</summary>
    public static partial class Rooms
    {
        [Template(RoomType.MeetMeRoom)]
        private static RoomTemplate MeetMeRoomTemplate(RoomType type) => new RoomTemplate
        {
            Structure = AddSmallRoomWalls,
            Dress = (dressing, cameras, layout) => DressMeetMeRoom(dressing, cameras),
        };

        /// <summary>Half width (x) and half depth (z) of the vertical ducts over the entrance cabinet and the console rack: a rounded rectangle a little wider than the tray.</summary>
        private const float FibreDuctHalfX = 0.22f;

        private const float FibreDuctHalfZ = 0.17f;

        /// <summary>Clear gap between a yellow floor line and the units or racks it keeps clear (the keep-out lines of the power room stand as close).</summary>
        private const float LineGap = 0.075f;

        /// <summary>Middle of the entrance cabinet (west) and of the console rack (east) on the back wall: two 0.8 m units on the original 1.7 x 0.5 m plinth footprint.</summary>
        private const float BackUnitX = 0.41f;

        /// <summary>
        /// Meet-me room, 6 x 6 m, where every fibre and copper trunk of the building is terminated and cross-connected. Opposite the door, two 0.8 x 0.6 x 2.2 m units: left of the axis the entrance cabinet (<c>FibreEntryPoint</c>, glazed door with the lock on its right, the
        /// outside ducts on its hinge side, a DC plant inside), right of it the <c>ConsoleRack</c> (closed at the sides, servers and the pull-out console drawer). Over each a vertical galvanised duct
        /// (rounded rectangle like the tray) joins it to the tray; every cable of the room ends in the cabinet's duct. Five racks along each side wall, the two closed cross-connect cabinets of each row at its two ends,
        /// an optical frame or copper patch rack between them, the rows starting one line gap south of the front line of the back units (z -0.6). Each rack's cord bundles (rack-local x ±0.327, z 0.235, behind the solid front bar of the top ring) climb over the
        /// roof in a green conduit that bends in a smooth S (two arcs of 0.2 m) and ends at the very middle of the tray over the rack's depth, the same perforated tray as in the hall
        /// (<c>InfrastructureModels.CableTray</c>, yellow and blue cables in it, with its bends): up the row (closed at its south end, at the last rack, by an end cap), a 0.4 m bend at the back
        /// corner and across the back wall into the console rack's duct, on to the cabinet's duct. Trapeze hangers between the conduits. Yellow floor lines one line gap clear of the two units run from the door to the
        /// keep-clear hatching in front of them. Two shaft grilles high on the back wall, two LED battens, sprinklers, a detector, a camera and an extinguisher as in the other technical rooms.
        /// </summary>
        private static void DressMeetMeRoom(Transform dressing, Transform cameras)
        {
            var plant = ModelUtility.CreateGroup("Plant", dressing);
            var cabling = ModelUtility.CreateGroup("Cabling", dressing);
            var air = ModelUtility.CreateGroup("Ventilation", dressing);
            var markings = ModelUtility.CreateGroup("Markings", dressing);
            var ceiling = ModelUtility.CreateGroup("Ceiling", dressing);
            float back = RoomContract.SmallRoomBackWall;
            // The racks stand against the side walls; each row's tray hangs over the middle of their depth.
            float rowX = Side - Props.MeetRackDepth / 2f;
            const float crossZ = -0.3f;
            const float bendRadius = 0.40f;
            // The rows start a line gap south of the front line of the back units (z -0.6), 0.15 m + that gap south of where the layout below starts; the last rack's south edge is where the trays end.
            const float rowShift = -0.15f - LineGap;
            const float lastCentre = -4.20f + rowShift;
            float trayStart = lastCentre - Props.MeetRackWidth / 2f;
            float trayBottom = Props.FibreTrayBottom;

            Put(Prop.FibreEntryPoint, plant, -BackUnitX, 0f, back, 180f, "EntryPoint");
            Put(Prop.ConsoleRack, plant, BackUnitX, 0f, back - Props.MeetRackDepth / 2f, 180f, "ConsoleRack");

            // Unevenly spaced and mixed on purpose (centres in z; 0.8 m wide, so every gap is at least a touch). The closed cabinets stand at both ends of both rows.
            var west = new[] { (Prop.CrossConnectCabinet, -0.85f), (Prop.OdfRack, -1.70f), (Prop.CopperPatchRack, -2.50f), (Prop.OdfRackSfp, -3.35f), (Prop.CrossConnectCabinet, -4.20f) };
            var east = new[] { (Prop.CrossConnectCabinet, -0.85f), (Prop.CopperPatchRack, -1.75f), (Prop.OdfRackSfp, -2.60f), (Prop.OdfRack, -3.40f), (Prop.CrossConnectCabinet, -4.20f) };
            var westBundles = new List<float>();
            var eastBundles = new List<float>();
            int index = 0;
            foreach (var (prop, z) in west)
            {
                Put(prop, plant, -rowX, 0f, z + rowShift, 90f, $"{prop}_W{index++}");
                westBundles.Add(z + rowShift - Props.FibreBundleX);
                westBundles.Add(z + rowShift + Props.FibreBundleX);
            }
            index = 0;
            foreach (var (prop, z) in east)
            {
                Put(prop, plant, rowX, 0f, z + rowShift, 270f, $"{prop}_E{index++}");
                eastBundles.Add(z + rowShift - Props.FibreBundleX);
                eastBundles.Add(z + rowShift + Props.FibreBundleX);
            }

            // One tray per row (yellow fibre and blue copper cables share it), centred over the racks: up the row, round the back corner and across the back wall into the console rack's duct
            // (the end stops inside it); the south end is closed by a cap. A short tray joins the two ducts, ending inside both, so every cable runs on to the cabinet.
            foreach (int side in new[] { -1, 1 })
            {
                float pipeX = side < 0 ? -BackUnitX : BackUnitX;
                var path = new[] { new Vector3(side * rowX, 0f, trayStart), new Vector3(side * rowX, 0f, crossZ), new Vector3(pipeX + side * 0.10f, 0f, crossZ) };
                string name = side < 0 ? "TrayWest" : "TrayEast";
                InfrastructureModels.CableTrayRun(cabling, name, path, Props.FibreTrayWidth, trayBottom, bendRadius, SurfaceMaterial.FibreCord, SurfaceMaterial.DataCable, Ceiling);
                InfrastructureModels.CableTrayEndCap(cabling, $"{name}_EndCap", path[0], Vector3.back, Props.FibreTrayWidth, trayBottom);
            }
            InfrastructureModels.CableTray(cabling, "TrayLink", new Vector3(BackUnitX - 0.10f, 0f, crossZ), new Vector3(-BackUnitX + 0.10f, 0f, crossZ), Props.FibreTrayWidth, trayBottom,
                SurfaceMaterial.FibreCord, SurfaceMaterial.DataCable, new float[0], Ceiling);
            Props.FibreDuct(cabling, "Pipe", new Vector3(-BackUnitX, 0f, crossZ), Props.MeetRackHeight, Ceiling, FibreDuctHalfX, FibreDuctHalfZ, trayBottom);
            Props.FibreDuct(cabling, "PipeConsole", new Vector3(BackUnitX, 0f, crossZ), Props.MeetRackHeight, Ceiling, FibreDuctHalfX, FibreDuctHalfZ, trayBottom);

            // Hangers between the conduits (never on one), about a metre apart along each row, and across the back wall.
            var hangers = new List<(Vector3, Vector3)>();
            foreach (float z in HangerPositions(westBundles)) hangers.Add((new Vector3(-rowX, trayBottom, z), Vector3.forward));
            foreach (float z in HangerPositions(eastBundles)) hangers.Add((new Vector3(rowX, trayBottom, z), Vector3.forward));
            foreach (float x in new[] { -1.7f, -1.0f, 0f, 1.0f, 1.7f }) hangers.Add((new Vector3(x, trayBottom, crossZ), Vector3.right));
            Props.FibreHangers(cabling, "TrayHangers", hangers, Props.FibreTrayWidth / 2f, Ceiling);

            // Yellow floor lines a line gap clear of the two units (their outer edges are at x +-0.81), from the door to the keep-clear hatching, which covers the console drawer
            // pulled out and the cabinet's door swing in front of them, a line gap south of their fronts (z -0.6).
            float lineX = BackUnitX + Props.MeetRackWidth / 2f + LineGap + 0.025f;
            float hatchNorth = -Props.MeetRackDepth - LineGap;
            const float hatchSouth = -1.15f;
            var lines = new List<(Vector3, Vector3)>();
            foreach (int side in new[] { -1, 1 })
            {
                lines.Add((new Vector3(side * lineX, 0f, -2f * Side), new Vector3(side * lineX, 0f, hatchSouth)));
            }
            PaintedLines(markings, "AisleLines", lines, -Side, Side, -2f * Side, back);
            Hatching(markings, "KeepClear_EntryPoint", -lineX - 0.025f, hatchSouth, lineX + 0.025f, hatchNorth);

            TechnicalCeiling(ceiling, air);
            SmallRoomBasics(dressing, cameras, TechnicalDetector);
        }

        /// <summary>
        /// Where the hangers of one row's tray go (room z, south to north): the middles of the gaps between the cord bundles that are wide enough for a hanger's rods (at least 0.26 m
        /// between bundles), taken from the south end with at least 0.95 m between two hangers, so no hanger ever stands on a bundle.
        /// </summary>
        private static List<float> HangerPositions(List<float> bundles)
        {
            var sorted = new List<float>(bundles);
            sorted.Sort();
            var result = new List<float>();
            for (int i = 0; i + 1 < sorted.Count; i++)
            {
                if (sorted[i + 1] - sorted[i] < 0.26f) continue;
                float z = (sorted[i] + sorted[i + 1]) / 2f;
                if (result.Count == 0 || z - result[result.Count - 1] >= 0.95f) result.Add(z);
            }
            return result;
        }
    }
}
