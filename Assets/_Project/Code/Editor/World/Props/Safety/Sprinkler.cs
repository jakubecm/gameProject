using System.Collections.Generic;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Quick-response pendent sprinkler, 15 mm thread, glass bulb: a chrome escutcheon (72 mm across, a rounded rim
        /// round a recess) flush with the ceiling, the hex body and the brass sealing ring under it, the 3 mm red
        /// glycerine bulb with its clear seal, the two bent chrome frame arms that carry it down to the hub, and the
        /// slotted deflector (16 tines) 63 mm below the ceiling. The pivot is on the ceiling surface, hanging down.
        /// </summary>
        [Model(Prop.Sprinkler)]
        private static void BuildSprinkler(Transform root)
        {
            const int round = 40;
            var chrome = new MeshBuilder();
            var brass = new MeshBuilder();
            var red = new MeshBuilder();
            var clear = new MeshBuilder();
            // The pivot is on the ceiling surface, but a drop ceiling's tile face is TileDrop lower: the sprinkler hangs from there,
            // and its escutcheon is deep enough to reach up to the surface.
            var origin = new Vector3(0f, -TileDrop, 0f);
            var none = Quaternion.identity;

            // Escutcheon: the inner wall of the recess (walking down faces the axis), the underside, the rounded rim.
            chrome.AddLathe(new[]
            {
                P(0.0200f, 0.0100f + TileDrop), P(0.0200f, -0.0035f), P(0.0215f, -0.0055f, true), P(0.0260f, -0.0062f), P(0.0310f, -0.0062f), P(0.0345f, -0.0052f),
                P(0.0360f, -0.0035f), P(0.0362f, -0.0015f), P(0.0350f, 0f, true), P(0.0350f, TileDrop, true),
            }, round, true, origin, none);

            // Hex body in the recess and the frame's boss; the brass ring below the body.
            chrome.AddLathe(new[]
            {
                P(0f, -0.0090f), P(0.0115f, -0.0090f, true), P(0.0115f, 0.0020f, true), P(0f, 0.0020f, true),
            }, 6, false, origin, none);
            brass.AddLathe(new[]
            {
                P(0f, -0.0130f), P(0.0100f, -0.0130f, true), P(0.0100f, -0.0090f, true), P(0.0085f, -0.0090f, true),
            }, round, true, origin, none);

            // Frame arms: flat bands bent from the body down and in to the hub, one each side.
            foreach (float side in new[] { -1f, 1f })
            {
                var path = new List<Vector3>();
                foreach (var xy in new[]
                {
                    (0.0105f, -0.0090f), (0.0113f, -0.0145f), (0.0119f, -0.0220f), (0.0118f, -0.0310f), (0.0106f, -0.0400f), (0.0080f, -0.0475f), (0.0045f, -0.0535f),
                })
                {
                    path.Add(new Vector3(side * xy.Item1, xy.Item2, 0f));
                }
                chrome.AddStrip(path, Vector3.forward, 0.0050f, 0.0018f, origin, none);
            }

            // Hub (a tapered boss under the arms) with the deflector's solid centre.
            chrome.AddLathe(new[]
            {
                P(0f, -0.0625f), P(0.0030f, -0.0625f, true), P(0.0045f, -0.0600f), P(0.0058f, -0.0560f), P(0.0068f, -0.0525f), P(0.0070f, -0.0510f, true), P(0f, -0.0510f, true),
            }, 24, true, origin, none);
            chrome.AddLathe(new[]
            {
                P(0f, -0.0634f), P(0.0090f, -0.0634f, true), P(0.0092f, -0.0630f, true), P(0.0090f, -0.0626f, true), P(0f, -0.0626f, true),
            }, 32, true, origin, none);

            // Deflector tines: 16 slightly cupped radial fingers.
            for (int i = 0; i < 16; i++)
            {
                var path = new List<Vector3>
                {
                    new Vector3(0.0070f, -0.0630f, 0f), new Vector3(0.0110f, -0.0631f, 0f), new Vector3(0.0145f, -0.0626f, 0f), new Vector3(0.0172f, -0.0617f, 0f),
                };
                chrome.AddStrip(path, Vector3.forward, 0.0036f, 0.0008f, origin, Quaternion.Euler(0f, i * 22.5f, 0f));
            }

            // Bulb: red glycerine above, the clear seal and rounded tip below.
            var liquid = new List<ProfilePoint> { P(0f, -0.0385f, true), P(0.0026f, -0.0385f, true), P(0.0026f, -0.0135f) };
            liquid.AddRange(ArcProfile(0f, -0.0135f, 0.0026f, 0.0026f, 0f, 90f, 8));
            red.AddLathe(liquid, 20, true, origin, none);
            var seal = new List<ProfilePoint>();
            seal.AddRange(ArcProfile(0f, -0.0475f, 0.0026f, 0.0026f, -90f, 0f, 8));
            seal.Add(P(0.0026f, -0.0385f, true));
            clear.AddLathe(seal, 20, true, origin, none);

            chrome.Attach("Chrome", root, SurfaceMaterial.Chrome);
            brass.Attach("Brass", root, SurfaceMaterial.Brass);
            red.Attach("BulbLiquid", root, SurfaceMaterial.BulbRed);
            clear.Attach("BulbSeal", root, SurfaceMaterial.BulbClear);
        }
    }
}
