using PlanetaryFormation.Models;
using PlanetaryFormation.SimulationCore.Config;

namespace PlanetaryFormation.SimulationCore.Micro;

/// <summary>
/// Evaluates how well a <see cref="Genome"/> fits a <see cref="Biome"/> and
/// applies population growth or decline accordingly.
/// Iteration 3: Micro-Simulation.
/// </summary>
public class EnvironmentalPressureEngine
{
    private readonly SimulationConfig _config;

    public EnvironmentalPressureEngine(SimulationConfig? config = null)
    {
        _config = config ?? SimulationConfig.Instance;
    }

    /// <summary>
    /// Returns a fitness score in [0, 1] representing how well
    /// <paramref name="genome"/> survives in <paramref name="biome"/>.
    ///
    /// Fitness formula (from GDD):
    ///   fitness = environmentalMatchScore + speedAdvantage + energyEfficiency
    ///             − instabilityPenalty
    ///
    /// Each component is normalised to [0, 1] before weighting so the total
    /// remains bounded. The weighted composite is then clamped to [0, 1].
    /// </summary>
    public float ComputeFitness(Genome genome, Biome biome)
    {
        // ── Environmental match score ─────────────────────────────────────────
        // Temperature fit: full score inside tolerance band, linearly decaying outside
        float tempDelta = Math.Abs(genome.IdealTempK - (float)biome.Temperature);
        float tempFit   = tempDelta <= genome.TempToleranceK
            ? 1.0f
            : Math.Max(0f, 1f - (tempDelta - genome.TempToleranceK) / (genome.TempToleranceK + 1f));

        // Humidity vs. metabolism: high-metabolism creatures need more available water
        float humidFit = genome.MetabolismRate <= 1.0f
            ? 1.0f
            : Math.Clamp((float)biome.Humidity / genome.MetabolismRate, 0f, 1f);

        // Mass vs. atmospheric pressure (proxy for surface gravity):
        // heavy creatures are less viable where pressure is significantly above 1 atm
        float gravityFit = genome.Mass <= 1.0f
            ? 1.0f
            : Math.Clamp(1f - (genome.Mass - 1f) * (float)(biome.AtmosphericPressure - 1.0) * 0.1f, 0f, 1f);

        // Weighted environmental match (temperature is the dominant pressure)
        float environmentalMatchScore = tempFit * 0.60f + humidFit * 0.25f + gravityFit * 0.15f;

        // ── Speed advantage ───────────────────────────────────────────────────
        // Faster creatures escape predators and find food more effectively.
        // Normalised to a 0–1 advantage where Speed = 2.0 is considered high.
        float speedAdvantage = Math.Clamp(genome.Speed / 2.0f, 0f, 1f) * 0.15f;

        // ── Energy efficiency ─────────────────────────────────────────────────
        // Lower metabolism relative to the maximum possible rate means the creature
        // can sustain itself on less food — a competitive advantage in sparse biomes.
        const float maxMetabolism = 2.0f;
        float energyEfficiency = Math.Clamp(1f - genome.MetabolismRate / maxMetabolism, 0f, 1f) * 0.10f;

        // ── Instability penalty ───────────────────────────────────────────────
        // High mutation volatility causes genomic instability — offspring are more
        // likely to be maladapted, reducing effective reproductive output.
        float instabilityPenalty = Math.Clamp(genome.MutationVolatility, 0f, 1f) * 0.10f;

        // ── Composite ─────────────────────────────────────────────────────────
        float raw = environmentalMatchScore + speedAdvantage + energyEfficiency - instabilityPenalty;
        return Math.Clamp(raw, 0f, 1f);
    }

    /// <summary>
    /// Applies one tick of population pressure to <paramref name="species"/>,
    /// growing or shrinking its population based on fitness and carrying capacity.
    /// </summary>
    public void ApplyPressure(SpeciesData species, Biome biome, long carryingCapacity)
    {
        float fitness = ComputeFitness(species.BaseGenome, biome);
        species.FitnessScore = fitness;

        if (fitness < 0.1f)
        {
            // Near-zero fitness → rapid population collapse
            species.Population = Math.Max(0, species.Population / 2);
            return;
        }

        // Logistic growth: rate scales with fitness; tapers as population nears capacity
        double growthRate     = species.BaseGenome.ReproductionRate * fitness;
        double capacityFactor = 1.0 - (double)species.Population / carryingCapacity;
        long   delta          = (long)(species.Population * growthRate * capacityFactor);

        species.Population = Math.Clamp(species.Population + delta, 0, carryingCapacity);
    }
}
