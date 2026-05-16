using PlanetaryFormation.Models;
using PlanetaryFormation.SimulationCore.Config;
using PlanetaryFormation.SimulationCore.Events;
using PlanetaryFormation.SimulationCore.Macro;
using PlanetaryFormation.SimulationCore.Micro;
using PlanetaryFormation.SimulationCore.Mutation;

namespace PlanetaryFormation.SimulationCore.Debug;

/// <summary>
/// Static toolkit for developer and QA use.
/// Every method bypasses normal simulation rules so that specific outcomes can
/// be forced instantly — useful for testing a particular life stage, verifying
/// render-bridge behaviour, or fast-forwarding to an interesting state.
///
/// None of these methods should be called from production game logic; they are
/// intended exclusively for editor tooling, automated tests, and manual QA runs.
/// </summary>
public static class DeveloperOverrides
{
    // ── Life emergence ────────────────────────────────────────────────────────

    /// <summary>
    /// Forces abiogenesis on <paramref name="planet"/> immediately, regardless of
    /// its current prebiotic chemistry score.
    /// <list type="bullet">
    ///   <item>Sets <c>PrebioticChemistryScore = 1.0</c></item>
    ///   <item>Sets <c>LifeStage = Microbial</c></item>
    ///   <item>Marks the planet as seeded in <paramref name="abiogenesisEngine"/></item>
    ///   <item>Calls <see cref="MicroSimulationManager.SeedLife"/> to create primordial species</item>
    ///   <item>Publishes <see cref="AbiogenesisEvent"/> so all subscribers are notified</item>
    /// </list>
    /// </summary>
    public static void ForceAbiogenesis(
        CelestialBody         planet,
        AbiogenesisEngine     abiogenesisEngine,
        MicroSimulationManager microManager,
        Random                rng,
        double                simulatedYears = 0)
    {
        bool alreadySeeded = abiogenesisEngine.HasLifeBeenSeeded(planet);
        abiogenesisEngine.ForceAbiogenesis(planet, simulatedYears);
        // Only seed the micro pool once — ForceAbiogenesis is a no-op when the planet
        // is already seeded, but SeedLife would still append duplicate primordial species.
        if (!alreadySeeded)
            microManager.SeedLife(planet, rng);
    }

    // ── Life stage ────────────────────────────────────────────────────────────

    /// <summary>
    /// Jumps <paramref name="planet"/>'s <see cref="LifeStage"/> directly to
    /// <paramref name="targetStage"/>, skipping all intermediate thresholds.
    ///
    /// If <paramref name="targetStage"/> is <see cref="LifeStage.Microbial"/> or
    /// higher and no life has been seeded yet, abiogenesis is also forced so the
    /// micro-simulation pool is not left empty.
    /// </summary>
    public static void ForceLifeStage(
        CelestialBody          planet,
        LifeStage              targetStage,
        AbiogenesisEngine?     abiogenesisEngine = null,
        MicroSimulationManager? microManager     = null,
        Random?                rng               = null)
    {
        if (targetStage >= LifeStage.Microbial
            && planet.LifeStage == LifeStage.Sterile
            && abiogenesisEngine is not null
            && microManager     is not null
            && rng              is not null)
        {
            abiogenesisEngine.ForceAbiogenesis(planet);
            microManager.SeedLife(planet, rng);
        }

        planet.LifeStage = targetStage;
    }

    // ── Speciation ────────────────────────────────────────────────────────────

    /// <summary>
    /// Forces an immediate speciation event in <paramref name="pool"/>.
    /// Picks the most populous species as the parent, forks off a branch with a
    /// randomised genome nudge, and publishes <see cref="SpeciationEvent"/>.
    /// Returns the new branch, or <c>null</c> if the pool has no eligible parent.
    /// </summary>
    public static SpeciesData? ForceSpeciation(
        PopulationPool pool,
        string         biomeName,
        Random         rng,
        SimulationConfig? config = null)
    {
        var cfg    = config ?? SimulationConfig.Instance;
        var parent = pool.ActiveSpecies
            .Where(s => s.Population > cfg.ExtinctionThreshold * 2)
            .OrderByDescending(s => s.Population)
            .FirstOrDefault();

        if (parent is null) return null;

        long branchPop = Math.Max(cfg.ExtinctionThreshold + 1, parent.Population / 5);
        branchPop      = Math.Min(branchPop, parent.Population - 1);
        if (branchPop <= 0) return null;

        parent.Population -= branchPop;

        var mutatedGenome = NudgeGenomeFull(parent.BaseGenome, rng, cfg);
        var offspring = new SpeciesData(mutatedGenome, branchPop, parent.SpeciesId)
        {
            GenerationIndex = parent.GenerationIndex
        };

        parent.Branches.Add(offspring);
        pool.AddSpecies(offspring);
        EventBus.Publish(new SpeciationEvent(parent, offspring, biomeName));
        return offspring;
    }

    // ── Extinction ────────────────────────────────────────────────────────────

    /// <summary>
    /// Forces <paramref name="species"/> to immediate extinction in <paramref name="pool"/>.
    /// Sets its population to 0, removes it from the pool, and publishes
    /// <see cref="ExtinctionEvent"/>.
    /// </summary>
    public static void ForceExtinction(
        SpeciesData    species,
        PopulationPool pool,
        string         biomeName)
    {
        species.Population = 0;
        pool.RemoveSpecies(species);
        EventBus.Publish(new ExtinctionEvent(species, biomeName));
    }

    // ── Genome / traits ───────────────────────────────────────────────────────

    /// <summary>
    /// Directly edits a species' genome via a caller-supplied transformation function.
    /// The function receives the current genome and must return the modified copy.
    /// Use this to pin specific trait values for testing without going through the
    /// mutation pipeline.
    /// <example>
    /// <code>
    /// DeveloperOverrides.ForceGenomeTraits(species, g =>
    /// {
    ///     g.Speed      = 2.0f;
    ///     g.LimbCount  = 6;
    ///     g.BodyPlan   = BodyPlan.Bilateral;
    ///     return g;
    /// });
    /// </code>
    /// </example>
    /// </summary>
    public static void ForceGenomeTraits(SpeciesData species, Func<Genome, Genome> configure) =>
        species.BaseGenome = configure(species.BaseGenome);

    // ── Population ────────────────────────────────────────────────────────────

    /// <summary>
    /// Immediately sets <paramref name="species"/>'s population to
    /// <paramref name="population"/>, clamped to [0, CarryingCapacityMax].
    /// </summary>
    public static void ForcePopulation(
        SpeciesData    species,
        long           population,
        SimulationConfig? config = null)
    {
        var cfg = config ?? SimulationConfig.Instance;
        species.Population = Math.Clamp(population, 0, cfg.CarryingCapacityMax);
    }

    // ── Planet environment ────────────────────────────────────────────────────

    /// <summary>
    /// Overrides the two atmosphere/water habitability inputs on a planet:
    /// sets <c>AtmosphericDensity = 0.90</c> and <c>SurfaceWaterPercent = 0.70</c>.
    /// These are the values that make a planet pass the atmosphere and water checks
    /// used by <see cref="PlanetaryFormation.Models.CelestialBody.IsHabitable"/> and
    /// <see cref="HabitabilityCalculator"/>.
    ///
    /// Note: <c>IsHabitable</c> also requires an orbital radius between 0.70 AU and
    /// 1.80 AU. This override does <b>not</b> change orbital radius, so a planet
    /// outside the habitable band (e.g. an outer ice planet) will still return
    /// <c>IsHabitable = false</c> after the call.
    /// Useful for bypassing early planetary-formation simulation when testing
    /// life-stage or abiogenesis logic on an otherwise atmosphere-less planet.
    /// </summary>
    public static void ForceMaxHabitability(CelestialBody planet)
    {
        planet.AtmosphericDensity  = 0.90;
        planet.SurfaceWaterPercent = 0.70;
    }

    /// <summary>
    /// Sets the prebiotic chemistry score directly.
    /// A value ≥ <see cref="SimulationConfig.AbiogenesisThreshold"/> will cause the
    /// next <c>AbiogenesisEngine.Tick</c> call to trigger abiogenesis naturally.
    /// </summary>
    public static void ForcePrebioticScore(CelestialBody planet, double score) =>
        planet.PrebioticChemistryScore = Math.Clamp(score, 0.0, 1.0);

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Produces a mutated genome by nudging every scalar field by a small Gaussian
    /// amount. Used internally by <see cref="ForceSpeciation"/> to generate an
    /// offspring genome that is visibly distinct from the parent.
    /// </summary>
    private static Genome NudgeGenomeFull(Genome g, Random rng, SimulationConfig cfg)
    {
        float Nudge(float value, float scale = 1f)
        {
            double delta = BiomeMutationEngine.SampleGaussian(rng, 0, cfg.GenomeMutationStdDev * 2);
            return Math.Max(0f, value + (float)delta * scale);
        }

        g.Mass              = Nudge(g.Mass,              5f);
        g.Speed             = Math.Clamp(Nudge(g.Speed,  2f), 0f, 5f);
        g.MetabolismRate    = Nudge(g.MetabolismRate,    2f);
        g.ReproductionRate  = Nudge(g.ReproductionRate,  0.1f);
        g.LimbLengthAvg     = Nudge(g.LimbLengthAvg,    1f);
        g.TempToleranceK    = Nudge(g.TempToleranceK,   50f);
        g.NeuralComplexity  = Math.Clamp(Nudge(g.NeuralComplexity), 0f, 1f);
        g.Aggression        = Math.Clamp(Nudge(g.Aggression), 0f, 1f);
        g.ColorH            = Math.Clamp(Nudge(g.ColorH), 0f, 1f);
        g.ColorS            = Math.Clamp(Nudge(g.ColorS), 0f, 1f);
        g.ColorV            = Math.Clamp(Nudge(g.ColorV), 0f, 1f);

        // Structural roll: occasionally flip body plan or adjust limb count
        if (rng.NextDouble() < 0.25)
            g.BodyPlan = (BodyPlan)rng.Next(4);
        if (rng.NextDouble() < 0.20)
            g.LimbCount = Math.Clamp(g.LimbCount + (rng.NextDouble() < 0.5 ? 1 : -1), 0, 8);

        return g;
    }
}
