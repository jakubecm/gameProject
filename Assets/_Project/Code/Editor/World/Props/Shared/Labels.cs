using UnityEngine;

namespace ColdShutdown.Editor
{
    /// <summary>Stickers and name plates: quads showing one cell of the English label atlas T_PlantLabels.png.</summary>
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
        private static void AddLabel(MeshBuilder builder, Vector3 centre, Vector3 right, Vector3 up, float width, float height, PlantLabel label) =>
            AddLabelCell(builder, centre, right, up, width, height, CellUv(label));

        /// <summary>Stickers of the meet-me room's machines (cells of T_MeetLabels, 2 columns x 8 rows), 4:1.</summary>
        private enum MeetLabel
        {
            EntryPoint,
            Console,
            OdfLc,
            OdfSfp,
            Copper,
            CrossConnect,
            RectModule,
            Controller,
            Battery,
            EarthBar,
            Caution,
        }

        private static void AddLabel(MeshBuilder builder, Vector3 centre, Vector3 right, Vector3 up, float width, float height, MeetLabel label)
        {
            int index = (int)label;
            AddLabelCell(builder, centre, right, up, width, height, new Rect((index % 2) * 0.5f + 0.004f, 1f - (index / 2 + 1) * 0.125f + 0.002f, 0.492f, 0.121f));
        }

        /// <summary>What an LCD shows (cells of T_DeviceDisplays, in this order, 2 columns x 4 rows).</summary>
        private enum DeviceDisplay
        {
            Dc,
            Transformer,
            Switchgear,
            Ups,
            Exchanger,
            Pump,
            Crah,
        }

        /// <summary>A flat LCD picture facing +Z over x0..x1, y0..y1 at <paramref name="z"/>, showing <paramref name="display"/> (the viewer's right is -X, so the text runs towards -X).</summary>
        private static void AddDisplayPicture(MeshBuilder builder, float x0, float y0, float x1, float y1, float z, DeviceDisplay display)
        {
            int index = (int)display;
            float u0 = (index % 2) * 0.5f;
            float v0 = 1f - (index / 2 + 1) * 0.25f;
            builder.AddPanel(new Vector3(x0, y0, z), new Vector3(x1, y0, z), new Vector3(x1, y1, z), new Vector3(x0, y1, z), Vector3.forward,
                new Vector2(u0 + 0.5f, v0), new Vector2(u0, v0), new Vector2(u0, v0 + 0.25f), new Vector2(u0 + 0.5f, v0 + 0.25f));
        }

        private static void AddLabelCell(MeshBuilder builder, Vector3 centre, Vector3 right, Vector3 up, float width, float height, Rect uv)
        {
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
