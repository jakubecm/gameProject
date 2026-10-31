using System.Collections.Generic;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>Pendant dome camera for the hall: a junction box clamped to a beam, a pole with the cable inside and the dome camera at its end.</summary>
        [Model(Prop.CameraPendant)]
        private static void BuildCameraPendant(Transform root)
        {
            float bottom = -CameraPendantDrop;
            ModelUtility.CreateBox("JunctionBox", root, new Vector3(0f, -0.04f, 0f), new Vector3(0.15f, 0.08f, 0.15f), SurfaceMaterial.Trunking, false);
            ModelUtility.CreatePipe("Pole", root, new Vector3(0f, -0.08f, 0f), new Vector3(0f, bottom, 0f), 0.025f, SurfaceMaterial.BlackHousing);
            var camera = ModelUtility.CreateGroup("Camera", root);
            camera.localPosition = new Vector3(0f, bottom, 0f);
            BuildDomeCameraModel(camera);
            AddSecurityCamera(root, new Vector3(0f, bottom - 0.065f, 0f), 35f);
        }

        /// <summary>Length of the pole of a pendant CCTV dome, from its mount to the dome.</summary>
        public const float CameraPendantDrop = 2.2f;
    }
}
