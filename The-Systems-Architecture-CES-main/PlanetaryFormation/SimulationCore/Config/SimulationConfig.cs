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
    /// </summary>
    public double ApplyRedditEngagementChaos(
        long upvotes,
        long comments,
        IEnumerable<Models.CelestialBody>? planets = null)
    {
        return base.ApplyRedditEngagementChaos(
            upvotes,
            comments,
            planets,
            ref MutationProbabilityMin,
            ref MutationProbabilityMax,
            ref ProceduralEventMeanIntervalYears);
    }
}
