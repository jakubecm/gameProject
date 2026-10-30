using System.Collections.Generic;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>Small wall ventilation grille 600 x 300 mm mounted high under the ceiling; the pivot is the middle of its back.</summary>
        [Model(Prop.WallVentGrille)]
        private static void BuildWallVentGrille(Transform root) => BuildLouveredGrille(root, 0.6f, WallGrilleHeight, 0f, 0.010f, 0.016f);

        /// <summary>Height of <see cref="BuildWallVentGrille"/>.</summary>
        public const float WallGrilleHeight = 0.3f;
    }
}
