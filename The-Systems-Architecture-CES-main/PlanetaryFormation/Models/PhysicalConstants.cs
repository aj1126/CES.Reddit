namespace PlanetaryFormation.Models;

/// <summary>
/// The fundamental physical constants that govern a Universe instance.
/// Varying these constants produces universes with different physics, enabling
/// multi-universe comparative simulation runs (optional GDD system).
/// Maps to the PhysicalConstants block in the GDD.
/// </summary>
public struct PhysicalConstants
{
    /// <summary>Gravitational force multiplier relative to standard (1.0 = our universe).</summary>
    public float GravityStrength;

    /// <summary>
    /// Electromagnetic force multiplier. Affects chemical bond energy and the
    /// complexity of molecules that can form — higher values favour life emergence.
    /// </summary>
    public float ElectromagneticStrength;

    /// <summary>
    /// Rate of entropy increase. Higher values accelerate stellar aging, increase
    /// radiation on planets, and shorten the habitable window for life.
    /// </summary>
    public float EntropyRate;

    /// <summary>
    /// Universe expansion rate (Hubble-constant analogue). Affects how quickly
    /// proto-galactic material disperses and consequently galaxy mass distribution.
    /// </summary>
    public float ExpansionRate;

    /// <summary>Standard constants matching our observed universe.</summary>
    public static readonly PhysicalConstants Standard = new()
    {
        GravityStrength         = 1.0f,
        ElectromagneticStrength = 1.0f,
        EntropyRate             = 1.0f,
        ExpansionRate           = 1.0f,
    };

    public override string ToString() =>
        $"g={GravityStrength:F2}  em={ElectromagneticStrength:F2}  " +
        $"entropy={EntropyRate:F2}  expansion={ExpansionRate:F2}";
}
