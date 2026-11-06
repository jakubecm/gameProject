using UnityEngine;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Copper patch rack 0.8 x 0.6 x 2.2 m: the same open frame and finger managers as the optical frame, 22 Cat6A patch panels of 24 real RJ45 jacks each, alternating
        /// with horizontal cord managers. Every sixth jack holds a real plug (clear housing, latch lever, ribbed boot); its blue or white cord bends sideways with a realistic
        /// radius into the bundle in front of the manager and climbs in it, in its own slot, up to the tray. A unit plate on the top ring. The front faces +Z.
        /// </summary>
        [Model(Prop.CopperPatchRack)]
        private static void BuildCopperPatchRack(Transform root)
        {
            var p = new RackParts();
            BuildMeetFrame(p);
            var bundles = NewBundles(p, FibreTrayBottom, 0.0075f, 0.0035f);
            foreach (int side in new[] { -1, 1 }) BuildFingerManager(p, side);
            for (int slot = 0; slot < FibreUnits; slot++)
            {
                if (slot % 2 == 0) AddCopperPanel(p, slot, bundles);
                else AddHorizontalManager(p, slot, true);
            }
            FinishBundles(p, bundles);
            var labels = new MeshBuilder();
            AddLabel(labels, new Vector3(0f, MeetRackHeight - 0.02f, MeetRackDepth / 2f), Vector3.left, Vector3.up, 0.14f, 0.035f, MeetLabel.Copper);

            p.Attach("Frame", root);
            labels.Attach("Labels", root, SurfaceMaterial.MeetLabels);
            AddBoxCollider(root, new Vector3(0f, MeetRackHeight / 2f, 0.01f), new Vector3(MeetRackWidth, MeetRackHeight, 0.62f));
        }
    }
}
