using System;
using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Ready-made fillings for the 42 U of a rack (built by Tools/Cold Shutdown/Create Graybox Props from random server layouts).
    /// Every <see cref="Rack"/> picks one by its seed, so thousands of racks need only a dozen shared meshes: the filling of a rack
    /// costs one array lookup, not a mesh build, and the racks keep sharing their GPU data.
    /// </summary>
    [CreateAssetMenu(menuName = "Cold Shutdown/Rack Content Library", fileName = "RackContents")]
    public class RackContentLibrary : ScriptableObject
    {
        [Serializable]
        public struct Variant
        {
            public Mesh mesh;
            public Material[] materials;
        }

        [SerializeField] private Variant[] variants = new Variant[0];

        public int Count => variants.Length;

        public Variant this[int index] => variants[index];

        /// <summary>The filling a rack with this seed gets.</summary>
        public Variant Pick(int seed)
        {
            if (variants.Length == 0) return default;
            return variants[new System.Random(seed).Next(variants.Length)];
        }

        public void SetVariants(Variant[] value) => variants = value;
    }
}
