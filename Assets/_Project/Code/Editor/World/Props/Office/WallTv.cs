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
        /// 65" display on a flat wall bracket, after a product photo: a very thin matt black slab (1.46 x 0.84 m, 28 mm deep, rounded
        /// edges) with a slim bezel round a dark glass screen. The screen centre is at 1.6 m; a wall prop.
        /// </summary>
        [Model(Prop.WallTv)]
        private static void BuildWallTv(Transform root)
        {
            const float depth = 0.028f;
            const float back = 0.012f;
            var body = new MeshBuilder();
            var screen = new MeshBuilder();
            var bracket = new MeshBuilder();
            bracket.AddBox(new Vector3(-0.2f, WallScreenCenter - 0.15f, 0f), new Vector3(0.2f, WallScreenCenter + 0.15f, back));
            body.AddRoundedSlab(new Vector3(0f, WallScreenCenter, back), 1.46f, 0.84f, 0.006f, depth, 0.003f, 3, true);
            float z = back + depth + 0.0003f;
            screen.AddPanel(new Vector3(-0.7225f, WallScreenCenter - 0.4025f, z), new Vector3(0.7225f, WallScreenCenter - 0.4025f, z),
                new Vector3(0.7225f, WallScreenCenter + 0.4025f, z), new Vector3(-0.7225f, WallScreenCenter + 0.4025f, z), Vector3.forward,
                Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            bracket.Attach("Bracket", root, SurfaceMaterial.Metal);
            body.Attach("Body", root, SurfaceMaterial.BlackHousing);
            screen.Attach("Screen", root, SurfaceMaterial.Screen);
        }

        /// <summary>Height of the middle of a wall TV's screen.</summary>
        public const float WallScreenCenter = 1.6f;
    }
}
