using UnityEngine;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Stickers and name plates of the plant rooms: quads (flat or wrapped round a cylinder) that show one cell of the label atlas
    /// <c>T_PlantLabels.png</c> (4 x 4 cells, texts in Czech) and sit 0.6 mm proud of the surface they are stuck on.
    /// </summary>
    public static partial class GrayboxPropsTool
    {
        /// <summary>How far a sticker stands off the surface it is on: enough for the depth buffer at any distance, so it never flickers.</summary>
        private const float LabelLift = 0.003f;

        /// <summary>Cells of the label atlas.</summary>
        private enum PlantLabel
        {
            Danger,
            Warning,
            MotorPlate,
            PumpPlate,
            ExchangerTag,
            PipeLabel,
            UnitTag,
            VesselLabel,
            TransformerPlate,
            SwitchgearTag,
            UpsTag,
            BreakerQ1,
            BreakerQ2,
            BreakerQ3,
        }

        /// <summary>
        /// A flat label centred on <paramref name="centre"/>, <paramref name="right"/> being the direction the viewer's right-hand
        /// goes (the text reads along it) and <paramref name="up"/> the way up; it faces the viewer.
        /// </summary>
        private static void AddLabel(GrayboxMeshBuilder builder, Vector3 centre, Vector3 right, Vector3 up, float width, float height, PlantLabel label)
        {
            var uv = CellUv(label);
            var r = right.normalized * width / 2f;
            var u = up.normalized * height / 2f;
            var lifted = centre + Vector3.Cross(up, right).normalized * LabelLift;
            builder.AddPanel(lifted - r - u, lifted + r - u, lifted + r + u, lifted - r + u, Vector3.Cross(up, right),
                new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMax, uv.yMin), new Vector2(uv.xMax, uv.yMax), new Vector2(uv.xMin, uv.yMax));
        }

        /// <summary>
        /// A label wrapped round a cylinder of <paramref name="radius"/> whose axis runs through <paramref name="origin"/> along
        /// <paramref name="axis"/>, on the side that faces <paramref name="front"/>. It covers <paramref name="alongSize"/> metres of the
        /// axis around <paramref name="alongMid"/> and <paramref name="arcSize"/> metres of the circumference; the text reads along the
        /// axis when <paramref name="textAlongAxis"/> (a motor), otherwise round it (a vertical tank).
        /// </summary>
        private static void AddCurvedLabel(GrayboxMeshBuilder builder, Vector3 origin, Vector3 axis, Vector3 front, float radius, float alongMid, float alongSize,
            float arcSize, bool textAlongAxis, PlantLabel label)
        {
            axis = axis.normalized;
            front = front.normalized;
            var viewerRight = Vector3.Cross(Vector3.up, -front).normalized;
            var tangent = Vector3.Cross(axis, front).normalized;
            float alongSign = 1f;
            if (textAlongAxis)
            {
                alongSign = Vector3.Dot(axis, viewerRight) >= 0f ? 1f : -1f;
                if (Vector3.Dot(tangent, Vector3.up) < 0f) tangent = -tangent;
            }
            else if (Vector3.Dot(tangent, viewerRight) < 0f)
            {
                tangent = -tangent;
            }

            var uv = CellUv(label);
            const int steps = 8;
            float surface = radius + LabelLift;
            Vector3 Point(float s, float t, out Vector3 normal)
            {
                float arcFraction = textAlongAxis ? t : s;
                float alongFraction = textAlongAxis ? s : t;
                float angle = (arcFraction - 0.5f) * arcSize / radius;
                normal = front * Mathf.Cos(angle) + tangent * Mathf.Sin(angle);
                float along = alongMid + (alongFraction - 0.5f) * alongSize * (textAlongAxis ? alongSign : 1f);
                return origin + axis * along + normal * surface;
            }
            for (int i = 0; i < steps; i++)
            {
                for (int j = 0; j < steps; j++)
                {
                    float s0 = i / (float)steps, s1 = (i + 1) / (float)steps, t0 = j / (float)steps, t1 = (j + 1) / (float)steps;
                    var a = Point(s0, t0, out var na);
                    var b = Point(s1, t0, out var nb);
                    var c = Point(s1, t1, out var nc);
                    var d = Point(s0, t1, out var nd);
                    builder.AddPanel(a, b, c, d, na + nb + nc + nd,
                        new Vector2(Mathf.Lerp(uv.xMin, uv.xMax, s0), Mathf.Lerp(uv.yMin, uv.yMax, t0)),
                        new Vector2(Mathf.Lerp(uv.xMin, uv.xMax, s1), Mathf.Lerp(uv.yMin, uv.yMax, t0)),
                        new Vector2(Mathf.Lerp(uv.xMin, uv.xMax, s1), Mathf.Lerp(uv.yMin, uv.yMax, t1)),
                        new Vector2(Mathf.Lerp(uv.xMin, uv.xMax, s0), Mathf.Lerp(uv.yMin, uv.yMax, t1)));
                }
            }
        }

        private static Rect CellUv(PlantLabel label)
        {
            int index = (int)label;
            int column = index % 4;
            int row = index / 4;
            return new Rect(column * 0.25f + 0.004f, 1f - (row + 1) * 0.25f + 0.004f, 0.242f, 0.242f);
        }
    }
}
