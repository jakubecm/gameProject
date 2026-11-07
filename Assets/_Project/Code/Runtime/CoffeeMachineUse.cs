using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Makes a <see cref="CoffeeMachine"/> usable with E. The machine switches this component off while it is brewing or being
    /// drunk from, so the interaction prompt only appears when there is something to do.
    /// </summary>
    public class CoffeeMachineUse : MonoBehaviour, IInteractable
    {
        [SerializeField] private CoffeeMachine machine;

        public string Prompt => machine != null ? machine.Prompt : "";

        public float HoldDuration => 0f;

        /// <summary>A coffee machine is used from about an arm's length, not from across the room.</summary>
        public float MaxReach => 0.8f;

        public void Interact(Interactor interactor)
        {
            if (machine != null) machine.Use(interactor.transform);
        }
    }
}
