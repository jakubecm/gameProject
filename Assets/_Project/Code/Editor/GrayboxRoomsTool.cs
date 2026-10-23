using System;
using System.Collections.Generic;
using System.IO;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEngine;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Creates a graybox room for every <see cref="RoomType"/> as a prefab variant of the room shell,
    /// plus a <see cref="RoomDefinition"/> for each. Types without a filler below get an empty shell to
    /// dress by hand. Existing prefabs and definitions are never replaced, so hand edits are safe and
    /// a new room type only needs a new enum value and another run. Run Create Room Shell first.
    /// </summary>
    public static class GrayboxRoomsTool
    {
        public const string DefinitionsFolder = "Assets/_Project/Data";

        // Office furniture sizes (W x H x D); placeholders until real models exist
        private static readonly Vector3 DeskSize = new Vector3(1.6f, 0.75f, 0.8f);
        private static readonly Vector3 CabinetSize = new Vector3(1.2f, 1.8f, 0.6f);

        private static readonly Dictionary<RoomType, Action<Transform>> Fillers = new Dictionary<RoomType, Action<Transform>>
        {
            { RoomType.ServerAisle, AddServerAisleSlots },
            { RoomType.Office, AddOfficeGraybox },
        };

        public static string PrefabPath(RoomType type) => $"{RoomShellTool.RoomsFolder}/Room_{type}.prefab";

        public static string DefinitionPath(RoomType type) => $"{DefinitionsFolder}/RoomDefinition_{type}.asset";

        [MenuItem("Tools/Cold Shutdown/Create Graybox Rooms")]
        public static void CreateGrayboxRooms()
        {
            var shell = AssetDatabase.LoadAssetAtPath<GameObject>(RoomShellTool.ShellPath);
            if (shell == null)
            {
                Debug.LogError($"{RoomShellTool.ShellPath} not found. Run Tools/Cold Shutdown/Create Room Shell first.");
                return;
            }

            foreach (RoomType type in Enum.GetValues(typeof(RoomType)))
            {
                var prefab = CreateRoom(shell, type);
                SaveDefinition(type, prefab);
            }
            AssetDatabase.SaveAssets();
        }

        private static GameObject CreateRoom(GameObject shell, RoomType type)
        {
            string prefabPath = PrefabPath(type);
            if (RoomShellTool.SkipExisting(prefabPath))
            {
                return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }

            var room = (GameObject)PrefabUtility.InstantiatePrefab(shell);
            room.name = $"Room_{type}";
            if (Fillers.TryGetValue(type, out var fill))
            {
                fill(room.transform);
            }
            var prefab = RoomShellTool.SaveAsPrefab(room, prefabPath);
            Debug.Log($"Room prefab saved to {prefabPath}.");
            return prefab;
        }

        private static void AddServerAisleSlots(Transform room)
        {
            var group = GrayboxUtility.CreateGroup("RackSlots", room);
            var poses = RoomContract.GetServerAisleRackSlots();
            for (int i = 0; i < poses.Length; i++)
            {
                var slot = new GameObject($"RackSlot_{i:00}", typeof(RackSlot)).transform;
                slot.SetParent(group, false);
                RoomShellTool.ApplyPose(slot, poses[i]);
            }
        }

        /// <summary>Desks and cabinets stand in the corners against the walls, clear of the walkway cross.</summary>
        private static void AddOfficeGraybox(Transform room)
        {
            var group = GrayboxUtility.CreateGroup("Graybox", room);
            float wall = RoomContract.InteriorHalfSize;
            float deskZ = wall - DeskSize.z / 2f - 0.1f;
            float deskX = 3.6f;

            GrayboxUtility.CreateStandingBox("Desk_NW", group, new Vector3(-deskX, 0f, deskZ), DeskSize);
            GrayboxUtility.CreateStandingBox("Desk_NE", group, new Vector3(deskX, 0f, deskZ), DeskSize);
            GrayboxUtility.CreateStandingBox("Desk_SE", group, new Vector3(deskX, 0f, -deskZ), DeskSize);

            // Cabinets stand along the west and east walls, rotated so their front faces into the room.
            float cabinetX = wall - CabinetSize.z / 2f;
            var westCabinet = GrayboxUtility.CreateStandingBox("Cabinet_W", group, new Vector3(-cabinetX, 0f, 3.2f), CabinetSize);
            westCabinet.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            var eastCabinet = GrayboxUtility.CreateStandingBox("Cabinet_E", group, new Vector3(cabinetX, 0f, -3.2f), CabinetSize);
            eastCabinet.transform.localRotation = Quaternion.Euler(0f, 270f, 0f);

            // Terminal in the south-west corner against the south wall, screen facing north.
            var terminal = new GameObject("TerminalSlot", typeof(TerminalSlot)).transform;
            terminal.SetParent(room, false);
            RoomShellTool.ApplyPose(terminal, new SlotPose(-deskX, -(wall - 0.4f), 0f));
        }

        /// <summary>
        /// Creates the definition if it is missing. An existing one is left alone unless it has no prefab,
        /// so a definition pointed at another prefab by hand keeps that choice.
        /// </summary>
        private static void SaveDefinition(RoomType type, GameObject prefab)
        {
            string path = DefinitionPath(type);
            var definition = AssetDatabase.LoadAssetAtPath<RoomDefinition>(path);
            if (definition != null && definition.Prefab != null)
            {
                return;
            }
            if (definition == null)
            {
                Directory.CreateDirectory(DefinitionsFolder);
                definition = ScriptableObject.CreateInstance<RoomDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("type").intValue = (int)type;
            serialized.FindProperty("prefab").objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            Debug.Log($"Room definition saved to {path}.");
        }
    }
}
