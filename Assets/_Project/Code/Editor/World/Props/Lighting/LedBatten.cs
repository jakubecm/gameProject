using System.Collections.Generic;
using UnityEngine;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Surface-mounted LED batten 1.5 m long (along Z) for rooms without a suspended ceiling, after a tri-proof fitting: a white
        /// powder-coated tray with rounded lower edges, a curved opal diffuser along its whole length, two steel retaining clips and
        /// rounded end caps. Pivot on the ceiling surface, hanging down. A wide downward spot without shadows.
        /// </summary>
        [Model(Prop.LedBatten)]
        private static void BuildLedBatten(Transform root)
        {
            const float length = 1.5f;
            const float cap = 0.012f;
            const float halfTray = 0.060f;
            const float trayDepth = 0.032f;
            const float halfLens = 0.050f;
            const float lensDepth = 0.026f;
            float half = length / 2f;

            // The diffuser: a half ellipse hanging below the tray, convex, counter-clockwise in (x, y).
            List<Vector2> Lens(float lensHalf, float depth, float chordY)
            {
                var loop = new List<Vector2>();
                const int steps = 16;
                for (int i = 0; i <= steps; i++)
                {
                    float angle = Mathf.Lerp(180f, 360f, i / (float)steps) * Mathf.Deg2Rad;
                    loop.Add(new Vector2(Mathf.Cos(angle) * lensHalf, chordY + Mathf.Sin(angle) * depth));
                }
                return loop;
            }

            var tray = MeshBuilder.RoundedRectLoop(-halfTray, halfTray, -trayDepth, 0f, new[] { 0.007f, 0.007f, 0.0015f, 0.0015f }, 3);
            var white = new MeshBuilder();
            white.AddExtrusion(tray, new Vector3(0f, 0f, -half + cap), new Vector3(0f, 0f, half - cap), Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
            var capTray = MeshBuilder.RoundedRectLoop(-halfTray - 0.002f, halfTray + 0.002f, -trayDepth - 0.002f, 0f, new[] { 0.009f, 0.009f, 0.002f, 0.002f }, 3);
            foreach (int end in new[] { -1, 1 })
            {
                white.AddExtrusion(capTray, new Vector3(0f, 0f, end * (half - cap)), new Vector3(0f, 0f, end * half), Vector3.right, Vector3.up,
                    end < 0 ? Vector3.forward : Vector3.back, end < 0 ? Vector3.back : Vector3.forward);
            }
            white.Attach("Tray", root, SurfaceMaterial.DetectorWhite);

            var lens = new MeshBuilder();
            lens.AddExtrusion(Lens(halfLens, lensDepth, -trayDepth + 0.002f), new Vector3(0f, 0f, -half + cap), new Vector3(0f, 0f, half - cap),
                Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
            lens.Attach("Diffuser", root, SurfaceMaterial.LightPanel);

            // End caps over the diffuser, a little larger than it, in the tray's white.
            var caps = new MeshBuilder();
            foreach (int end in new[] { -1, 1 })
            {
                caps.AddExtrusion(Lens(halfLens + 0.003f, lensDepth + 0.003f, -trayDepth + 0.002f), new Vector3(0f, 0f, end * (half - cap)), new Vector3(0f, 0f, end * half),
                    Vector3.right, Vector3.up, end < 0 ? Vector3.forward : Vector3.back, end < 0 ? Vector3.back : Vector3.forward);
            }
            caps.Attach("EndCaps", root, SurfaceMaterial.DetectorWhite);

            // Two spring clips holding the diffuser: thin steel bands over its curve.
            var clips = new MeshBuilder();
            foreach (float z in new[] { -0.55f, 0.55f })
            {
                clips.AddExtrusion(Lens(halfLens + 0.0015f, lensDepth + 0.0015f, -trayDepth + 0.002f), new Vector3(0f, 0f, z - 0.005f), new Vector3(0f, 0f, z + 0.005f),
                    Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
            }
            clips.Attach("Clips", root, SurfaceMaterial.ValveSteel);

            AddSpotLight(root, new Vector3(0f, -trayDepth - lensDepth - 0.01f, 0f), 150f, 100f, 7f, 5.5f, LedPanelColor);
        }
    }
}
