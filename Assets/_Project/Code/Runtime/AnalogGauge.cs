using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// An analog meter on a panel or a pipe: the needle turns around the dial's local Z axis between
    /// <see cref="minAngle"/> and <see cref="maxAngle"/>. Gauges in the power room (load per hall section) and the
    /// cooling plant (coolant temperature, air flow) are physical clues the rogue AI cannot fake; the run logic
    /// sets <see cref="Value"/>, nothing on the gauge itself decides it.
    /// </summary>
    public class AnalogGauge : MonoBehaviour
    {
        [SerializeField] private Transform needle;
        [Tooltip("What the gauge measures, for the clue system (for example \"Sekce B\" or \"Výstup chladiva\").")]
        [SerializeField] private string label;
        [SerializeField, Range(0f, 1f)] private float value = 0.35f;
        [SerializeField] private float minAngle = 135f;
        [SerializeField] private float maxAngle = -135f;

        public string Label
        {
            get => label;
            set => label = value;
        }

        /// <summary>Reading between 0 (needle at <see cref="minAngle"/>) and 1 (at <see cref="maxAngle"/>).</summary>
        public float Value
        {
            get => value;
            set
            {
                this.value = Mathf.Clamp01(value);
                Apply();
            }
        }

        private void OnEnable() => Apply();

        private void Apply()
        {
            if (needle == null) return;
            needle.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(minAngle, maxAngle, value));
        }
    }
}
