using System.Collections.Generic;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEngine;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        public const float FreightDoorHeight = 2.2f;

        /// <summary>
        /// Landing of a goods lift on a wall (a wall prop, only looks: it never opens and has one collider), the passenger lift's
        /// design in galvanised steel instead of dark stainless, heavier and plainer: a two-panel centre-opening door 2.0 x 2.2 m
        /// whose meeting edges are chamfered, a frame round it, a sill, the call buttons on a light plate (round backlit buttons with a
        /// dark arrow each) and the floor indicator with its green digits over the door. The yellow keep-clear hatching on the floor in
        /// front of it is painted by the room.
        /// </summary>
        [Model(Prop.FreightLift)]
        private static void BuildFreightLift(Transform root)
        {
            const float doorHalf = FreightDoorWidth / 2f;
            const float doorHeight = FreightDoorHeight;
            const float jamb = 0.12f;
            const float frameDepth = 0.06f;
            const float leafWidth = doorHalf + 0.05f;
            const float leafThickness = 0.035f;
            const float leafZ = 0.03f;
            const float chamfer = 0.006f;

            // Doors: one panel per leaf, 5 cm wider than half the opening so they slip behind the frame; the edges where the leaves meet
            // are chamfered on both faces, so the dividing line shows without a gap between them.
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                var leaf = ModelUtility.CreateGroup(i == 0 ? "Leaf_Left" : "Leaf_Right", root);
                leaf.localPosition = new Vector3(side * leafWidth / 2f, 0f, 0f);
                float x0 = -leafWidth / 2f;
                float x1 = leafWidth / 2f;
                float z0 = leafZ - leafThickness / 2f;
                float z1 = leafZ + leafThickness / 2f;
                var outline = side < 0
                    ? new[] { new Vector2(x0, z0), new Vector2(x1 - chamfer, z0), new Vector2(x1, z0 + chamfer), new Vector2(x1, z1 - chamfer), new Vector2(x1 - chamfer, z1), new Vector2(x0, z1) }
                    : new[] { new Vector2(x0 + chamfer, z0), new Vector2(x1, z0), new Vector2(x1, z1), new Vector2(x0 + chamfer, z1), new Vector2(x0, z1 - chamfer), new Vector2(x0, z0 + chamfer) };
                var panels = new MeshBuilder();
                panels.AddPrism(outline, 0.008f, doorHeight, Vector3.zero);
                panels.Attach("Panels", leaf, SurfaceMaterial.GalvanisedSteel);
            }

            // Landing: frame, sill, call buttons, floor indicator.
            foreach (int side in new[] { -1, 1 })
            {
                ModelUtility.CreateStandingBox(side < 0 ? "Frame_Left" : "Frame_Right", root, new Vector3(side * (doorHalf + jamb / 2f), 0f, frameDepth / 2f),
                    new Vector3(jamb, doorHeight + jamb, frameDepth), SurfaceMaterial.GalvanisedSteel, false);
            }
            ModelUtility.CreateStandingBox("Frame_Head", root, new Vector3(0f, doorHeight, frameDepth / 2f), new Vector3(2f * (doorHalf + jamb), jamb, frameDepth), SurfaceMaterial.GalvanisedSteel, false);
            ModelUtility.CreateStandingBox("Sill", root, new Vector3(0f, 0f, 0.1f), new Vector3(2f * doorHalf + 0.1f, 0.008f, 0.2f), SurfaceMaterial.FloorVent, false);

            // Call buttons on a small light plate, as at the passenger lift: two round backlit buttons, each with a dark arrow (up, down).
            float buttonsX = -(doorHalf + jamb + 0.2f);
            ModelUtility.CreateBox("CallButtons", root, new Vector3(buttonsX, 1.0f, 0.008f), new Vector3(0.09f, 0.18f, 0.016f), SurfaceMaterial.DetectorWhite, false);
            var arrows = new MeshBuilder();
            foreach (var (y, name, up) in new[] { (1.04f, "Up", true), (0.96f, "Down", false) })
            {
                var button = ModelUtility.CreateCylinder($"Button_{name}", root, new Vector3(buttonsX, y, 0.0185f), 0.02f, 0.006f, SurfaceMaterial.LightPanel);
                button.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var ring = ModelUtility.CreateCylinder($"ButtonRing_{name}", root, new Vector3(buttonsX, y, 0.0165f), 0.025f, 0.002f, SurfaceMaterial.GalvanisedSteel);
                ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                const float zArrow = 0.0221f;
                if (up) arrows.AddFlatTriangle(new Vector3(buttonsX, y + 0.007f, zArrow), new Vector3(buttonsX - 0.007f, y - 0.005f, zArrow), new Vector3(buttonsX + 0.007f, y - 0.005f, zArrow), Vector3.forward);
                else arrows.AddFlatTriangle(new Vector3(buttonsX, y - 0.007f, zArrow), new Vector3(buttonsX + 0.007f, y + 0.005f, zArrow), new Vector3(buttonsX - 0.007f, y + 0.005f, zArrow), Vector3.forward);
            }
            arrows.Attach("Button_Arrows", root, SurfaceMaterial.BlackHousing);

            float indicatorY = doorHeight + jamb + 0.1f;
            ModelUtility.CreateBox("Indicator", root, new Vector3(0f, indicatorY, 0.015f), new Vector3(0.3f, 0.1f, 0.03f), SurfaceMaterial.GalvanisedSteel, false);
            ModelUtility.CreateBox("Indicator_Display", root, new Vector3(0f, indicatorY, 0.031f), new Vector3(0.22f, 0.06f, 0.002f), SurfaceMaterial.Screen, false);
            var indicatorDigits = new MeshBuilder();
            AddSegmentText(indicatorDigits, LiftFloorText, new Vector3(0f, indicatorY, 0.0322f), 0.04f);
            indicatorDigits.Attach("Indicator_Digits", root, SurfaceMaterial.SafetyGreen);

            AddBoxCollider(root, new Vector3(0f, (doorHeight + jamb) / 2f, frameDepth / 2f), new Vector3(2f * (doorHalf + jamb), doorHeight + jamb, frameDepth));
        }
    }
}
