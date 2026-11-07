using UnityEngine;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Optical distribution frame (ODF) 0.8 x 0.6 x 2.2 m: an open black 19 inch frame with a finger manager either side, 22 panel units (6 of them 48-port SFP+ panels,
        /// the rest 36-port duplex LC panels) alternating with horizontal cord managers. Every plugged connector is a real duplex LC plug (housings, clip, latch levers, boots);
        /// its two cords bend sideways with a realistic radius into the bundle in front of the manager and climb in it, each in its own slot, up to the tray. A unit plate on
        /// the top ring. The front faces +Z.
        /// </summary>
        [Model(Prop.OdfRack)]
        private static void BuildOdfRack(Transform root) => BuildOdfFrame(root, 28, 7, 5, MeetLabel.OdfLc);

        /// <summary>The optical frame of the same size, nearly all of it SFP+ panels (16 of 22 units; the rest LC), plugged more sparsely, so its cords look different.</summary>
        [Model(Prop.OdfRackSfp)]
        private static void BuildOdfRackSfp(Transform root) => BuildOdfFrame(root, 75, 9, 9, MeetLabel.OdfSfp);

        private static void BuildOdfFrame(Transform root, int sfpPercent, int lcEvery, int sfpEvery, MeetLabel label)
        {
            var p = new RackParts();
            BuildMeetFrame(p);
            var bundles = NewBundles(p, FibreTrayBottom, 0.0046f, 0.0021f);
            foreach (int side in new[] { -1, 1 }) BuildFingerManager(p, side);
            for (int slot = 0; slot < FibreUnits; slot++)
            {
                if (slot % 2 != 0)
                {
                    AddHorizontalManager(p, slot, true);
                    continue;
                }
                if ((slot * 37 + 11) % 100 < sfpPercent) AddSfpPanel(p, slot, bundles, sfpEvery);
                else AddFibrePanel(p, slot, bundles, lcEvery);
            }
            FinishBundles(p, bundles);
            var labels = new MeshBuilder();
            AddLabel(labels, new Vector3(0f, MeetRackHeight - 0.02f, MeetRackDepth / 2f), Vector3.left, Vector3.up, 0.14f, 0.035f, label);

            p.Attach("Frame", root);
            labels.Attach("Labels", root, SurfaceMaterial.MeetLabels);
            AddBoxCollider(root, new Vector3(0f, MeetRackHeight / 2f, 0.01f), new Vector3(MeetRackWidth, MeetRackHeight, 0.62f));
        }
    }
}
