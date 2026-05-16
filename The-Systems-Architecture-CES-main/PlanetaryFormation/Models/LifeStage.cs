namespace PlanetaryFormation.Models;

/// <summary>
/// Tracks the highest stage of biological complexity that has evolved on a planet.
/// Advances automatically as species diversify and accumulate during simulation.
/// Maps to the LifeStage enum referenced by PlanetComponent (IComponentData) in the GDD.
///
/// Placed in the Models namespace so <see cref="CelestialBody"/> can carry this field
/// without creating a dependency from Models onto SimulationCore.
/// </summary>
public enum LifeStage
{
    /// <summary>No life has emerged; the planet is geologically active but sterile.</summary>
    Sterile = 0,

    /// <summary>
    /// Single-celled or simple microbial life has appeared following abiogenesis.
    /// Set automatically when <c>AbiogenesisEngine</c> crosses the prebiotic threshold.
    /// </summary>
    Microbial = 1,

    /// <summary>Multi-cellular organisms are present and diversifying.</summary>
    Multicellular = 2,

    /// <summary>
    /// Complex, differentiated organisms with specialised tissue systems have evolved.
    /// Triggered when a species reaches a sufficiently high generation index with
    /// multiple sensory organs.
    /// </summary>
    Complex = 3,

    /// <summary>
    /// Tool-using, socially organised beings with proto-civilisation potential.
    /// Optional GDD system: Civilization Emergence.
    /// </summary>
    Sapient = 4,
}
