using System;
using System.Collections.Generic;
using System.Linq;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEngine;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        public const string RackContentsPath = "Assets/_Project/Data/RackContents.asset";

        private const string RackMeshFolder = "Assets/_Project/Art/Meshes/Racks";

        /// <summary>How many random fillings the library holds.</summary>
        public const int ContentVariants = 12;

        /// <summary>Height of one rack unit (1U = 44.45 mm).</summary>
        private const float RackUnit = 0.04445f;

        private const int RackUnits = 42;

        /// <summary>Front plane of the servers (the rack's middle is z = 0, its front z = 0.6).</summary>
        private const float ServerFront = 0.50f;

        /// <summary>First rack unit: just above the floor plate.</summary>
        private const float RackBottom = 0.085f;

        /// <summary>What can stand in a rack (one builder per item in <c>Props/Hall/RackItems/</c>).</summary>
        private enum RackItem
        {
            BlankPanel1U,
            DiskServer1U,
            DiskServer2U,
            ClosedServer3U,
            Switch1U,
            Ups2U,
        }

        private static int Units(RackItem item)
        {
            switch (item)
            {
                case RackItem.BlankPanel1U:
                case RackItem.DiskServer1U:
                case RackItem.Switch1U:
                    return 1;
                case RackItem.ClosedServer3U:
                    return 3;
                default:
                    return 2;
            }
        }

        private static bool IsServer(RackItem item) => item == RackItem.DiskServer1U || item == RackItem.DiskServer2U || item == RackItem.ClosedServer3U;

        /// <summary>
        /// A random filling of the 42 U, from the bottom: maybe a UPS first, then servers in a random mix with the odd blanking panel and at most
        /// one switch (not at the top); the top unit is always a server, and it always adds up to 42 U.
        /// </summary>
        private static List<RackItem> RandomContents(System.Random rng)
        {
            for (int attempt = 0; attempt < 10000; attempt++)
            {
                var list = new List<RackItem>();
                int used = 0;
                bool hasSwitch = false;
                if (rng.NextDouble() < 0.7)
                {
                    list.Add(RackItem.Ups2U);
                    used += Units(RackItem.Ups2U);
                }
                while (used < RackUnits)
                {
                    int remaining = RackUnits - used;
                    var choices = new List<(RackItem item, double weight)> { (RackItem.DiskServer2U, 3), (RackItem.DiskServer1U, 3) };
                    if (remaining >= 3) choices.Add((RackItem.ClosedServer3U, 2));
                    if (list.Count > 0 && list[list.Count - 1] != RackItem.BlankPanel1U && remaining > 2) choices.Add((RackItem.BlankPanel1U, 0.5));
                    if (!hasSwitch && remaining > 2) choices.Add((RackItem.Switch1U, 0.8));
                    double roll = rng.NextDouble() * choices.Sum(c => c.weight);
                    var pick = choices[choices.Count - 1].item;
                    foreach (var (item, weight) in choices)
                    {
                        if ((roll -= weight) < 0)
                        {
                            pick = item;
                            break;
                        }
                    }
                    if (Units(pick) > remaining) break;
                    hasSwitch |= pick == RackItem.Switch1U;
                    list.Add(pick);
                    used += Units(pick);
                }
                if (used == RackUnits && IsServer(list[list.Count - 1]))
                {
                    return list;
                }
            }
            throw new InvalidOperationException("No random rack filling adds up to 42 U.");
        }

        /// <summary>Fills a rack with the servers of <paramref name="items"/>, from the bottom.</summary>
        private static void BuildRackContents(RackParts parts, IList<RackItem> items)
        {
            float y = RackBottom;
            for (int i = 0; i < items.Count; i++)
            {
                BuildRackItem(parts, items[i], y, Units(items[i]) * RackUnit - 0.003f, i);
                y += Units(items[i]) * RackUnit;
            }
        }

        /// <summary>
        /// Builds <see cref="ContentVariants"/> random fillings (the same ones every time) as meshes in <see cref="RackMeshFolder"/> and lists
        /// them in the <see cref="RackContentLibrary"/> asset. Existing assets are rewritten in place (same GUIDs).
        /// </summary>
        public static RackContentLibrary BuildContentLibrary()
        {
            System.IO.Directory.CreateDirectory(RackMeshFolder);
            var library = AssetDatabase.LoadAssetAtPath<RackContentLibrary>(RackContentsPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<RackContentLibrary>();
                AssetDatabase.CreateAsset(library, RackContentsPath);
            }

            var variants = new RackContentLibrary.Variant[ContentVariants];
            for (int i = 0; i < ContentVariants; i++)
            {
                var parts = new RackParts();
                BuildRackContents(parts, RandomContents(new System.Random(1000 + i * 7919)));
                var mesh = parts.ToMesh($"RackContent_{i:00}", out var materials);
                string path = $"{RackMeshFolder}/RackContent_{i:00}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (existing == null)
                {
                    AssetDatabase.CreateAsset(mesh, path);
                    existing = mesh;
                }
                else
                {
                    EditorUtility.CopySerialized(mesh, existing);
                    UnityEngine.Object.DestroyImmediate(mesh);
                    EditorUtility.SetDirty(existing);
                }
                variants[i] = new RackContentLibrary.Variant { mesh = existing, materials = materials };
            }
            library.SetVariants(variants);
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            return library;
        }

        private static void BuildRackItem(RackParts p, RackItem item, float y, float h, int index)
        {
            switch (item)
            {
                case RackItem.BlankPanel1U: BuildBlankPanel1U(p, y, h, index); break;
                case RackItem.DiskServer1U: BuildDiskServer1U(p, y, h, index); break;
                case RackItem.DiskServer2U: BuildDiskServer2U(p, y, h, index); break;
                case RackItem.ClosedServer3U: BuildClosedServer3U(p, y, h, index); break;
                case RackItem.Switch1U: BuildSwitch1U(p, y, h, index); break;
                default: BuildUps2U(p, y, h, index); break;
            }
        }
    }
}
