using UnityEngine;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// The one light of a passenger-lift car, built into its ceiling like the lay-in panels of a suspended ceiling, but in the size a lift
        /// takes: a 300 x 300 mm LED panel (opal diffuser set 12 mm up in the ceiling void) in a slim satin stainless bezel 30 mm wide that
        /// stands 4 mm proud of the ceiling and has a 14 mm deep reveal. One realtime spot light down, no shadows. Pivot: the middle of the
        /// panel on the ceiling surface, hanging down; it fills the opening of <see cref="Prop.LiftCeiling"/>.
        /// </summary>
        [Model(Prop.LiftCeilingLight)]
        private static void BuildLiftCeilingLight(Transform root)
        {
            const float half = 0.15f;
            const float bezel = 0.03f;
            const float proud = -0.004f;
            const float reveal = 0.014f;
            const float diffuserY = 0.012f;
            var frame = new MeshBuilder();
            var diffuser = new MeshBuilder();

            frame.AddBox(new Vector3(-half - bezel, proud, -half - bezel), new Vector3(-half, reveal, half + bezel), false);
            frame.AddBox(new Vector3(half, proud, -half - bezel), new Vector3(half + bezel, reveal, half + bezel), false);
            frame.AddBox(new Vector3(-half, proud, -half - bezel), new Vector3(half, reveal, -half), false);
            frame.AddBox(new Vector3(-half, proud, half), new Vector3(half, reveal, half + bezel), false);
            diffuser.AddBox(new Vector3(-half, diffuserY - 0.001f, -half), new Vector3(half, diffuserY, half), false);

            frame.Attach("Frame", root, SurfaceMaterial.StainlessSteel);
            diffuser.Attach("Diffuser", root, SurfaceMaterial.LightPanel);
            AddSpotLight(root, new Vector3(0f, -0.05f, 0f), 160f, 120f, 3.5f, 2.2f, LedPanelColor);
        }
    }
}
