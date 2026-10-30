using System.Collections.Generic;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEngine;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>How far the lift door's photo cells see into the car: less than the player's distance at the start.</summary>
        public const float LiftSensorInCar = 0.35f;

        [Model(Prop.PassengerLift)]
        private static void BuildPassengerLift(Transform root)
        {
            float doorHalf = RoomContract.LiftDoorWidth / 2f;
            float doorHeight = RoomContract.LiftDoorHeight;
            float wall = RoomContract.LiftWallThickness;
            float carHalf = RoomContract.LiftCarWidth / 2f;
            float carHeight = RoomContract.LiftCarHeight;
            // Prop-local Z of the car's inner front and back faces (the car is behind the wall, at -Z).
            float carFront = -(RoomContract.LiftCarFront - RoomContract.LiftWallFace);
            float carBack = carFront - RoomContract.LiftCarDepth;
            const float panel = 0.04f;
            const float roof = 0.1f;

            // Doors: a landing panel and a car panel per leaf, 5 cm wider than half the opening, so they overlap the jambs. The edges
            // where the leaves meet are chamfered on both faces, so the dividing line shows without a gap between them.
            float leafWidth = doorHalf + 0.05f;
            const float chamfer = 0.006f;
            var leaves = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                var leaf = ModelUtility.CreateGroup(i == 0 ? "Leaf_Left" : "Leaf_Right", root);
                leaf.localPosition = new Vector3(side * leafWidth / 2f, 0f, 0f);
                var panels = new MeshBuilder();
                foreach (var (zCentre, thickness) in new[] { (-wall - 0.025f, 0.035f), (carFront + 0.06f, 0.03f) })
                {
                    float x0 = -leafWidth / 2f;
                    float x1 = leafWidth / 2f;
                    float z0 = zCentre - thickness / 2f;
                    float z1 = zCentre + thickness / 2f;
                    var outline = side < 0
                        ? new[] { new Vector2(x0, z0), new Vector2(x1 - chamfer, z0), new Vector2(x1, z0 + chamfer), new Vector2(x1, z1 - chamfer), new Vector2(x1 - chamfer, z1), new Vector2(x0, z1) }
                        : new[] { new Vector2(x0 + chamfer, z0), new Vector2(x1, z0), new Vector2(x1, z1), new Vector2(x0 + chamfer, z1), new Vector2(x0, z1 - chamfer), new Vector2(x0, z0 + chamfer) };
                    panels.AddPrism(outline, 0f, doorHeight, Vector3.zero);
                }
                panels.Attach("Panels", leaf, SurfaceMaterial.StainlessSteel);
                // One collider over both panels.
                float back = carFront + 0.045f;
                float front = -wall - 0.0075f;
                AddBoxCollider(leaf, new Vector3(0f, doorHeight / 2f, (back + front) / 2f), new Vector3(leafWidth, doorHeight, front - back));
                leaves[i] = leaf;
            }

            // Landing: frame, sill, call buttons, floor indicator, signs.
            const float jamb = 0.08f;
            foreach (int side in new[] { -1, 1 })
            {
                ModelUtility.CreateStandingBox(side < 0 ? "Frame_Left" : "Frame_Right", root, new Vector3(side * (doorHalf + jamb / 2f), 0f, 0.015f),
                    new Vector3(jamb, doorHeight + jamb, 0.03f), SurfaceMaterial.StainlessSteel, false);
                // The wall's reveals inside the opening are lined with steel too.
                ModelUtility.CreateStandingBox(side < 0 ? "Reveal_Left" : "Reveal_Right", root, new Vector3(side * (doorHalf - 0.005f), 0f, -wall / 2f),
                    new Vector3(0.01f, doorHeight, wall), SurfaceMaterial.StainlessSteel, false);
            }
            ModelUtility.CreateStandingBox("Frame_Head", root, new Vector3(0f, doorHeight, 0.015f), new Vector3(2f * (doorHalf + jamb), jamb, 0.03f), SurfaceMaterial.StainlessSteel, false);
            ModelUtility.CreateStandingBox("Sill", root, new Vector3(0f, 0f, (carFront + 0.08f) / 2f + 0.02f), new Vector3(2f * doorHalf + 0.1f, 0.006f, -carFront + 0.04f), SurfaceMaterial.FloorVent, false);

            // Call buttons on a small light plate, to the right of the door as seen from the corridor: two round buttons, backlit,
            // each with a dark arrow (up, down).
            float buttonsX = -(doorHalf + jamb + 0.2f);
            ModelUtility.CreateBox("CallButtons", root, new Vector3(buttonsX, 1.0f, 0.008f), new Vector3(0.09f, 0.18f, 0.016f), SurfaceMaterial.DetectorWhite, false);
            var arrows = new MeshBuilder();
            foreach (var (y, name, up) in new[] { (1.04f, "Up", true), (0.96f, "Down", false) })
            {
                var button = ModelUtility.CreateCylinder($"Button_{name}", root, new Vector3(buttonsX, y, 0.0185f), 0.02f, 0.006f, SurfaceMaterial.LightPanel);
                button.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var ring = ModelUtility.CreateCylinder($"ButtonRing_{name}", root, new Vector3(buttonsX, y, 0.0165f), 0.025f, 0.002f, SurfaceMaterial.StainlessSteel);
                ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                const float zArrow = 0.0221f;
                if (up) arrows.AddFlatTriangle(new Vector3(buttonsX, y + 0.007f, zArrow), new Vector3(buttonsX - 0.007f, y - 0.005f, zArrow), new Vector3(buttonsX + 0.007f, y - 0.005f, zArrow), Vector3.forward);
                else arrows.AddFlatTriangle(new Vector3(buttonsX, y - 0.007f, zArrow), new Vector3(buttonsX + 0.007f, y + 0.005f, zArrow), new Vector3(buttonsX - 0.007f, y + 0.005f, zArrow), Vector3.forward);
            }
            arrows.Attach("Button_Arrows", root, SurfaceMaterial.BlackHousing);

            float indicatorY = doorHeight + jamb + 0.1f;
            ModelUtility.CreateBox("Indicator", root, new Vector3(0f, indicatorY, 0.015f), new Vector3(0.3f, 0.1f, 0.03f), SurfaceMaterial.StainlessSteel, false);
            ModelUtility.CreateBox("Indicator_Display", root, new Vector3(0f, indicatorY, 0.031f), new Vector3(0.22f, 0.06f, 0.002f), SurfaceMaterial.Screen, false);
            var indicatorDigits = new MeshBuilder();
            AddSegmentText(indicatorDigits, LiftFloorText, new Vector3(0f, indicatorY, 0.0322f), 0.04f);
            indicatorDigits.Attach("Indicator_Digits", root, SurfaceMaterial.SafetyGreen);
            var exit = Place(Prop.ExitSign, root, new Vector3(0f, indicatorY + 0.2f, 0f), 0f);
            exit.name = "ExitSign";

            // Car, after a product photo: dark grey matt steel panels with vertical joints, a light floor, a frame round the door, a
            // light strip across the ceiling, a handrail across the whole back wall under the mirror and the operating panel on the
            // right-hand wall. The walls stop the player, so they have colliders; the fittings on them do not.
            var car = ModelUtility.CreateGroup("Car", root);
            float outer = carHalf + panel;
            float frontFace = carFront + panel;
            ModelUtility.CreateBoxBetween("Floor", car, new Vector3(-carHalf, 0f, carBack), new Vector3(carHalf, 0.008f, carFront), SurfaceMaterial.Furniture);
            ModelUtility.CreateBoxBetween("Wall_Back", car, new Vector3(-outer, 0f, carBack - panel), new Vector3(outer, carHeight + roof, carBack), SurfaceMaterial.StainlessSteel, true);
            float middle = (carFront + carBack) / 2f;
            foreach (int side in new[] { -1, 1 })
            {
                string name = side < 0 ? "Left" : "Right";
                ModelUtility.CreateBoxBetween($"Wall_{name}", car, new Vector3(side * carHalf, 0f, carBack - panel), new Vector3(side * outer, carHeight + roof, frontFace), SurfaceMaterial.StainlessSteel, true);
                // The car's front returns beside the door.
                ModelUtility.CreateBoxBetween($"Return_{name}", car, new Vector3(side * doorHalf, 0f, carFront), new Vector3(side * carHalf, carHeight + roof, frontFace), SurfaceMaterial.StainlessSteel, true);
                ModelUtility.CreateBoxBetween($"Kickplate_{name}", car, new Vector3(side * (carHalf - 0.003f), 0f, carBack), new Vector3(side * carHalf, 0.15f, carFront), SurfaceMaterial.Trim);
                // The frame round the door, standing 15 mm proud of the front wall.
                ModelUtility.CreateBox($"DoorFrame_{name}", car, new Vector3(side * (doorHalf + 0.03f), doorHeight / 2f + 0.03f, carFront - 0.0075f), new Vector3(0.06f, doorHeight + 0.06f, 0.015f), SurfaceMaterial.StainlessSteel, false);
                // A vertical joint between two wall panels, halfway along the side wall.
                ModelUtility.CreateBox($"Joint_{name}", car, new Vector3(side * (carHalf - 0.001f), carHeight / 2f, middle), new Vector3(0.003f, carHeight, 0.004f), SurfaceMaterial.BlackHousing, false);
            }
            ModelUtility.CreateBoxBetween("Transom", car, new Vector3(-doorHalf, doorHeight, carFront), new Vector3(doorHalf, carHeight + roof, frontFace), SurfaceMaterial.StainlessSteel, true);
            ModelUtility.CreateBox("DoorFrame_Head", car, new Vector3(0f, doorHeight + 0.03f, carFront - 0.0075f), new Vector3(2f * doorHalf + 0.12f, 0.06f, 0.015f), SurfaceMaterial.StainlessSteel, false);
            // Handrail under the mirror: a round steel tube, shorter than the wall (0.25 m clear at each end) and bent back into the
            // wall at both ends, each end covered by a round rosette (the way a real lift rail is finished).
            {
                const float railRadius = 0.0175f;
                const float standOff = 0.065f;
                const float bend = 0.035f;
                float railX = carHalf - 0.25f;
                float railY = 0.90f;
                var rail = new MeshBuilder();
                var path = new List<Vector3>();
                void Corner(float centreX, float fromDegrees, float toDegrees)
                {
                    for (int step = 0; step <= 5; step++)
                    {
                        float angle = Mathf.Lerp(fromDegrees, toDegrees, step / 5f) * Mathf.Deg2Rad;
                        path.Add(new Vector3(centreX + Mathf.Cos(angle) * bend, railY, carBack + standOff - bend + Mathf.Sin(angle) * bend));
                    }
                }
                path.Add(new Vector3(-railX, railY, carBack));
                Corner(-railX + bend, 180f, 90f);
                Corner(railX - bend, 90f, 0f);
                path.Add(new Vector3(railX, railY, carBack));
                rail.AddTube(path, railRadius, 12, Vector3.zero, Quaternion.identity);
                foreach (float x in new[] { -railX, railX })
                {
                    rail.AddLathe(new[] { MeshBuilder.P(0f, 0f, true), MeshBuilder.P(0.032f, 0f, true), MeshBuilder.P(0.032f, 0.006f, true), MeshBuilder.P(0.024f, 0.010f), MeshBuilder.P(0f, 0.010f, true) },
                        24, true, new Vector3(x, railY, carBack), Quaternion.Euler(90f, 0f, 0f));
                }
                rail.Attach("Handrail_Back", car, SurfaceMaterial.StainlessSteel);
            }
            ModelUtility.CreateBoxBetween("Mirror", car, new Vector3(-carHalf + 0.05f, 0.95f, carBack), new Vector3(carHalf - 0.05f, 2.1f, carBack + 0.004f), SurfaceMaterial.Glass);
            ModelUtility.CreateBoxBetween("Ceiling", car, new Vector3(-outer, carHeight, carBack - panel), new Vector3(outer, carHeight + roof, frontFace), SurfaceMaterial.StainlessSteel, true);
            // Light strip across the ceiling over the door.
            ModelUtility.CreateBoxBetween("CeilingLight", car, new Vector3(-carHalf + 0.15f, carHeight - 0.006f, carFront - 0.2f), new Vector3(carHalf - 0.15f, carHeight, carFront - 0.12f), SurfaceMaterial.LightPanel);
            AddSpotLight(car, new Vector3(0f, carHeight - 0.03f, (carFront + carBack) / 2f), 160f, 120f, 3.5f, 2.2f, LedPanelColor);

            // Car operating panel on the right-hand wall (+X, as you face the door), in the middle of the wall panel next to the door (the
            // joint halfway along the wall splits the wall in two): the same light plate and backlit round buttons as the call buttons
            // outside, a display with the green floor number on top, two columns of five buttons.
            float copX = carHalf - 0.012f;
            float copZ = (middle + carFront) / 2f;
            ModelUtility.CreateBox("OperatingPanel", car, new Vector3(carHalf - 0.006f, 1.2f, copZ), new Vector3(0.012f, 0.42f, 0.13f), SurfaceMaterial.DetectorWhite, false);
            ModelUtility.CreateBox("OperatingPanel_Display", car, new Vector3(copX - 0.0005f, 1.375f, copZ), new Vector3(0.001f, 0.04f, 0.09f), SurfaceMaterial.Screen, false);
            var digitsGroup = ModelUtility.CreateGroup("OperatingPanel_Digits", car);
            digitsGroup.localPosition = new Vector3(copX - 0.0012f, 1.375f, copZ);
            digitsGroup.localRotation = Quaternion.Euler(0f, -90f, 0f);
            var carDigits = new MeshBuilder();
            AddSegmentText(carDigits, LiftFloorText, Vector3.zero, 0.026f);
            carDigits.Attach("Digits", digitsGroup, SurfaceMaterial.SafetyGreen);
            for (int i = 0; i < 10; i++)
            {
                float y = 1.285f - (i / 2) * 0.055f;
                float z = copZ + (i % 2 == 0 ? 0.03f : -0.03f);
                var ring = ModelUtility.CreateCylinder($"OperatingPanel_Ring_{i}", car, new Vector3(copX - 0.0005f, y, z), 0.022f, 0.002f, SurfaceMaterial.StainlessSteel);
                ring.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                var button = ModelUtility.CreateCylinder($"OperatingPanel_Button_{i}", car, new Vector3(copX - 0.0035f, y, z), 0.016f, 0.006f, SurfaceMaterial.LightPanel);
                button.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }

            ConfigureDoor(root, leaves, RoomContract.LiftDoorWidth / 2f, new Renderer[0],
                // Photo cells watch the landing in front of the door and the front of the car, not where the player starts.
                new Vector3(0f, 1f, (1.2f + carFront - LiftSensorInCar) / 2f), new Vector3(1.4f, 2f, 1.2f - carFront + LiftSensorInCar));
            var serialized = new SerializedObject(root.GetComponent<AutoSlidingDoor>());
            serialized.FindProperty("openSeconds").floatValue = 1.4f;
            serialized.FindProperty("closeSeconds").floatValue = 1.8f;
            serialized.FindProperty("holdOpenSeconds").floatValue = 2.5f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
