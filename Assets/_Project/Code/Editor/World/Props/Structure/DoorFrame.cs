using System;
using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Casing on the room side of the wall around an opening. It frames the opening without entering
        /// it, so the opening stays 2.4 x 2.6 m and a wall plug inside it reads as a closed door.
        /// </summary>
        [Model(Prop.DoorFrame)]
        private static void BuildDoorFrame(Transform root)
        {
            const float width = 0.1f;
            const float depth = 0.04f;
            float halfOpening = RoomContract.OpeningWidth / 2f;
            float openingHeight = RoomContract.OpeningHeight;
            float z = RoomContract.WallThickness / 2f + depth / 2f;

            var jambSize = new Vector3(width, openingHeight + width, depth);
            ModelUtility.CreateStandingBox("Jamb_Left", root, new Vector3(-(halfOpening + width / 2f), 0f, z), jambSize, SurfaceMaterial.Trim, false);
            ModelUtility.CreateStandingBox("Jamb_Right", root, new Vector3(halfOpening + width / 2f, 0f, z), jambSize, SurfaceMaterial.Trim, false);
            ModelUtility.CreateStandingBox("Head", root, new Vector3(0f, openingHeight, z), new Vector3(halfOpening * 2f, width, depth), SurfaceMaterial.Trim, false);
        }
    }
}
