namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Something the player can use by looking at it. Put an implementation on the object that has
    /// the collider, or on one of its parents.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Short text shown next to the key hint while the player looks at this object.</summary>
        string Prompt { get; }

        /// <summary>Seconds the key must be held. 0 means a single press is enough.</summary>
        float HoldDuration { get; }

        void Interact(Interactor interactor);
    }
}
