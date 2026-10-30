using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace ColdShutdown.Editor
{
    /// <summary>Every corridor piece (cross, straight, T, corner, dead end) and the lift end: 3 m arms between partition blocks, the fire kit in the fire arm.</summary>
    public static partial class Rooms
    {
        [Template(RoomType.CorridorCross)]
        [Template(RoomType.CorridorStraight)]
        [Template(RoomType.CorridorT)]
        [Template(RoomType.CorridorCorner)]
        [Template(RoomType.CorridorDeadEnd)]
        [Template(RoomType.LiftEnd)]
        private static RoomTemplate CorridorTemplate(RoomType type) => new RoomTemplate
        {
            Structure = type == RoomType.LiftEnd ? AddLiftShaft : (Action<Transform>)null,
            Dress = (dressing, cameras, layout) => DressCorridor(dressing, cameras, type, layout),
        };

        /// <summary>Half width of the corridor arms (the keep-clear cross is 1.2 m either side).</summary>
        public const float CorridorHalfWidth = 1.5f;

        // ---------------------------------------------------------------- corridors

        /// <summary>
        /// A corridor 3 m wide with arms where <see cref="RoomRules.DoorSides"/> says (cross, T, straight, corner or
        /// dead end), between partition blocks (technical rooms behind them); a missing arm is walled up. In the suspended
        /// ceiling, on the corridor axis: a 600 mm LED panel in the middle of the crossing and of every arm, and between
        /// them the sprinklers (1.8 and 4.8 m from the center, so they are 3.0 to 3.6 m apart all along the corridor). One
        /// smoke detector and one loudspeaker per cell, one camera in the middle of a crossing, T-junction or corner (none in straight corridors and dead ends).
        ///
        /// The services are spread over the arms, starting with the fire arm of the layout
        /// (<see cref="RoomRules.FireArmSide"/>, which the layout turns towards a room): the fire-water riser with its hose
        /// cabinet, an extinguisher and a call point stand there, by the room's entrance and off the through-corridor. The
        /// next arm has the water and gas risers and an outlet box fed from the ceiling.
        /// The cell's one ventilation grille (a large square one at the floor; no duct shows) and its light switch above
        /// it share the middle of one wall, as far from the fire kit as the cell allows: the arm opposite the fire arm, or
        /// the wall that closes it. Mirrored layouts (<see cref="RoomRules.IsMirroredCorridor"/>) mirror every arm and go
        /// round the arms the other way.
        ///
        /// The lift end (<see cref="RoomType.LiftEnd"/>) is a dead end whose closed north end is the landing of the
        /// passenger lift (structure from <see cref="Rooms"/>, the <see cref="Prop.PassengerLift"/> on it); its
        /// grille and switch go to the middle of the west wall of the crossing instead.
        /// </summary>
        private static void DressCorridor(Transform dressing, Transform cameras, RoomType type, int layout)
        {
            int arms = RoomRules.DoorSides(type);
            bool Has(Direction side) => (arms & RoomRules.Mask(side)) != 0;
            var fireSide = RoomRules.FireArmSide(type, layout);
            bool flip = RoomRules.IsMirroredCorridor(type, layout);

            var architecture = ModelUtility.CreateGroup("Architecture", dressing);
            float extent = Wall - CorridorHalfWidth;
            float center = CorridorHalfWidth + extent / 2f;
            const float skirtingHeight = 0.1f;
            const float skirtingDepth = 0.015f;

            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    string corner = Corner(sx, sz);
                    ModelUtility.CreateStandingBox($"Partition_{corner}", architecture, new Vector3(sx * center, 0f, sz * center),
                        new Vector3(extent, Ceiling, extent), SurfaceMaterial.Wall);

                    // Skirting on the partition faces that line an arm.
                    float faceX = sx * (CorridorHalfWidth - skirtingDepth / 2f);
                    float faceZ = sz * (CorridorHalfWidth - skirtingDepth / 2f);
                    if (Has(sz > 0 ? Direction.North : Direction.South))
                    {
                        ModelUtility.CreateStandingBox($"Skirting_{corner}_X", architecture, new Vector3(faceX, 0f, sz * center),
                            new Vector3(skirtingDepth, skirtingHeight, extent), SurfaceMaterial.Trim, false);
                    }
                    if (Has(sx > 0 ? Direction.East : Direction.West))
                    {
                        ModelUtility.CreateStandingBox($"Skirting_{corner}_Z", architecture, new Vector3(sx * center, 0f, faceZ),
                            new Vector3(extent, skirtingHeight, skirtingDepth), SurfaceMaterial.Trim, false);
                    }
                }
            }

            // Walled-up arms: a block between the partitions, its face closing the crossing.
            var fills = new Dictionary<Direction, Transform>();
            bool lift = type == RoomType.LiftEnd;
            foreach (Direction side in Enum.GetValues(typeof(Direction)))
            {
                if (Has(side)) continue;
                if (lift && side == Direction.North)
                {
                    // The landing wall closes the crossing here; skirting beside the lift door's frame.
                    float from = RoomContract.LiftDoorWidth / 2f + 0.1f;
                    foreach (int sx in new[] { -1, 1 })
                    {
                        ModelUtility.CreateBoxBetween(sx < 0 ? "Skirting_Lift_W" : "Skirting_Lift_E", architecture,
                            new Vector3(sx * from, 0f, RoomContract.LiftWallFace - skirtingDepth), new Vector3(sx * CorridorHalfWidth, skirtingHeight, RoomContract.LiftWallFace), SurfaceMaterial.Trim);
                    }
                    var landing = ModelUtility.CreateGroup("Lift", dressing);
                    Props.Place(Prop.PassengerLift, landing, new Vector3(0f, 0f, RoomContract.LiftWallFace), 180f, "PassengerLift");
                    continue;
                }
                var fill = Arm(architecture, $"Fill_{side}", side);
                ModelUtility.CreateStandingBox("Block", fill, new Vector3(0f, 0f, center), new Vector3(2f * CorridorHalfWidth, Ceiling, extent), SurfaceMaterial.Wall);
                ModelUtility.CreateStandingBox("Skirting", fill, new Vector3(0f, 0f, CorridorHalfWidth - skirtingDepth / 2f),
                    new Vector3(2f * CorridorHalfWidth, skirtingHeight, skirtingDepth), SurfaceMaterial.Trim, false);
                fills.Add(side, fill);
            }

            var ceiling = ModelUtility.CreateGroup("Ceiling", dressing);
            // One light per arm (3.6 m from the middle); only a straight corridor also has one in the middle, so the lights stay
            // 3.6 m apart all along it.
            if (type == RoomType.CorridorStraight)
            {
                Props.Place(Prop.LedPanel, ceiling, new Vector3(0f, Ceiling, 0f), 0f, "Light_Center");
            }
            PutOnCeiling(Prop.CeilingSpeaker, ceiling, (0.6f, -0.6f));
            // Where a corner or a dead end closes the crossing on two sides, the arms' sprinklers do not reach its far
            // walls: one more on the axis.
            if (type == RoomType.CorridorCorner || type == RoomType.CorridorDeadEnd || lift)
            {
                PutOnCeiling(Prop.Sprinkler, ceiling, (0f, 0.6f));
            }
            // A dead end (and the lift end) has its arm light 3.6 m from the middle; a second one 3.0 m nearer the closed end
            // spreads the two evenly along the axis.
            if (type == RoomType.CorridorDeadEnd || lift)
            {
                PutOnCeiling(Prop.LedPanel, ceiling, (0f, -0.6f));
            }
            // A crossing or T-junction has its camera in the middle: the smoke detector goes diagonally across from the
            // loudspeaker, right next to the camera.
            bool crossOrT = type == RoomType.CorridorCross || type == RoomType.CorridorT;
            if (crossOrT)
            {
                Props.Place(Prop.SmokeDetector, ceiling, new Vector3(-0.6f, Ceiling, 0.6f), 0f, "SmokeDetector");
            }
            PlaceCorridorCameras(cameras, type);

            // The arms in order from the fire arm, clockwise (anticlockwise when mirrored).
            var armList = new List<Direction>();
            for (int i = 0; i < DirectionExtensions.Count; i++)
            {
                var side = (Direction)(((int)fireSide + (flip ? -i : i) + 2 * DirectionExtensions.Count) % DirectionExtensions.Count);
                if (Has(side)) armList.Add(side);
            }
            for (int i = 0; i < armList.Count; i++)
            {
                DressCorridorArm(Arm(dressing, $"Arm_{armList[i]}", armList[i]), i, i == armList.Count - 1, flip, !crossOrT);
            }

            // The grille and the light switch: the middle of the arm opposite the fire arm (on the face the services leave
            // free), or the middle of the wall that closes it.
            var opposite = fireSide.Opposite();
            var air = ModelUtility.CreateGroup("Ventilation", dressing);
            var safety = ModelUtility.CreateGroup("Safety", dressing);
            if (lift)
            {
                PlaceGrilleAndSwitch(fills[Direction.West], 0f, CorridorHalfWidth, 180f);
            }
            else if (Has(opposite))
            {
                var arm = dressing.Find($"Arm_{opposite}");
                float x = (flip ? -1f : 1f) * CorridorHalfWidth;
                float yaw = flip ? 90f : 270f;
                PlaceGrilleAndSwitch(arm, x, CorridorArmMiddle, yaw);
            }
            else
            {
                PlaceGrilleAndSwitch(fills[opposite], 0f, CorridorHalfWidth, 180f);
            }

            void PlaceGrilleAndSwitch(Transform wall, float x, float z, float yaw)
            {
                var at = ModelUtility.CreateGroup("WallMiddle", wall);
                var grille = Props.Place(Prop.FloorVentGrille, at, new Vector3(x, 0f, z), yaw, "ReturnGrille");
                grille.SetParent(air, true);
                var light = Props.Place(Prop.LightSwitch, at, new Vector3(x, Props.LightSwitchHeight, z), yaw, "LightSwitch");
                light.SetParent(safety, true);
                UnityEngine.Object.DestroyImmediate(at.gameObject);
            }
        }

        /// <summary>Distance from the room center to the middle of an arm's partition face (from the crossing to the cell wall).</summary>
        private static float CorridorArmMiddle => (CorridorHalfWidth + Wall) / 2f;

        /// <summary>
        /// One corridor arm, authored as the north arm (it runs from z = 1.5 to the wall; the partition faces are at
        /// x = ±1.5) and mirrored east-west if <paramref name="flip"/>. <paramref name="index"/> counts the arms from the
        /// fire arm: the fire kit in arm 0, the water and gas risers and the outlets (all on the west face, so the east face
        /// stays free for a grille) in arm 1.
        /// </summary>
        private static void DressCorridorArm(Transform arm, int index, bool last, bool flip, bool detectorInArm)
        {
            float s = flip ? -1f : 1f;
            float face = CorridorHalfWidth;
            float westFacing = flip ? 90f : 270f;
            float eastFacing = flip ? 270f : 90f;
            var fromEastFace = Vector3.left * s;
            var fromWestFace = Vector3.right * s;

            var ceiling = ModelUtility.CreateGroup("Ceiling", arm);
            // A 600 panel in the middle tile row: in a 3 m corridor a 1200 panel would wash out the walls next to it.
            Props.Place(Prop.LedPanel, ceiling, new Vector3(0f, Ceiling, 3.6f), 0f, "Light");
            foreach (float z in new[] { 1.8f, 4.8f })
            {
                Props.Place(Prop.Sprinkler, ceiling, new Vector3(0f, Ceiling, z), 0f, $"Sprinkler_{z:0.0}");
            }

            var pipes = ModelUtility.CreateGroup("Pipes", arm);
            var power = ModelUtility.CreateGroup("PowerAndData", arm);
            var safety = ModelUtility.CreateGroup("Safety", arm);
            if (index == 0)
            {
                // Fire water: riser clamped to the east partition, hose cabinet beside it, fed by a branch.
                var fire = RoomInfrastructure.Riser(pipes, "FireWaterRiser", new Vector3(s * face, 0f, 4.5f), fromEastFace, RoomPipes.FireRadius, SurfaceMaterial.SafetyRed);
                const float hydrantZ = 3.3f;
                Props.Place(Prop.HoseCabinet, safety, new Vector3(s * face, 0f, hydrantZ), westFacing);
                RoomPipes.Pipe(pipes, "HydrantBranch", fire + Vector3.up * 1.25f, new Vector3(fire.x, 1.25f, hydrantZ + 0.325f), RoomPipes.FireRadius, SurfaceMaterial.SafetyRed);
                Props.Place(Prop.FireExtinguisher, safety, new Vector3(-s * face, 0f, 3.6f), eastFacing);
                Props.Place(Prop.FireCallPoint, safety, new Vector3(-s * face, Props.CallPointHeight, 4.3f), eastFacing);
                if (detectorInArm)
                {
                    Props.Place(Prop.SmokeDetector, ceiling, new Vector3(0f, Ceiling, 2.4f), 0f, "SmokeDetector");
                }
            }
            else if (index == 1)
            {
                RoomInfrastructure.Riser(pipes, "WaterRiser", new Vector3(-s * face, 0f, 4.6f), fromWestFace, RoomPipes.NarrowRadius, SurfaceMaterial.PipeWater, 1.2f);
                RoomInfrastructure.Riser(pipes, "GasRiser", new Vector3(-s * face, 0f, 2.3f), fromWestFace, RoomPipes.NarrowRadius, SurfaceMaterial.PipeGas, 1.4f);
                RoomInfrastructure.WiredOutlet(power, "Outlets", new Vector3(-s * face, 0f, 3.45f), fromWestFace);
            }
        }

        /// <summary>
        /// One ceiling dome in the middle of a crossing, a T-junction or a corner, looking along the arms it watches
        /// (both arms of a corner). Straight corridors, dead ends and the lift end have no camera.
        /// </summary>
        private static void PlaceCorridorCameras(Transform cameras, RoomType type)
        {
            float yaw;
            switch (type)
            {
                case RoomType.CorridorCross: yaw = 0f; break;
                case RoomType.CorridorT: yaw = 180f; break;
                case RoomType.CorridorCorner: yaw = 135f; break;
                default: return;
            }
            Props.Place(Prop.CameraDome, cameras, new Vector3(0f, Ceiling, 0f), yaw, "CameraDome");
        }

        /// <summary>A group turned so that content authored for the north side ends up on <paramref name="side"/>.</summary>
        private static Transform Arm(Transform parent, string name, Direction side)
        {
            var group = ModelUtility.CreateGroup(name, parent);
            group.localRotation = Quaternion.Euler(0f, (int)side * RoomContract.RotationStepDegrees, 0f);
            return group;
        }

        /// <summary>
        /// The lift end's landing wall and shaft: where a dead end's crossing is closed to the north, a full-height wall
        /// across the 3 m corridor at <see cref="RoomContract.LiftWallFace"/> with the lift door's opening in the middle,
        /// and the block behind the shaft. The shaft's sides are the corridor's partition blocks.
        /// </summary>
        private static void AddLiftShaft(Transform room)
        {
            var structure = ModelUtility.CreateGroup(StructureName, room);
            float face = RoomContract.LiftWallFace;
            float thickness = RoomContract.LiftWallThickness;
            float corridor = CorridorHalfWidth;
            float half = RoomContract.LiftDoorWidth / 2f;
            float height = RoomContract.CeilingHeight;
            float door = RoomContract.LiftDoorHeight;
            float z = face + thickness / 2f;
            foreach (int x in new[] { -1, 1 })
            {
                ModelUtility.CreateStandingBox(x < 0 ? "LiftWall_W" : "LiftWall_E", structure, new Vector3(x * (corridor + half) / 2f, 0f, z),
                    new Vector3(corridor - half, height, thickness), SurfaceMaterial.Wall);
            }
            ModelUtility.CreateStandingBox("LiftWall_Lintel", structure, new Vector3(0f, door, z), new Vector3(2f * half, height - door, thickness), SurfaceMaterial.Wall);
            // Behind the car (and its counterweight) the cell is built up to the north wall.
            float back = RoomContract.LiftCarFront + RoomContract.LiftCarDepth + 0.25f;
            float wall = RoomContract.InteriorHalfSize;
            ModelUtility.CreateStandingBox("ShaftBack", structure, new Vector3(0f, 0f, (back + wall) / 2f), new Vector3(2f * corridor, height, wall - back), SurfaceMaterial.Wall);
        }
    }
}
