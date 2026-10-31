using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// One screen of the video wall. It owns a small render texture and shows it on its screen mesh
    /// (as base and emission map, so the picture glows in a dark room). Without a feed the screen stays black.
    /// </summary>
    public class SecurityMonitor : MonoBehaviour
    {
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionMap = Shader.PropertyToID("_EmissionMap");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Renderer screen;

        private RenderTexture texture;
        private Material material;

        public RenderTexture Texture => texture;

        /// <summary>The camera currently shown, or null.</summary>
        public SecurityCamera Feed { get; internal set; }

        /// <summary>Creates the render texture and the screen material; called by the wall before the first feed.</summary>
        internal void Prepare(int width, int height)
        {
            if (texture != null || screen == null) return;

            texture = new RenderTexture(width, height, 16) { name = $"{name}_Feed" };
            material = new Material(screen.sharedMaterial) { name = $"{name}_Screen" };
            material.SetTexture(BaseMap, texture);
            material.SetColor(BaseColor, Color.white);
            material.SetTexture(EmissionMap, texture);
            material.SetColor(EmissionColor, Color.white * 0.8f);
            material.EnableKeyword("_EMISSION");
            screen.sharedMaterial = material;
        }

        private void OnDestroy()
        {
            if (texture != null)
            {
                texture.Release();
                Destroy(texture);
            }
            if (material != null) Destroy(material);
        }
    }
}
