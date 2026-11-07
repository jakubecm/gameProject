using System;
using UnityEngine;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Draws the placeholder pictures of the terminal desktop with signed distance functions: a shape is described by
    /// its distance to the nearest edge (negative inside), which gives clean edges at any size. The icons are a coloured
    /// rounded badge with a white symbol; the wallpapers are soft gradients. They are stand-ins: a real icon or photo with
    /// the same file name replaces them.
    /// </summary>
    public static class DesktopArtPainter
    {
        public const int IconSize = 256;

        // ------------------------------------------------------------------ shapes (distances in pixels, y up)

        public static float Circle(Vector2 p, Vector2 center, float radius) => (p - center).magnitude - radius;

        public static float RoundedRect(Vector2 p, Vector2 center, Vector2 half, float radius)
        {
            var q = new Vector2(Mathf.Abs(p.x - center.x), Mathf.Abs(p.y - center.y)) - half + new Vector2(radius, radius);
            return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        public static float Segment(Vector2 p, Vector2 a, Vector2 b, float thickness)
        {
            var pa = p - a;
            var ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - thickness * 0.5f;
        }

        /// <summary>A line of the given thickness along the edge of a shape whose distance is <paramref name="edgeDistance"/> (zero on the edge).</summary>
        public static float Outline(float edgeDistance, float thickness) => Mathf.Abs(edgeDistance) - thickness * 0.5f;

        /// <summary>
        /// Distance to the edge of an ellipse, accurate close to the edge (a line of constant thickness stays constant,
        /// also at the ends of the ellipse).
        /// </summary>
        public static float EllipseEdge(Vector2 p, Vector2 center, Vector2 radii)
        {
            var q = p - center;
            float k0 = new Vector2(q.x / radii.x, q.y / radii.y).magnitude;
            float k1 = new Vector2(q.x / (radii.x * radii.x), q.y / (radii.y * radii.y)).magnitude;
            return k1 > 1e-6f ? k0 * (k0 - 1f) / k1 : -Mathf.Min(radii.x, radii.y);
        }

        public static float Polygon(Vector2 p, Vector2[] vertices)
        {
            float d = Vector2.Dot(p - vertices[0], p - vertices[0]);
            float sign = 1f;
            for (int i = 0, j = vertices.Length - 1; i < vertices.Length; j = i, i++)
            {
                var e = vertices[j] - vertices[i];
                var w = p - vertices[i];
                var b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));

                bool c1 = p.y >= vertices[i].y;
                bool c2 = p.y < vertices[j].y;
                bool c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) sign = -sign;
            }
            return sign * Mathf.Sqrt(d);
        }

        public static float Union(float a, float b) => Mathf.Min(a, b);

        public static float Intersect(float a, float b) => Mathf.Max(a, b);

        /// <summary>How much of a pixel a shape covers, from its distance at the pixel's center: a soft edge one pixel wide.</summary>
        public static float Coverage(float distance) => Mathf.Clamp01(0.5f - distance);

        // ------------------------------------------------------------------ icons

        /// <summary>A globe: a ring, a meridian, the equator and two parallels.</summary>
        public static Texture2D BrowserIcon()
        {
            return PaintIcon(new Color(0.20f, 0.52f, 0.88f), p =>
            {
                float ring = Outline(Circle(p, Vector2.zero, 70f), 12f);
                float meridian = Outline(EllipseEdge(p, Vector2.zero, new Vector2(34f, 70f)), 9f);
                float equator = Segment(p, new Vector2(-64f, 0f), new Vector2(64f, 0f), 9f);
                float northern = Segment(p, new Vector2(-58f, 36f), new Vector2(58f, 36f), 7f);
                float southern = Segment(p, new Vector2(-58f, -36f), new Vector2(58f, -36f), 7f);
                return Union(Union(ring, meridian), Union(equator, Union(northern, southern)));
            }, p => 1000f);
        }

        /// <summary>An open book with lines of text on its pages.</summary>
        public static Texture2D KnowledgeIcon()
        {
            var left = new[] { new Vector2(-104f, 58f), new Vector2(-8f, 46f), new Vector2(-8f, -62f), new Vector2(-104f, -50f) };
            var right = new[] { new Vector2(104f, 58f), new Vector2(8f, 46f), new Vector2(8f, -62f), new Vector2(104f, -50f) };
            return PaintIcon(new Color(0.93f, 0.58f, 0.12f), p => Union(Polygon(p, left), Polygon(p, right)), p =>
            {
                float lines = 1000f;
                foreach (float y in new[] { 28f, 6f, -16f, -38f })
                {
                    lines = Union(lines, Segment(p, new Vector2(-86f, y), new Vector2(-28f, y - 7f), 6f));
                    lines = Union(lines, Segment(p, new Vector2(86f, y), new Vector2(28f, y - 7f), 6f));
                }
                return lines;
            });
        }

        /// <summary>A padlock with a keyhole.</summary>
        public static Texture2D SecurityIcon()
        {
            return PaintIcon(new Color(0.78f, 0.20f, 0.22f), p =>
            {
                float body = RoundedRect(p, new Vector2(0f, -22f), new Vector2(58f, 46f), 14f);
                // The shackle: the upper half of a ring, and two legs down into the body.
                float arch = Intersect(Outline(Circle(p, new Vector2(0f, 26f), 38f), 18f), 26f - p.y);
                float legs = Union(Segment(p, new Vector2(-38f, 26f), new Vector2(-38f, 0f), 18f), Segment(p, new Vector2(38f, 26f), new Vector2(38f, 0f), 18f));
                return Union(body, Union(arch, legs));
            }, p => Union(Circle(p, new Vector2(0f, -12f), 11f), Segment(p, new Vector2(0f, -12f), new Vector2(0f, -40f), 12f)));
        }

        /// <summary>
        /// A rounded badge in the given colour with a white symbol. <paramref name="symbol"/> is the distance to the white
        /// shapes, <paramref name="cutout"/> the distance to shapes cut out of them again (letting the badge show through).
        /// </summary>
        public static Texture2D PaintIcon(Color badge, Func<Vector2, float> symbol, Func<Vector2, float> cutout)
        {
            var texture = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
            var pixels = new Color32[IconSize * IconSize];
            float half = IconSize / 2f;
            for (int y = 0; y < IconSize; y++)
            {
                for (int x = 0; x < IconSize; x++)
                {
                    var p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    float inside = Coverage(RoundedRect(p, Vector2.zero, new Vector2(half - 8f, half - 8f), 56f));

                    // The badge is a little lighter at the top and darker at the bottom.
                    float shade = Mathf.Lerp(-0.16f, 0.18f, (p.y + half) / IconSize);
                    var background = shade >= 0f ? Color.Lerp(badge, Color.white, shade) : Color.Lerp(badge, Color.black, -shade);

                    var color = background;
                    color = Color.Lerp(color, Color.white, Coverage(symbol(p)));
                    color = Color.Lerp(color, background, Coverage(cutout(p)) * Coverage(symbol(p)));
                    color.a = inside;
                    pixels[y * IconSize + x] = color;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        // ------------------------------------------------------------------ wallpapers

        public static readonly string[] WallpaperNames = { "night", "dusk", "teal", "graphite" };

        /// <summary>A soft gradient with a few glowing shapes, one look per variant (0 to 3).</summary>
        public static Texture2D PaintWallpaper(int variant, int width = 1280, int height = 720)
        {
            Color top, bottom, glow;
            Vector2[] centers;
            switch (variant % WallpaperNames.Length)
            {
                case 0:
                    top = new Color(0.04f, 0.09f, 0.22f); bottom = new Color(0.12f, 0.32f, 0.58f); glow = new Color(0.45f, 0.80f, 1.00f);
                    centers = new[] { new Vector2(0.72f, 0.38f), new Vector2(0.25f, 0.70f) };
                    break;
                case 1:
                    top = new Color(0.17f, 0.09f, 0.28f); bottom = new Color(0.88f, 0.46f, 0.30f); glow = new Color(1.00f, 0.78f, 0.45f);
                    centers = new[] { new Vector2(0.30f, 0.30f), new Vector2(0.80f, 0.62f) };
                    break;
                case 2:
                    top = new Color(0.02f, 0.17f, 0.22f); bottom = new Color(0.12f, 0.52f, 0.50f); glow = new Color(0.55f, 1.00f, 0.85f);
                    centers = new[] { new Vector2(0.60f, 0.25f), new Vector2(0.15f, 0.55f) };
                    break;
                default:
                    top = new Color(0.07f, 0.08f, 0.10f); bottom = new Color(0.24f, 0.27f, 0.32f); glow = new Color(0.65f, 0.75f, 0.95f);
                    centers = new[] { new Vector2(0.78f, 0.70f), new Vector2(0.35f, 0.35f) };
                    break;
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float v = y / (float)(height - 1);
                var gradient = Color.Lerp(bottom, top, v);
                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)(width - 1);
                    var color = gradient;
                    foreach (var center in centers)
                    {
                        float distance = new Vector2((u - center.x) * width / height, v - center.y).magnitude;
                        float falloff = Mathf.Clamp01(1f - distance / 0.55f);
                        color += glow * (0.30f * falloff * falloff);
                    }

                    // A soft vignette.
                    float edge = new Vector2(u - 0.5f, v - 0.5f).magnitude;
                    color *= 1f - 0.35f * Mathf.Clamp01(edge - 0.25f) * 2f;
                    color.a = 1f;
                    pixels[y * width + x] = color;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }
    }
}
