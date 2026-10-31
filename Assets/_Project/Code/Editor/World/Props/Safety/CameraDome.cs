using System.Collections.Generic;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>Ceiling dome camera: hangs from the ceiling, the feed looking along +Z and 30 degrees down.</summary>
        [Model(Prop.CameraDome)]
        private static void BuildCameraDome(Transform root)
        {
            BuildDomeCameraModel(root);
            AddSecurityCamera(root, new Vector3(0f, -0.06f, 0f), 30f);
        }
    }
}
