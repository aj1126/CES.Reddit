using PlanetaryFormation.Models;

namespace PlanetaryFormation.SimulationCore.Config;

/// <summary>
/// Universe-level balancing settings for emergent chaos and thermodynamic pressure.
/// </summary>
public class UniverseSettings
{
    // ── Engagement → Chaos Link ───────────────────────────────────────────────

    /// <summary>Weight applied to each upvote when computing engagement pressure.</summary>
    public double UpvoteEngagementWeight { get; set; } = 1.0;

    /// <summary>Weight applied to each comment when computing engagement pressure.</summary>
    public double CommentEngagementWeight { get; set; } = 2.0;

    /// <summary>
    /// Non-linear response factor that converts weighted Reddit engagement into a
    /// 0–1 chaos pressure score.
    /// </summary>
    public double EngagementChaosResponse { get; set; } = 0.0004;

    /// <summary>
    /// Maximum additive boost on top of 1.0 for mutation/event acceleration.
    /// Example: 2.0 means max multiplier is 3.0x.
    /// </summary>
    public double MaxChaosMultiplierBoost { get; set; } = 2.0;

    /// <summary>Minimum allowable mean interval between procedural events.</summary>
    public double MinProceduralEventMeanIntervalYears { get; set; } = 10_000.0;

    /// <summary>Maximum biome-level mutation rate after engagement scaling.</summary>
    public double MaxBiomeMutationRateUnderEngagement { get; set; } = 0.95;

    /// <summary>
    /// Last multiplier generated from Reddit engagement. 1.0 means neutral.
    /// </summary>
    public double CurrentEngagementChaosMultiplier { get; private set; } = 1.0;

    private bool _engagementBaselineCaptured;
    private double _baseMutationProbabilityMin;
    private double _baseMutationProbabilityMax;
    private double _baseProceduralEventMeanIntervalYears;
    private readonly Dictionary<Biome, double> _baseBiomeMutationRates = new();

    /// <summary>
    /// Applies external Reddit engagement (upvotes/comments) to globally scale
    /// mutation pressure and procedural-event cadence.
    /// </summary>
    /// <param name="upvotes">Raw upvote count for the current engagement window.</param>
    /// <param name="comments">Raw comment count for the current engagement window.</param>
    /// <param name="planets">
    /// Optional planet set whose biome mutation rates are scaled alongside global
    /// genome mutation probabilities.
    /// </param>
    /// <param name="mutationProbabilityMin">Reference to config minimum mutation probability.</param>
    /// <param name="mutationProbabilityMax">Reference to config maximum mutation probability.</param>
    /// <param name="proceduralEventMeanIntervalYears">Reference to mean event interval years.</param>
    /// <returns>The applied chaos multiplier (>= 1.0).</returns>
    public double ApplyRedditEngagementChaos(
        long upvotes,
        long comments,
        IEnumerable<CelestialBody>? planets,
        ref double mutationProbabilityMin,
        ref double mutationProbabilityMax,
        ref double proceduralEventMeanIntervalYears)
    {
        upvotes  = Math.Max(0, upvotes);
        comments = Math.Max(0, comments);

        if (!_engagementBaselineCaptured)
        {
            _baseMutationProbabilityMin          = mutationProbabilityMin;
            _baseMutationProbabilityMax          = mutationProbabilityMax;
            _baseProceduralEventMeanIntervalYears = proceduralEventMeanIntervalYears;
            _engagementBaselineCaptured          = true;
        }

        double weightedEngagement =
            upvotes * UpvoteEngagementWeight +
            comments * CommentEngagementWeight;

        double chaosPressure = 1.0 - Math.Exp(-weightedEngagement * EngagementChaosResponse);
        CurrentEngagementChaosMultiplier = 1.0 + chaosPressure * MaxChaosMultiplierBoost;

        mutationProbabilityMin = Math.Clamp(
            _baseMutationProbabilityMin * CurrentEngagementChaosMultiplier,
            0.0,
            1.0);

        mutationProbabilityMax = Math.Clamp(
            _baseMutationProbabilityMax * CurrentEngagementChaosMultiplier,
            mutationProbabilityMin,
            1.0);

        proceduralEventMeanIntervalYears = Math.Max(
            MinProceduralEventMeanIntervalYears,
            _baseProceduralEventMeanIntervalYears / CurrentEngagementChaosMultiplier);

        if (planets is not null)
            ScaleBiomeMutationRates(planets, CurrentEngagementChaosMultiplier);

        return CurrentEngagementChaosMultiplier;
    }

    private void ScaleBiomeMutationRates(IEnumerable<CelestialBody> planets, double multiplier)
    {
        foreach (var biome in planets.SelectMany(p => p.Biomes))
        {
            if (!_baseBiomeMutationRates.ContainsKey(biome))
                _baseBiomeMutationRates[biome] = biome.MutationRate;

            double baseline = _baseBiomeMutationRates[biome];
            biome.MutationRate = Math.Clamp(
                baseline * multiplier,
                0.0,
                MaxBiomeMutationRateUnderEngagement);
        }
    }

    // ── Complexity / Heat-Death tuning ────────────────────────────────────────

    /// <summary>Natural entropy drift added each tick (percentage points).</summary>
    public double EntropyCreepPerTick { get; set; } = 0.08;

    /// <summary>Lower bound so entropy always creeps upward every tick.</summary>
    public double MinimumEntropyIncreasePerTick { get; set; } = 0.01;

    /// <summary>Maximum entropy reduction pressure from complexity (still respects minimum creep).</summary>
    public double ComplexityEntropyMitigationMax { get; set; } = 0.06;

    /// <summary>Target chaos floor; below this, complexity plateaus from over-stability.</summary>
    public double ComplexityStableChaosFloor { get; set; } = 0.20;

    /// <summary>Target chaos ceiling; above this, ecosystems begin to collapse.</summary>
    public double ComplexityStableChaosCeiling { get; set; } = 0.65;

    /// <summary>Extra entropy pressure when the simulation is too stable.</summary>
    public double StabilityEntropyPenaltyPerTick { get; set; } = 0.09;

    /// <summary>Extra entropy pressure when the simulation is too chaotic.</summary>
    public double ChaosEntropyPenaltyPerTick { get; set; } = 0.25;

    /// <summary>Additional entropy spike on active collapse under high chaos.</summary>
    public double CollapseEntropySpikePerTick { get; set; } = 0.35;

    /// <summary>Maximum complexity suppression when the universe is too stable.</summary>
    public double StabilityPlateauPenaltyMax { get; set; } = 0.40;

    /// <summary>Maximum complexity suppression when the universe is too chaotic.</summary>
    public double ChaoticCollapsePenaltyMax { get; set; } = 0.70;

    /// <summary>Target species richness used for normalizing complexity.</summary>
    public int ComplexityTargetSpeciesCount { get; set; } = 250;

    /// <summary>Target biomass used for normalizing complexity.</summary>
    public double ComplexityTargetBiomass { get; set; } = 5_000_000_000;

    /// <summary>Baseline mutation-rate target used for chaos-index normalization.</summary>
    public double ComplexityTargetMutationRate { get; set; } = 0.18;

    /// <summary>Weight for diversity in the complexity composite score.</summary>
    public double ComplexityDiversityWeight { get; set; } = 0.45;

    /// <summary>Weight for biomass in the complexity composite score.</summary>
    public double ComplexityBiomassWeight { get; set; } = 0.30;

    /// <summary>Weight for life-stage advancement in the complexity composite score.</summary>
    public double ComplexityLifeStageWeight { get; set; } = 0.25;
}
