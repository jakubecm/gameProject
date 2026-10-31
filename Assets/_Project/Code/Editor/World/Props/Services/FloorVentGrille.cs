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
        /// <summary>Large square ventilation grille 600 x 600 mm at the foot of a wall, <see cref="GrilleGap"/> above the floor; the pivot is on the floor at the wall.</summary>
        [Model(Prop.FloorVentGrille)]
        private static void BuildFloorVentGrille(Transform root) =>
            BuildLouveredGrille(root, FloorGrilleSize, FloorGrilleSize, GrilleGap + FloorGrilleSize / 2f, 0.010f, 0.016f);

        /// <summary>Width and height of <see cref="BuildFloorVentGrille"/> (it is square).</summary>
        public const float FloorGrilleSize = 0.6f;
    }
}
