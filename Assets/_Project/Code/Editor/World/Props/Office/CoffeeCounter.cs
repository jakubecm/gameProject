using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Kitchenette 1.8 m long against a wall, after a product photo of a modern kitchen: white high-gloss base units (three
        /// modules of 0.6 m: three drawers, two drawers, and the sink unit with a drawer front over a door), long thin steel bar
        /// handles, a recessed plinth, a light stone worktop, a dark double-bowl sink with a matt dark gooseneck tap, and wall
        /// cabinets above. The front faces +Z; coffee machine and mugs stand on the worktop (<see cref="CounterHeight"/>) left of the sink.
        /// </summary>
        [Model(Prop.CoffeeCounter)]
        private static void BuildCoffeeCounter(Transform root)
        {
            const float width = 1.8f;
            const float depth = 0.6f;
            const float top = 0.04f;
            const float plinth = 0.1f;
            const float panel = 0.018f;
            float worktop = CounterHeight - top;
            float front = depth / 2f - 0.02f;
            // The sink: a 0.52 x 0.34 m opening in the worktop in the right-hand module, centered at x = 0.6.
            const float sinkX0 = 0.34f;
            const float sinkX1 = 0.86f;
            const float sinkZ0 = -0.15f;
            const float sinkZ1 = 0.19f;

            // Recessed plinth and the carcasses: solid for the two drawer modules, hollow under the sink (the basin hangs in it).
            ModelUtility.CreateStandingBox("Plinth", root, new Vector3(0f, 0f, -0.02f), new Vector3(width, plinth, depth - 0.06f), SurfaceMaterial.Upholstery, false);
            ModelUtility.CreateBoxBetween("Carcass_Drawers", root, new Vector3(-width / 2f, plinth, -depth / 2f), new Vector3(0.3f, worktop, front), SurfaceMaterial.Furniture);
            ModelUtility.CreateBoxBetween("Carcass_Bottom", root, new Vector3(0.3f, plinth, -depth / 2f), new Vector3(width / 2f, plinth + panel, front), SurfaceMaterial.Furniture);
            ModelUtility.CreateBoxBetween("Carcass_Back", root, new Vector3(0.3f, plinth, -depth / 2f), new Vector3(width / 2f, worktop, -depth / 2f + panel), SurfaceMaterial.Furniture);
            ModelUtility.CreateBoxBetween("Carcass_Right", root, new Vector3(width / 2f - panel, plinth, -depth / 2f), new Vector3(width / 2f, worktop, front), SurfaceMaterial.Furniture);
            ModelUtility.CreateBoxBetween("Carcass_Divider", root, new Vector3(0.3f, plinth, -depth / 2f), new Vector3(0.3f + panel, worktop, front), SurfaceMaterial.Furniture);

            // Gloss white fronts with a 3 mm joint between them and a long thin steel bar handle near the top edge of each.
            var fronts = new MeshBuilder();
            var handles = new MeshBuilder();
            float[][] modules = { new[] { -0.9f, -0.3f, 1f, 1f, 1f }, new[] { -0.3f, 0.3f, 1f, 1f }, new[] { 0.3f, 0.9f, 0.25f, 0.75f } };
            foreach (var module in modules)
            {
                float x0 = module[0] + 0.003f;
                float x1 = module[1] - 0.003f;
                float total = 0f;
                for (int k = 2; k < module.Length; k++) total += module[k];
                float y = worktop;
                for (int k = 2; k < module.Length; k++)
                {
                    float h = (worktop - plinth) * module[k] / total;
                    float yTop = y - 0.0015f;
                    float yBottom = y - h + 0.0015f;
                    fronts.AddBox(new Vector3(x0, yBottom, front), new Vector3(x1, yTop, front + 0.018f));
                    // Handle: a 0.36 m bar, 12 mm tall, held 22 mm off the front by two small posts.
                    float cx = (x0 + x1) / 2f;
                    float hy = yTop - 0.04f;
                    handles.AddBox(new Vector3(cx - 0.18f, hy - 0.006f, front + 0.04f), new Vector3(cx + 0.18f, hy + 0.006f, front + 0.052f));
                    foreach (float post in new[] { -0.17f, 0.17f })
                    {
                        handles.AddBox(new Vector3(cx + post - 0.005f, hy - 0.004f, front + 0.018f), new Vector3(cx + post + 0.005f, hy + 0.004f, front + 0.04f));
                    }
                    y -= h;
                }
            }
            fronts.Attach("Fronts", root, SurfaceMaterial.DeskWhite);
            handles.Attach("Handles", root, SurfaceMaterial.Metal);

            // Light stone worktop in four pieces around the sink opening.
            ModelUtility.CreateBoxBetween("Worktop_Left", root, new Vector3(-width / 2f, worktop, -depth / 2f), new Vector3(sinkX0, CounterHeight, depth / 2f), SurfaceMaterial.StoneTop);
            ModelUtility.CreateBoxBetween("Worktop_Right", root, new Vector3(sinkX1, worktop, -depth / 2f), new Vector3(width / 2f, CounterHeight, depth / 2f), SurfaceMaterial.StoneTop);
            ModelUtility.CreateBoxBetween("Worktop_Back", root, new Vector3(sinkX0, worktop, -depth / 2f), new Vector3(sinkX1, CounterHeight, sinkZ0), SurfaceMaterial.StoneTop);
            ModelUtility.CreateBoxBetween("Worktop_Front", root, new Vector3(sinkX0, worktop, sinkZ1), new Vector3(sinkX1, CounterHeight, depth / 2f), SurfaceMaterial.StoneTop);

            // Double-bowl under-mount sink in dark matt steel: a wide bowl (0.31 m, 0.2 m deep) and a narrower one (0.21 m, half as deep) with a low divider. A mug stands in the wide bowl.
            const float wall = 0.008f;
            const float splitX = 0.65f;
            var basin = new MeshBuilder();
            var drains = new MeshBuilder();
            foreach (var (x0, x1, bottom) in new[] { (sinkX0, splitX, CounterHeight - 0.2f), (splitX, sinkX1, CounterHeight - 0.1f) })
            {
                basin.AddBox(new Vector3(x0, bottom, sinkZ0), new Vector3(x1, bottom + wall, sinkZ1));
                basin.AddBox(new Vector3(x0, bottom, sinkZ0), new Vector3(x1, CounterHeight, sinkZ0 + wall), true, false);
                basin.AddBox(new Vector3(x0, bottom, sinkZ1 - wall), new Vector3(x1, CounterHeight, sinkZ1), true, false);
                drains.AddLathe(new[] { P(0f, 0f, true), P(0.028f, 0f, true), P(0.028f, 0.003f, true), P(0f, 0.003f, true) }, 24, true,
                    new Vector3((x0 + x1) / 2f, bottom + wall, (sinkZ0 + sinkZ1) / 2f), Quaternion.identity);
            }
            basin.AddBox(new Vector3(sinkX0, CounterHeight - 0.2f, sinkZ0), new Vector3(sinkX0 + wall, CounterHeight, sinkZ1), true, false);
            basin.AddBox(new Vector3(sinkX1 - wall, CounterHeight - 0.1f, sinkZ0), new Vector3(sinkX1, CounterHeight, sinkZ1), true, false);
            basin.AddBox(new Vector3(splitX - wall / 2f, CounterHeight - 0.2f, sinkZ0), new Vector3(splitX + wall / 2f, CounterHeight - 0.03f, sinkZ1), true, false);
            basin.Attach("Basin", root, SurfaceMaterial.StainlessSteel);
            BuildMug(ModelUtility.CreateGroup("Mug_Sink", root), new Vector3(sinkX0 + 0.075f, CounterHeight - 0.2f + wall, 0.04f));
            drains.Attach("Drains", root, SurfaceMaterial.BlackHousing);

            // Gooseneck tap in the same matt dark steel, on the back strip: a round foot, a tall column bent over in a half circle and down
            // into the spout with its pull-out head, and a single side lever.
            float tapX = (sinkX0 + splitX) / 2f; // centred on the wide bowl
            float tapZ = (-depth / 2f + sinkZ0) / 2f;
            const float archRadius = 0.09f;
            float columnTop = CounterHeight + 0.30f;
            var tap = new MeshBuilder();
            tap.AddLathe(new[] { P(0f, 0f, true), P(0.03f, 0f, true), P(0.03f, 0.012f, true), P(0.022f, 0.014f, true), P(0f, 0.014f, true) }, 32, true, new Vector3(tapX, CounterHeight, tapZ), Quaternion.identity);
            var neck = new List<Vector3> { new Vector3(tapX, CounterHeight + 0.012f, tapZ), new Vector3(tapX, columnTop - 0.05f, tapZ), new Vector3(tapX, columnTop, tapZ) };
            for (int step = 1; step <= 12; step++)
            {
                float angle = (180f - 180f * step / 12f) * Mathf.Deg2Rad;
                neck.Add(new Vector3(tapX, columnTop + Mathf.Sin(angle) * archRadius, tapZ + archRadius + Mathf.Cos(angle) * archRadius));
            }
            neck.Add(new Vector3(tapX, columnTop - 0.06f, tapZ + 2f * archRadius));
            neck.Add(new Vector3(tapX, columnTop - 0.10f, tapZ + 2f * archRadius));
            tap.AddTube(neck, 0.0135f, 16, Vector3.zero, Quaternion.identity);
            // Pull-out head at the end of the spout.
            float headTop = columnTop - 0.10f;
            tap.AddTube(new List<Vector3> { new Vector3(tapX, headTop, tapZ + 2f * archRadius), new Vector3(tapX, headTop - 0.10f, tapZ + 2f * archRadius) }, 0.0185f, 16, Vector3.zero, Quaternion.identity);
            tap.AddLathe(new[] { P(0f, 0f, true), P(0.0185f, 0f, true) }, 16, true, new Vector3(tapX, headTop - 0.10f, tapZ + 2f * archRadius), Quaternion.identity);
            // Lever: a short horizontal stub out of the column and a thin rod up from its end.
            tap.AddLathe(new[] { P(0f, 0f, true), P(0.011f, 0f, true), P(0.011f, 0.05f, true), P(0f, 0.05f, true) }, 20, true, new Vector3(tapX, CounterHeight + 0.09f, tapZ), Quaternion.Euler(0f, 0f, -90f));
            tap.AddTube(new List<Vector3> { new Vector3(tapX + 0.04f, CounterHeight + 0.09f, tapZ), new Vector3(tapX + 0.04f, CounterHeight + 0.17f, tapZ) }, 0.004f, 8, Vector3.zero, Quaternion.identity);
            tap.Attach("Tap", root, SurfaceMaterial.StainlessSteel);

            // Wall cabinets: a carcass with three gloss doors and a bar handle on each.
            var wallDoors = new MeshBuilder();
            var wallHandles = new MeshBuilder();
            ModelUtility.CreateStandingBox("WallCabinets", root, new Vector3(0f, 1.5f, -depth / 2f + 0.17f), new Vector3(width, 0.7f, 0.34f), SurfaceMaterial.Furniture, false);
            for (int i = 0; i < 3; i++)
            {
                float x0 = -width / 2f + i * width / 3f + 0.003f;
                float x1 = -width / 2f + (i + 1) * width / 3f - 0.003f;
                wallDoors.AddBox(new Vector3(x0, 1.5015f, -depth / 2f + 0.34f), new Vector3(x1, 2.1985f, -depth / 2f + 0.358f));
                float cx = (x0 + x1) / 2f;
                wallHandles.AddBox(new Vector3(cx - 0.12f, 1.55f, -depth / 2f + 0.38f), new Vector3(cx + 0.12f, 1.562f, -depth / 2f + 0.392f));
                foreach (float post in new[] { -0.11f, 0.11f })
                {
                    wallHandles.AddBox(new Vector3(cx + post - 0.005f, 1.552f, -depth / 2f + 0.358f), new Vector3(cx + post + 0.005f, 1.56f, -depth / 2f + 0.38f));
                }
            }
            wallDoors.Attach("WallFronts", root, SurfaceMaterial.DeskWhite);
            wallHandles.Attach("WallHandles", root, SurfaceMaterial.Metal);
            AddBoxCollider(root, new Vector3(0f, CounterHeight / 2f, 0f), new Vector3(width, CounterHeight, depth));
        }
    }
}
