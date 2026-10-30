using System.Collections.Generic;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEngine;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>Distance of the room door leaves from the wall center: directly in front of the 4 cm door frame casing, with a 5 mm clearance.</summary>
        public const float RoomDoorLeafOffset = RoomContract.WallThickness / 2f + 0.045f;

        /// <summary>Along the wall from the opening's middle: the access reader on the side the leaves slide on.</summary>
        public const float ReaderAlongWall = 2.59f;

        /// <summary>
        /// Automatic bi-parting sliding glass door for a room opening (2.4 x 2.6 m). The pivot is the opening pose
        /// (<see cref="RoomContract.GetOpeningPose"/>); +Z is the owner's side, where the leaves slide along the wall
        /// under the operator box. Photo cells on the frame on both sides, an access reader with a status LED on both
        /// sides, and an exit sign on the front of the operator box, on the owner's side only: the owner is the room, so
        /// the sign shows the way out of the room into the corridor, and the corridor side has none (an exit sign there
        /// would point into a dead end).
        /// </summary>
        [Model(Prop.RoomDoor)]
        private static void BuildRoomDoor(Transform root)
        {
            float width = RoomContract.OpeningWidth;
            float height = RoomContract.OpeningHeight;
            float half = width / 2f;
            float ownerFace = RoomContract.WallThickness / 2f;
            // The neighbour's wall stands back to back with the owner's, so its face is one more wall thickness out.
            float otherFace = -(ownerFace + RoomContract.WallThickness);
            const float casing = 0.04f;

            var leds = new System.Collections.Generic.List<Renderer>();
            var leaves = BuildLeaves(root, width, height - 0.01f, RoomDoorLeafOffset);

            // Slim header (the operator cover) over the opening, standing right on the leaves and the casing, in the colour of the lift steel,
            // with the photo cell in the middle of its front: a dark lens module with a small status light.
            float operatorEnd = half + half + 0.1f;
            float headerFront = RoomDoorLeafOffset + 0.07f;
            var header = new MeshBuilder();
            var sensor = new MeshBuilder();
            var sensorLed = new MeshBuilder();
            header.AddBox(new Vector3(-operatorEnd, height, RoomDoorLeafOffset), new Vector3(operatorEnd, height + 0.10f, headerFront));
            header.Attach("Header", root, SurfaceMaterial.StainlessSteel);
            // The photo cell: a black landscape housing like the access reader's (125 x 55 x 20 mm, rounded) on the middle of the header,
            // with one round status light exactly in its centre.
            sensor.AddRoundedSlab(new Vector3(0f, height + 0.05f, headerFront), 0.125f, 0.055f, 0.008f, 0.02f, 0.004f, 4);
            sensorLed.AddLathe(new[] { MeshBuilder.P(0f, 0f, true), MeshBuilder.P(0.0065f, 0f, true), MeshBuilder.P(0.0065f, 0.0015f, true), MeshBuilder.P(0f, 0.0015f, true) }, 20, true,
                new Vector3(0f, height + 0.05f, headerFront + 0.02f), Quaternion.Euler(90f, 0f, 0f));
            sensor.Attach("PhotoCell_In", root, SurfaceMaterial.ReaderBlack);
            sensorLed.Attach("PhotoCell_In_Led", root, SurfaceMaterial.SafetyGreen);
            // The same photo cell on the corridor side, on the wall over the opening, facing the corridor.
            var outGroup = ModelUtility.CreateGroup("PhotoCell_Out", root);
            outGroup.localPosition = new Vector3(0f, height + 0.05f, otherFace);
            outGroup.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var sensorOut = new MeshBuilder();
            var sensorOutLed = new MeshBuilder();
            sensorOut.AddRoundedSlab(Vector3.zero, 0.125f, 0.055f, 0.008f, 0.02f, 0.004f, 4);
            sensorOutLed.AddLathe(new[] { MeshBuilder.P(0f, 0f, true), MeshBuilder.P(0.0065f, 0f, true), MeshBuilder.P(0.0065f, 0.0015f, true), MeshBuilder.P(0f, 0.0015f, true) }, 20, true,
                new Vector3(0f, 0f, 0.02f), Quaternion.Euler(90f, 0f, 0f));
            sensorOut.Attach("Housing", outGroup, SurfaceMaterial.ReaderBlack);
            sensorOutLed.Attach("Led", outGroup, SurfaceMaterial.SafetyGreen);

            leds.Add(BuildReader(root, "Reader_In", new Vector3(ReaderAlongWall, ReaderHeight, ownerFace), 0f));
            leds.Add(BuildReader(root, "Reader_Out", new Vector3(OuterReaderAlongWall, ReaderHeight, otherFace), 180f));

            // The exit sign hangs on the wall on the room side, facing into the room, exactly midway between the top of the header and the suspended ceiling.
            var exit = Place(Prop.ExitSign, root, new Vector3(0f, (height + 0.10f + RoomContract.CeilingHeight - CeilingGridDepth) / 2f, ownerFace + 0.01f), 0f);
            exit.name = "ExitSign";

            ConfigureDoor(root, leaves, half, leds.ToArray(), new Vector3(0f, 1f, (ownerFace + otherFace) / 2f), new Vector3(width, 2f, 3.6f));
        }
    }
}
