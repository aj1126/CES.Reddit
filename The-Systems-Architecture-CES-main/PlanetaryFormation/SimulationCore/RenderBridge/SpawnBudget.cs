using PlanetaryFormation.SimulationCore.Config;
using PlanetaryFormation.SimulationCore.Micro;

namespace PlanetaryFormation.SimulationCore.RenderBridge;

/// <summary>
/// Selects a performance-budget-limited, representative sample of species to
/// physically spawn from the full <see cref="PopulationPool"/>.
/// Iteration 4: The Render Layer.
/// </summary>
public class SpawnBudget
{
    private readonly SimulationConfig _config;

    public SpawnBudget(SimulationConfig? config = null)
    {
        _config = config ?? SimulationConfig.Instance;
    }

    /// <summary>
    /// Returns at most <see cref="SimulationConfig.MaxActiveCreatures"/> species,
    /// each with a spawn count proportional to that species' share of the total
    /// biome biomass. More populous species are always represented first.
    /// </summary>
    public IReadOnlyList<(SpeciesData Species, int SpawnCount)> SelectSample(PopulationPool pool)
    {
        long totalBiomass = Math.Max(1, pool.TotalBiomass);
        var  result       = new List<(SpeciesData, int)>();
        int  budget       = _config.MaxActiveCreatures;

        foreach (var species in pool.ActiveSpecies.OrderByDescending(s => s.Population))
        {
            if (budget <= 0) break;

            float share = (float)species.Population / totalBiomass;
            int   count = Math.Max(1, (int)Math.Round(share * _config.MaxActiveCreatures));
            count       = Math.Min(count, budget);

            result.Add((species, count));
            budget -= count;
        }

        return result;
    }
}
