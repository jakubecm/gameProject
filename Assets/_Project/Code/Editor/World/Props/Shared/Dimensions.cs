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
    /// <summary>Prop dimensions the rooms place things by (mounting heights, worktop and shelf heights, box sizes).</summary>
    public static partial class Props
    {
        /// <summary>Height of the manual call points and the gas release station.</summary>
        public const float CallPointHeight = 1.4f;

        /// <summary>
        /// Gap between a wall grille and the floor or the ceiling: a low grille starts this high above the floor, a high
        /// grille ends this far below the ceiling (the suspended ceiling, or the girders in the hall), the same in every
        /// room, corridor and hall.
        /// </summary>
        public const float GrilleGap = 0.15f;

        /// <summary>Center height of a small high grille under a ceiling at <paramref name="ceiling"/>.</summary>
        public static float HighGrilleCenter(float ceiling) => ceiling - GrilleGap - WallGrilleHeight / 2f;

        /// <summary>Height of the middle of a light switch (1.05 m, the usual height in Europe, beside the door).</summary>
        public const float LightSwitchHeight = 1.05f;

        /// <summary>Clear opening of the freight lift's landing door (a 2000 kg goods lift).</summary>
        public const float FreightDoorWidth = 2.0f;

        /// <summary>Distance from the roof deck down to the top of a hanging sign (4.2 m above the floor).</summary>
        public const float HallSignDrop = 1.8f;

        /// <summary>Height of the worktop of the coffee counter.</summary>
        public const float CounterHeight = 0.9f;

        /// <summary>Where the coffee machine stands on the worktop (counter-local): centred on the left-hand base unit (x = -0.6) and on the worktop's depth.</summary>
        public static readonly Vector3 CounterMachinePosition = new Vector3(-0.6f, CounterHeight, -0.04f);

        /// <summary>Height of the top of each shelf board of <see cref="BuildShelf"/>, where boxes and crates stand.</summary>
        public static readonly float[] ShelfBoardTops = { 0.10f, 0.62f, 1.14f, 1.66f };

        // Cardboard boxes after two product photos: a lidded box (the lid overlaps the base) and a shipping carton (flaps meeting in a
        // seam, handle holes in both end faces). Each in two sizes, a cube and a lower square. Pivot on the floor in the middle, the
        // handle holes face +Z and -Z.
        public const float LidBoxSize = 0.28f;

        public const float LidCubeHeight = 0.28f;

        public const float LidFlatHeight = 0.14f;

        public const float CartonBoxSize = 0.30f;

        public const float CartonCubeHeight = 0.30f;

        public const float CartonFlatHeight = 0.17f;
    }
}
