using ColdShutdown.Core;
using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// The one map of the game, as text (format: <see cref="WorldMapText"/>; one character per 12 x 12 m cell, north at the top).
    /// Edit it in the Inspector of <c>Data/WorldMap.asset</c>, then run Tools/Cold Shutdown/Create Preview Scene: it rebuilds the hall
    /// for this map and <c>Sandbox_B</c>. The <see cref="LevelBuilder"/> builds it in every scene.
    /// </summary>
    [CreateAssetMenu(menuName = "Cold Shutdown/World Map", fileName = "WorldMap")]
    public class WorldMap : ScriptableObject
    {
        [TextArea(8, 40)]
        [SerializeField] private string text = "";

        public string Text
        {
            get => text;
            set => text = value;
        }

        /// <summary>Reads the map; check <see cref="WorldMapText.Result.IsValid"/> and its errors.</summary>
        public WorldMapText.Result Parse() => WorldMapText.Parse(text);
    }
}
