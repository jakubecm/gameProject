using System;
using UnityEngine;

namespace ColdShutdown.Runtime
{
    public enum CoffeeMachineState
    {
        /// <summary>The cup is empty; E brews a coffee.</summary>
        Idle,
        /// <summary>Two streams run into the cup and the level rises.</summary>
        Brewing,
        /// <summary>The cup is full; E drinks it.</summary>
        Ready,
        /// <summary>The player lifts the cup, sips and puts it back; the level sinks.</summary>
        Drinking,
    }

    /// <summary>
    /// The coffee machine of the office: E starts brewing (two streams out of the spouts fill the cup standing on the drip tray, the level
    /// rises), and once the cup is full E drinks it (the cup comes up in front of the camera, tilts, the coffee runs over the rim, the cup goes
    /// back empty). Logic and animation only, no sound; the events are for the sound and later the game systems. The prop is built by
    /// the editor tool (<c>CoffeeMachine</c> prop), which wires the fields.
    /// </summary>
    public class CoffeeMachine : MonoBehaviour
    {
        // Measures of the mug in its local space (pivot at the centre of its base), see the Mug prop.
        private const float CupCentre = 0.04f;
        private const float FullLevelY = 0.058f;

        // Below this share of a full cup the coffee is gone.
        private const float MinVisibleFill = CoffeeLiquidMesh.VanishFill;
        
        // Brewing, seconds: the streams fall into the cup, the level rises, the streams break off.
        private const float StreamFallTime = 0.5f;
        private const float FillTime = 4f;
        private const float StreamEndTime = 0.3f;

        // Drinking is one continuous movement: every part of it (sliding out, lifting, going to the mouth, tipping, lowering, putting back) is
        // a smooth curve over a window of the timeline, and the windows overlap, so the cup never stops between two parts. The cup is pulled
        // out horizontally first, because the dispenser head is only 2 cm above it, and goes back the same way.
        private const float SlideDistance = 0.09f;
        private const float MaxSipTilt = 92f;
        private const float DrinkDuration = 5.9f;

        private static readonly Vector2 SlideOutWindow = new Vector2(0f, 0.65f);
        private static readonly Vector2 LiftUpWindow = new Vector2(0.55f, 1.85f);
        private static readonly Vector2 MouthWindow = new Vector2(0.55f, 1.85f);
        private static readonly Vector2 TipWindow = new Vector2(1.5f, 4.0f);
        private static readonly Vector2 UntipWindow = new Vector2(3.6f, 4.5f);
        private static readonly Vector2 LiftDownWindow = new Vector2(4.1f, 5.3f);
        private static readonly Vector2 SlideInWindow = new Vector2(5.2f, 5.9f);

        [SerializeField] private Transform cup;
        [SerializeField] private MeshFilter liquidFilter;
        [SerializeField] private Renderer liquidRenderer;
        [Tooltip("The streams out of the spouts: pivot at the bottom, one metre long along +Y, scaled to their length.")]
        [SerializeField] private Transform[] streams;

        private CoffeeMachineUse use;
        private Mesh liquidMesh;
        private readonly CoffeeLiquidMesh liquidBuilder = new CoffeeLiquidMesh();
        private Vector3 cupHomePosition;
        private Quaternion cupHomeRotation;
        private float spoutY;
        private float timer;
        private float fill;
        private Transform viewer;
        private PlayerInputReader viewerInput;
        private bool inputWasEnabled;

        public CoffeeMachineState State { get; private set; }

        /// <summary>0 for an empty cup to 1 for a full one.</summary>
        public float Fill => fill;

        public bool CanUse => State == CoffeeMachineState.Idle || State == CoffeeMachineState.Ready;

        public string Prompt => State == CoffeeMachineState.Ready ? "Drink coffee" : "Brew coffee";

        public event Action<CoffeeMachineState> StateChanged;

        private void Awake()
        {
            use = GetComponent<CoffeeMachineUse>();
            cupHomePosition = cup.localPosition;
            cupHomeRotation = cup.localRotation;
            spoutY = streams.Length > 0 ? streams[0].localPosition.y : 0f;
            liquidMesh = new Mesh { name = "CoffeeLiquid" };
            liquidMesh.MarkDynamic();
            liquidFilter.sharedMesh = liquidMesh;
            ShowStreams(0f, 0f);
            ShowLiquid(0f);
            SetState(CoffeeMachineState.Idle);
        }

        private void OnDestroy()
        {
            if (liquidMesh != null) Destroy(liquidMesh);
        }

        /// <summary>Brews if the cup is empty, drinks if it is full. <paramref name="viewerTransform"/> is the player camera.</summary>
        public void Use(Transform viewerTransform)
        {
            if (State == CoffeeMachineState.Idle)
            {
                timer = 0f;
                fill = 0f;
                SetState(CoffeeMachineState.Brewing);
            }
            else if (State == CoffeeMachineState.Ready)
            {
                viewer = viewerTransform;
                timer = 0f;
                // The player stands still while the cup is in their hand.
                viewerInput = viewer.GetComponentInParent<PlayerInputReader>();
                if (viewerInput != null)
                {
                    inputWasEnabled = viewerInput.InputEnabled;
                    viewerInput.InputEnabled = false;
                }
                SetState(CoffeeMachineState.Drinking);
            }
        }

        private void Update()
        {
            if (State == CoffeeMachineState.Brewing) UpdateBrewing();
        }

        private void LateUpdate()
        {
            // After the camera has moved this frame: the cup follows it.
            if (State == CoffeeMachineState.Drinking) UpdateDrinking();
        }

        private void SetState(CoffeeMachineState state)
        {
            State = state;
            if (use != null) use.enabled = CanUse;
            StateChanged?.Invoke(state);
        }

        private static float Ease(float t) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

        private float SurfaceLocalY(float level) => cupHomePosition.y + Mathf.Lerp(CoffeeLiquidMesh.FloorY, FullLevelY, level);

        private void UpdateBrewing()
        {
            timer += Time.deltaTime;
            float top = spoutY;
            float bottom;
            float level = 0f;
            if (timer < StreamFallTime)
            {
                // The streams fall from the spouts to the bottom of the cup.
                bottom = Mathf.Lerp(spoutY, SurfaceLocalY(0f), Ease(timer / StreamFallTime));
            }
            else if (timer < StreamFallTime + FillTime)
            {
                // The surface rises evenly; the volume under it follows the shape of the cup.
                level = (timer - StreamFallTime) / FillTime;
                fill = CoffeeLiquidMesh.FillAtLevel(level, FullLevelY);
                bottom = SurfaceLocalY(level);
            }
            else if (timer < StreamFallTime + FillTime + StreamEndTime)
            {
                // The top of the streams runs down to the surface and they are gone.
                fill = 1f;
                bottom = SurfaceLocalY(1f);
                top = Mathf.Lerp(spoutY, bottom, Ease((timer - StreamFallTime - FillTime) / StreamEndTime));
            }
            else
            {
                fill = 1f;
                ShowStreams(0f, 0f);
                SetState(CoffeeMachineState.Ready);
                RebuildLiquid();
                return;
            }
            ShowStreams(bottom, top);
            RebuildLiquid();
        }

        private static float Window(float time, Vector2 window) => Ease((time - window.x) / (window.y - window.x));

        private void UpdateDrinking()
        {
            timer += Time.deltaTime;
            if (timer >= DrinkDuration)
            {
                FinishDrinking();
                return;
            }

            float slide = Window(timer, SlideOutWindow) * (1f - Window(timer, SlideInWindow));
            float lift = Window(timer, LiftUpWindow) * (1f - Window(timer, LiftDownWindow));
            float mouth = Window(timer, MouthWindow) * (1f - Window(timer, UntipWindow));
            // The tip eases out hard, so the last of the coffee runs along the wall to the rim slowly instead of draining at the speed of the tilt.
            float tip = 1f - Mathf.Pow(1f - Window(timer, TipWindow), 2f);
            float tilt = MaxSipTilt * tip * (1f - Window(timer, UntipWindow));
            PoseCup(lift, tilt, slide, mouth);
            // The coffee leaves only over the rim: it never exceeds what the tilted cup holds, so the pool narrows as it runs along the
            // wall to the lowest point of the rim and ends exactly there (it never refills when the cup comes back).
            // The coffee leaves only over the lowest point of the rim: it never exceeds what the tilted cup holds, so the pool narrows as the
            // cup tips and the last of it is a thin film that gathers at the rim and is gone. It never refills when the cup comes back.
            var up = SurfaceUp();
            fill = Mathf.Min(fill, CoffeeLiquidMesh.Capacity(up, FullLevelY));
            RebuildLiquid(up);
        }

        /// <summary>
        /// Puts the cup between the tray and the player's hand (<paramref name="lift"/>), slid out from under the dispenser head
        /// (<paramref name="slideOut"/>), brings it to the player's mouth (<paramref name="mouth"/>) and tilts it that way.
        /// </summary>
        private void PoseCup(float lift, float tiltDegrees, float slideOut, float mouth)
        {
            var parent = cup.parent;
            var homeRotation = parent.rotation * cupHomeRotation;
            // Slid out along the machine's front (+Z): clear of the dispenser head before it goes up, and under it again only at the very end.
            var homeCentre = parent.TransformPoint(cupHomePosition) + homeRotation * (Vector3.up * CupCentre) + parent.forward * (SlideDistance * slideOut);

            // The cup goes straight from the tray to the mouth (the lift and the approach run together), with no detour to the side. The cup is centred on the view; its handle points right.
            var flat = Vector3.ProjectOnPlane(viewer.forward, Vector3.up);
            if (flat.sqrMagnitude < 1e-4f) flat = viewer.up;
            var yaw = Quaternion.LookRotation(flat.normalized, Vector3.up);
            var hand = viewer.position + viewer.forward * 0.34f - viewer.up * 0.12f;
            var mouthPosition = viewer.position + viewer.forward * 0.25f - viewer.up * 0.07f;
            var held = Vector3.Lerp(hand, mouthPosition, mouth);

            var centre = Vector3.Lerp(homeCentre, held, lift) + Vector3.up * (0.06f * Mathf.Sin(Mathf.PI * lift));
            var rotation = Quaternion.Slerp(homeRotation, yaw, lift) * Quaternion.Euler(-tiltDegrees, 0f, 0f);
            cup.SetPositionAndRotation(centre - rotation * (Vector3.up * CupCentre), rotation);
        }

        private void FinishDrinking()
        {
            cup.localPosition = cupHomePosition;
            cup.localRotation = cupHomeRotation;
            if (viewerInput != null) viewerInput.InputEnabled = inputWasEnabled;
            viewer = null;
            viewerInput = null;
            fill = 0f;
            ShowLiquid(0f);
            SetState(CoffeeMachineState.Idle);
        }

        // World up in the cup's space; while drinking the surface sloshes a little: a damped wobble that grows with the speed of the tilt.
        private Vector3 SurfaceUp()
        {
            var up = cup.InverseTransformDirection(Vector3.up);
            if (State == CoffeeMachineState.Drinking)
            {
                float sway = 0.05f * Mathf.Exp(-0.6f * timer) + 0.025f * Mathf.Abs(Mathf.Sin(timer * 1.3f));
                up += new Vector3(Mathf.Sin(timer * 9f), 0f, Mathf.Cos(timer * 7f)) * sway;
            }
            return up;
        }

        private void RebuildLiquid() => RebuildLiquid(SurfaceUp());

        private void RebuildLiquid(Vector3 up)
        {
            ShowLiquid(fill);
            if (fill <= MinVisibleFill) return;

            liquidBuilder.Rebuild(liquidMesh, up, fill, FullLevelY, State == CoffeeMachineState.Drinking);
        }

        private void ShowLiquid(float level) => liquidRenderer.enabled = level > MinVisibleFill;

        /// <summary>Stretches the streams from <paramref name="bottom"/> to <paramref name="top"/> (heights in the machine's space); a stream shorter than 2 mm is hidden.</summary>
        private void ShowStreams(float bottom, float top)
        {
            float length = top - bottom;
            bool visible = length > 0.002f;
            // A slight flicker in the width, so the stream looks like it flows.
            float width = 1f + 0.18f * Mathf.Sin(Time.time * 45f);
            foreach (var stream in streams)
            {
                stream.gameObject.SetActive(visible);
                if (!visible) continue;

                var position = stream.localPosition;
                position.y = bottom;
                stream.localPosition = position;
                stream.localScale = new Vector3(width, length, width);
            }
        }
    }
}
