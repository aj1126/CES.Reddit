namespace PlanetaryFormation.SimulationCore.Mutation;

/// <summary>
/// Classifies each genome mutation event by the kind of change it introduces.
/// Used for telemetry, lineage tracking, and selective mutation-pressure tuning.
/// Maps to the three Mutation Types defined in the GDD:
///   1. Scalar  — continuous numeric field nudge
///   2. Structural — topology / limb-count / body-plan change
///   3. Material — color / pattern / visual-material change
/// </summary>
public enum MutationType
{
    /// <summary>
    /// A continuous numeric field (size, speed, metabolism, limb length, temperature
    /// tolerance, etc.) was nudged by a Gaussian delta.
    /// Most common mutation type; drives gradual trait drift.
    /// </summary>
    Scalar = 0,

    /// <summary>
    /// A structural property was altered — limb count increased/decreased, body plan
    /// changed, or a sensory organ was added/removed.
    /// Less frequent; produces macroevolutionary jumps.
    /// </summary>
    Structural = 1,

    /// <summary>
    /// A visual or material property was changed — color HSV channels, bone scale
    /// ratios, or surface pattern seed.
    /// Does not directly affect fitness; drives visual species diversity.
    /// </summary>
    Material = 2,
}
