using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>Dial thermometer on a short brass stem; the pivot is where the stem meets the pipe, the dial faces +Z.</summary>
        [Model(Prop.PipeThermometer)]
        private static void BuildPipeThermometer(Transform root)
        {
            var dial = new DialMeshes();
            dial.AddDial(new Vector3(0f, 0f, 0.07f), 0.045f, 0.07f, 0.3f);
            dial.Attach("Dial", root);
        }
    }
}
