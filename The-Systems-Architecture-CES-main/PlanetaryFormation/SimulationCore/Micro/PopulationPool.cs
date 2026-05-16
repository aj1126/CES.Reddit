namespace PlanetaryFormation.SimulationCore.Micro;

/// <summary>
/// Holds all active species for a single biome.
/// Iteration 3: Micro-Simulation.
/// </summary>
public class PopulationPool
{
    public List<SpeciesData> ActiveSpecies { get; } = new();

    /// <summary>Sum of all individual organisms across every active species.</summary>
    public long TotalBiomass => ActiveSpecies.Sum(s => s.Population);

    public void AddSpecies(SpeciesData species) => ActiveSpecies.Add(species);

    public void RemoveSpecies(SpeciesData species) => ActiveSpecies.Remove(species);

    /// <summary>
    /// Removes all extinct species from the pool and returns them as a read-only list.
    /// </summary>
    public IReadOnlyList<SpeciesData> PruneExtinct()
    {
        var extinct = ActiveSpecies.Where(s => s.IsExtinct).ToList();
        foreach (var s in extinct)
            ActiveSpecies.Remove(s);
        return extinct;
    }
}
