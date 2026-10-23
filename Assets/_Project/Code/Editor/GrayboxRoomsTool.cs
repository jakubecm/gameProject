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
    /// Creates a room for every <see cref="RoomType"/> as a prefab variant of the room shell, plus a
    /// <see cref="RoomDefinition"/> for each. This tool only adds the game logic of a room (rack and
    /// terminal slots); furniture and fittings come from Dress Graybox Rooms. Existing prefabs and
    /// definitions are never replaced, so hand edits are safe and a new room type only needs a new enum
    /// value and another run. Run Create Room Shell first.
    /// </summary>
    public static class GrayboxRoomsTool
    {
        public const string DefinitionsFolder = "Assets/_Project/Data";

        private static readonly Dictionary<RoomType, Action<Transform>> Fillers = new Dictionary<RoomType, Action<Transform>>
        {
            { RoomType.ServerAisle, AddServerAisleSlots },
            { RoomType.Office, AddOfficeSlots },
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

        /// <summary>The terminal stands in the south-west corner against the south wall, screen facing north.</summary>
        private static void AddOfficeSlots(Transform room)
        {
            float wall = RoomContract.InteriorHalfSize;
            var terminal = new GameObject("TerminalSlot", typeof(TerminalSlot)).transform;
            terminal.SetParent(room, false);
            RoomShellTool.ApplyPose(terminal, new SlotPose(-3.6f, -(wall - 0.4f), 0f));
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
