using PlanetaryFormation.Models;
using PlanetaryFormation.SimulationCore.Config;
using PlanetaryFormation.SimulationCore.Events;
using PlanetaryFormation.SimulationCore.Micro;
using PlanetaryFormation.SimulationCore.Time;

namespace PlanetaryFormation.SimulationCore.Game;

/// <summary>Published every tick after the complexity/entropy state is updated.</summary>
public record ComplexityTickEvent(
    double SimulatedYears,
    double Complexity,
    double EntropyPercent,
    double ChaosIndex,
    double EntropyDelta);

/// <summary>Published when entropy reaches heat death and a reboot is performed.</summary>
public record BigBangRebootEvent(double SimulatedYears, int RebootCount);

/// <summary>
/// Heat-death loop:
/// - Entropy creeps upward each tick.
/// - Community-generated biological complexity counteracts entropy pressure.
/// - Over-stability causes complexity plateaus.
/// - Over-chaos causes collapse, reducing complexity and spiking entropy.
/// - At 100% entropy, a Big Bang reboot resets the biosphere.
/// </summary>
public class ComplexityEngine
{
    private readonly Universe _universe;
    private readonly SolarSystem _system;
    private readonly MicroSimulationManager _microManager;
    private readonly SimulationConfig _config;

    private long _previousBiomass;
    private int _previousSpeciesCount;

    public double EntropyPercent { get; private set; }
    public double Complexity { get; private set; }
    public int RebootCount { get; private set; }

    public ComplexityEngine(
        Universe universe,
        SolarSystem system,
        MicroSimulationManager microManager,
        SimulationConfig? config = null)
    {
        _universe = universe;
        _system = system;
        _microManager = microManager;
        _config = config ?? SimulationConfig.Instance;

        EventBus.Subscribe<TickEvent>(OnTick);
    }

    /// <summary>Removes the TickEvent subscription.</summary>
    public void Detach() => EventBus.Unsubscribe<TickEvent>(OnTick);

    private void OnTick(TickEvent tick)
    {
        var metrics = MeasureComplexity();
        double chaosIndex = MeasureChaosIndex();

        double stabilityPenalty = chaosIndex < _config.ComplexityStableChaosFloor
            ? Math.Clamp(
                (_config.ComplexityStableChaosFloor - chaosIndex) / Math.Max(_config.ComplexityStableChaosFloor, 1e-6),
                0.0,
                1.0)
            : 0.0;

        double chaosPenalty = chaosIndex > _config.ComplexityStableChaosCeiling
            ? Math.Clamp(
                (chaosIndex - _config.ComplexityStableChaosCeiling) / Math.Max(1.0 - _config.ComplexityStableChaosCeiling, 1e-6),
                0.0,
                1.0)
            : 0.0;

        double plateauFactor = 1.0 - stabilityPenalty * _config.StabilityPlateauPenaltyMax;
        double collapseFactor = 1.0 - chaosPenalty * _config.ChaoticCollapsePenaltyMax;
        Complexity = Math.Clamp(metrics.BaseComplexity * plateauFactor * collapseFactor, 0.0, 1.0);

        bool biomassCollapsed = metrics.TotalBiomass < _previousBiomass;
        bool speciesCollapsed = metrics.SpeciesCount < _previousSpeciesCount;
        bool activeCollapse = chaosPenalty > 0.0 && (biomassCollapsed || speciesCollapsed);

        double entropyIncrease = _config.EntropyCreepPerTick * _universe.Constants.EntropyRate;
        entropyIncrease += stabilityPenalty * _config.StabilityEntropyPenaltyPerTick;
        entropyIncrease += chaosPenalty * _config.ChaosEntropyPenaltyPerTick;
        if (activeCollapse)
            entropyIncrease += chaosPenalty * _config.CollapseEntropySpikePerTick;

        entropyIncrease -= Complexity * _config.ComplexityEntropyMitigationMax;
        entropyIncrease = Math.Max(_config.MinimumEntropyIncreasePerTick, entropyIncrease);

        EntropyPercent = Math.Clamp(EntropyPercent + entropyIncrease, 0.0, 100.0);

        _previousBiomass = metrics.TotalBiomass;
        _previousSpeciesCount = metrics.SpeciesCount;

        EventBus.Publish(new ComplexityTickEvent(
            tick.SimulatedYears,
            Complexity,
            EntropyPercent,
            chaosIndex,
            entropyIncrease));

        if (EntropyPercent >= 100.0)
            TriggerBigBangReboot(tick.SimulatedYears);
    }

    /// <summary>
    /// Resets life and complexity state after heat death.
    /// This is the "Big Bang reboot" hook.
    /// </summary>
    public void TriggerBigBangReboot(double simulatedYears)
    {
        foreach (var planet in _system.Planets)
        {
            planet.LifeStage = LifeStage.Sterile;
            planet.PrebioticChemistryScore = 0.0;
        }

        _microManager.Detach();
        EntropyPercent = 0.0;
        Complexity = 0.0;
        _previousBiomass = 0;
        _previousSpeciesCount = 0;
        RebootCount++;

        EventBus.Publish(new BigBangRebootEvent(simulatedYears, RebootCount));
    }

    private (double BaseComplexity, long TotalBiomass, int SpeciesCount) MeasureComplexity()
    {
        var pools = _microManager.Pools.Values.ToList();
        int speciesCount = pools.Sum(p => p.ActiveSpecies.Count);
        long totalBiomass = pools.Sum(p => p.TotalBiomass);

        double diversityScore = MeasureDiversityScore(pools, speciesCount);
        double biomassScore = Math.Clamp(
            Math.Log10(totalBiomass + 1.0) / Math.Log10(_config.ComplexityTargetBiomass + 1.0),
            0.0,
            1.0);

        double lifeStageScore = _system.Planets.Count == 0
            ? 0.0
            : _system.Planets.Average(p => (double)p.LifeStage / (int)LifeStage.Sapient);

        double weighted =
            diversityScore * _config.ComplexityDiversityWeight +
            biomassScore * _config.ComplexityBiomassWeight +
            lifeStageScore * _config.ComplexityLifeStageWeight;

        double weightTotal =
            _config.ComplexityDiversityWeight +
            _config.ComplexityBiomassWeight +
            _config.ComplexityLifeStageWeight;

        return (Math.Clamp(weighted / Math.Max(weightTotal, 1e-6), 0.0, 1.0), totalBiomass, speciesCount);
    }

    private double MeasureDiversityScore(List<PopulationPool> pools, int speciesCount)
    {
        double shannonAccumulator = 0.0;
        int shannonSamples = 0;

        foreach (var pool in pools)
        {
            int n = pool.ActiveSpecies.Count;
            long total = pool.TotalBiomass;
            if (n <= 1 || total <= 0) continue;

            double h = 0.0;
            foreach (var species in pool.ActiveSpecies)
            {
                double p = species.Population / (double)total;
                if (p <= 0.0) continue;
                h -= p * Math.Log(p);
            }

            double normalizedH = h / Math.Log(n);
            shannonAccumulator += Math.Clamp(normalizedH, 0.0, 1.0);
            shannonSamples++;
        }

        double evennessScore = shannonSamples > 0 ? shannonAccumulator / shannonSamples : 0.0;
        double richnessScore = Math.Clamp(
            speciesCount / (double)Math.Max(_config.ComplexityTargetSpeciesCount, 1),
            0.0,
            1.0);

        return 0.6 * evennessScore + 0.4 * richnessScore;
    }

    private double MeasureChaosIndex()
    {
        var biomes = _system.Planets.SelectMany(p => p.Biomes).ToList();
        if (biomes.Count == 0) return 0.0;

        double meanMutationRate = biomes.Average(b => b.MutationRate);
        return Math.Clamp(
            meanMutationRate / Math.Max(_config.ComplexityTargetMutationRate, 1e-6),
            0.0,
            1.0);
    }
}
