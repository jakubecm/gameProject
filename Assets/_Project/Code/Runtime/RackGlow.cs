using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// The very faint glow of an openable rack, lit only while its door is shut perfectly: a soft white-beige rim that follows the outline of the door
    /// (frame, stiles and rails), so the shape and structure of the door stay readable. The glow mesh carries an additive material; this only switches
    /// its renderer with the door angle and modulates its alpha with a property block (each rack at its own phase).
    /// </summary>
    public class RackGlow : MonoBehaviour
    {
        [SerializeField] private Renderer target;
        [Tooltip("Breaths per second.")]
        [SerializeField, Min(0f)] private float speed = 0.35f;
        [Tooltip("How much of the glow the breathing takes away (0 = steady).")]
        [SerializeField, Range(0f, 1f)] private float depth = 0.35f;
        [Tooltip("Door angle (degrees) below which the door counts as perfectly shut.")]
        [SerializeField, Min(0f)] private float shutTolerance = 0.05f;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock block;
        private Color baseColor = Color.white;
        private float phase;
        private RackDoor door;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            if (target != null && target.sharedMaterial != null && target.sharedMaterial.HasProperty(BaseColor))
            {
                baseColor = target.sharedMaterial.GetColor(BaseColor);
            }
            var position = transform.position;
            phase = (position.x * 1.7f + position.z * 2.3f) % (2f * Mathf.PI);
        }

        /// <summary>True while the door is shut perfectly (the glow shows only then).</summary>
        public bool IsLit => door != null && door.enabled && door.Angle <= shutTolerance;

        private void Update() => Refresh();

        /// <summary>Shows or hides the glow for the current door angle and breathes it.</summary>
        public void Refresh()
        {
            if (target == null) return;
            if (door == null) door = GetComponentInParent<RackDoor>();
            bool lit = IsLit;
            if (target.enabled != lit) target.enabled = lit;
            if (!lit) return;
            float breath = 0.5f + 0.5f * Mathf.Sin(Time.time * speed * 2f * Mathf.PI + phase);
            var color = baseColor;
            color.a *= 1f - depth * (1f - breath);
            block.SetColor(BaseColor, color);
            target.SetPropertyBlock(block);
        }
    }
}
