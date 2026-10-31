using System.Collections.Generic;
using UnityEngine;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        public const float ShelfWidth = 1.0f;

        public const float ShelfDepth = 0.4f;

        public const float ShelfHeight = 1.8f;

        /// <summary>
        /// Boltless galvanised shelving 1.0 x 1.8 x 0.4 m after a product photo: four slotted angle posts (slots every 50 mm, black
        /// rubber feet), four chipboard boards each held in a C-shaped steel edge channel on all four sides and braced underneath by
        /// two hat sections. The open side faces +Z.
        /// </summary>
        [Model(Prop.Shelf)]
        private static void BuildShelf(Transform root)
        {
            const float halfX = ShelfWidth / 2f;
            const float halfZ = ShelfDepth / 2f;
            const float sheet = 0.0025f;
            const float flange = 0.038f;
            const float foot = 0.012f;
            const float round = 0.0008f;

            var steel = new MeshBuilder();
            var slots = new MeshBuilder();
            var feet = new MeshBuilder();
            var boards = new MeshBuilder();

            // Angle posts: one flange across the front (or back), one across the side, the corner outside; slots on both.
            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    float cx = sx * (halfX - flange / 2f);
                    float cz = sz * (halfZ - flange / 2f);
                    var start = new Vector3(0f, foot, 0f);
                    var end = new Vector3(0f, ShelfHeight, 0f);
                    float z0 = sz > 0 ? halfZ - sheet : -halfZ;
                    float x0 = sx > 0 ? halfX - sheet : -halfX;
                    steel.AddExtrusion(MeshBuilder.RoundedRectLoop(cx - flange / 2f, cx + flange / 2f, z0, z0 + sheet, round, 1),
                        start, end, Vector3.right, Vector3.forward, Vector3.down, Vector3.up);
                    steel.AddExtrusion(MeshBuilder.RoundedRectLoop(x0, x0 + sheet, cz - flange / 2f, cz + flange / 2f, round, 1),
                        start, end, Vector3.right, Vector3.forward, Vector3.down, Vector3.up);

                    for (float y = 0.05f; y < ShelfHeight - 0.04f; y += 0.05f)
                    {
                        const float slotWidth = 0.005f;
                        const float slotHeight = 0.012f;
                        const float slotDepth = 0.0004f;
                        float faceZ = sz * halfZ;
                        float faceX = sx * halfX;
                        slots.AddBox(new Vector3(cx - slotWidth / 2f, y - slotHeight / 2f, Mathf.Min(faceZ, faceZ + sz * slotDepth)),
                            new Vector3(cx + slotWidth / 2f, y + slotHeight / 2f, Mathf.Max(faceZ, faceZ + sz * slotDepth)), false, false);
                        slots.AddBox(new Vector3(Mathf.Min(faceX, faceX + sx * slotDepth), y - slotHeight / 2f, cz - slotWidth / 2f),
                            new Vector3(Mathf.Max(faceX, faceX + sx * slotDepth), y + slotHeight / 2f, cz + slotWidth / 2f), false, false);
                    }
                    feet.AddRoundedBlock(new Vector3(cx, 0f, cz), flange, flange, 0.008f, foot, 0.003f);
                }
            }

            // Boards: chipboard in a C channel (web, bottom flange, upper lip) on every side, two hat sections underneath.
            const float channelBottom = 0.018f;
            const float channelTop = 0.022f;
            const float flangeWidth = 0.026f;
            const float lipWidth = 0.012f;
            const float boardThickness = 0.016f;
            float railHalfX = halfX - 2f * sheet;
            float railHalfZ = halfZ - 2f * sheet;
            foreach (float top in ShelfBoardTops)
            {
                float y0 = top - channelBottom;
                float y1 = top + channelTop;
                // Front and back channels run along X, the side ones along Z. `outer` is the face of the web away from the middle.
                foreach (int s in new[] { -1, 1 })
                {
                    float outerZ = s * (halfZ - sheet);
                    steel.AddExtrusion(MeshBuilder.RoundedRectLoop(Mathf.Min(outerZ, outerZ - s * sheet), Mathf.Max(outerZ, outerZ - s * sheet), y0, y1, round, 1),
                        new Vector3(-railHalfX, 0f, 0f), new Vector3(railHalfX, 0f, 0f), Vector3.forward, Vector3.up, Vector3.left, Vector3.right);
                    steel.AddExtrusion(MeshBuilder.RoundedRectLoop(Mathf.Min(outerZ, outerZ - s * flangeWidth), Mathf.Max(outerZ, outerZ - s * flangeWidth), y0, y0 + sheet, round, 1),
                        new Vector3(-railHalfX, 0f, 0f), new Vector3(railHalfX, 0f, 0f), Vector3.forward, Vector3.up, Vector3.left, Vector3.right);
                    steel.AddExtrusion(MeshBuilder.RoundedRectLoop(Mathf.Min(outerZ, outerZ - s * lipWidth), Mathf.Max(outerZ, outerZ - s * lipWidth), y1 - sheet, y1, round, 1),
                        new Vector3(-railHalfX, 0f, 0f), new Vector3(railHalfX, 0f, 0f), Vector3.forward, Vector3.up, Vector3.left, Vector3.right);

                    float outerX = s * (halfX - sheet);
                    steel.AddExtrusion(MeshBuilder.RoundedRectLoop(Mathf.Min(outerX, outerX - s * sheet), Mathf.Max(outerX, outerX - s * sheet), y0, y1, round, 1),
                        new Vector3(0f, 0f, -railHalfZ), new Vector3(0f, 0f, railHalfZ), Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
                    steel.AddExtrusion(MeshBuilder.RoundedRectLoop(Mathf.Min(outerX, outerX - s * flangeWidth), Mathf.Max(outerX, outerX - s * flangeWidth), y0, y0 + sheet, round, 1),
                        new Vector3(0f, 0f, -railHalfZ), new Vector3(0f, 0f, railHalfZ), Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
                    steel.AddExtrusion(MeshBuilder.RoundedRectLoop(Mathf.Min(outerX, outerX - s * lipWidth), Mathf.Max(outerX, outerX - s * lipWidth), y1 - sheet, y1, round, 1),
                        new Vector3(0f, 0f, -railHalfZ), new Vector3(0f, 0f, railHalfZ), Vector3.right, Vector3.up, Vector3.back, Vector3.forward);

                    // Hat section under the board: a bottom plate and two upright sides.
                    float bx = s * 0.22f;
                    float zEnd = halfZ - 2f * sheet;
                    steel.AddBox(new Vector3(bx - 0.014f, y0 - 0.030f, -zEnd), new Vector3(bx + 0.014f, y0 - 0.030f + sheet, zEnd));
                    steel.AddBox(new Vector3(bx - 0.014f, y0 - 0.030f, -zEnd), new Vector3(bx - 0.014f + sheet, y0, zEnd));
                    steel.AddBox(new Vector3(bx + 0.014f - sheet, y0 - 0.030f, -zEnd), new Vector3(bx + 0.014f, y0, zEnd));
                }
                boards.AddRoundedBlock(new Vector3(0f, top - boardThickness, 0f), 2f * (railHalfX - 0.002f), 2f * (railHalfZ - 0.002f), 0.003f, boardThickness, 0.0015f);
            }

            steel.Attach("Steel", root, SurfaceMaterial.GalvanisedSteel);
            slots.Attach("Slots", root, SurfaceMaterial.BlackHousing);
            feet.Attach("Feet", root, SurfaceMaterial.BlackHousing);
            boards.Attach("Boards", root, SurfaceMaterial.ShelfBoard);
            AddBoxCollider(root, new Vector3(0f, ShelfHeight / 2f, 0f), new Vector3(ShelfWidth, ShelfHeight, ShelfDepth));
        }
    }
}
