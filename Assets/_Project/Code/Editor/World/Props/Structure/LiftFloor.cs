using UnityEngine;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Floor of a passenger-lift car, made like its ceiling (<see cref="BuildLiftCeiling"/>): a flat quad with the texture <c>T_LiftFloor</c>,
        /// which is one rubber tile of 400 mm square with a dark joint round it. The grid is centred on the floor, so the tiles are cut
        /// evenly at both ends. Pivot: the middle of the floor surface, facing up.
        /// </summary>
        [Model(Prop.LiftFloor)]
        private static void BuildLiftFloor(Transform root)
        {
            const float tile = 0.4f;
            float halfX = LiftCeilingWidth / 2f;
            float halfZ = LiftCeilingDepth / 2f;
            var tiles = new MeshBuilder();
            float Uv(float metres) => metres / tile + 0.5f;
            tiles.AddPanel(new Vector3(-halfX, 0f, halfZ), new Vector3(halfX, 0f, halfZ), new Vector3(halfX, 0f, -halfZ), new Vector3(-halfX, 0f, -halfZ), Vector3.up,
                new Vector2(Uv(-halfX), Uv(halfZ)), new Vector2(Uv(halfX), Uv(halfZ)), new Vector2(Uv(halfX), Uv(-halfZ)), new Vector2(Uv(-halfX), Uv(-halfZ)));
            tiles.Attach("Tiles", root, SurfaceMaterial.LiftFloor);
        }
    }
}
