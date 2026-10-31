using System;
using System.Collections.Generic;
using System.Linq;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    /// <summary>Sizes of one texture tile on a prop; Surfaces draws the textures to match.</summary>
    public static partial class Props
    {
        /// <summary>Size of one tile of the diamond mesh texture (<c>T_PcVent</c>) in metres, for the planar UVs.</summary>
        private const float PcVentTile = 0.018f;

        /// <summary>Size of one tile of the door's hex grille (the lattice pitch) and of the honeycomb vents, in metres.</summary>
        private const float RackHexTile = 0.016f;

        private const float HoneycombTile = 0.0045f;

        /// <summary>One cell of the 3 U chassis' drive-bay cover texture (<c>T_ChassisVent</c>), square, in metres.</summary>
        private const float ChassisVentTile = 0.011f;
    }
}
