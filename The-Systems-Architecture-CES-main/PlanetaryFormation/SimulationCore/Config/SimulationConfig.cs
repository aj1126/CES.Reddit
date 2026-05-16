namespace PlanetaryFormation.SimulationCore.Config;

/// <summary>
/// Central repository for every tunable variable in the simulation.
/// In Unity this would be backed by a ScriptableObject; here it is a plain
/// C# class loaded once at start-up and injected into all systems.
/// Pillar V: Scientific Telemetry over Assumption — all balancing knobs live here.
/// </summary>
public class SimulationConfig : UniverseSettings
{
    // ── Time ──────────────────────────────────────────────────────────────────

    /// <summary>Simulated years advanced per real second at macro scale.</summary>
    public double MacroYearsPerSecond { get; set; } = 1_000_000.0;

    /// <summary>Simulated days advanced per real second at micro scale.</summary>
    public double MicroDaysPerSecond { get; set; } = 1.0;

    // ── Abiogenesis ───────────────────────────────────────────────────────────

    /// <summary>Prebiotic chemistry score (0–1) required to trigger abiogenesis.</summary>
    public double AbiogenesisThreshold { get; set; } = 0.75;

    /// <summary>Score increment per macro tick on an Optimal-band planet.</summary>
    public double AbiogenesisScoreRateOptimal { get; set; } = 0.005;

    /// <summary>Score increment per macro tick on a Habitable-band planet.</summary>
    public double AbiogenesisScoreRateHabitable { get; set; } = 0.001;

    // ── Mutation ──────────────────────────────────────────────────────────────

    /// <summary>Minimum per-genome mutation probability each reproduction cycle.</summary>
    public double MutationProbabilityMin { get; set; } = 0.01;

    /// <summary>Maximum per-genome mutation probability each reproduction cycle.</summary>
    public double MutationProbabilityMax { get; set; } = 0.30;

    /// <summary>
    /// If the absolute genome-field delta exceeds this fraction of the field's
    /// nominal range a new species branch is created.
    /// </summary>
    public double SpeciationThreshold { get; set; } = 0.25;

    /// <summary>Standard deviation of the Gaussian nudge applied to genome fields.</summary>
    public double GenomeMutationStdDev { get; set; } = 0.05;

    // ── Population ────────────────────────────────────────────────────────────

    /// <summary>Population at or below this value triggers an extinction check.</summary>
    public long ExtinctionThreshold { get; set; } = 10;

    /// <summary>Maximum population any single species can reach per biome.</summary>
    public long CarryingCapacityMax { get; set; } = 1_000_000;

    /// <summary>Starting population when a species is first seeded.</summary>
    public long InitialPopulation { get; set; } = 1_000;

    // ── Render ────────────────────────────────────────────────────────────────

    /// <summary>Hard cap on simultaneously spawned creature GameObjects.</summary>
    public int MaxActiveCreatures { get; set; } = 50;

    /// <summary>
    /// World-unit distance below which creatures enter the simplified-visuals LOD tier
    /// (Near — simplified visuals, no full physics rig).
    /// Maps to Level 2 (Planet scale) → Level 3 (Local/Player view) transition in the GDD.
    /// </summary>
    public float NearLodDistance { get; set; } = 200f;

    /// <summary>
    /// World-unit distance below which creatures receive a full physics rig
    /// (Rigidbody + ConfigurableJoints + Colliders) — the Close LOD tier.
    /// The GDD recommends limiting active physics creatures to 20–100.
    /// </summary>
    public float CloseLodDistance { get; set; } = 50f;

    /// <summary>
    /// Maximum number of creatures allowed in the full-physics Close LOD tier.
    /// Beyond this count, new candidates remain in the Near (simplified) tier.
    /// </summary>
    public int MaxPhysicsCreatures { get; set; } = 50;

    // ── Telemetry ─────────────────────────────────────────────────────────────

    /// <summary>How many logged events to buffer before flushing to CSV.</summary>
    public int TelemetryFlushInterval { get; set; } = 100;

    /// <summary>Directory where telemetry CSV files are written.</summary>
    public string TelemetryOutputDirectory { get; set; } = "Telemetry";

    // ── Procedural Events ─────────────────────────────────────────────────────

    /// <summary>
    /// Mean interval in simulated years between catastrophic procedural events
    /// (Poisson process). Higher values mean rarer events.
    /// </summary>
    public double ProceduralEventMeanIntervalYears { get; set; } = 500_000.0;

    /// <summary>
    /// Fraction of a biome's active species wiped out by a mass-extinction
    /// procedural event (0–1).
    /// </summary>
    public double MassExtinctionFraction { get; set; } = 0.5;

    /// <summary>
    /// Prebiotic score boost delivered to a random sterile planet by a
    /// panspermia / comet-delivery event.
    /// </summary>
    public double PanspermiaScoreBoost { get; set; } = 0.35;

    // ── Goals ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Default scenario time limit in simulated years.
    /// The player loses if all habitable planets are sterile before this elapses.
    /// </summary>
    public double DefaultScenarioTimeLimitYears { get; set; } = 50_000_000.0;
    // ── Reddit Game Loop ──────────────────────────────────────────────────────
    //
    // V2 delta notes:
    //   • MutationVolatilityModifier replaces V2's flat mutationVolatility = 15.0f
    //     (an accumulator counter, ~7% effective per-tick rate) with an additive
    //     multiplier on top of GenomeMutationStdDev. V1/current default (0.0) is
    //     neutral; a Reddit "Bombard with Cosmic Rays" vote adds +0.08 per ballot.
    //   • ChaosFactor exposes the previously local chaosPressure variable from
    //     ApplyRedditEngagementChaos() as a persistent, observable property so
    //     DataMiningAutomator can log it every tick without recomputing.
    //   • CurrentRedditEngagement stores the raw weighted score
    //     (upvotes × UpvoteEngagementWeight + comments × CommentEngagementWeight)
    //     from the last poll window. V2 had no equivalent — engagement was
    //     recomputed inline and discarded.
    //   • EdenSystemLifeChance is the V2 "Pity Timer". V2 defaulted to 1.0
    //     (guaranteed life in every star system). We lower it to 0.15 so life
    //     emergence is earned, not automatic — the community must vote to steer
    //     a comet or trigger abiogenesis to reach 1.0 via StewardCommands.
    //   • StewardshipCosmicEnergyRechargePerTick / CosmicEnergyPoolMax are the
    //     balancing knobs for the command-queue economy added in the refinement.

    /// <summary>
    /// Normalized chaos pressure [0, 1] from the most recent Reddit engagement
    /// window. 0 = no engagement, 1 = maximum chaos. Persisted here so
    /// TelemetryLogger and ComplexityEngine can read it without recomputing.
    /// </summary>
    public double ChaosFactor { get; set; } = 0.0;

    /// <summary>
    /// Raw weighted Reddit engagement score from the last poll parse:
    ///   upvotes × UpvoteEngagementWeight + comments × CommentEngagementWeight.
    /// Written by SimulationConfig.ApplyRedditEngagementChaos() each weekly tick.
    /// </summary>
    public double CurrentRedditEngagement { get; set; } = 0.0;

    /// <summary>
    /// Additive modifier applied on top of GenomeMutationStdDev during NudgeGenome().
    /// Accumulated from Reddit vote payloads ("Bombard with Cosmic Rays" += 0.08).
    /// Decays by MutationVolatilityDecayPerTick each tick toward 0.
    /// Range [0, 1]; values above ~0.3 cause near-guaranteed speciation every cycle.
    /// V2 equivalent: mutationVolatility accumulator threshold of 15.0f (~7%/tick).
    /// </summary>
    public double MutationVolatilityModifier { get; set; } = 0.0;

    /// <summary>
    /// How much MutationVolatilityModifier decays per tick toward baseline.
    /// Prevents a single "Solar Flare" vote from permanently destabilizing the genome pool.
    /// Set to 0 to make volatility permanent until overridden (V2 behaviour).
    /// </summary>
    public double MutationVolatilityDecayPerTick { get; set; } = 0.005;

    /// <summary>
    /// Probability [0, 1] that any newly generated star system is guaranteed to
    /// contain at least one life-bearing planet regardless of abiogenesis RNG.
    /// V2 default: 1.0 (always guaranteed — acts as a pity timer so the subreddit
    /// is never stuck watching a dead rock for weeks).
    /// CES.Reddit default: 0.15 — life must be earned; community votes push it higher.
    /// </summary>
    public double EdenSystemLifeChance { get; set; } = 0.15;

    /// <summary>
    /// Cosmic Energy recharged per tick for the StewardshipManager command queue.
    /// Higher values let the subreddit apply catalysts more frequently.
    /// </summary>
    public float StewardshipCosmicEnergyRechargePerTick { get; set; } = 10f;

    /// <summary>
    /// Hard cap on the community's Cosmic Energy pool.
    /// At 200 the community can bank ~20 ticks of energy before it is wasted.
    /// </summary>
    public float StewardshipCosmicEnergyPoolMax { get; set; } = 200f;
    // ── Singleton ─────────────────────────────────────────────────────────────

    private static SimulationConfig? _instance;

    /// <summary>
    /// Returns the global singleton, creating it with defaults if needed.
    /// Replace with ScriptableObject asset loading in Unity.
    /// </summary>
    public static SimulationConfig Instance => _instance ??= new SimulationConfig();

    /// <summary>Allows injecting a custom config (e.g. from tests).</summary>
    public static void SetInstance(SimulationConfig config) => _instance = config;

    /// <summary>
    /// Convenience wrapper for applying the Reddit engagement-to-chaos link using
    /// this config instance's mutation and procedural-event settings.
    /// Also persists ChaosFactor and CurrentRedditEngagement for telemetry.
    /// </summary>
    public double ApplyRedditEngagementChaos(
        long upvotes,
        long comments,
        IEnumerable<Models.CelestialBody>? planets = null)
    {
        var updated = base.ApplyRedditEngagementChaos(
            upvotes,
            comments,
            planets,
            MutationProbabilityMin,
            MutationProbabilityMax,
            ProceduralEventMeanIntervalYears);

        MutationProbabilityMin = updated.MutationProbabilityMin;
        MutationProbabilityMax = updated.MutationProbabilityMax;
        ProceduralEventMeanIntervalYears = updated.ProceduralEventMeanIntervalYears;

        // Persist the observable Reddit state so DataMiningAutomator and
        // TelemetryLogger can read without recomputing.
        CurrentRedditEngagement =
            Math.Max(0, upvotes)  * UpvoteEngagementWeight +
            Math.Max(0, comments) * CommentEngagementWeight;
        ChaosFactor = Math.Clamp(
            1.0 - Math.Exp(-CurrentRedditEngagement * EngagementChaosResponse),
            0.0, 1.0);

        return updated.ChaosMultiplier;
    }

    /// <summary>
    /// Decays MutationVolatilityModifier by MutationVolatilityDecayPerTick.
    /// Call once per TickEvent from MacroSimulationManager.OnTick() so Reddit
    /// vote volatility spikes fade naturally between weekly polls.
    /// </summary>
    public void TickVolatilityDecay()
    {
        MutationVolatilityModifier =
            Math.Max(0.0, MutationVolatilityModifier - MutationVolatilityDecayPerTick);
    }
}
