using System;
using UnityEngine;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Draws the placeholder photos of the in-game news site (16:9 scenes: power lines, a rack wall, a courthouse, a football match, a comet, ...) from flat
    /// shapes and glows, so the site has pictures before real photos exist. A photo with the same key in
    /// Assets/ThirdParty/WebImages replaces a placeholder, see <see cref="DesktopArtTool"/>.
    /// </summary>
    public static class WebImagePainter
    {
        public const int Width = 960;
        public const int Height = 540;

        /// <summary>The keys of the placeholder pictures; an article names one in its <c>image</c> field.</summary>
        public static readonly string[] Keys = { "grid", "datacenter", "ghost", "law", "talks", "heat", "reservoir", "chips", "ports", "city",
            "football", "marathon", "tennis", "scoreboard", "concert", "lighthouse", "bakery", "comet", "deepsea" };

        public static Texture2D Paint(string key)
        {
            var canvas = new Raster(Width, Height);
            switch (key)
            {
                case "grid": PowerLines(canvas); break;
                case "datacenter": RackWall(canvas, lit: 0.85f, dark: false); break;
                case "ghost": RackWall(canvas, lit: 0.04f, dark: true); break;
                case "law": Courthouse(canvas); break;
                case "talks": RoundTable(canvas); break;
                case "heat": HeatWave(canvas); break;
                case "reservoir": DryReservoir(canvas); break;
                case "chips": Chip(canvas); break;
                case "ports": Harbour(canvas); break;
                case "city": NightCity(canvas); break;
                case "football": FootballMatch(canvas); break;
                case "marathon": Marathon(canvas); break;
                case "tennis": TennisCourt(canvas); break;
                case "scoreboard": Scoreboard(canvas); break;
                case "concert": Concert(canvas); break;
                case "lighthouse": Lighthouse(canvas); break;
                case "bakery": Bakery(canvas); break;
                case "comet": Comet(canvas); break;
                case "deepsea": DeepSea(canvas); break;
                default: canvas.Gradient(new Color(0.3f, 0.3f, 0.35f), new Color(0.5f, 0.5f, 0.55f)); break;
            }
            canvas.Vignette(0.45f);
            return canvas.ToTexture();
        }

        private static Color C(float r, float g, float b) => new Color(r, g, b);

        // Same inputs, same number between 0 and 1.
        private static float Hash(int a, int b)
        {
            unchecked
            {
                uint h = (uint)(a * 374761393 + b * 668265263 + 12345);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }

        // ------------------------------------------------------------------ scenes

        // Three pylons with sagging wires against a dusk sky.
        private static void PowerLines(Raster c)
        {
            c.Gradient(C(0.16f, 0.13f, 0.32f), C(0.96f, 0.56f, 0.30f));
            c.Glow(new Vector2(700f, 170f), 460f, C(1f, 0.70f, 0.35f), 0.55f);
            c.Disc(new Vector2(700f, 170f), 48f, C(1f, 0.88f, 0.62f));
            c.Poly(new[] { new Vector2(0f, 0f), new Vector2(0f, 150f), new Vector2(210f, 178f), new Vector2(430f, 140f), new Vector2(650f, 168f), new Vector2(960f, 132f), new Vector2(960f, 0f) }, C(0.09f, 0.07f, 0.15f));

            var dark = C(0.06f, 0.05f, 0.10f);
            var towers = new[] { (x: 170f, y: 90f, h: 380f), (x: 520f, y: 112f, h: 300f), (x: 820f, y: 100f, h: 240f) };
            var arms = new Vector2[towers.Length][];
            for (int i = 0; i < towers.Length; i++) arms[i] = Pylon(c, towers[i].x, towers[i].y, towers[i].h, dark);

            for (int arm = 0; arm < 4; arm++)
            {
                for (int i = 0; i + 1 < towers.Length; i++) Wire(c, arms[i][arm], arms[i + 1][arm], 26f, dark);
            }
            // The first pylon's wires continue off the left edge.
            for (int arm = 0; arm < 4; arm++) Wire(c, new Vector2(-40f, arms[0][arm].y - 30f), arms[0][arm], 18f, dark);
        }

        // A lattice pylon; returns the four wire attachment points (upper left/right, lower left/right).
        private static Vector2[] Pylon(Raster c, float x, float baseY, float height, Color color)
        {
            float width = height * 0.26f;
            float t = Mathf.Max(3f, height * 0.013f);
            float shoulder = baseY + height * 0.74f;
            c.Line(new Vector2(x - width, baseY), new Vector2(x - width * 0.18f, shoulder), t, color);
            c.Line(new Vector2(x + width, baseY), new Vector2(x + width * 0.18f, shoulder), t, color);
            c.Line(new Vector2(x - width * 0.18f, shoulder), new Vector2(x, baseY + height), t, color);
            c.Line(new Vector2(x + width * 0.18f, shoulder), new Vector2(x, baseY + height), t, color);

            for (int i = 0; i < 4; i++)
            {
                float f0 = i / 4f, f1 = (i + 1) / 4f;
                float ya = Mathf.Lerp(baseY, shoulder, f0), yb = Mathf.Lerp(baseY, shoulder, f1);
                float wa = Mathf.Lerp(width, width * 0.18f, f0), wb = Mathf.Lerp(width, width * 0.18f, f1);
                c.Line(new Vector2(x - wa, ya), new Vector2(x + wb, yb), t * 0.6f, color);
                c.Line(new Vector2(x + wa, ya), new Vector2(x - wb, yb), t * 0.6f, color);
                c.Line(new Vector2(x - wb, yb), new Vector2(x + wb, yb), t * 0.6f, color);
            }

            float upper = baseY + height * 0.92f;
            float reachUpper = width * 1.1f, reachLower = width * 1.5f;
            c.Line(new Vector2(x - reachUpper, upper), new Vector2(x + reachUpper, upper), t, color);
            c.Line(new Vector2(x - reachLower, shoulder), new Vector2(x + reachLower, shoulder), t, color);
            return new[]
            {
                new Vector2(x - reachUpper, upper - t * 2f), new Vector2(x + reachUpper, upper - t * 2f),
                new Vector2(x - reachLower, shoulder - t * 2f), new Vector2(x + reachLower, shoulder - t * 2f),
            };
        }

        // A wire that sags in the middle.
        private static void Wire(Raster c, Vector2 from, Vector2 to, float sag, Color color)
        {
            const int steps = 24;
            var previous = from;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                var point = Vector2.Lerp(from, to, t) + Vector2.down * (sag * 4f * t * (1f - t));
                c.Line(previous, point, 2f, color);
                previous = point;
            }
        }

        // A wall of server racks with rows of status lights. Dark: an abandoned hall with one red light.
        private static void RackWall(Raster c, float lit, bool dark)
        {
            c.Gradient(dark ? C(0.02f, 0.03f, 0.05f) : C(0.05f, 0.08f, 0.14f), dark ? C(0.05f, 0.07f, 0.10f) : C(0.11f, 0.17f, 0.28f));
            c.Box(new Vector2(0f, 0f), new Vector2(Width, 66f), dark ? C(0.03f, 0.04f, 0.06f) : C(0.06f, 0.09f, 0.14f));

            const int racks = 7;
            const float rackWidth = 120f, gap = 10f, floor = 66f, top = 496f;
            float startX = (Width - (racks * rackWidth + (racks - 1) * gap)) / 2f;
            for (int r = 0; r < racks; r++)
            {
                float x0 = startX + r * (rackWidth + gap);
                c.Box(new Vector2(x0, floor), new Vector2(x0 + rackWidth, top), dark ? C(0.07f, 0.09f, 0.12f) : C(0.12f, 0.15f, 0.21f), 4f);
                for (int unit = 0; unit < 12; unit++)
                {
                    float y0 = floor + 14f + unit * 34f;
                    c.Box(new Vector2(x0 + 8f, y0), new Vector2(x0 + rackWidth - 8f, y0 + 28f), dark ? C(0.09f, 0.11f, 0.15f) : C(0.17f, 0.21f, 0.29f), 2f);
                    for (int slit = 0; slit < 4; slit++)
                    {
                        c.Line(new Vector2(x0 + 52f + slit * 14f, y0 + 8f), new Vector2(x0 + 52f + slit * 14f, y0 + 20f), 3f, dark ? C(0.04f, 0.05f, 0.07f) : C(0.09f, 0.11f, 0.16f));
                    }
                    for (int led = 0; led < 3; led++)
                    {
                        if (Hash(r * 31 + unit, led + 7) > lit) continue;
                        float pick = Hash(r + 100, unit * 3 + led);
                        var color = pick < 0.72f ? C(0.20f, 0.90f, 0.42f) : pick < 0.88f ? C(1f, 0.70f, 0.15f) : C(0.30f, 0.62f, 1f);
                        var spot = new Vector2(x0 + 18f + led * 11f, y0 + 14f);
                        c.Glow(spot, 16f, color, dark ? 0.25f : 0.35f);
                        c.Disc(spot, 3.2f, color);
                    }
                }
            }

            if (dark)
            {
                var red = new Vector2(startX + 3 * (rackWidth + gap) + 51f, floor + 14f + 6 * 34f + 14f);
                c.Glow(red, 150f, C(1f, 0.10f, 0.08f), 0.55f);
                c.Disc(red, 5f, C(1f, 0.28f, 0.22f));
                c.Glow(new Vector2(Width / 2f, 30f), 520f, C(0.10f, 0.14f, 0.20f), 0.35f);
            }
            else
            {
                c.Glow(new Vector2(Width / 2f, Height), 560f, C(0.30f, 0.60f, 1f), 0.20f);
                c.Box(new Vector2(0f, 502f), new Vector2(Width, 520f), C(0.40f, 0.32f, 0.10f));
            }
        }

        // A building with a pediment and columns.
        private static void Courthouse(Raster c)
        {
            c.Gradient(C(0.50f, 0.62f, 0.78f), C(0.92f, 0.90f, 0.84f));
            c.Glow(new Vector2(150f, 470f), 420f, C(1f, 0.95f, 0.80f), 0.4f);
            foreach (var cloud in new[] { (new Vector2(260f, 400f), 120f), (new Vector2(700f, 460f), 150f), (new Vector2(860f, 380f), 90f) })
            {
                c.Ellipse(cloud.Item1, new Vector2(cloud.Item2, cloud.Item2 * 0.22f), Color.white, 0.55f);
                c.Ellipse(cloud.Item1 + new Vector2(cloud.Item2 * 0.3f, 16f), new Vector2(cloud.Item2 * 0.6f, cloud.Item2 * 0.2f), Color.white, 0.55f);
            }

            c.Box(new Vector2(0f, 0f), new Vector2(Width, 46f), C(0.42f, 0.43f, 0.46f));
            var stone = C(0.80f, 0.78f, 0.73f);
            c.Box(new Vector2(140f, 40f), new Vector2(820f, 62f), stone * 0.88f);
            c.Box(new Vector2(170f, 62f), new Vector2(790f, 84f), stone * 0.94f);
            c.Box(new Vector2(200f, 84f), new Vector2(760f, 106f), stone);
            for (int i = 0; i < 6; i++)
            {
                float x = 250f + i * 92f;
                c.Box(new Vector2(x - 24f, 106f), new Vector2(x + 24f, 336f), C(0.91f, 0.89f, 0.85f), 3f);
                c.Box(new Vector2(x + 8f, 106f), new Vector2(x + 24f, 336f), C(0.74f, 0.72f, 0.68f), 3f);
                c.Box(new Vector2(x - 30f, 336f), new Vector2(x + 30f, 348f), stone);
                c.Box(new Vector2(x - 30f, 106f), new Vector2(x + 30f, 116f), stone);
            }
            c.Box(new Vector2(210f, 348f), new Vector2(750f, 390f), stone);
            c.Poly(new[] { new Vector2(196f, 390f), new Vector2(764f, 390f), new Vector2(480f, 480f) }, C(0.84f, 0.82f, 0.77f));
            c.Poly(new[] { new Vector2(260f, 400f), new Vector2(700f, 400f), new Vector2(480f, 462f) }, C(0.70f, 0.68f, 0.64f));
            c.Box(new Vector2(430f, 106f), new Vector2(530f, 250f), C(0.22f, 0.20f, 0.20f), 4f);
        }

        // A round table under a lamp, people as dark shapes around it.
        private static void RoundTable(Raster c)
        {
            c.Gradient(C(0.09f, 0.07f, 0.07f), C(0.27f, 0.18f, 0.13f));
            c.Glow(new Vector2(480f, 260f), 520f, C(1f, 0.78f, 0.45f), 0.55f);

            var silhouette = C(0.07f, 0.055f, 0.06f);
            foreach (float x in new[] { 270f, 400f, 560f, 690f })
            {
                c.Ellipse(new Vector2(x, 262f), new Vector2(56f, 52f), silhouette);
                c.Disc(new Vector2(x, 336f), 27f, silhouette);
            }
            c.Ellipse(new Vector2(480f, 160f), new Vector2(310f, 86f), C(0.30f, 0.18f, 0.11f));
            c.Ellipse(new Vector2(480f, 172f), new Vector2(300f, 78f), C(0.58f, 0.37f, 0.22f));
            c.Ellipse(new Vector2(480f, 176f), new Vector2(220f, 52f), C(0.64f, 0.43f, 0.26f));
            foreach (var paper in new[] { new Vector2(380f, 170f), new Vector2(480f, 190f), new Vector2(590f, 168f), new Vector2(430f, 150f) })
            {
                c.Box(paper - new Vector2(22f, 8f), paper + new Vector2(22f, 8f), C(0.92f, 0.90f, 0.84f), 1f);
            }
            foreach (float x in new[] { 330f, 630f })
            {
                c.Ellipse(new Vector2(x, 80f), new Vector2(66f, 54f), silhouette);
                c.Disc(new Vector2(x, 144f), 29f, silhouette);
            }

            c.Line(new Vector2(480f, Height), new Vector2(480f, 452f), 3f, C(0.05f, 0.05f, 0.06f));
            c.Poly(new[] { new Vector2(436f, 452f), new Vector2(524f, 452f), new Vector2(548f, 408f), new Vector2(412f, 408f) }, C(0.10f, 0.09f, 0.09f));
            c.Box(new Vector2(420f, 402f), new Vector2(540f, 410f), C(1f, 0.92f, 0.70f));
            c.Glow(new Vector2(480f, 400f), 120f, C(1f, 0.90f, 0.60f), 0.8f);
        }

        // A white-hot sun over a skyline, heat shimmering above the roofs.
        private static void HeatWave(Raster c)
        {
            c.Gradient(C(0.98f, 0.45f, 0.20f), C(1f, 0.84f, 0.50f));
            c.Glow(new Vector2(500f, 300f), 640f, C(1f, 0.92f, 0.65f), 0.6f);
            c.Disc(new Vector2(500f, 300f), 118f, C(1f, 0.97f, 0.82f));

            float x = -10f;
            int index = 0;
            while (x < Width)
            {
                float width = 44f + Hash(index, 1) * 46f;
                float height = 70f + Hash(index, 2) * 150f + (index % 5 == 0 ? 60f : 0f);
                c.Box(new Vector2(x, 0f), new Vector2(x + width, height), C(0.30f, 0.12f, 0.10f) * (0.85f + Hash(index, 3) * 0.3f));
                x += width + 3f;
                index++;
            }

            for (int row = 0; row < 7; row++)
            {
                float y = 200f + row * 24f;
                var previous = new Vector2(0f, y);
                for (int step = 1; step <= 48; step++)
                {
                    var point = new Vector2(step * (Width / 48f), y + Mathf.Sin(step * 0.55f + row * 1.7f) * 5f);
                    c.Line(previous, point, 3f, Color.white, 0.14f);
                    previous = point;
                }
            }
        }

        // A dry lake bed ringed by pale bands where the water used to stand.
        private static void DryReservoir(Raster c)
        {
            c.Gradient(C(0.70f, 0.82f, 0.92f), C(0.94f, 0.91f, 0.82f));
            c.Disc(new Vector2(780f, 440f), 36f, C(1f, 0.98f, 0.88f));
            c.Glow(new Vector2(780f, 440f), 260f, C(1f, 0.98f, 0.88f), 0.4f);
            c.Poly(new[] { new Vector2(0f, 200f), new Vector2(0f, 300f), new Vector2(150f, 360f), new Vector2(300f, 300f), new Vector2(470f, 380f), new Vector2(650f, 310f), new Vector2(820f, 350f), new Vector2(960f, 290f), new Vector2(960f, 200f) }, C(0.58f, 0.66f, 0.74f));
            c.Poly(new[] { new Vector2(0f, 180f), new Vector2(0f, 250f), new Vector2(210f, 290f), new Vector2(420f, 235f), new Vector2(640f, 275f), new Vector2(960f, 240f), new Vector2(960f, 180f) }, C(0.45f, 0.49f, 0.53f));

            c.Box(new Vector2(0f, 0f), new Vector2(Width, 200f), C(0.78f, 0.68f, 0.52f));
            c.Ellipse(new Vector2(480f, 110f), new Vector2(560f, 100f), C(0.84f, 0.76f, 0.62f));
            c.Ellipse(new Vector2(480f, 100f), new Vector2(430f, 74f), C(0.70f, 0.60f, 0.46f));
            c.Ellipse(new Vector2(520f, 84f), new Vector2(190f, 30f), C(0.24f, 0.44f, 0.57f));
            c.Ellipse(new Vector2(520f, 90f), new Vector2(150f, 18f), C(0.40f, 0.62f, 0.74f), 0.6f);

            for (int i = 0; i < 90; i++)
            {
                var start = new Vector2(Hash(i, 11) * Width, Hash(i, 12) * 170f);
                var angle = Hash(i, 13) * Mathf.PI;
                var end = start + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (14f + Hash(i, 14) * 26f);
                c.Line(start, end, 2f, C(0.45f, 0.34f, 0.22f), 0.5f);
            }
        }

        // A processor on a circuit board.
        private static void Chip(Raster c)
        {
            c.Gradient(C(0.02f, 0.09f, 0.11f), C(0.03f, 0.22f, 0.24f));
            var trace = C(0.12f, 0.62f, 0.56f);
            for (int i = 0; i < 14; i++)
            {
                float y = 40f + i * 36f;
                float bend = 120f + Hash(i, 21) * 120f;
                var left = new Vector2(0f, y);
                var corner = new Vector2(bend, y);
                var up = new Vector2(bend + 70f, y + (i % 2 == 0 ? 70f : -70f));
                var chipEdgeLeft = new Vector2(350f, up.y);
                c.Line(left, corner, 4f, trace, 0.5f);
                c.Line(corner, up, 4f, trace, 0.5f);
                c.Line(up, chipEdgeLeft, 4f, trace, 0.5f);
                c.Disc(left + new Vector2(24f, 0f), 7f, C(0.85f, 0.70f, 0.30f), 0.7f);

                var right = new Vector2(Width, y + 14f);
                var rightCorner = new Vector2(Width - bend, y + 14f);
                var rightUp = new Vector2(Width - bend - 70f, y + 14f + (i % 2 == 0 ? -70f : 70f));
                c.Line(right, rightCorner, 4f, trace, 0.5f);
                c.Line(rightCorner, rightUp, 4f, trace, 0.5f);
                c.Line(rightUp, new Vector2(610f, rightUp.y), 4f, trace, 0.5f);
            }

            var center = new Vector2(480f, 270f);
            c.Glow(center, 360f, C(0.20f, 0.90f, 0.80f), 0.35f);
            c.Box(center - new Vector2(142f, 142f), center + new Vector2(142f, 142f), C(0.30f, 0.33f, 0.36f), 16f);
            c.Box(center - new Vector2(134f, 134f), center + new Vector2(134f, 134f), C(0.07f, 0.08f, 0.10f), 12f);
            for (int i = 0; i < 11; i++)
            {
                float p = -110f + i * 22f;
                var gold = C(0.88f, 0.72f, 0.30f);
                c.Box(new Vector2(center.x + p - 5f, center.y + 134f), new Vector2(center.x + p + 5f, center.y + 154f), gold);
                c.Box(new Vector2(center.x + p - 5f, center.y - 154f), new Vector2(center.x + p + 5f, center.y - 134f), gold);
                c.Box(new Vector2(center.x - 154f, center.y + p - 5f), new Vector2(center.x - 134f, center.y + p + 5f), gold);
                c.Box(new Vector2(center.x + 134f, center.y + p - 5f), new Vector2(center.x + 154f, center.y + p + 5f), gold);
            }
            c.Box(center - new Vector2(78f, 78f), center + new Vector2(78f, 78f), C(0.13f, 0.17f, 0.22f), 6f);
            for (int i = 1; i < 6; i++)
            {
                float p = -78f + i * 26f;
                c.Line(new Vector2(center.x + p, center.y - 78f), new Vector2(center.x + p, center.y + 78f), 2f, C(0.25f, 0.85f, 0.78f), 0.35f);
                c.Line(new Vector2(center.x - 78f, center.y + p), new Vector2(center.x + 78f, center.y + p), 2f, C(0.25f, 0.85f, 0.78f), 0.35f);
            }
            c.Glow(center, 90f, C(0.30f, 1f, 0.90f), 0.55f);
        }

        // A container port at dusk: stacks of containers and two gantry cranes.
        private static void Harbour(Raster c)
        {
            c.Gradient(C(0.22f, 0.32f, 0.54f), C(0.96f, 0.70f, 0.50f));
            c.Glow(new Vector2(250f, 220f), 460f, C(1f, 0.78f, 0.50f), 0.4f);
            c.Box(new Vector2(0f, 0f), new Vector2(Width, 150f), C(0.09f, 0.19f, 0.31f));
            for (int i = 0; i < 10; i++)
            {
                c.Line(new Vector2(60f + i * 100f, 60f + (i % 3) * 22f), new Vector2(120f + i * 100f, 60f + (i % 3) * 22f), 3f, Color.white, 0.12f);
            }
            c.Box(new Vector2(0f, 120f), new Vector2(Width, 196f), C(0.20f, 0.22f, 0.26f));

            var palette = new[] { C(0.74f, 0.22f, 0.18f), C(0.16f, 0.38f, 0.64f), C(0.88f, 0.60f, 0.14f), C(0.20f, 0.50f, 0.36f), C(0.55f, 0.55f, 0.58f), C(0.40f, 0.24f, 0.42f) };
            for (int column = 0; column < 15; column++)
            {
                int levels = 1 + (int)(Hash(column, 31) * 4f);
                for (int level = 0; level < levels; level++)
                {
                    float x0 = 14f + column * 63f;
                    float y0 = 196f + level * 32f;
                    var color = palette[(int)(Hash(column, level + 40) * palette.Length) % palette.Length];
                    c.Box(new Vector2(x0, y0), new Vector2(x0 + 58f, y0 + 30f), color, 1f);
                    for (int rib = 1; rib < 5; rib++)
                    {
                        c.Line(new Vector2(x0 + rib * 11.5f, y0 + 3f), new Vector2(x0 + rib * 11.5f, y0 + 27f), 1.5f, Color.black, 0.18f);
                    }
                }
            }

            var steel = C(0.88f, 0.34f, 0.22f);
            foreach (float x in new[] { 90f, 560f })
            {
                c.Line(new Vector2(x, 196f), new Vector2(x, 392f), 9f, steel);
                c.Line(new Vector2(x + 96f, 196f), new Vector2(x + 96f, 392f), 9f, steel);
                c.Line(new Vector2(x, 300f), new Vector2(x + 96f, 392f), 5f, steel);
                c.Line(new Vector2(x + 96f, 300f), new Vector2(x, 392f), 5f, steel);
                c.Box(new Vector2(x - 36f, 392f), new Vector2(x + 340f, 408f), steel, 2f);
                c.Line(new Vector2(x + 48f, 392f), new Vector2(x + 48f, 482f), 8f, steel);
                c.Line(new Vector2(x + 48f, 482f), new Vector2(x - 36f, 408f), 3f, steel);
                c.Line(new Vector2(x + 48f, 482f), new Vector2(x + 340f, 408f), 3f, steel);
                c.Box(new Vector2(x + 200f, 372f), new Vector2(x + 240f, 392f), C(0.18f, 0.20f, 0.24f));
                c.Line(new Vector2(x + 220f, 372f), new Vector2(x + 220f, 330f), 2f, C(0.12f, 0.12f, 0.14f));
            }
        }

        // A night skyline; one stretch of windows is dark.
        private static void NightCity(Raster c)
        {
            c.Gradient(C(0.03f, 0.04f, 0.10f), C(0.17f, 0.14f, 0.28f));
            c.Glow(new Vector2(790f, 440f), 340f, C(0.80f, 0.85f, 1f), 0.25f);
            c.Disc(new Vector2(790f, 440f), 34f, C(0.96f, 0.96f, 0.88f));
            for (int i = 0; i < 70; i++)
            {
                c.Disc(new Vector2(Hash(i, 51) * Width, 300f + Hash(i, 52) * 240f), 1.2f + Hash(i, 53), Color.white, 0.5f);
            }

            for (int layer = 0; layer < 2; layer++)
            {
                float x = -20f;
                int index = 0;
                bool near = layer == 1;
                while (x < Width)
                {
                    float width = (near ? 64f : 46f) + Hash(index + layer * 100, 61) * (near ? 70f : 40f);
                    float height = (near ? 150f : 190f) + Hash(index + layer * 100, 62) * (near ? 230f : 150f);
                    c.Box(new Vector2(x, 0f), new Vector2(x + width, height), near ? C(0.07f, 0.07f, 0.12f) : C(0.12f, 0.12f, 0.21f));

                    float spacing = near ? 17f : 13f;
                    for (float wx = x + 9f; wx < x + width - 9f; wx += spacing)
                    {
                        for (float wy = 14f; wy < height - 10f; wy += spacing + 6f)
                        {
                            bool blackout = near && wx > 360f && wx < 580f;
                            if (blackout || Hash((int)wx + layer * 1000, (int)wy) > 0.55f) continue;
                            var glow = Hash((int)wy, (int)wx) < 0.8f ? C(1f, 0.82f, 0.45f) : C(0.70f, 0.85f, 1f);
                            c.Box(new Vector2(wx, wy), new Vector2(wx + (near ? 8f : 6f), wy + (near ? 10f : 8f)), glow * (near ? 1f : 0.7f));
                        }
                    }
                    x += width + (near ? 4f : 2f);
                    index++;
                }
            }
        }

        // ------------------------------------------------------------------ scenes: sport, culture, science

        // A little person from boxes and lines, standing on a point; the height is the whole body.
        private static void Figure(Raster c, Vector2 foot, float height, Color shirt, Color shorts, Color skin, bool bib = false, float stride = 0f)
        {
            float t = Mathf.Max(2f, height * 0.06f);
            float hip = foot.y + height * 0.46f;
            c.Line(new Vector2(foot.x - height * 0.07f - stride, foot.y), new Vector2(foot.x - height * 0.04f, hip), t, skin);
            c.Line(new Vector2(foot.x + height * 0.07f + stride, foot.y), new Vector2(foot.x + height * 0.04f, hip), t, skin);
            c.Box(new Vector2(foot.x - height * 0.10f, hip - height * 0.04f), new Vector2(foot.x + height * 0.10f, hip + height * 0.10f), shorts, 2f);
            c.Box(new Vector2(foot.x - height * 0.11f, hip + height * 0.08f), new Vector2(foot.x + height * 0.11f, foot.y + height * 0.80f), shirt, height * 0.04f);
            c.Line(new Vector2(foot.x - height * 0.11f, foot.y + height * 0.78f), new Vector2(foot.x - height * 0.17f - stride, hip + height * 0.02f), t * 0.8f, skin);
            c.Line(new Vector2(foot.x + height * 0.11f, foot.y + height * 0.78f), new Vector2(foot.x + height * 0.17f + stride, hip + height * 0.02f), t * 0.8f, skin);
            c.Disc(new Vector2(foot.x, foot.y + height * 0.89f), height * 0.085f, skin);
            if (bib) c.Box(new Vector2(foot.x - height * 0.06f, hip + height * 0.14f), new Vector2(foot.x + height * 0.06f, hip + height * 0.25f), Color.white, 1f);
        }

        // A floodlit football pitch at night: stands, floodlights, a goal and a ball.
        private static void FootballMatch(Raster c)
        {
            c.Gradient(C(0.03f, 0.05f, 0.14f), C(0.10f, 0.16f, 0.30f));
            c.Box(new Vector2(0f, 228f), new Vector2(Width, 340f), C(0.07f, 0.09f, 0.15f));
            var crowd = new[] { C(0.95f, 0.45f, 0.30f), C(0.95f, 0.85f, 0.40f), C(0.40f, 0.65f, 0.95f), C(0.90f, 0.90f, 0.95f) };
            for (int i = 0; i < 360; i++)
            {
                c.Disc(new Vector2(Hash(i, 71) * Width, 236f + Hash(i, 72) * 96f), 3f, crowd[(int)(Hash(i, 73) * crowd.Length) % crowd.Length], 0.75f);
            }

            foreach (float x in new[] { 110f, 850f })
            {
                c.Line(new Vector2(x, 330f), new Vector2(x, 450f), 7f, C(0.04f, 0.05f, 0.09f));
                c.Box(new Vector2(x - 52f, 450f), new Vector2(x + 52f, 490f), C(0.12f, 0.14f, 0.20f), 3f);
                for (int i = 0; i < 4; i++) c.Disc(new Vector2(x - 36f + i * 24f, 470f), 7f, C(1f, 0.97f, 0.86f));
                c.Glow(new Vector2(x, 470f), 420f, C(1f, 0.96f, 0.82f), 0.42f);
            }

            for (int stripe = 0; stripe < 8; stripe++)
            {
                c.Box(new Vector2(stripe * 120f, 0f), new Vector2(stripe * 120f + 120f, 228f), stripe % 2 == 0 ? C(0.10f, 0.42f, 0.18f) : C(0.13f, 0.49f, 0.22f));
            }
            var chalk = new Color(1f, 1f, 1f);
            c.Line(new Vector2(0f, 226f), new Vector2(Width, 226f), 3f, chalk, 0.8f);
            c.Line(new Vector2(300f, 0f), new Vector2(300f, 226f), 3f, chalk, 0.6f);

            // The goal, seen from the side of the box, with its net.
            for (float x = 668f; x <= 860f; x += 16f) c.Line(new Vector2(x, 44f), new Vector2(x, 150f), 1.5f, chalk, 0.35f);
            for (float y = 44f; y <= 150f; y += 16f) c.Line(new Vector2(668f, y), new Vector2(860f, y), 1.5f, chalk, 0.35f);
            c.Line(new Vector2(668f, 44f), new Vector2(668f, 154f), 7f, chalk);
            c.Line(new Vector2(860f, 44f), new Vector2(860f, 154f), 7f, chalk);
            c.Line(new Vector2(665f, 152f), new Vector2(863f, 152f), 7f, chalk);

            Figure(c, new Vector2(420f, 40f), 150f, C(0.82f, 0.14f, 0.16f), C(0.95f, 0.95f, 0.95f), C(0.80f, 0.60f, 0.46f), stride: 16f);
            Figure(c, new Vector2(560f, 22f), 130f, C(0.16f, 0.32f, 0.78f), C(0.12f, 0.14f, 0.40f), C(0.55f, 0.38f, 0.28f), stride: 12f);
            Figure(c, new Vector2(760f, 56f), 120f, C(0.95f, 0.80f, 0.12f), C(0.12f, 0.14f, 0.20f), C(0.85f, 0.66f, 0.52f));
            c.Ellipse(new Vector2(486f, 22f), new Vector2(22f, 6f), Color.black, 0.3f);
            c.Disc(new Vector2(486f, 38f), 18f, Color.white);
            foreach (var spot in new[] { new Vector2(480f, 42f), new Vector2(494f, 34f), new Vector2(482f, 28f) }) c.Disc(spot, 5f, C(0.1f, 0.1f, 0.12f));
        }

        // A road race at sunset: a road running to the horizon with runners on it.
        private static void Marathon(Raster c)
        {
            c.Gradient(C(0.52f, 0.30f, 0.52f), C(1f, 0.72f, 0.42f));
            c.Glow(new Vector2(480f, 250f), 560f, C(1f, 0.82f, 0.50f), 0.55f);
            c.Disc(new Vector2(480f, 262f), 62f, C(1f, 0.93f, 0.72f));
            float x = 0f;
            int index = 0;
            while (x < Width)
            {
                float width = 30f + Hash(index, 81) * 40f;
                c.Box(new Vector2(x, 230f), new Vector2(x + width, 250f + Hash(index, 82) * 70f), C(0.33f, 0.20f, 0.36f));
                x += width + 2f;
                index++;
            }
            c.Box(new Vector2(0f, 0f), new Vector2(Width, 232f), C(0.28f, 0.18f, 0.30f));
            c.Poly(new[] { new Vector2(210f, 0f), new Vector2(750f, 0f), new Vector2(505f, 232f), new Vector2(455f, 232f) }, C(0.20f, 0.17f, 0.24f));
            for (int i = 0; i < 6; i++)
            {
                float f0 = 0.04f + i * 0.16f, f1 = f0 + 0.07f;
                float y0 = Mathf.Lerp(0f, 232f, f0), y1 = Mathf.Lerp(0f, 232f, f1);
                float w0 = Mathf.Lerp(7f, 1.5f, f0), w1 = Mathf.Lerp(7f, 1.5f, f1);
                c.Poly(new[] { new Vector2(480f - w0, y0), new Vector2(480f + w0, y0), new Vector2(480f + w1, y1), new Vector2(480f - w1, y1) }, C(0.95f, 0.88f, 0.60f), 0.85f);
            }
            var shirts = new[] { C(0.90f, 0.22f, 0.20f), C(0.20f, 0.55f, 0.90f), C(0.95f, 0.80f, 0.15f), C(0.25f, 0.72f, 0.45f), C(0.90f, 0.45f, 0.10f), C(0.65f, 0.30f, 0.80f) };
            var runners = new[] { (x: 390f, y: 24f, h: 230f), (x: 520f, y: 40f, h: 200f), (x: 600f, y: 12f, h: 250f), (x: 450f, y: 96f, h: 130f), (x: 540f, y: 118f, h: 110f), (x: 485f, y: 150f, h: 70f) };
            for (int i = runners.Length - 1; i >= 0; i--)
            {
                Figure(c, new Vector2(runners[i].x, runners[i].y), runners[i].h, shirts[i % shirts.Length], C(0.12f, 0.12f, 0.18f), C(0.78f, 0.58f, 0.44f), bib: true, stride: runners[i].h * 0.1f);
            }
        }

        // A tennis court from above, with the ball in the air.
        private static void TennisCourt(Raster c)
        {
            c.Gradient(C(0.10f, 0.34f, 0.28f), C(0.14f, 0.44f, 0.34f));
            var white = Color.white;
            c.Box(new Vector2(110f, 50f), new Vector2(850f, 490f), white, 2f);
            c.Box(new Vector2(116f, 56f), new Vector2(844f, 484f), C(0.17f, 0.32f, 0.62f), 2f);
            // Lines: baselines, doubles and singles sidelines, service lines and the centre service line.
            foreach (float x in new[] { 150f, 810f }) c.Line(new Vector2(x, 90f), new Vector2(x, 450f), 4f, white);
            foreach (float y in new[] { 90f, 133f, 407f, 450f }) c.Line(new Vector2(150f, y), new Vector2(810f, y), 4f, white);
            foreach (float x in new[] { 280f, 680f }) c.Line(new Vector2(x, 133f), new Vector2(x, 407f), 4f, white);
            c.Line(new Vector2(280f, 270f), new Vector2(680f, 270f), 4f, white);

            // The net runs across the middle, its shadow beside it.
            c.Line(new Vector2(480f, 40f), new Vector2(480f, 500f), 16f, Color.black, 0.2f);
            c.Line(new Vector2(472f, 40f), new Vector2(472f, 500f), 6f, C(0.92f, 0.92f, 0.95f));
            for (float y = 44f; y < 500f; y += 14f) c.Line(new Vector2(464f, y), new Vector2(472f, y), 2f, Color.black, 0.5f);

            // A racket and a ball.
            var racket = new Vector2(250f, 190f);
            c.Shape(racket - new Vector2(60f, 80f), racket + new Vector2(60f, 80f), p => DesktopArtPainter.Outline(DesktopArtPainter.EllipseEdge(p, racket, new Vector2(34f, 44f)), 7f), C(0.10f, 0.10f, 0.12f));
            for (int i = -3; i <= 3; i++)
            {
                c.Line(racket + new Vector2(i * 8f, -38f), racket + new Vector2(i * 8f, 38f), 1.2f, white, 0.6f);
                c.Line(racket + new Vector2(-30f, i * 10f), racket + new Vector2(30f, i * 10f), 1.2f, white, 0.6f);
            }
            c.Line(racket + new Vector2(0f, -44f), racket + new Vector2(0f, -110f), 9f, C(0.85f, 0.25f, 0.15f));
            c.Ellipse(new Vector2(640f, 262f), new Vector2(16f, 12f), Color.black, 0.3f);
            c.Glow(new Vector2(628f, 290f), 70f, C(0.95f, 1f, 0.30f), 0.6f);
            c.Disc(new Vector2(628f, 290f), 14f, C(0.86f, 0.95f, 0.22f));
            c.Shape(new Vector2(612f, 274f), new Vector2(644f, 306f), p => DesktopArtPainter.Outline(DesktopArtPainter.Circle(p, new Vector2(618f, 290f), 14f), 1.8f), Color.white, 0.8f);
        }

        // A stadium scoreboard with a seven-segment score.
        private static void Scoreboard(Raster c)
        {
            c.Gradient(C(0.02f, 0.03f, 0.08f), C(0.08f, 0.10f, 0.20f));
            for (int i = 0; i < 240; i++) c.Disc(new Vector2(Hash(i, 91) * Width, Hash(i, 92) * 200f), 3f, new Color(0.9f, 0.8f, 0.6f), 0.5f);
            c.Box(new Vector2(0f, 0f), new Vector2(Width, 80f), C(0.04f, 0.05f, 0.09f));
            c.Box(new Vector2(60f, 110f), new Vector2(900f, 490f), C(0.12f, 0.13f, 0.17f), 14f);
            c.Box(new Vector2(76f, 126f), new Vector2(884f, 474f), C(0.02f, 0.03f, 0.04f), 8f);
            c.Box(new Vector2(110f, 410f), new Vector2(380f, 446f), C(0.95f, 0.55f, 0.12f), 4f);
            c.Box(new Vector2(580f, 410f), new Vector2(850f, 446f), C(0.20f, 0.60f, 0.90f), 4f);
            c.Box(new Vector2(110f, 160f), new Vector2(380f, 176f), C(0.95f, 0.55f, 0.12f), 4f, 0.7f);
            c.Box(new Vector2(580f, 160f), new Vector2(850f, 176f), C(0.20f, 0.60f, 0.90f), 4f, 0.7f);

            var on = C(1f, 0.74f, 0.18f);
            var off = C(0.16f, 0.12f, 0.06f);
            SevenSegment(c, new Vector2(200f, 205f), 180f, 2, on, off);
            SevenSegment(c, new Vector2(670f, 205f), 180f, 1, on, off);
            c.Disc(new Vector2(480f, 270f), 10f, on);
            c.Disc(new Vector2(480f, 330f), 10f, on);
            c.Glow(new Vector2(480f, 300f), 520f, on, 0.16f);
            foreach (float x in new[] { 440f, 520f }) c.Line(new Vector2(x, 126f), new Vector2(x, 100f), 8f, C(0.20f, 0.22f, 0.28f));
        }

        // A digit of seven bars; the unlit ones stay faintly visible.
        private static void SevenSegment(Raster c, Vector2 origin, float height, int digit, Color on, Color off)
        {
            const string lit = "abcdef;bc;abdeg;abcdg;bcfg;acdfg;acdefg;abc;abcdefg;abcdfg";
            string segments = lit.Split(';')[Mathf.Clamp(digit, 0, 9)];
            float w = height * 0.5f, h = height * 0.5f, t = height * 0.09f;
            var corner = origin;
            var bars = new System.Collections.Generic.Dictionary<char, (Vector2 a, Vector2 b)>
            {
                ['a'] = (corner + new Vector2(t, h * 2f), corner + new Vector2(w - t, h * 2f)),
                ['b'] = (corner + new Vector2(w, h * 2f - t), corner + new Vector2(w, h + t)),
                ['c'] = (corner + new Vector2(w, h - t), corner + new Vector2(w, t)),
                ['d'] = (corner + new Vector2(t, 0f), corner + new Vector2(w - t, 0f)),
                ['e'] = (corner + new Vector2(0f, h - t), corner + new Vector2(0f, t)),
                ['f'] = (corner + new Vector2(0f, h * 2f - t), corner + new Vector2(0f, h + t)),
                ['g'] = (corner + new Vector2(t, h), corner + new Vector2(w - t, h)),
            };
            foreach (var pair in bars)
            {
                bool isOn = segments.IndexOf(pair.Key) >= 0;
                c.Line(pair.Value.a, pair.Value.b, t, isOn ? on : off);
            }
        }

        // An open-air concert: coloured beams from above, a singer on the stage, a crowd with raised arms.
        private static void Concert(Raster c)
        {
            c.Gradient(C(0.04f, 0.02f, 0.12f), C(0.20f, 0.07f, 0.28f));
            var beams = new[] { (x: 220f, color: C(1f, 0.25f, 0.65f)), (x: 480f, color: C(0.30f, 0.80f, 1f)), (x: 740f, color: C(1f, 0.85f, 0.30f)) };
            foreach (var beam in beams)
            {
                c.Poly(new[] { new Vector2(beam.x, Height), new Vector2(beam.x - 150f, 150f), new Vector2(beam.x + 150f, 150f) }, beam.color, 0.20f);
                c.Poly(new[] { new Vector2(beam.x, Height), new Vector2(beam.x - 70f, 150f), new Vector2(beam.x + 70f, 150f) }, beam.color, 0.20f);
                c.Glow(new Vector2(beam.x, 490f), 150f, beam.color, 0.6f);
            }
            c.Box(new Vector2(0f, 150f), new Vector2(Width, 200f), C(0.10f, 0.07f, 0.16f));
            c.Box(new Vector2(250f, 200f), new Vector2(710f, 330f), C(0.07f, 0.05f, 0.11f));
            c.Glow(new Vector2(480f, 260f), 300f, C(1f, 0.70f, 0.90f), 0.35f);
            Figure(c, new Vector2(480f, 200f), 110f, C(0.95f, 0.95f, 0.98f), C(0.08f, 0.08f, 0.12f), C(0.70f, 0.50f, 0.40f));
            c.Line(new Vector2(496f, 262f), new Vector2(560f, 300f), 6f, C(0.55f, 0.30f, 0.12f));

            for (int i = 0; i < 70; i++)
            {
                float x = i * 14f + Hash(i, 101) * 8f;
                float y = 70f + Hash(i, 102) * 60f;
                var dark = C(0.02f, 0.01f, 0.05f);
                c.Disc(new Vector2(x, y + 36f), 15f, dark);
                c.Ellipse(new Vector2(x, y - 6f), new Vector2(20f, 34f), dark);
                if (Hash(i, 103) < 0.45f) c.Line(new Vector2(x + 8f, y + 10f), new Vector2(x + 16f + Hash(i, 104) * 8f, y + 76f), 5f, dark);
                if (Hash(i, 105) < 0.15f) c.Disc(new Vector2(x + 14f, y + 84f), 4f, Color.white, 0.9f);
            }
        }

        // A lighthouse on a rock at night, its beam sweeping over a quiet sea.
        private static void Lighthouse(Raster c)
        {
            c.Gradient(C(0.02f, 0.04f, 0.12f), C(0.08f, 0.20f, 0.30f));
            for (int i = 0; i < 90; i++) c.Disc(new Vector2(Hash(i, 111) * Width, 250f + Hash(i, 112) * 290f), 1.1f + Hash(i, 113), Color.white, 0.6f);
            c.Disc(new Vector2(190f, 430f), 30f, C(0.96f, 0.96f, 0.86f));
            c.Glow(new Vector2(190f, 430f), 220f, C(0.8f, 0.9f, 1f), 0.25f);
            c.Box(new Vector2(0f, 0f), new Vector2(Width, 170f), C(0.05f, 0.13f, 0.20f));
            for (int i = 0; i < 12; i++)
            {
                float y = 20f + i * 12f;
                c.Line(new Vector2(60f + (i * 97) % 700, y), new Vector2(170f + (i * 97) % 700, y), 2f, Color.white, 0.10f);
            }
            for (int i = 0; i < 7; i++)
            {
                float y = 160f - i * 22f;
                float half = 34f - i * 3f + Hash(i, 114) * 8f;
                c.Line(new Vector2(190f - half, y), new Vector2(190f + half, y), 4f, C(0.95f, 0.95f, 0.85f), 0.16f);
            }

            // The beam goes both ways from the lamp.
            c.Poly(new[] { new Vector2(700f, 395f), new Vector2(0f, 330f), new Vector2(0f, 470f) }, C(1f, 0.92f, 0.55f), 0.26f);
            c.Poly(new[] { new Vector2(700f, 395f), new Vector2(960f, 372f), new Vector2(960f, 420f) }, C(1f, 0.92f, 0.55f), 0.16f);

            c.Poly(new[] { new Vector2(520f, 0f), new Vector2(560f, 150f), new Vector2(820f, 180f), new Vector2(960f, 140f), new Vector2(960f, 0f) }, C(0.04f, 0.05f, 0.08f));
            var tower = new[] { new Vector2(660f, 168f), new Vector2(740f, 168f), new Vector2(724f, 360f), new Vector2(676f, 360f) };
            c.Poly(tower, C(0.94f, 0.93f, 0.88f));
            foreach (float y in new[] { 215f, 290f })
            {
                float f = (y - 168f) / 192f;
                float half = Mathf.Lerp(40f, 24f, f);
                c.Poly(new[] { new Vector2(700f - half, y), new Vector2(700f + half, y), new Vector2(700f + half - 2f, y + 34f), new Vector2(700f - half + 2f, y + 34f) }, C(0.78f, 0.16f, 0.14f));
            }
            c.Box(new Vector2(668f, 360f), new Vector2(732f, 370f), C(0.10f, 0.10f, 0.14f));
            c.Box(new Vector2(676f, 370f), new Vector2(724f, 410f), C(1f, 0.92f, 0.55f));
            c.Glow(new Vector2(700f, 392f), 190f, C(1f, 0.90f, 0.50f), 0.8f);
            c.Poly(new[] { new Vector2(670f, 410f), new Vector2(730f, 410f), new Vector2(700f, 440f) }, C(0.78f, 0.16f, 0.14f));
            c.Line(new Vector2(700f, 440f), new Vector2(700f, 456f), 4f, C(0.10f, 0.10f, 0.14f));
        }

        // The window of a village bakery: an awning, shelves of loaves.
        private static void Bakery(Raster c)
        {
            c.Gradient(C(0.55f, 0.36f, 0.24f), C(0.75f, 0.52f, 0.34f));
            for (int row = 0; row < 14; row++)
            {
                float y = row * 40f;
                c.Line(new Vector2(0f, y), new Vector2(Width, y), 2f, Color.black, 0.10f);
                for (float x = (row % 2) * 40f; x < Width; x += 80f) c.Line(new Vector2(x, y), new Vector2(x, y + 40f), 2f, Color.black, 0.10f);
            }
            c.Box(new Vector2(110f, 40f), new Vector2(850f, 400f), C(0.30f, 0.18f, 0.10f), 8f);
            c.Box(new Vector2(130f, 60f), new Vector2(830f, 380f), C(0.98f, 0.80f, 0.50f), 4f);
            c.Glow(new Vector2(480f, 240f), 560f, C(1f, 0.75f, 0.35f), 0.45f);
            for (int shelf = 0; shelf < 3; shelf++)
            {
                float y = 120f + shelf * 100f;
                c.Box(new Vector2(130f, y - 10f), new Vector2(830f, y), C(0.42f, 0.26f, 0.14f));
            }

            var crust = new[] { C(0.72f, 0.42f, 0.16f), C(0.80f, 0.52f, 0.22f), C(0.62f, 0.34f, 0.12f) };
            for (int shelf = 0; shelf < 3; shelf++)
            {
                float y = 120f + shelf * 100f;
                for (int i = 0; i < 7; i++)
                {
                    float x = 175f + i * 98f + Hash(shelf, i + 120) * 14f;
                    float w = 34f + Hash(i, shelf + 130) * 12f;
                    var colour = crust[(i + shelf) % crust.Length];
                    c.Ellipse(new Vector2(x, y + 24f), new Vector2(w, 26f), colour);
                    for (int cut = -1; cut <= 1; cut++) c.Line(new Vector2(x + cut * 16f - 6f, y + 34f), new Vector2(x + cut * 16f + 6f, y + 18f), 3f, C(0.95f, 0.78f, 0.45f), 0.8f);
                }
            }

            for (int stripe = 0; stripe < 12; stripe++)
            {
                var colour = stripe % 2 == 0 ? C(0.80f, 0.18f, 0.16f) : C(0.97f, 0.94f, 0.88f);
                c.Poly(new[] { new Vector2(60f + stripe * 70f, 540f), new Vector2(130f + stripe * 70f, 540f), new Vector2(130f + stripe * 70f, 430f), new Vector2(60f + stripe * 70f, 430f) }, colour);
                c.Ellipse(new Vector2(95f + stripe * 70f, 430f), new Vector2(35f, 18f), colour);
            }
            c.Box(new Vector2(0f, 0f), new Vector2(Width, 40f), C(0.35f, 0.33f, 0.32f));
        }

        // A comet over dark hills, with a lit cottage window.
        private static void Comet(Raster c)
        {
            c.Gradient(C(0.02f, 0.03f, 0.10f), C(0.14f, 0.12f, 0.30f));
            for (int i = 0; i < 160; i++) c.Disc(new Vector2(Hash(i, 141) * Width, 130f + Hash(i, 142) * 410f), 0.8f + Hash(i, 143) * 1.4f, Color.white, 0.4f + Hash(i, 144) * 0.5f);
            var head = new Vector2(700f, 390f);
            c.Poly(new[] { head, new Vector2(120f, 190f), new Vector2(210f, 300f) }, C(0.55f, 0.75f, 1f), 0.20f);
            c.Poly(new[] { head, new Vector2(240f, 215f), new Vector2(300f, 285f) }, C(0.75f, 0.90f, 1f), 0.30f);
            c.Poly(new[] { head, new Vector2(360f, 255f), new Vector2(400f, 300f) }, Color.white, 0.35f);
            c.Glow(head, 190f, C(0.70f, 0.90f, 1f), 0.8f);
            c.Disc(head, 15f, Color.white);

            c.Poly(new[] { new Vector2(0f, 0f), new Vector2(0f, 140f), new Vector2(200f, 175f), new Vector2(420f, 120f), new Vector2(660f, 160f), new Vector2(960f, 110f), new Vector2(960f, 0f) }, C(0.03f, 0.04f, 0.08f));
            for (int i = 0; i < 6; i++)
            {
                float x = 760f + i * 38f, y = 90f + (i % 2) * 20f, scale = 1f + (i % 3) * 0.3f;
                c.Poly(new[] { new Vector2(x - 20f * scale, y), new Vector2(x + 20f * scale, y), new Vector2(x, y + 90f * scale) }, C(0.02f, 0.05f, 0.07f));
            }
            c.Box(new Vector2(250f, 120f), new Vector2(350f, 180f), C(0.02f, 0.02f, 0.05f));
            c.Poly(new[] { new Vector2(240f, 180f), new Vector2(360f, 180f), new Vector2(300f, 222f) }, C(0.02f, 0.02f, 0.05f));
            c.Box(new Vector2(280f, 135f), new Vector2(305f, 160f), C(1f, 0.82f, 0.45f));
            c.Glow(new Vector2(292f, 148f), 90f, C(1f, 0.80f, 0.40f), 0.5f);
        }

        // Glowing jellyfish in a deep dark sea.
        private static void DeepSea(Raster c)
        {
            c.Gradient(C(0.00f, 0.02f, 0.06f), C(0.01f, 0.20f, 0.28f));
            foreach (float x in new[] { 200f, 450f, 700f }) c.Poly(new[] { new Vector2(x, Height), new Vector2(x - 90f, 0f), new Vector2(x + 60f, 0f) }, C(0.30f, 0.80f, 0.90f), 0.07f);
            for (int i = 0; i < 110; i++) c.Disc(new Vector2(Hash(i, 151) * Width, Hash(i, 152) * Height), 1f + Hash(i, 153) * 2f, C(0.60f, 0.95f, 1f), 0.25f);

            var jellies = new[] { (x: 330f, y: 330f, r: 120f, color: C(0.20f, 0.85f, 1f)), (x: 660f, y: 250f, r: 80f, color: C(1f, 0.35f, 0.80f)), (x: 820f, y: 420f, r: 46f, color: C(0.55f, 1f, 0.70f)), (x: 150f, y: 170f, r: 50f, color: C(1f, 0.60f, 0.35f)) };
            foreach (var jelly in jellies)
            {
                var center = new Vector2(jelly.x, jelly.y);
                c.Glow(center, jelly.r * 3f, jelly.color, 0.5f);
                for (int t = -3; t <= 3; t++)
                {
                    var previous = center + new Vector2(t * jelly.r * 0.25f, 0f);
                    for (int step = 1; step <= 14; step++)
                    {
                        var point = center + new Vector2(t * jelly.r * 0.25f + Mathf.Sin(step * 0.7f + t) * jelly.r * 0.12f, -step * jelly.r * 0.22f);
                        c.Line(previous, point, Mathf.Max(1.5f, jelly.r * 0.035f), jelly.color, 0.55f * (1f - step / 16f));
                        previous = point;
                    }
                }
                float cy = center.y, rx = jelly.r, ry = jelly.r * 0.8f;
                c.Shape(center - new Vector2(rx, 4f), center + new Vector2(rx, ry), p => Mathf.Max(DesktopArtPainter.EllipseEdge(p, center, new Vector2(rx, ry)), cy - p.y), jelly.color, 0.75f);
                c.Shape(center - new Vector2(rx * 0.7f, 4f), center + new Vector2(rx * 0.7f, ry * 0.7f), p => Mathf.Max(DesktopArtPainter.EllipseEdge(p, center, new Vector2(rx * 0.7f, ry * 0.7f)), cy - p.y), Color.white, 0.25f);
            }
            c.Poly(new[] { new Vector2(0f, 0f), new Vector2(0f, 70f), new Vector2(180f, 40f), new Vector2(420f, 90f), new Vector2(700f, 36f), new Vector2(960f, 80f), new Vector2(960f, 0f) }, C(0.00f, 0.03f, 0.05f));
        }

        // ------------------------------------------------------------------ a tiny drawing surface

        private sealed class Raster
        {
            private readonly Color[] pixels;
            private readonly int width;
            private readonly int height;

            public Raster(int width, int height)
            {
                this.width = width;
                this.height = height;
                pixels = new Color[width * height];
            }

            public void Gradient(Color top, Color bottom)
            {
                for (int y = 0; y < height; y++)
                {
                    var color = Color.Lerp(bottom, top, y / (float)(height - 1));
                    for (int x = 0; x < width; x++) pixels[y * width + x] = color;
                }
            }

            /// <summary>Paints a shape given by its distance function (negative inside), only within the box min..max.</summary>
            public void Shape(Vector2 min, Vector2 max, Func<Vector2, float> distance, Color color, float opacity = 1f)
            {
                int x0 = Mathf.Max(0, Mathf.FloorToInt(min.x) - 1);
                int x1 = Mathf.Min(width - 1, Mathf.CeilToInt(max.x) + 1);
                int y0 = Mathf.Max(0, Mathf.FloorToInt(min.y) - 1);
                int y1 = Mathf.Min(height - 1, Mathf.CeilToInt(max.y) + 1);
                for (int y = y0; y <= y1; y++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        float cover = DesktopArtPainter.Coverage(distance(new Vector2(x + 0.5f, y + 0.5f))) * opacity;
                        if (cover <= 0f) continue;
                        pixels[y * width + x] = Color.Lerp(pixels[y * width + x], color, cover);
                    }
                }
            }

            public void Box(Vector2 min, Vector2 max, Color color, float radius = 0f, float opacity = 1f)
            {
                var center = (min + max) * 0.5f;
                var half = (max - min) * 0.5f;
                float r = Mathf.Min(radius, Mathf.Min(half.x, half.y));
                Shape(min, max, p => DesktopArtPainter.RoundedRect(p, center, half, r), color, opacity);
            }

            public void Disc(Vector2 center, float radius, Color color, float opacity = 1f)
            {
                Shape(center - Vector2.one * radius, center + Vector2.one * radius, p => DesktopArtPainter.Circle(p, center, radius), color, opacity);
            }

            public void Ellipse(Vector2 center, Vector2 radii, Color color, float opacity = 1f)
            {
                Shape(center - radii, center + radii, p => DesktopArtPainter.EllipseEdge(p, center, radii), color, opacity);
            }

            public void Line(Vector2 a, Vector2 b, float thickness, Color color, float opacity = 1f)
            {
                var pad = Vector2.one * thickness;
                Shape(Vector2.Min(a, b) - pad, Vector2.Max(a, b) + pad, p => DesktopArtPainter.Segment(p, a, b, thickness), color, opacity);
            }

            public void Poly(Vector2[] vertices, Color color, float opacity = 1f)
            {
                var min = vertices[0];
                var max = vertices[0];
                foreach (var vertex in vertices)
                {
                    min = Vector2.Min(min, vertex);
                    max = Vector2.Max(max, vertex);
                }
                Shape(min, max, p => DesktopArtPainter.Polygon(p, vertices), color, opacity);
            }

            /// <summary>Adds light around a point; it fades out smoothly at the radius.</summary>
            public void Glow(Vector2 center, float radius, Color color, float strength)
            {
                int x0 = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
                int x1 = Mathf.Min(width - 1, Mathf.CeilToInt(center.x + radius));
                int y0 = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
                int y1 = Mathf.Min(height - 1, Mathf.CeilToInt(center.y + radius));
                for (int y = y0; y <= y1; y++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        float falloff = Mathf.Clamp01(1f - new Vector2(x + 0.5f - center.x, y + 0.5f - center.y).magnitude / radius);
                        if (falloff <= 0f) continue;
                        var light = color * (strength * falloff * falloff);
                        var current = pixels[y * width + x];
                        pixels[y * width + x] = new Color(current.r + light.r, current.g + light.g, current.b + light.b, 1f);
                    }
                }
            }

            public void Vignette(float strength)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float edge = new Vector2(x / (float)(width - 1) - 0.5f, y / (float)(height - 1) - 0.5f).magnitude;
                        pixels[y * width + x] *= 1f - strength * Mathf.Clamp01(edge - 0.3f) * 2f;
                        pixels[y * width + x].a = 1f;
                    }
                }
            }

            public Texture2D ToTexture()
            {
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.SetPixels(pixels);
                texture.Apply();
                return texture;
            }
        }
    }
}
