using PlanetaryFormation.Models;
using PlanetaryFormation.SimulationCore.Mutation;

namespace PlanetaryFormation.Simulation;

/// <summary>
/// Retained for backward compatibility with existing code that imports this class.
/// All logic now lives in <see cref="BiomeMutationEngine"/>.
/// </summary>
[Obsolete("Use PlanetaryFormation.SimulationCore.Mutation.BiomeMutationEngine directly.")]
public static class MutationEngine
{
    /// <inheritdoc cref="BiomeMutationEngine.ApplyMutations"/>
    public static void ApplyMutations(SolarSystem system, Random rng)
        => BiomeMutationEngine.ApplyMutations(system, rng);
}
