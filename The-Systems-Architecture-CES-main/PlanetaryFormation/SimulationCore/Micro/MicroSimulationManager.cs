using PlanetaryFormation.Models;
using PlanetaryFormation.SimulationCore.Config;
using PlanetaryFormation.SimulationCore.Events;
using PlanetaryFormation.SimulationCore.Mutation;

namespace PlanetaryFormation.SimulationCore.Micro;

/// <summary>Published when a new species branches off from a parent.</summary>
public record SpeciationEvent(SpeciesData Parent, SpeciesData Offspring, string BiomeName);

/// <summary>Published when a species goes extinct.</summary>
public record ExtinctionEvent(SpeciesData Species, string BiomeName);

/// <summary>
/// Published whenever a planet's <see cref="LifeStage"/> is automatically promoted
/// by <see cref="MicroSimulationManager"/>.
/// </summary>
public record LifeStageAdvancedEvent(CelestialBody Planet, LifeStage OldStage, LifeStage NewStage);

/// <summary>
/// Manages the per-biome data layer for life: seeding, environmental pressure,
/// mutation, and speciation. Runs purely as in-memory data — no GameObjects,
/// no rendering.
/// Iteration 3: Micro-Simulation.
/// </summary>
public class MicroSimulationManager
{
    private readonly SimulationConfig _config;
    private readonly EnvironmentalPressureEngine _pressureEngine;

    /// <summary>All active population pools, keyed by biome name.</summary>
    private readonly Dictionary<string, PopulationPool> _pools = new();

    public IReadOnlyDictionary<string, PopulationPool> Pools => _pools;

    /// <summary>
    /// Returns the existing <see cref="PopulationPool"/> for <paramref name="biomeName"/>,
    /// or creates and registers a new empty pool so the render layer always receives a
    /// pool that will be updated by future simulation ticks.
    /// </summary>
    public PopulationPool GetOrCreatePool(string biomeName)
    {
        if (!_pools.TryGetValue(biomeName, out var pool))
        {
            pool = new PopulationPool();
            _pools[biomeName] = pool;
        }
        return pool;
    }

    public MicroSimulationManager(SimulationConfig? config = null)
    {
        _config         = config ?? SimulationConfig.Instance;
        _pressureEngine = new EnvironmentalPressureEngine(_config);
    }

    // ── Seeding ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates the first primordial species in every biome on <paramref name="planet"/>.
    /// Called by <see cref="AbiogenesisEngine"/> when the chemistry threshold is crossed.
    /// </summary>
    public void SeedLife(CelestialBody planet, Random rng)
    {
        foreach (var biome in planet.Biomes)
        {
            if (!_pools.ContainsKey(biome.Name))
                _pools[biome.Name] = new PopulationPool();

            var genome    = GeneratePrimordialGenome(biome, rng);
            var primordial = new SpeciesData(genome, _config.InitialPopulation);
            _pools[biome.Name].AddSpecies(primordial);
        }
    }

    // ── Tick ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Advances the simulation by one step for the given biome.
    /// Call once per clock tick while a planet is under observation
    /// or running as a background data simulation.
    /// </summary>
    public void Tick(Biome biome, Random rng)
    {
        if (!_pools.TryGetValue(biome.Name, out var pool)) return;

        // Carrying capacity is proportional to vegetation density
        long carryingCapacity = Math.Max(
            _config.InitialPopulation,
            (long)(biome.VegetationDensity * _config.CarryingCapacityMax));

        // 1. Environmental pressure (growth / decline)
        foreach (var species in pool.ActiveSpecies.ToList())
            _pressureEngine.ApplyPressure(species, biome, carryingCapacity);

        // 2. Mutation & potential speciation
        MutationStep(pool, rng, biome);

        // 3. Extinction check
        CheckExtinction(pool, biome);
    }

    /// <summary>
    /// Overload that additionally advances the host planet's <see cref="LifeStage"/>
    /// based on the current population pool's diversity and complexity.
    /// </summary>
    public void Tick(Biome biome, CelestialBody planet, Random rng)
    {
        Tick(biome, rng);
        AdvanceLifeStage(planet, biome);
    }

    /// <summary>
    /// Evaluates the current population pool and promotes the planet's LifeStage
    /// if the biological thresholds for the next stage have been met.
    /// </summary>
    private void AdvanceLifeStage(CelestialBody planet, Biome biome)
    {
        if (!_pools.TryGetValue(biome.Name, out var pool)) return;

        var oldStage = planet.LifeStage;

        switch (planet.LifeStage)
        {
            case LifeStage.Microbial:
                // Advance to Multicellular when total biomass exceeds a threshold
                // and at least two distinct species exist
                if (pool.TotalBiomass >= 50_000 && pool.ActiveSpecies.Count >= 2)
                    planet.LifeStage = LifeStage.Multicellular;
                break;

            case LifeStage.Multicellular:
                // Advance to Complex when a species reaches a high generation index
                // and has multiple sensory organs
                bool hasComplex = pool.ActiveSpecies.Any(s =>
                    s.GenerationIndex >= 10 &&
                    s.BaseGenome.SensoryOrgans != SensoryOrganFlags.None &&
                    s.BaseGenome.LimbCount >= 2);
                if (hasComplex)
                    planet.LifeStage = LifeStage.Complex;
                break;

            case LifeStage.Complex:
                // Advance to Sapient when neural complexity is high and species
                // population is large and stable (optional GDD civilisation system)
                bool hasSapient = pool.ActiveSpecies.Any(s =>
                    s.BaseGenome.NeuralComplexity >= 0.75f &&
                    s.Population >= 100_000);
                if (hasSapient)
                    planet.LifeStage = LifeStage.Sapient;
                break;

            // Sterile and Sapient have no further automatic transitions here
        }

        if (planet.LifeStage != oldStage)
            EventBus.Publish(new LifeStageAdvancedEvent(planet, oldStage, planet.LifeStage));
    }

    // ── Mutation ──────────────────────────────────────────────────────────────

    private void MutationStep(PopulationPool pool, Random rng, Biome biome)
    {
        var newBranches = new List<SpeciesData>();

        foreach (var species in pool.ActiveSpecies)
        {
            // Generation index advances every tick regardless of whether a mutation fires.
            species.GenerationIndex++;

            // MutationVolatilityModifier is accumulated from Reddit vote payloads
            // (e.g. "Bombard with Cosmic Rays" += 0.08) and decays each tick.
            // Adding it to the per-genome volatility before clamping means active
            // vote events push even conservative genomes toward their mutation ceiling.
            double chaosMaxProb = Math.Clamp(
                _config.MutationProbabilityMax + _config.ChaosFactor * 0.20,
                0.0, 1.0);

            double mutProb = Math.Clamp(
                species.BaseGenome.MutationVolatility + _config.MutationVolatilityModifier,
                _config.MutationProbabilityMin,
                chaosMaxProb);

            if (rng.NextDouble() > mutProb) continue;

            var (mutated, didSpeciate, _) = NudgeGenome(species.BaseGenome, rng);

            if (didSpeciate)
            {
                // Skip speciation if the parent population is too small to split.
                if (species.Population <= 1) continue;

                // Split off a founding population for the new branch, but never
                // consume the entire parent population or drive it negative.
                long desiredBranchPop = Math.Max(_config.ExtinctionThreshold + 1, species.Population / 10);
                long branchPop        = Math.Min(desiredBranchPop, species.Population - 1);

                if (branchPop <= 0) continue;

                species.Population -= branchPop;

                var offspring = new SpeciesData(mutated, branchPop, species.SpeciesId)
                {
                    GenerationIndex = species.GenerationIndex
                };

                species.Branches.Add(offspring);
                newBranches.Add(offspring);
                EventBus.Publish(new SpeciationEvent(species, offspring, biome.Name));
            }
            else
            {
                // Minor mutation: nudge the existing genome in place
                species.BaseGenome = mutated;
            }
        }

        foreach (var branch in newBranches)
            pool.AddSpecies(branch);
    }

    private (Genome Mutated, bool DidSpeciate, MutationType MutationKind) NudgeGenome(Genome genome, Random rng)
    {
        // MutationVolatilityModifier widens the Gaussian spread proportionally to
        // Reddit vote chaos. At modifier = 0 (baseline): stdDev = GenomeMutationStdDev.
        // At modifier = 0.30 (three stacked Cosmic Ray votes): stdDev is 7× wider,
        // producing far more radical trait jumps per cycle.
        double stdDev = _config.GenomeMutationStdDev + _config.MutationVolatilityModifier;

        // Under high ChaosFactor, the speciation threshold tightens: smaller field
        // deltas are sufficient to branch a new species. This is the mathematical
        // expression of the "arms race" — chaotic environments accelerate divergence.
        // Floor at 0.05 to prevent every mutation becoming a speciation event.
        double effectiveSpeciationThreshold = Math.Max(
            _config.SpeciationThreshold * (1.0 - _config.ChaosFactor * 0.50),
            0.05);

        var  mutated   = genome;
        bool speciated = false;

        // Pick one field to mutate per reproduction cycle.
        // Fields 0–5 are scalar; 6 is structural; 7–9 are material.
        int field = rng.Next(10);
        MutationType kind;

        switch (field)
        {
            // ── Scalar mutations ──────────────────────────────────────────────
            case 0: NudgeField(ref mutated.IdealTempK,       nominalRange: 100f); kind = MutationType.Scalar; break;
            case 1: NudgeField(ref mutated.TempToleranceK,   nominalRange:  50f); kind = MutationType.Scalar; break;
            case 2: NudgeField(ref mutated.Mass,             nominalRange:   5f); kind = MutationType.Scalar; break;
            case 3: NudgeField(ref mutated.MetabolismRate,   nominalRange:   2f); kind = MutationType.Scalar; break;
            case 4: NudgeField(ref mutated.ReproductionRate, nominalRange: 0.1f); kind = MutationType.Scalar; break;
            case 5: NudgeField(ref mutated.LimbLengthAvg,    nominalRange: 1.0f); kind = MutationType.Scalar; break;

            // ── Structural mutation ───────────────────────────────────────────
            case 6:
                // Rarely add or remove a limb (capped 0–8)
                int priorLimbCount = mutated.LimbCount;
                int limbDelta = rng.NextDouble() < 0.5 ? 1 : -1;
                mutated.LimbCount = Math.Clamp(mutated.LimbCount + limbDelta, 0, 8);
                speciated = mutated.LimbCount != priorLimbCount;
                kind = MutationType.Structural;
                break;

            // ── Material mutations ────────────────────────────────────────────
            case 7:  NudgeFieldClamped(ref mutated.ColorH, 0f, 1f); kind = MutationType.Material; break;
            case 8:  NudgeFieldClamped(ref mutated.ColorS, 0f, 1f); kind = MutationType.Material; break;
            default: NudgeFieldClamped(ref mutated.ColorV, 0f, 1f); kind = MutationType.Material; break;
        }

        return (mutated, speciated, kind);

        void NudgeField(ref float f, float nominalRange)
        {
            double delta = BiomeMutationEngine.SampleGaussian(rng, 0, stdDev);
            f = Math.Max(0f, f + (float)delta);
            if (Math.Abs(delta) / nominalRange > effectiveSpeciationThreshold)
                speciated = true;
        }

        void NudgeFieldClamped(ref float f, float min, float max)
        {
            double delta = BiomeMutationEngine.SampleGaussian(rng, 0, stdDev);
            f = Math.Clamp(f + (float)delta, min, max);
        }
    }

    // ── Extinction ────────────────────────────────────────────────────────────

    private void CheckExtinction(PopulationPool pool, Biome biome)
    {
        foreach (var dead in pool.PruneExtinct())
            EventBus.Publish(new ExtinctionEvent(dead, biome.Name));

        // Also explicitly remove species that have fallen below the minimum threshold
        var belowThreshold = pool.ActiveSpecies
            .Where(s => s.Population < _config.ExtinctionThreshold)
            .ToList();

        foreach (var s in belowThreshold)
        {
            s.Population = 0;
            pool.RemoveSpecies(s);
            EventBus.Publish(new ExtinctionEvent(s, biome.Name));
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Genome GeneratePrimordialGenome(Biome biome, Random rng)
    {
        return new Genome
        {
            BodyPlan          = (BodyPlan)rng.Next(4),
            LimbCount         = rng.Next(0, 7),
            LimbLengthAvg     = (float)(0.3 + rng.NextDouble() * 0.7),
            BoneScaleX        = (float)(0.8 + rng.NextDouble() * 0.4),
            BoneScaleY        = (float)(0.8 + rng.NextDouble() * 0.4),
            BoneScaleZ        = (float)(0.8 + rng.NextDouble() * 0.4),
            SensoryOrgans     = SensoryOrganFlags.Eyes,
            ColorH            = (float)rng.NextDouble(),
            ColorS            = (float)(0.3 + rng.NextDouble() * 0.7),
            ColorV            = (float)(0.4 + rng.NextDouble() * 0.6),
            NeuralComplexity  = (float)(0.1 + rng.NextDouble() * 0.3),
            Speed             = (float)(0.2 + rng.NextDouble() * 0.8),
            Aggression        = (float)rng.NextDouble(),
            DietType          = (DietType)rng.Next(3),
            IdealTempK        = (float)biome.Temperature,
            TempToleranceK    = (float)(10 + rng.NextDouble() * 20),
            Mass              = (float)(0.1 + rng.NextDouble() * 0.9),
            MetabolismRate    = (float)(0.5 + rng.NextDouble() * 0.5),
            ReproductionRate  = (float)(0.001 + rng.NextDouble() * 0.009),
            MutationVolatility= (float)(0.05  + rng.NextDouble() * 0.10),
        };
    }

    /// <summary>Clean up any event subscriptions held by this manager.</summary>
    public void Detach()
    {
        _pools.Clear();
    }
}
