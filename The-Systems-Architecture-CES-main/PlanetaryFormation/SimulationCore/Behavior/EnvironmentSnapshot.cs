namespace PlanetaryFormation.SimulationCore.Behavior;

/// <summary>
/// Lightweight read-only snapshot of the immediate environment around a creature.
/// Populated from <see cref="PopulationPool"/> data each tick.
/// Iteration 5: Physics &amp; Locomotion.
/// </summary>
public struct EnvironmentSnapshot
{
    /// <summary>True if a predator species is within the creature's sight range.</summary>
    public bool ThreatNearby;

    /// <summary>Food availability in the immediate area (0.0 = none, 1.0 = abundant).</summary>
    public float FoodDensity;

    /// <summary>True if a compatible mate is within interaction range.</summary>
    public bool MateAvailable;
}
