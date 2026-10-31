using UnityEngine;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>Width and depth of a passenger-lift car's ceiling (the inside of the car).</summary>
        public const float LiftCeilingWidth = 1.6f;
        public const float LiftCeilingDepth = 1.5f;

        /// <summary>Half the side of the square opening in the ceiling for the recessed light (<see cref="Prop.LiftCeilingLight"/>).</summary>
        public const float LiftCeilingHoleHalf = 0.16f;

        /// <summary>
        /// Suspended ceiling of a passenger-lift car: matt brushed stainless panels 400 mm square with fine dark joints in the grey of the
        /// car's walls (the texture <c>T_LiftCeiling</c> is one panel; the grid is centred so the light's opening is in the middle of a panel),
        /// a square opening in the middle for the recessed light, a narrow steel edge trim round the ceiling. Pivot: the middle of the ceiling
        /// surface, hanging down.
        /// </summary>
        [Model(Prop.LiftCeiling)]
        private static void BuildLiftCeiling(Transform root)
        {
            const float panel = 0.4f;
            const float trim = 0.025f;
            float halfX = LiftCeilingWidth / 2f;
            float halfZ = LiftCeilingDepth / 2f;
            float hole = LiftCeilingHoleHalf;
            var panels = new MeshBuilder();
            float Uv(float metres) => metres / panel + 0.5f;
            void Quad(float x0, float z0, float x1, float z1) =>
                panels.AddPanel(new Vector3(x0, 0f, z0), new Vector3(x1, 0f, z0), new Vector3(x1, 0f, z1), new Vector3(x0, 0f, z1), Vector3.down,
                    new Vector2(Uv(x0), Uv(z0)), new Vector2(Uv(x1), Uv(z0)), new Vector2(Uv(x1), Uv(z1)), new Vector2(Uv(x0), Uv(z1)));
            Quad(-halfX, -halfZ, halfX, -hole);
            Quad(-halfX, hole, halfX, halfZ);
            Quad(-halfX, -hole, -hole, hole);
            Quad(hole, -hole, halfX, hole);
            panels.Attach("Panels", root, SurfaceMaterial.LiftCeiling);

            var edge = new MeshBuilder();
            const float drop = -0.006f;
            edge.AddBox(new Vector3(-halfX, drop, -halfZ), new Vector3(-halfX + trim, 0f, halfZ), false);
            edge.AddBox(new Vector3(halfX - trim, drop, -halfZ), new Vector3(halfX, 0f, halfZ), false);
            edge.AddBox(new Vector3(-halfX + trim, drop, -halfZ), new Vector3(halfX - trim, 0f, -halfZ + trim), false);
            edge.AddBox(new Vector3(-halfX + trim, drop, halfZ - trim), new Vector3(halfX - trim, 0f, halfZ), false);
            edge.Attach("EdgeTrim", root, SurfaceMaterial.StainlessSteel);
        }
    }
}
