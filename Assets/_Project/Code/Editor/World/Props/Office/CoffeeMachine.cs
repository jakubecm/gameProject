using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Bean-to-cup coffee machine after product photos, 0.19 x 0.34 x 0.48 m: a slim matt graphite body whose water tank is built
        /// into its upper rear (clear, flush with the body's sides and top, nothing sticks out), a lighter grey control strip, only as big as its round
        /// buttons, on the top front, a squared dispenser column on the front in the body's colour with a copper band at its foot and
        /// two spouts, and a drip tray with a black grille reaching forward from the foot. The espresso cup stands on the grille
        /// under the spouts, clear of the machine. A desk-top prop, its front faces +Z.
        /// </summary>
        [Model(Prop.CoffeeMachine)]
        private static void BuildCoffeeMachine(Transform root)
        {
            const float half = 0.095f;
            const float bodyBack = -0.20f;
            const float bodyFront = 0.15f;
            const float trayFront = 0.28f;
            const float trayTop = 0.032f;
            const float shoulder = 0.20f;
            const float top = 0.335f;
            const float tankFront = -0.03f;
            var graphite = new MeshBuilder();
            var copper = new MeshBuilder();
            var tank = new MeshBuilder();
            var grille = new MeshBuilder();
            var buttons = new MeshBuilder();
            var panel = new MeshBuilder();

            // Body: the full-depth lower block, and over it the upper block in front of the tank (the tank itself fills the upper rear).
            graphite.AddRoundedBlock(new Vector3(0f, 0.012f, (bodyBack + bodyFront) / 2f), 2f * half, bodyFront - bodyBack, 0.015f, shoulder - 0.012f, 0.004f, 5);
            graphite.AddRoundedBlock(new Vector3(0f, shoulder, (tankFront + bodyFront) / 2f), 2f * half, bodyFront - tankFront, 0.015f, top - shoulder, 0.006f, 5);
            tank.AddRoundedBlock(new Vector3(0f, shoulder, (bodyBack + tankFront) / 2f), 2f * half, tankFront - bodyBack, 0.015f, top - shoulder, 0.006f, 5);
            // Drip tray: a shallow tray out of the foot with a raised black grille of slats.
            graphite.AddRoundedBlock(new Vector3(0f, 0f, (bodyFront + trayFront) / 2f - 0.005f), 0.17f, trayFront - bodyFront + 0.01f, 0.008f, trayTop, 0.003f, 4);
            for (int i = 0; i < 8; i++)
            {
                float z = bodyFront + 0.012f + i * 0.0145f;
                grille.AddBox(new Vector3(-0.072f, trayTop, z), new Vector3(0.072f, trayTop + 0.003f, z + 0.007f));
            }
            // Dispenser column, all in the body's colour except the copper band at its foot (it stays below the top, no coplanar faces).
            graphite.AddRoundedBlock(new Vector3(0f, 0.14f, bodyFront + 0.0425f), 0.075f, 0.085f, 0.006f, 0.115f, 0.003f, 3);
            copper.AddRoundedBlock(new Vector3(0f, 0.14f, bodyFront + 0.0425f), 0.077f, 0.087f, 0.006f, 0.04f, 0.002f, 3);
            // The underside with the spouts is in the body's colour: a graphite plate under the copper band, just inside its outline.
            graphite.AddRoundedBlock(new Vector3(0f, 0.1385f, bodyFront + 0.0425f), 0.0764f, 0.0864f, 0.006f, 0.0015f, 0.0005f, 3);
            foreach (float x in new[] { -0.013f, 0.013f })
            {
                grille.AddLathe(new[] { P(0f, 0f, true), P(0.0032f, 0f, true), P(0.0032f, 0.006f, true), P(0f, 0.006f, true) }, 10, true, new Vector3(x, 0.1325f, bodyFront + 0.05f), Quaternion.identity);
            }
            // Control strip on the top front: a flat lighter grey plate with two rows of round buttons (the middle one is the power button).
            panel.AddRoundedBlock(new Vector3(0f, top, bodyFront - 0.0363f), 0.178f, 0.0535f, 0.009f, 0.002f, 0.0007f, 5);
            for (int i = 0; i < 9; i++)
            {
                bool row = i % 2 == 0;
                float x = -0.072f + i * 0.018f;
                float z = bodyFront - (row ? 0.045f : 0.025f);
                float r = i == 4 ? 0.0095f : 0.0068f;
                buttons.AddLathe(new[] { P(0f, 0f, true), P(r, 0f, true), P(r, 0.0018f, true), P(0f, 0.0018f, true) }, 18, true, new Vector3(x, top + 0.002f, z), Quaternion.identity);
            }

            graphite.Attach("Body", root, SurfaceMaterial.CoffeeGraphite);
            panel.Attach("ControlPanel", root, SurfaceMaterial.CoffeePanel);
            copper.Attach("Copper", root, SurfaceMaterial.CopperTrim);
            tank.Attach("Tank", root, SurfaceMaterial.SmokedTank);
            grille.Attach("Grille", root, SurfaceMaterial.BlackHousing);
            buttons.Attach("Buttons", root, SurfaceMaterial.Chrome);
            // The cup stands on the grille under the spouts (their z is bodyFront + 0.05), 4 cm in front of the body and well under the column.
            BuildMug(ModelUtility.CreateGroup("Cup", root), new Vector3(0f, trayTop + 0.003f, bodyFront + 0.05f));
        }
    }
}
