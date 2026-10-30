using UnityEngine;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// A label wrapped round a cylinder of <paramref name="radius"/> whose axis runs through <paramref name="origin"/> along
        /// <paramref name="axis"/>, on the side that faces <paramref name="front"/>. It covers <paramref name="alongSize"/> metres of the
        /// axis around <paramref name="alongMid"/> and <paramref name="arcSize"/> metres of the circumference; the text reads along the
        /// axis when <paramref name="textAlongAxis"/> (a motor), otherwise round it (a vertical tank).
        /// </summary>
        private static void AddCurvedLabel(MeshBuilder builder, Vector3 origin, Vector3 axis, Vector3 front, float radius, float alongMid, float alongSize,
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
    }
}
