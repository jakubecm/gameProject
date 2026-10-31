using System.Collections.Generic;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Recessed ceiling loudspeaker (public address and voice alarm), 216 mm across: a flat white trim ring with a
        /// rounded rim, flush with the tile, and the metal grille in front of it: a slightly domed disc 185 mm across
        /// that covers the ring's inner edge and stands 3 mm proud of it. It sits in the middle of a tile.
        /// </summary>
        [Model(Prop.CeilingSpeaker)]
        private static void BuildCeilingSpeaker(Transform root)
        {
            var trim = new MeshBuilder();
            var grille = new MeshBuilder();
            var origin = Vector3.zero;
            var none = Quaternion.identity;

            trim.AddLathe(new[]
            {
                P(0.0800f, -0.0145f, true), P(0.0990f, -0.0148f), P(0.1040f, -0.0144f), P(0.1068f, -0.0132f), P(0.1078f, -0.0122f), P(0.1074f, -0.0116f, true),
            }, 64, true, origin, none);
            grille.AddLathe(new[]
            {
                P(0f, -0.0185f), P(0.0400f, -0.0181f), P(0.0800f, -0.0170f), P(0.0905f, -0.0162f), P(0.0925f, -0.0153f, true), P(0.0925f, -0.0146f, true), P(0f, -0.0146f, true),
            }, 64, true, origin, none);

            trim.Attach("Trim", root, SurfaceMaterial.SpeakerWhite);
            grille.Attach("Grille", root, SurfaceMaterial.SpeakerGrille);
        }
    }
}
