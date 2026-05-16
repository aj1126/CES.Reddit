using PlanetaryFormation.Models;
using PlanetaryFormation.SimulationCore.Config;
using PlanetaryFormation.SimulationCore.Events;

namespace PlanetaryFormation.SimulationCore.Macro;

/// <summary>Published when a planet crosses the abiogenesis threshold.</summary>
public record AbiogenesisEvent(CelestialBody Planet, double SimulatedYears);

/// <summary>
/// Advances the prebiotic chemistry score on each planet every macro tick and
/// triggers the Micro-Simulation when the configured threshold is crossed.
/// Iteration 2: Macro-Simulation — Planetary Formation &amp; Abiogenesis.
/// </summary>
public class AbiogenesisEngine
{
    private readonly SimulationConfig _config;

    /// <summary>
    /// Planet names that have already undergone abiogenesis.
    /// Prevents re-triggering on subsequent ticks.
    /// </summary>
    private readonly HashSet<string> _seeded = new();

    public AbiogenesisEngine(SimulationConfig? config = null)
    {
        _config = config ?? SimulationConfig.Instance;
    }

    /// <summary>
    /// Tick all planets in <paramref name="system"/>. Planets that meet Habitable
    /// or Optimal criteria have their prebiotic score advanced. When a planet
    /// crosses the threshold, <see cref="AbiogenesisEvent"/> is published.
    /// </summary>
    public void Tick(SolarSystem system, double simulatedYears)
    {
        foreach (var planet in system.Planets)
            TickPlanet(planet, system, simulatedYears);
    }

    /// <summary>Returns true if life has already been seeded on the given planet.</summary>
    public bool HasLifeBeenSeeded(CelestialBody planet) =>
        _seeded.Contains(planet.Name);

    /// <summary>
    /// Developer override: immediately triggers abiogenesis on <paramref name="planet"/>
    /// regardless of its current prebiotic chemistry score.
    /// Safe to call even if abiogenesis has already fired (no-op in that case).
    /// </summary>
    public void ForceAbiogenesis(CelestialBody planet, double simulatedYears = 0)
    {
        if (_seeded.Contains(planet.Name)) return;

        planet.PrebioticChemistryScore = 1.0;
        planet.LifeStage               = LifeStage.Microbial;
        _seeded.Add(planet.Name);
        EventBus.Publish(new AbiogenesisEvent(planet, simulatedYears));
    }

    private void TickPlanet(CelestialBody planet, SolarSystem system, double simulatedYears)
    {
        if (_seeded.Contains(planet.Name)) return;

        var (_, band) = HabitabilityCalculator.GetGoldilocksScore(planet, system);

        double rate = band switch
        {
            HabitabilityBand.Optimal   => _config.AbiogenesisScoreRateOptimal,
            HabitabilityBand.Habitable => _config.AbiogenesisScoreRateHabitable,
            _                          => 0.0
        };

        if (rate <= 0) return;

        // Surface water is required for the prebiotic chemistry pathway
        if (planet.SurfaceWaterPercent < 0.05) return;

        planet.PrebioticChemistryScore = Math.Min(1.0, planet.PrebioticChemistryScore + rate);

        if (planet.PrebioticChemistryScore >= _config.AbiogenesisThreshold)
        {
            _seeded.Add(planet.Name);
            planet.LifeStage = LifeStage.Microbial;
            EventBus.Publish(new AbiogenesisEvent(planet, simulatedYears));
        }
    }
}
