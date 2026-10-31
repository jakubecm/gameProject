using System;
using System.Collections.Generic;
using System.Linq;
using ColdShutdown.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace ColdShutdown.Editor
{
    /// <summary>The cooling plant: heat exchangers, pumps, buffer tank, air handler, expansion vessel and the thermometer wall (a clue).</summary>
    public static partial class Rooms
    {
        [Template(RoomType.CoolingPlant)]
        private static RoomTemplate CoolingPlantTemplate(RoomType type) => new RoomTemplate
        {
            Structure = AddSmallRoomWalls,
            Dress = (dressing, cameras, layout) => DressCoolingPlant(dressing, cameras),
        };

        /// <summary>
        /// Cooling plant (HVAC room, 6 x 6 m), a chilled-water plant for the data centre that fills the room except for the 2.4 m aisle
        /// from the door to the thermometer wall. Everything is piped into the two large plate heat exchangers at the back end of the
        /// side walls, and every pipe runs along the walls or rises from the floor or ceiling or into a wall, none crosses the room. On one
        /// side the pump set (two base-mounted pumps with blue motors, their discharge into the exchanger's flow line along the wall and
        /// the return line down to their suction flanges); on the other the buffer tank, the air handling unit and the expansion vessel,
        /// tapped into the second exchanger's flow and return lines. The six thermometers on the back wall run into the exchangers' end
        /// faces. The only gauges are the large ones on that wall and the pumps' thermometers. The pumps are on the east side.
        /// Nothing stands in the aisle or the door pockets, nor in the extinguisher corner.
        /// </summary>
        private static void DressCoolingPlant(Transform dressing, Transform cameras)
        {
            var plant = ModelUtility.CreateGroup("Plant", dressing);
            var pipes = ModelUtility.CreateGroup("Pipes", dressing);
            var air = ModelUtility.CreateGroup("Ventilation", dressing);
            var markings = ModelUtility.CreateGroup("Markings", dressing);
            var ceiling = ModelUtility.CreateGroup("Ceiling", dressing);

            // Yellow lines 1.4 m either side of the middle (clear of the door frame) along the aisle from the door to the thermometer wall: everything between them stays clear.
            foreach (int side in new[] { -1, 1 })
            {
                Marking(markings, $"KeepClear_{(side < 0 ? "W" : "E")}", new Vector3(side * 1.4f, 0f, -2f * Side), new Vector3(side * 1.4f, 0f, RoomContract.SmallRoomBackWall));
            }

            const float s = 1f;                               // the side of the pumps: east
            float pumpSideYaw = s > 0f ? 270f : 90f;          // faces the aisle from that side
            float otherSideYaw = s > 0f ? 90f : 270f;
            float wallLine = Side - RoomPipes.WideRadius - 0.04f;
            float wallP = s * wallLine;                       // axis of the wall pipes on the pump side
            float wallO = -s * wallLine;                      // ... and on the other side
            float exchangerX = wallLine - 0.5f;
            const float laneFlow = 2.62f;
            const float laneReturn = 2.40f;
            const float flowZ = -2.00f;                       // where the exchangers' secondary flow and return pipes rise
            const float returnZ = -2.30f;
            float wide = RoomPipes.WideRadius;
            var towardsAisleP = Vector3.left * s;
            var towardsAisleO = Vector3.right * s;

            Put(Prop.CoolingMeterPanel, plant, 0f, 0f, RoomContract.SmallRoomBackWall, 180f, "MeterPanel");
            Put(s > 0f ? Prop.HeatExchanger : Prop.HeatExchangerMirrored, plant, s * exchangerX, 0f, -1.55f, pumpSideYaw, "HeatExchanger_A");
            Put(s > 0f ? Prop.HeatExchangerMirrored : Prop.HeatExchanger, plant, -s * exchangerX, 0f, -1.55f, otherSideYaw, "HeatExchanger_B");

            // Pump side: two pumps with the motors to the aisle and the suction flanges to the wall.
            float[] pumpZ = { -3.4f, -4.2f };
            float pumpX = s * 2.4f;
            float flangeX = s * (2.4f + 0.285f);
            for (int i = 0; i < 2; i++)
            {
                float z = pumpZ[i];
                Put(Prop.CirculationPump, plant, pumpX, 0f, z, s > 0f ? 180f : 0f, $"Pump_{i + 1}");
                // Discharge up to the flow lane, to the wall line (it ends on the header that runs along the wall to the exchanger).
                PipePath($"Discharge_{i + 1}", pipes, new[] { new Vector3(pumpX, 0.7f, z), new Vector3(pumpX, laneFlow, z), new Vector3(wallP, laneFlow, z) }, wide, SurfaceMaterial.PipeWater);
                RoomPipes.BallValve(pipes, $"DischargeValve_{i + 1}", new Vector3(pumpX, 1.05f, z), Vector3.up, towardsAisleP, wide);
                Put(Prop.PipeThermometer, pipes, new Vector3(pumpX, 1.5f, z) + towardsAisleP * wide, pumpSideYaw, $"Thermometer_{i + 1}");
            }
            // Flow header along the wall from the exchanger to the far pump; return header back from the exchanger, down to the suctions.
            PipePath("FlowHeader_A", pipes, new[] { new Vector3(wallP, laneFlow, flowZ), new Vector3(wallP, laneFlow, pumpZ[1]) }, wide, SurfaceMaterial.PipeWater);
            PipePath("ReturnHeader_A", pipes, new[]
            {
                new Vector3(wallP, laneReturn, returnZ), new Vector3(wallP, laneReturn, pumpZ[1]), new Vector3(wallP, 0.34f, pumpZ[1]), new Vector3(flangeX, 0.34f, pumpZ[1]),
            }, wide, SurfaceMaterial.PipeWater);
            PipePath("ReturnBranch_A", pipes, new[] { new Vector3(wallP, laneReturn, pumpZ[0]), new Vector3(wallP, 0.34f, pumpZ[0]), new Vector3(flangeX, 0.34f, pumpZ[0]) }, wide, SurfaceMaterial.PipeWater);
            WallClamps("FlowClamps_A", pipes, wallP, laneFlow, flowZ, pumpZ[1], towardsAisleP);
            WallClamps("ReturnClamps_A", pipes, wallP, laneReturn, returnZ, pumpZ[1], towardsAisleP);

            // The other side: buffer tank, air handling unit and expansion vessel tapped into the second exchanger's lines.
            float tankX = -s * 2.28f;
            const float tankZ = -3.12f;
            Put(Prop.BufferTank, plant, tankX, 0f, tankZ, otherSideYaw, "BufferTank");
            var otherTurn = Quaternion.Euler(0f, otherSideYaw, 0f);
            var tankFlow = new Vector3(tankX, 0f, tankZ) + otherTurn * new Vector3(-0.12f, 0f, 0f);
            var tankReturn = new Vector3(tankX, 0f, tankZ) + otherTurn * new Vector3(0.12f, 0f, 0f);
            PipePath("TankFlow", pipes, new[] { new Vector3(tankFlow.x, 2.23f, tankFlow.z), new Vector3(tankFlow.x, laneFlow, tankFlow.z), new Vector3(wallO, laneFlow, tankFlow.z) }, wide, SurfaceMaterial.PipeWater);
            RoomPipes.BallValve(pipes, "TankFlowValve", new Vector3(tankFlow.x, 2.43f, tankFlow.z), Vector3.up, towardsAisleO, wide);
            PipePath("TankReturn", pipes, new[] { new Vector3(tankReturn.x, 2.23f, tankReturn.z), new Vector3(tankReturn.x, laneReturn, tankReturn.z), new Vector3(wallO, laneReturn, tankReturn.z) }, wide, SurfaceMaterial.PipeWater);

            const float handlerZ = -3.95f;
            float handlerX = -s * 2.48f;
            Put(Prop.AirHandler, plant, handlerX, 0f, handlerZ, otherSideYaw, "AirHandler");
            var handlerFlow = new Vector3(handlerX, 0f, handlerZ) + otherTurn * new Vector3(-0.2f, 0f, -0.23f);
            var handlerReturn = new Vector3(handlerX, 0f, handlerZ) + otherTurn * new Vector3(0.2f, 0f, -0.23f);
            PipePath("HandlerFlow", pipes, new[] { new Vector3(handlerFlow.x, 1.99f, handlerFlow.z), new Vector3(handlerFlow.x, laneFlow, handlerFlow.z), new Vector3(wallO, laneFlow, handlerFlow.z) }, wide, SurfaceMaterial.PipeWater);
            RoomPipes.BallValve(pipes, "HandlerFlowValve", new Vector3(handlerFlow.x, 2.3f, handlerFlow.z), Vector3.up, towardsAisleO, wide);
            PipePath("HandlerReturn", pipes, new[] { new Vector3(handlerReturn.x, 1.99f, handlerReturn.z), new Vector3(handlerReturn.x, laneReturn, handlerReturn.z), new Vector3(wallO, laneReturn, handlerReturn.z) }, wide, SurfaceMaterial.PipeWater);

            const float vesselZ = -4.57f;
            float vesselX = -s * 2.35f;
            Put(Prop.ExpansionVessel, plant, vesselX, 0f, vesselZ, otherSideYaw, "ExpansionVessel");
            var nozzle = new Vector3(vesselX, 0.5f, vesselZ) + otherTurn * new Vector3(0f, 0f, -0.28f);
            PipePath("VesselReturn", pipes, new[] { new Vector3(wallO, laneReturn, vesselZ), new Vector3(wallO, 0.5f, vesselZ), new Vector3(nozzle.x, 0.5f, vesselZ) }, wide, SurfaceMaterial.PipeWater);

            // Flow and return headers along the wall on the other side.
            PipePath("FlowHeader_B", pipes, new[] { new Vector3(wallO, laneFlow, flowZ), new Vector3(wallO, laneFlow, handlerFlow.z) }, wide, SurfaceMaterial.PipeWater);
            PipePath("ReturnHeader_B", pipes, new[] { new Vector3(wallO, laneReturn, returnZ), new Vector3(wallO, laneReturn, vesselZ) }, wide, SurfaceMaterial.PipeWater);
            WallClamps("FlowClamps_B", pipes, wallO, laneFlow, flowZ, handlerFlow.z, towardsAisleO);
            WallClamps("ReturnClamps_B", pipes, wallO, laneReturn, returnZ, vesselZ, towardsAisleO);

            TechnicalCeiling(ceiling, air);
            SmallRoomBasics(dressing, cameras, TechnicalDetector);
        }

        /// <summary>Blue pipe clamps every metre along a horizontal run on the wall (x = <paramref name="wallX"/>, between two z), at height <paramref name="y"/>.</summary>
        private static void WallClamps(string name, Transform parent, float wallX, float y, float fromZ, float toZ, Vector3 normal)
        {
            var strap = new MeshBuilder();
            var screws = new MeshBuilder();
            float direction = Mathf.Sign(toZ - fromZ);
            for (float z = fromZ + direction * 0.5f; direction * (toZ - z) > 0.3f; z += direction * 1.0f)
            {
                RoomPipes.Clamp(strap, screws, new Vector3(wallX, y, z), Vector3.forward, normal, RoomPipes.WideRadius, 0.04f);
            }
            strap.Attach($"{name}_Clamps", parent, SurfaceMaterial.SupportBlue);
            screws.Attach($"{name}_Screws", parent, SurfaceMaterial.Chrome);
        }
    }
}
