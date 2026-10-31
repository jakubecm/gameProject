using System.Collections.Generic;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Lay-in LED panel for the 600 mm ceiling grid: 0.6 m wide, <paramref name="length"/> long (along Z), a white frame
        /// 18 mm wide that covers the T-bars under it, the opal diffuser set back 1 mm into it. The light is a wide downward
        /// spot with no shadows.
        /// </summary>
        private static void BuildLedPanel(Transform root, float length, float intensity)
        {
            const float width = 0.6f;
            const float rim = 0.018f;
            const float top = -0.0118f;
            const float bottom = -0.0158f;
            float halfX = width / 2f;
            float halfZ = length / 2f;
            var frame = new MeshBuilder();
            var diffuser = new MeshBuilder();

            // Four frame bars: the long sides along Z, the short ones between them.
            frame.AddBox(new Vector3(-halfX, bottom, -halfZ), new Vector3(-halfX + rim, top, halfZ), false);
            frame.AddBox(new Vector3(halfX - rim, bottom, -halfZ), new Vector3(halfX, top, halfZ), false);
            frame.AddBox(new Vector3(-halfX + rim, bottom, -halfZ), new Vector3(halfX - rim, top, -halfZ + rim), false);
            frame.AddBox(new Vector3(-halfX + rim, bottom, halfZ - rim), new Vector3(halfX - rim, top, halfZ), false);
            diffuser.AddBox(new Vector3(-halfX + rim, bottom + 0.0008f, -halfZ + rim), new Vector3(halfX - rim, top, halfZ - rim), false);

            frame.Attach("Frame", root, SurfaceMaterial.DetectorWhite);
            diffuser.Attach("Diffuser", root, SurfaceMaterial.LightPanel);
            AddSpotLight(root, new Vector3(0f, bottom - 0.03f, 0f), 150f, 100f, 6.5f, intensity, LedPanelColor);
        }

        [Model(Prop.LedPanel)]
        private static void BuildLedPanel(Transform root) => BuildLedPanel(root, 0.6f, 3.0f);
    }
}
