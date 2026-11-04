namespace ColdShutdown.Core
{
    /// <summary>
    /// How bright self-lit surfaces glow (the emission colour multiplier). Two levels for the whole game: every status light (exit sign,
    /// LEDs of readers, photo cells, coffee machine, printer, computers, racks, lift digits) glows at <see cref="Indicator"/>, every
    /// monitor, TV and display at <see cref="Display"/>. The light fittings' own diffusers are separate (they follow their light).
    /// </summary>
    public static class EmissionLevels
    {
        public const float Indicator = 2f;
        public const float Display = 0.5f;
    }
}
