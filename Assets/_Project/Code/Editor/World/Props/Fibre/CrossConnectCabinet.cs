using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Cross-connect cabinet 0.8 x 0.6 x 2.2 m: the optical frame closed in black side panels and a roof (open only where the two bundles leave), behind a
        /// glass door (fills the rack's opening, set back into a square-cornered frame, rounded edges; no hinges, lever lock centred on the right stile). Through the glass: 22 LC patch panels with green status LEDs over the adapters (a few amber), real duplex LC plugs,
        /// duplex jumpers (two smoothly bent cords each, bulging forward) between every second panel and a shifted port of the panel two slots above it, and the uplink cords of
        /// the two outer ports of every panel in the two bundles with their straps. The front faces +Z.
        /// </summary>
        [Model(Prop.CrossConnectCabinet)]
        private static void BuildCrossConnectCabinet(Transform root)
        {
            const float half = MeetRackWidth / 2f;
            const float d = MeetRackDepth / 2f;
            float top = MeetRackHeight - 0.04f;
            var p = new RackParts();
            BuildMeetFrame(p);
            var bundles = NewBundles(p, FibreTrayBottom, 0.0046f, 0.0021f);
            foreach (int side in new[] { -1, 1 }) BuildFingerManager(p, side);
            for (int slot = 0; slot < FibreUnits; slot++)
            {
                if (slot % 2 == 0) AddCrossConnectPanel(p, slot, bundles);
                else AddHorizontalManager(p, slot, false);
            }
            FinishBundles(p, bundles);

            // Side panels and the roof; the roof stays open over the two bundles (x 0.282-0.37, z 0.19-0.3).
            var m = SurfaceMaterial.RackBlack;
            p.Box(m, -half, MeetPlinth, -d, -half + 0.012f, top, d);
            p.Box(m, half - 0.012f, MeetPlinth, -d, half, top, d);
            p.Box(m, -0.282f, top, -d + 0.03f, 0.282f, MeetRackHeight, d - 0.03f);
            foreach (int s in new[] { -1, 1 })
            {
                float x0 = s < 0 ? -0.37f : 0.282f;
                p.Box(m, x0, top, -d + 0.03f, x0 + 0.088f, MeetRackHeight, 0.19f);
            }

            // The glass door fills the whole opening between the rack's posts, plinth and top bar: a graphite leaf with rounded corners (stiles, rails, pane; the right stile wider, for the
            // lock) set 5 mm back into a frame with square corners like the open racks', 3 mm clear all round; no hinges, lever lock centred on the free width of the right stile.
            const float face = MeetRackDepth / 2f;
            const float seam = 0.003f;
            const float leafX = 0.367f;
            const float leafY0 = 0.078f;
            const float leafY1 = 2.142f;
            const float paneX0 = -0.327f;
            const float paneX1 = 0.300f;
            const float leafBack = face - 0.020f;
            const float leafFront = face - 0.005f;
            AddDoorSurround(p[m], -half, half, MeetPlinth, top, -leafX - seam, leafX + seam, leafY0 - seam, leafY1 + seam, leafBack, face, 0f);
            var leaf = p[SurfaceMaterial.RackDoor];
            AddDoorStiles(leaf, -leafX, leafX, leafY0, leafY1, paneX0, paneX1, leafBack, leafFront, 0.006f);
            p.Box(SurfaceMaterial.RackDoor, paneX0, leafY0, leafBack, paneX1, 0.12f, leafFront);
            p.Box(SurfaceMaterial.RackDoor, paneX0, 2.10f, leafBack, paneX1, leafY1, leafFront);
            p.Box(SurfaceMaterial.ClearGlass, paneX0, 0.12f, face - 0.0125f, paneX1, 2.10f, face - 0.0105f);
            AddLeverLock(p[SurfaceMaterial.BlackHousing], p[SurfaceMaterial.Chrome], (paneX1 + leafX) / 2f, (leafY0 + leafY1) / 2f, 0.8f, leafFront);

            var labels = new MeshBuilder();
            AddLabel(labels, new Vector3(0f, MeetRackHeight - 0.02f, face), Vector3.left, Vector3.up, 0.14f, 0.035f, MeetLabel.CrossConnect);

            p.Attach("Frame", root);
            labels.Attach("Labels", root, SurfaceMaterial.MeetLabels);
            AddBoxCollider(root, new Vector3(0f, MeetRackHeight / 2f, 0.01f), new Vector3(MeetRackWidth, MeetRackHeight, 0.62f));
        }
    }
}
