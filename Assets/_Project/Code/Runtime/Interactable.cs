using System;
using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Generic interactable object: shows a prompt, and raises an event when the player uses it.
    /// Other components on the same object (a terminal, a rack door...) subscribe to
    /// <see cref="Interacted"/> instead of the player knowing about them.
    /// </summary>
    public class Interactable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string prompt;
        [Min(0f)]
        [SerializeField] private float holdDuration;

        public string Prompt => prompt;
        public float HoldDuration => holdDuration;

        public event Action<Interactor> Interacted;

        public void Interact(Interactor interactor) => Interacted?.Invoke(interactor);
    }
}
