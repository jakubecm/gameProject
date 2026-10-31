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
        /// Conference table 1.8 x 0.9 m after product photos: a light 30 mm top with slightly rounded edges on two T-legs (a
        /// square steel column set in 0.15 m from each end, on a flat foot with rounded ends running across the table, with
        /// rubber glides and a mounting plate under the top). The long side runs along X.
        /// </summary>
        [Model(Prop.MeetingTable)]
        private static void BuildMeetingTable(Transform root)
        {
            const float width = 1.8f;
            const float depth = 0.9f;
            const float top = 0.03f;
            const float footHeight = 0.025f;
            const float footLength = 0.70f;
            const float footWidth = 0.07f;
            var topMesh = new MeshBuilder();
            var steel = new MeshBuilder();
            var glide = new MeshBuilder();
            topMesh.AddRoundedBlock(new Vector3(0f, DeskHeight - top, 0f), width, depth, 0.012f, top, 0.004f, 4);
            foreach (int side in new[] { -1, 1 })
            {
                float x = side * (width / 2f - 0.15f);
                steel.AddRoundedBlock(new Vector3(x, 0.006f, 0f), footWidth, footLength, footWidth / 2f, footHeight - 0.006f, 0.004f, 8);
                steel.AddRoundedBlock(new Vector3(x, footHeight - 0.002f, 0f), 0.06f, 0.10f, 0.004f, DeskHeight - top - footHeight + 0.002f, 0.002f, 2);
                steel.AddRoundedBlock(new Vector3(x, DeskHeight - top - 0.012f, 0f), 0.09f, 0.34f, 0.006f, 0.012f, 0.002f, 3);
                foreach (int end in new[] { -1, 1 })
                {
                    glide.AddLathe(new[] { P(0f, 0f, true), P(0.014f, 0f, true), P(0.014f, 0.006f, true), P(0f, 0.006f, true) }, 16, true, new Vector3(x, 0f, end * (footLength / 2f - 0.04f)), Quaternion.identity);
                }
            }
            topMesh.Attach("Top", root, SurfaceMaterial.OfficeTopLight);
            steel.Attach("Legs", root, SurfaceMaterial.OfficeLegWhite);
            glide.Attach("Glides", root, SurfaceMaterial.MouseBlack);
            AddBoxCollider(root, new Vector3(0f, DeskHeight / 2f, 0f), new Vector3(width, DeskHeight, depth));
        }
    }
}
