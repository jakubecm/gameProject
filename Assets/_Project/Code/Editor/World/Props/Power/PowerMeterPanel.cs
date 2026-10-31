using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// The panel of six power meters opposite the door of the power room (load of the hall sections): large dials, the same size as the
        /// thermometers of the cooling plant, but reading the input power in kW, in two rows of three, exactly centered on a board with rounded corners
        /// (the same 10 cm margin all round, no labels) that is fed by the one electrical trunking of the power room up the wall to the ceiling
        /// (<see cref="RoomInfrastructure.Trunking"/>). The needles stand still: the panel is only a look. A wall prop.
        /// </summary>
        [Model(Prop.PowerMeterPanel)]
        private static void BuildPowerMeterPanel(Transform root)
        {
            const float dialRadius = 0.15f;
            const float margin = 0.10f;
            const float pitchX = 0.40f;
            const float pitchY = 0.42f;
            const float width = 2f * pitchX + 2f * dialRadius + 2f * margin;
            const float height = pitchY + 2f * dialRadius + 2f * margin;
            const float bottom = 1.15f;
            var board = new MeshBuilder();
            var lip = new MeshBuilder();
            board.AddExtrusion(RoundedRectLoop(-width / 2f, width / 2f, bottom, bottom + height, 0.05f, 4), Vector3.zero, new Vector3(0f, 0f, 0.04f),
                Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
            lip.AddExtrusion(RoundedRectLoop(-width / 2f + 0.012f, width / 2f - 0.012f, bottom + 0.012f, bottom + height - 0.012f, 0.04f, 4), new Vector3(0f, 0f, 0.04f), new Vector3(0f, 0f, 0.046f),
                Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
            board.Attach("Board", root, SurfaceMaterial.CabinetGrey);
            lip.Attach("BoardLip", root, SurfaceMaterial.ElectricGrey);

            var dials = new DialMeshes(SurfaceMaterial.PowerDialFace);
            float[] readings = { 0.28f, 0.34f, 0.31f, 0.40f, 0.26f, 0.36f };
            float centreY = bottom + height / 2f;
            for (int row = 0; row < 2; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    float x = (column - 1) * pitchX;
                    float y = centreY + (row == 0 ? 1f : -1f) * pitchY / 2f;
                    ModelUtility.CreateGroup($"Thermometer_{row}{column}", root).localPosition = new Vector3(x, y, 0.046f);
                    dials.AddDial(new Vector3(x, y, 0.046f), dialRadius, 0.005f, readings[row * 3 + column]);
                }
            }
            dials.Attach("Dials", root);
            RoomInfrastructure.Trunking(root, "Trunking", new Vector3(0f, bottom + height, 0f), new Vector3(0f, RoomContract.CeilingHeight, 0f), Vector3.forward);
        }
    }
}
