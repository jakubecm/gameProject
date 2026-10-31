using System.Collections.Generic;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEngine;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Automatic sliding door at the end of a contained cold aisle: ONE leaf, which always slides to the LEFT as seen from outside (local +X, over the end of
        /// the left-hand rack row) so the RFID reader can sit on the right, on the end of the right-hand row, as far from the door's edge as the reader of a room door
        /// is (<see cref="ReaderEdgeGap"/>). The leaf is built exactly like a room door's (frame, one clear pane, <see cref="BuildLeaves"/>) but in the aisle's black. It is the
        /// full width of the aisle plus 2 cm overlap each side and touches the rack ends (no gap), so a closed door shuts the whole gap between the two rack rows, from the
        /// floor (no floor rail at all) to the header, whose top is flush with the containment roof; a jamb on the right closes the gap there.
        /// Pivot: the middle of the aisle end on the floor, +Z points out of the aisle. A separate prop and type (<see cref="AisleDoor"/>) from the room doors.
        /// </summary>
        [Model(Prop.AisleDoor)]
        private static void BuildAisleDoor(Transform root)
        {
            float aisleHalf = HallContract.AisleWidth / 2f;
            float top = HallContract.ContainmentHeight + HallContract.ContainmentPanel;
            const float offset = 0.002f;
            const float travel = 1.1f;
            float leafWidth = HallContract.AisleWidth + 0.04f;
            float headerBottom = top - 0.11f;
            var black = new MeshBuilder();

            // The jamb on the right (-X) beside the leaf, and the header over the opening and the track to the left, where the leaf parks.
            black.AddBox(new Vector3(-aisleHalf, 0f, 0f), new Vector3(-aisleHalf + 0.03f, top, offset));
            black.AddBox(new Vector3(-aisleHalf, headerBottom, 0f), new Vector3(aisleHalf + travel + 0.06f, top, offset + 0.07f));
            black.Attach("Surround", root, SurfaceMaterial.RackBlack);

            var leaf = BuildLeaves(root, leafWidth, headerBottom - 0.003f, offset, true, SurfaceMaterial.RackBlack)[0];
            // The reader on the end of the right-hand rack row, the same distance from the door's edge as at a room door; the leaf slides away from it.
            var led = BuildReader(root, "Reader", new Vector3(-(aisleHalf + ReaderEdgeGap), ReaderHeight, 0f), 0f);
            ConfigureDoor<AisleDoor>(root, new[] { null, leaf }, travel, new[] { led }, new Vector3(0f, 1f, 0.5f), new Vector3(1.6f, 2f, 3.2f));
        }

        /// <summary>Distance from the edge of a doorway to the centre of its reader (a room door's outer reader: <see cref="OuterReaderAlongWall"/> from the middle, the opening 1.2 m either side).</summary>
        public const float ReaderEdgeGap = OuterReaderAlongWall - RoomContract.OpeningWidth / 2f;
    }
}
