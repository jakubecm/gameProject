using UnityEngine;

namespace ColdShutdown.Editor
{
    /// <summary>Stickers and name plates: quads showing one cell of the Czech label atlas T_PlantLabels.png.</summary>
    public static partial class Props
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
        private static void AddLabel(MeshBuilder builder, Vector3 centre, Vector3 right, Vector3 up, float width, float height, PlantLabel label)
        {
            var uv = CellUv(label);
            var r = right.normalized * width / 2f;
            var u = up.normalized * height / 2f;
            var lifted = centre + Vector3.Cross(up, right).normalized * LabelLift;
            builder.AddPanel(lifted - r - u, lifted + r - u, lifted + r + u, lifted - r + u, Vector3.Cross(up, right),
                new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMax, uv.yMin), new Vector2(uv.xMax, uv.yMax), new Vector2(uv.xMin, uv.yMax));
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
