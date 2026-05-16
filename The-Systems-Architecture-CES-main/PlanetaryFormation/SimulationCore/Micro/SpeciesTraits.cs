namespace PlanetaryFormation.SimulationCore.Micro;

/// <summary>
/// Phenotype-level summary of a species' key traits, derived from its <see cref="Genome"/>.
/// All fields are value types so arrays of SpeciesTraits are cache-friendly and can be
/// trivially ported to Unity ECS NativeArrays.
/// Maps to SpeciesTraits (IComponentData) in the GDD and the <c>averageTraits</c> field
/// of SpeciesComponent (IComponentData).
/// </summary>
public struct SpeciesTraits
{
    /// <summary>Body size in Earth-relative mass units (mirrors Genome.Mass).</summary>
    public float Size;

    /// <summary>Maximum movement speed in body-lengths per second.</summary>
    public float Speed;

    /// <summary>Energy consumed per tick relative to baseline.</summary>
    public float Metabolism;

    /// <summary>
    /// Likelihood of conflict with other species (0.0 = fully passive, 1.0 = apex predator).
    /// </summary>
    public float Aggression;

    /// <summary>Offspring produced per tick under ideal conditions.</summary>
    public float ReproductionRate;

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Derives the phenotype trait summary directly from a <see cref="Genome"/>.
    /// Call once per fitness evaluation cycle rather than storing redundant data.
    /// </summary>
    public static SpeciesTraits FromGenome(in Genome genome) => new()
    {
        Size             = genome.Mass,
        Speed            = genome.Speed,
        Metabolism       = genome.MetabolismRate,
        Aggression       = genome.Aggression,
        ReproductionRate = genome.ReproductionRate,
    };
}
