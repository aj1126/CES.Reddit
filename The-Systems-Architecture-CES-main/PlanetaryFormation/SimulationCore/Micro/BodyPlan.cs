namespace PlanetaryFormation.SimulationCore.Micro;

/// <summary>
/// Describes the primary morphological symmetry plan of a species' body.
/// Stored as a top-level <see cref="Genome"/> field and drives which prefab
/// skeleton is selected in the Unity render layer (Creature Generation pipeline).
/// </summary>
public enum BodyPlan
{
    /// <summary>
    /// Radial symmetry — identical along any axis through the centre.
    /// Examples: jellyfish, starfish analogues.
    /// </summary>
    Radial = 0,

    /// <summary>
    /// Bilateral symmetry — mirror-image left and right halves.
    /// The most common plan in complex animals; required for directed locomotion.
    /// </summary>
    Bilateral = 1,

    /// <summary>No consistent symmetry axis; highly plastic body shape.</summary>
    Asymmetric = 2,

    /// <summary>
    /// Anchored, non-locomoting body.
    /// Examples: coral, tree, sponge analogues.
    /// Zero locomotion limbs; high VegetationDensity contribution.
    /// </summary>
    Sessile = 3,
}
