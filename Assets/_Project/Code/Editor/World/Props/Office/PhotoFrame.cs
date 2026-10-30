using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Photo frame 15 x 20 cm on a desk (photo-based): a black mitred moulding with a rounded outer edge round a white mount,
        /// the picture set back behind a pane of glass, a thin back panel, and a slim flat leg on a hinge on the back that props
        /// it up leaning back 12 degrees. The picture faces +Z; the pivot is on the desk under the frame's back edge.
        /// </summary>
        [Model(Prop.PhotoFrame)]
        private static void BuildPhotoFrame(Transform root)
        {
            const float width = 0.15f;
            const float height = 0.2f;
            const float depth = 0.016f;
            const float border = 0.018f;
            const float lean = 12f;
            var frame = ModelUtility.CreateGroup("Frame", root);
            frame.localRotation = Quaternion.Euler(-lean, 0f, 0f);

            var moulding = new MeshBuilder();
            var corners = new[]
            {
                new Vector3(-width / 2f + border / 2f, border / 2f, 0f), new Vector3(width / 2f - border / 2f, border / 2f, 0f),
                new Vector3(width / 2f - border / 2f, height - border / 2f, 0f), new Vector3(-width / 2f + border / 2f, height - border / 2f, 0f),
            };
            // Bars run round the frame counter-clockwise seen from the front, mitred at every corner; the profile's u points
            // outwards, v towards the viewer: a rounded outer edge and a sharp inner one.
            var section = MeshBuilder.RoundedRectLoop(-border / 2f, border / 2f, 0f, depth, new[] { 0f, 0f, 0f, 0.005f }, 3);
            for (int i = 0; i < 4; i++)
            {
                var from = corners[i];
                var to = corners[(i + 1) % 4];
                var direction = (to - from).normalized;
                var before = (from - corners[(i + 3) % 4]).normalized;
                var after = (corners[(i + 2) % 4] - to).normalized;
                var outwards = new Vector3(direction.y, -direction.x, 0f);
                moulding.AddExtrusion(section, from, to, -outwards, Vector3.forward, -(direction + before).normalized, (direction + after).normalized);
            }
            moulding.Attach("Moulding", frame, SurfaceMaterial.BlackHousing);

            float inner = border;
            var back = new MeshBuilder();
            // The back panel is set into the moulding, 2 mm behind its back face, and its edges lie hidden inside the moulding.
            const float backInset = 0.002f;
            back.AddBox(new Vector3(-width / 2f + border - 0.004f, border - 0.004f, backInset), new Vector3(width / 2f - border + 0.004f, height - border + 0.004f, backInset + 0.003f));
            back.Attach("Back", frame, SurfaceMaterial.Furniture);
            float mountZ = 0.007f;
            float margin = inner + 0.012f;
            float pictureZ = mountZ - 0.0015f;
            var picture = new MeshBuilder();
            picture.AddPanel(new Vector3(-width / 2f + margin, margin, pictureZ), new Vector3(width / 2f - margin, margin, pictureZ),
                new Vector3(width / 2f - margin, height - margin, pictureZ), new Vector3(-width / 2f + margin, height - margin, pictureZ), Vector3.forward,
                Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            picture.Attach("Picture", frame, SurfaceMaterial.PhotoPicture);
            // The white mount is four strips round the picture, which sits a millimetre and a half behind its opening.
            var mountFrame = new MeshBuilder();
            float m0 = inner - 0.003f;
            float m1 = margin;
            foreach (var strip in new[]
            {
                (new Vector2(-width / 2f + m0, m0), new Vector2(width / 2f - m0, m1)),
                (new Vector2(-width / 2f + m0, height - m1), new Vector2(width / 2f - m0, height - m0)),
                (new Vector2(-width / 2f + m0, m1), new Vector2(-width / 2f + m1, height - m1)),
                (new Vector2(width / 2f - m1, m1), new Vector2(width / 2f - m0, height - m1)),
            })
            {
                mountFrame.AddPanel(new Vector3(strip.Item1.x, strip.Item1.y, mountZ), new Vector3(strip.Item2.x, strip.Item1.y, mountZ),
                    new Vector3(strip.Item2.x, strip.Item2.y, mountZ), new Vector3(strip.Item1.x, strip.Item2.y, mountZ), Vector3.forward,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            }
            mountFrame.Attach("Mount", frame, SurfaceMaterial.Whiteboard);
            var glass = new MeshBuilder();
            float glassZ = depth - 0.003f;
            glass.AddPanel(new Vector3(-width / 2f + border - 0.003f, border - 0.003f, glassZ), new Vector3(width / 2f - border + 0.003f, border - 0.003f, glassZ),
                new Vector3(width / 2f - border + 0.003f, height - border + 0.003f, glassZ), new Vector3(-width / 2f + border - 0.003f, height - border + 0.003f, glassZ), Vector3.forward,
                Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            glass.Attach("Glass", frame, SurfaceMaterial.DomeClear);

            // Hinge on the back panel near the top and the flat leg, in the desk's frame: the leg runs down and back to the desk.
            float c = Mathf.Cos(lean * Mathf.Deg2Rad);
            float sn = Mathf.Sin(lean * Mathf.Deg2Rad);
            float hingeHeight = 0.085f;
            var hinge = new Vector3(0f, hingeHeight * c - 0.0022f * sn, -hingeHeight * sn - 0.0022f * c);
            // The foot stays under the frame: no further back than the top edge of the leaning frame seen from above.
            float footZ = -height * sn + 0.004f;
            var legDirection = new Vector3(0f, -hinge.y, footZ - hinge.z).normalized;
            var foot = hinge + legDirection * (hinge.y / -legDirection.y);
            var steel = new MeshBuilder();
            var legSection = MeshBuilder.RoundedRectLoop(-0.0125f, 0.0125f, -0.001f, 0.001f, 0.0005f, 1);
            var legUp = Vector3.Cross(legDirection, Vector3.right).normalized;
            steel.AddExtrusion(legSection, hinge, foot, Vector3.right, legUp, -legDirection, legDirection);
            steel.AddTube(new[] { hinge + Vector3.left * 0.016f, hinge + Vector3.right * 0.016f }, 0.003f, 10, Vector3.zero, Quaternion.identity);
            steel.AddDisc(hinge + Vector3.left * 0.016f, Vector3.left, 0.003f, 10);
            steel.AddDisc(hinge + Vector3.right * 0.016f, Vector3.right, 0.003f, 10);
            steel.Attach("Leg", root, SurfaceMaterial.Chrome);
            var plate = new MeshBuilder();
            var backNormal = new Vector3(0f, -sn, -c);
            var up = new Vector3(0f, c, -sn);
            var plateCentre = new Vector3(0f, hingeHeight * c, -hingeHeight * sn) - backNormal * (backInset - 0.0006f);
            plate.AddBox(new Vector3(-0.015f, -0.007f, -0.0006f), new Vector3(0.015f, 0.007f, 0.0006f));
            var plateTurn = Quaternion.LookRotation(-backNormal, up);
            var plateMesh = new MeshBuilder();
            plateMesh.Append(plate, plateCentre, plateTurn);
            plateMesh.Attach("HingePlate", root, SurfaceMaterial.Chrome);
        }
    }
}
