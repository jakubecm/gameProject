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
        /// A black dome camera housing (111 mm across, 80 mm high) hanging from the ceiling surface: the satin black shell
        /// with its rounded top edge, the seam and the flared lower bezel with a small screw port, a clear glass dome
        /// under it and, inside the glass with a gap of about 1.5 cm all round, a smaller black dome (the camera
        /// is not modelled). The pivot is the middle of the top face; the model hangs along -Y.
        /// </summary>
        private static void BuildDomeCameraModel(Transform parent)
        {
            var model = ModelUtility.CreateGroup("Model", parent);
            var housing = new MeshBuilder();
            var dome = new MeshBuilder();
            var inner = new MeshBuilder();
            var port = new MeshBuilder();
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

        /// <summary>Adds the disabled feed camera (looking along +Z, pitched down) and the <see cref="SecurityCamera"/> on the root.</summary>
        private static Camera AddSecurityCamera(Transform root, Vector3 lensPosition, float pitch)
        {
            var lensObject = new GameObject("FeedCamera");
            lensObject.transform.SetParent(root, false);
            lensObject.transform.localPosition = lensPosition;
            lensObject.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            var camera = lensObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.fieldOfView = 75f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 40f;
            camera.allowMSAA = false;
            camera.allowHDR = false;

            var securityCamera = root.gameObject.AddComponent<SecurityCamera>();
            var serialized = new SerializedObject(securityCamera);
            serialized.FindProperty("feedCamera").objectReferenceValue = camera;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return camera;
        }
    }
}
