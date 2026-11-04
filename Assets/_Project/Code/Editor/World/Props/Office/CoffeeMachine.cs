using System.Collections.Generic;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
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
        /// Usable: <see cref="CoffeeMachine"/> (E brews, E again drinks) with the cup on its own pivot (the centre of its base), a "Liquid" mesh
        /// in it that the component rebuilds, two thin "Stream" tubes under the spouts (pivot at their bottom, 1 m long, scaled by the component)
        /// one box collider over the whole machine. The cup rests with its handle on -X (the right-hand side seen from the front).
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
            var icons = new MeshBuilder();
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
            // Control strip on the top front: a flat lighter grey plate with five round buttons in a darker colour on two rows, each with a glowing solid icon (circle, square, triangle, pentagon, hexagon) in its middle.
            panel.AddRoundedBlock(new Vector3(0f, top, bodyFront - 0.0363f), 0.104f, 0.0535f, 0.009f, 0.002f, 0.0007f, 5);
            // Five buttons on the two middle rows: three in front (the middle one larger), two between them behind.
            var shapes = new[] { ButtonIcon.Circle, ButtonIcon.Square, ButtonIcon.Triangle, ButtonIcon.Pentagon, ButtonIcon.Hexagon };
            for (int i = 2; i <= 6; i++)
            {
                bool row = i % 2 == 0;
                float x = -0.072f + i * 0.018f;
                float z = bodyFront - (row ? 0.045f : 0.025f);
                float r = i == 4 ? 0.0095f : 0.0068f;
                var at = new Vector3(x, top + 0.002f, z);
                buttons.AddLathe(new[] { P(r, 0f, true), P(r, 0.0016f), P(r * 0.86f, 0.0025f), P(0f, 0.0029f, true) }, 20, true, at, Quaternion.identity);
                AddButtonIcon(icons, shapes[i - 2], at + Vector3.up * 0.0028f, r * 0.5f);
            }

            graphite.Attach("Body", root, SurfaceMaterial.CoffeeGraphite);
            panel.Attach("ControlPanel", root, SurfaceMaterial.CoffeePanel);
            copper.Attach("Copper", root, SurfaceMaterial.CopperTrim);
            tank.Attach("Tank", root, SurfaceMaterial.SmokedTank);
            grille.Attach("Grille", root, SurfaceMaterial.BlackHousing);
            buttons.Attach("Buttons", root, SurfaceMaterial.CoffeeButton);
            icons.Attach("ButtonIcons", root, SurfaceMaterial.LedWhite);
            // The cup stands on the grille under the spouts (their z is bodyFront + 0.05), 4 cm in front of the body and well under the column.
            // Its pivot is the centre of its base, so the component can lift and tilt it.
            var cup = ModelUtility.CreateGroup("Cup", root);
            cup.localPosition = new Vector3(0f, trayTop + 0.003f, bodyFront + 0.05f);
            // The mug model has its handle on +X; here it points to -X, which is the right-hand side for someone standing in front of the machine.
            cup.localRotation = Quaternion.Euler(0f, 180f, 0f);
            BuildMug(cup, Vector3.zero);

            // The coffee in the cup: a placeholder mesh, the component builds the real one at runtime.
            var liquid = ModelUtility.CreateGroup("Liquid", cup);
            var placeholder = new MeshBuilder();
            placeholder.AddBox(new Vector3(-0.001f, 0.007f, -0.001f), new Vector3(0.001f, 0.008f, 0.001f));
            var liquidPart = placeholder.Attach("CoffeeLiquid", liquid, SurfaceMaterial.Coffee);
            var liquidRenderer = liquidPart.GetComponent<MeshRenderer>();
            liquidRenderer.shadowCastingMode = ShadowCastingMode.Off;

            // The two streams out of the spouts, authored at the spouts' height; the component sets their height and length.
            var streams = new List<Transform>();
            foreach (var (name, x) in new[] { ("StreamLeft", -0.013f), ("StreamRight", 0.013f) })
            {
                var stream = ModelUtility.CreateGroup(name, root);
                stream.localPosition = new Vector3(x, 0.1325f, bodyFront + 0.05f);
                var tube = new MeshBuilder();
                tube.AddLathe(new[] { P(0f, 0f, true), P(0.0018f, 0f, true), P(0.0018f, 1f, true), P(0f, 1f, true) }, 8, true, Vector3.zero, Quaternion.identity);
                tube.Attach(name + "Tube", stream, SurfaceMaterial.Coffee).GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                stream.gameObject.SetActive(false);
                streams.Add(stream);
            }

            // One box over the whole machine (body, tank, buttons, dispenser, tray and cup), 1 cm roomy, so looking at any part of it offers the use.
            AddBoxCollider(root, new Vector3(0f, top / 2f, (bodyBack + trayFront) / 2f), new Vector3(2f * half + 0.02f, top + 0.01f, trayFront - bodyBack + 0.02f));

            var use = root.gameObject.AddComponent<CoffeeMachineUse>();
            var machine = root.gameObject.AddComponent<CoffeeMachine>();
            var machineSerialized = new SerializedObject(machine);
            machineSerialized.FindProperty("cup").objectReferenceValue = cup;
            machineSerialized.FindProperty("liquidFilter").objectReferenceValue = liquidPart.GetComponent<MeshFilter>();
            machineSerialized.FindProperty("liquidRenderer").objectReferenceValue = liquidRenderer;
            var streamsProperty = machineSerialized.FindProperty("streams");
            streamsProperty.arraySize = streams.Count;
            for (int i = 0; i < streams.Count; i++) streamsProperty.GetArrayElementAtIndex(i).objectReferenceValue = streams[i];
            machineSerialized.ApplyModifiedPropertiesWithoutUndo();
            var useSerialized = new SerializedObject(use);
            useSerialized.FindProperty("machine").objectReferenceValue = machine;
            useSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private enum ButtonIcon { Circle, Square, Triangle, Pentagon, Hexagon }

        /// <summary>
        /// The glowing icon in the middle of a coffee machine button, lying flat on its cap: a solid circle, square, triangle, pentagon or
        /// hexagon with rounded corners (no sharp edges). <paramref name="radius"/> is the circumradius, half the button's radius. <paramref name="pointing"/>
        /// is the angle (radians, in the XZ plane from +X towards +Z) of the first corner; a triangle points that way.
        /// </summary>
        private static void AddButtonIcon(MeshBuilder icons, ButtonIcon icon, Vector3 at, float radius, float? pointing = null)
        {
            const float height = 0.0004f;
            if (icon == ButtonIcon.Circle)
            {
                icons.AddLathe(new[] { P(radius * 0.9f, 0f, true), P(radius * 0.9f, height, true), P(0f, height, true) }, 24, true, at, Quaternion.identity);
                return;
            }
            int sides = icon == ButtonIcon.Triangle ? 3 : icon == ButtonIcon.Square ? 4 : icon == ButtonIcon.Pentagon ? 5 : 6;
            // A triangle points to the front (+Z), a square sits on its side, the others point to the front too.
            float start = pointing ?? (icon == ButtonIcon.Square ? Mathf.PI / 4f : Mathf.PI / 2f);
            icons.AddPrism(RoundedPolygon(sides, radius, radius * 0.3f, start), 0f, height, at);
        }

        /// <summary>
        /// A regular polygon (<paramref name="sides"/>, circumradius <paramref name="radius"/>) whose corners are rounded with radius
        /// <paramref name="corner"/>, counter-clockwise from above: the polygon shrunk until its rounded corners just touch the circumcircle.
        /// </summary>
        private static List<Vector2> RoundedPolygon(int sides, float radius, float corner, float start)
        {
            var outline = new List<Vector2>();
            float inner = radius - corner;
            float turn = 2f * Mathf.PI / sides;
            for (int k = 0; k < sides; k++)
            {
                float angle = start + k * turn;
                var centre = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * inner;
                for (int step = 0; step <= 5; step++)
                {
                    float a = angle - turn / 2f + turn * step / 5f;
                    outline.Add(centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * corner);
                }
            }
            return outline;
        }
    }
}
