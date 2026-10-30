using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Two-door steel clothes locker 0.6 x 1.8 x 0.5 m (two 0.3 m compartments, product photo): a light grey carcass on a
        /// recessed plinth, two slate-blue doors set 4 mm back in the frame with soft edges, a name plate slot and five slanted
        /// vent slots above and below, and a white cylinder lock by the outer edge of each door. The doors face +Z.
        /// </summary>
        [Model(Prop.Locker)]
        private static void BuildLocker(Transform root)
        {
            const float width = 0.6f;
            const float height = 1.8f;
            const float depth = 0.5f;
            const float plinth = 0.07f;
            const float frame = 0.025f;
            const float gap = 0.002f;
            float front = depth / 2f;
            float doorFront = front - 0.004f;
            float doorBack = front - 0.022f;
            float doorBottom = plinth + frame;
            float doorTop = height - frame;
            float doorWidth = width / 2f - frame - frame / 2f;

            var body = new MeshBuilder();
            body.AddBox(new Vector3(-width / 2f + 0.01f, 0f, -front + 0.01f), new Vector3(width / 2f - 0.01f, plinth, front - 0.02f));
            body.AddBox(new Vector3(-width / 2f, plinth, -front), new Vector3(-width / 2f + frame, height, front));
            body.AddBox(new Vector3(width / 2f - frame, plinth, -front), new Vector3(width / 2f, height, front));
            body.AddBox(new Vector3(-width / 2f + frame, doorTop, -front), new Vector3(width / 2f - frame, height, front));
            body.AddBox(new Vector3(-width / 2f + frame, plinth, -front), new Vector3(width / 2f - frame, doorBottom, front));
            body.AddBox(new Vector3(-frame / 2f, doorBottom, -front), new Vector3(frame / 2f, doorTop, front));
            body.AddBox(new Vector3(-width / 2f + frame, doorBottom, -front), new Vector3(width / 2f - frame, doorTop, -front + 0.01f));
            body.Attach("Body", root, SurfaceMaterial.LockerBody);

            var doors = new MeshBuilder();
            var plates = new MeshBuilder();
            var vents = new MeshBuilder();
            var locks = new MeshBuilder();
            var keyholes = new MeshBuilder();
            foreach (int side in new[] { -1, 1 })
            {
                float inner = side * (frame / 2f + gap);
                float outer = side * (width / 2f - frame - gap);
                float left = Mathf.Min(inner, outer);
                float right = Mathf.Max(inner, outer);
                float centre = (left + right) / 2f;
                doors.AddExtrusion(MeshBuilder.RoundedRectLoop(left, right, doorBottom + gap, doorTop - gap, 0.004f, 2),
                    new Vector3(0f, 0f, doorBack), new Vector3(0f, 0f, doorFront), Vector3.right, Vector3.up, Vector3.back, Vector3.forward);

                float faceZ = doorFront + 0.0008f;
                plates.AddPanel(new Vector3(centre - 0.035f, 1.64f, faceZ), new Vector3(centre + 0.035f, 1.64f, faceZ),
                    new Vector3(centre + 0.035f, 1.66f, faceZ), new Vector3(centre - 0.035f, 1.66f, faceZ), Vector3.forward,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up);

                foreach (float y in new[] { 1.715f, 0.135f })
                {
                    for (int i = 0; i < 5; i++)
                    {
                        float x = centre + (i - 2) * 0.0155f;
                        var slant = new Vector3(0.0045f, 0.0105f, 0f);
                        var half = new Vector3(0.0012f, -0.0005f, 0f);
                        var c = new Vector3(x, y, faceZ);
                        vents.AddPanel(c - slant - half, c - slant + half, c + slant + half, c + slant - half, Vector3.forward,
                            Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
                    }
                }

                var lockCentre = new Vector3(left + 0.045f, 0.88f, doorFront);
                locks.AddExtrusion(MeshBuilder.RoundedRectLoop(-0.009f, 0.009f, -0.014f, 0.014f, 0.004f, 3),
                    lockCentre, lockCentre + new Vector3(0f, 0f, 0.012f), Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
                var keyFace = lockCentre + new Vector3(0f, 0f, 0.0128f);
                keyholes.AddPanel(keyFace + new Vector3(-0.0012f, -0.005f, 0f), keyFace + new Vector3(0.0012f, -0.005f, 0f),
                    keyFace + new Vector3(0.0012f, 0.005f, 0f), keyFace + new Vector3(-0.0012f, 0.005f, 0f), Vector3.forward,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            }
            doors.Attach("Doors", root, SurfaceMaterial.LockerDoor);
            plates.Attach("NamePlates", root, SurfaceMaterial.LockerPlate);
            vents.Attach("VentSlots", root, SurfaceMaterial.BlackHousing);
            locks.Attach("Locks", root, SurfaceMaterial.DetectorWhite);
            keyholes.Attach("Keyholes", root, SurfaceMaterial.BlackHousing);
            AddBoxCollider(root, new Vector3(0f, height / 2f, 0f), new Vector3(width, height, depth));
        }
    }
}
