using PlanetaryFormation.Models;
using PlanetaryFormation.SimulationCore.Config;
using PlanetaryFormation.SimulationCore.Events;
using PlanetaryFormation.SimulationCore.Micro;

namespace PlanetaryFormation.SimulationCore.IO;

/// <summary>
/// External poll input item (simulated Reddit vote result).
/// </summary>
public readonly record struct RedditPollResult(string Option, int Votes);

/// <summary>
/// Parsed internal vote directives that downstream systems can apply on a weekly tick.
/// </summary>
public sealed class RedditVotePayload
{
    public float MutationVolatilityDelta { get; set; }
    public double GlobalTemperatureModifierDelta { get; set; }
    public double SurfaceWaterPercentDelta { get; set; }
    public double PrebioticChemistryScoreDelta { get; set; }
    public bool ForceAlphaCarnivore { get; set; }
    public double OrbitalRadiusDeltaAu { get; set; }
}

/// <summary>
/// Published on the <see cref="EventBus"/> after a winning Reddit poll is fully
/// applied to <see cref="SimulationConfig"/>.  DataMiningAutomator and any other
/// observer subscribe here — they never hold a direct reference to the parser or
/// the config, preserving the decoupled EventBus contract.
/// </summary>
public record PollAppliedEvent(
    /// <summary>The poll option that received the most votes this window.</summary>
    string WinningOption,
    /// <summary>Raw upvote count supplied to the poll window.</summary>
    long Upvotes,
    /// <summary>Raw comment count supplied to the poll window.</summary>
    long Comments,
    /// <summary>Chaos multiplier returned by ApplyRedditEngagementChaos (1.0 = neutral).</summary>
    double ChaosMultiplier,
    /// <summary>MutationVolatilityModifier after this poll's delta was applied.</summary>
    double MutationVolatilityModifier,
    /// <summary>ProceduralEventMeanIntervalYears after engagement scaling.</summary>
    double ProceduralEventMeanIntervalYears);

/// <summary>
/// Maps external Reddit poll outcomes to simulation-facing variables and effects.
/// </summary>
public sealed class RedditVoteParser
{
    public const string BombardWithCosmicRaysOption   = "Bombard with Cosmic Rays";
    public const string TriggerVolcanicActivityOption = "Trigger Volcanic Activity";
    public const string SteerCometSwarmOption         = "Steer a Comet Swarm";
    public const string AggressivePredationDriveOption = "Aggressive Predation Drive";
    public const string IncreaseSolarProximityOption  = "Increase Solar Proximity";
    private const double MinimumOrbitalRadiusAu = 0.01;

    private readonly Dictionary<string, double> _planetTemperatureModifiers = new(StringComparer.OrdinalIgnoreCase);

    private readonly float _mutationVolatilitySpike;
    private readonly double _globalTemperatureModifierStep;
    private readonly double _surfaceWaterBoost;
    private readonly double _prebioticChemistryBoost;
    private readonly double _orbitalRadiusReductionAu;

    public RedditVoteParser(
        float mutationVolatilitySpike = 0.08f,
        double globalTemperatureModifierStep = 4.0,
        double surfaceWaterBoost = 0.08,
        double prebioticChemistryBoost = 0.20,
        double orbitalRadiusReductionAu = 0.03)
    {
        _mutationVolatilitySpike = mutationVolatilitySpike;
        _globalTemperatureModifierStep = globalTemperatureModifierStep;
        _surfaceWaterBoost = surfaceWaterBoost;
        _prebioticChemistryBoost = prebioticChemistryBoost;
        _orbitalRadiusReductionAu = orbitalRadiusReductionAu;
    }

    /// <summary>
    /// Parses a set of poll result rows into a single internal payload.
    /// Each row's vote count scales the effect magnitude.
    /// </summary>
    public RedditVotePayload Parse(IEnumerable<RedditPollResult> pollResults)
    {
        if (pollResults is null) throw new ArgumentNullException(nameof(pollResults));

        var payload = new RedditVotePayload();

        foreach (var result in pollResults)
        {
            int intensity = Math.Max(0, result.Votes);
            if (intensity == 0) continue;

            string option = result.Option?.Trim() ?? string.Empty;

            if (option.Equals(BombardWithCosmicRaysOption, StringComparison.OrdinalIgnoreCase))
                HandleBombardWithCosmicRays(payload, intensity);
            else if (option.Equals(TriggerVolcanicActivityOption, StringComparison.OrdinalIgnoreCase))
                HandleTriggerVolcanicActivity(payload, intensity);
            else if (option.Equals(SteerCometSwarmOption, StringComparison.OrdinalIgnoreCase))
                HandleSteerCometSwarm(payload, intensity);
            else if (option.Equals(AggressivePredationDriveOption, StringComparison.OrdinalIgnoreCase))
                HandleAggressivePredationDrive(payload, intensity);
            else if (option.Equals(IncreaseSolarProximityOption, StringComparison.OrdinalIgnoreCase))
                HandleIncreaseSolarProximity(payload, intensity);
        }

        return payload;
    }

    /// <summary>
    /// "Bombard with Cosmic Rays" -> spikes mutation volatility.
    /// </summary>
    public void HandleBombardWithCosmicRays(RedditVotePayload payload, int intensity = 1)
    {
        EnsurePayload(payload);
        payload.MutationVolatilityDelta += _mutationVolatilitySpike * Math.Max(1, intensity);
    }

    /// <summary>
    /// "Trigger Volcanic Activity" -> increases a planet's global temperature modifier.
    /// </summary>
    public void HandleTriggerVolcanicActivity(RedditVotePayload payload, int intensity = 1)
    {
        EnsurePayload(payload);
        payload.GlobalTemperatureModifierDelta += _globalTemperatureModifierStep * Math.Max(1, intensity);
    }

    /// <summary>
    /// "Steer a Comet Swarm" -> increases surface water and prebiotic chemistry score.
    /// </summary>
    public void HandleSteerCometSwarm(RedditVotePayload payload, int intensity = 1)
    {
        EnsurePayload(payload);
        int scaled = Math.Max(1, intensity);
        payload.SurfaceWaterPercentDelta += _surfaceWaterBoost * scaled;
        payload.PrebioticChemistryScoreDelta += _prebioticChemistryBoost * scaled;
    }

    /// <summary>
    /// "Aggressive Predation Drive" -> forces alpha species to carnivore.
    /// </summary>
    public void HandleAggressivePredationDrive(RedditVotePayload payload, int intensity = 1)
    {
        EnsurePayload(payload);
        payload.ForceAlphaCarnivore = true;
    }

    /// <summary>
    /// "Increase Solar Proximity" -> decreases orbital radius.
    /// </summary>
    public void HandleIncreaseSolarProximity(RedditVotePayload payload, int intensity = 1)
    {
        EnsurePayload(payload);
        payload.OrbitalRadiusDeltaAu -= _orbitalRadiusReductionAu * Math.Max(1, intensity);
    }

    /// <summary>
    /// Applies parsed planet-level effects to a target planet.
    /// </summary>
    public void ApplyToPlanet(CelestialBody planet, RedditVotePayload payload)
    {
        if (planet is null) throw new ArgumentNullException(nameof(planet));
        EnsurePayload(payload);

        if (!_planetTemperatureModifiers.TryGetValue(planet.Name, out var currentModifier))
            currentModifier = 0.0;

        currentModifier += payload.GlobalTemperatureModifierDelta;
        _planetTemperatureModifiers[planet.Name] = currentModifier;

        if (Math.Abs(payload.GlobalTemperatureModifierDelta) > double.Epsilon)
        {
            foreach (var biome in planet.Biomes)
                biome.Temperature = Math.Max(0.0, biome.Temperature + payload.GlobalTemperatureModifierDelta);
        }

        planet.SurfaceWaterPercent = Math.Clamp(
            planet.SurfaceWaterPercent + payload.SurfaceWaterPercentDelta,
            0.0,
            1.0);

        planet.PrebioticChemistryScore = Math.Clamp(
            planet.PrebioticChemistryScore + payload.PrebioticChemistryScoreDelta,
            0.0,
            1.0);

        planet.OrbitalRadiusAU = Math.Max(
            MinimumOrbitalRadiusAu,
            planet.OrbitalRadiusAU + payload.OrbitalRadiusDeltaAu);
    }

    /// <summary>
    /// Returns the accumulated global temperature modifier tracked for a planet.
    /// </summary>
    public double GetGlobalTemperatureModifier(CelestialBody planet)
    {
        if (planet is null) throw new ArgumentNullException(nameof(planet));
        return _planetTemperatureModifiers.TryGetValue(planet.Name, out var value) ? value : 0.0;
    }

    /// <summary>
    /// Finds the current alpha species (highest population) in a pool.
    /// </summary>
    public SpeciesData? GetAlphaSpecies(PopulationPool pool)
    {
        if (pool is null) throw new ArgumentNullException(nameof(pool));
        return pool.ActiveSpecies.OrderByDescending(s => s.Population).FirstOrDefault();
    }

    /// <summary>
    /// Applies parsed species-level effects to the current alpha species.
    /// </summary>
    public void ApplyToAlphaSpecies(SpeciesData? alphaSpecies, RedditVotePayload payload)
    {
        EnsurePayload(payload);
        if (alphaSpecies is null) return;

        Genome mutatedGenome = alphaSpecies.BaseGenome;
        mutatedGenome.MutationVolatility = Math.Clamp(
            mutatedGenome.MutationVolatility + payload.MutationVolatilityDelta,
            0f,
            1f);

        if (payload.ForceAlphaCarnivore)
            mutatedGenome.DietType = DietType.Carnivore;

        alphaSpecies.BaseGenome = mutatedGenome;
    }

    /// <summary>
    /// Applies a parsed payload's <em>SimulationConfig-level</em> effects:
    /// <list type="bullet">
    ///   <item>
    ///     "Bombard with Cosmic Rays" → accumulates
    ///     <see cref="SimulationConfig.MutationVolatilityModifier"/>, which widens
    ///     the Gaussian genome nudge in <c>MicroSimulationManager.NudgeGenome()</c>
    ///     and decays naturally each tick via <c>TickVolatilityDecay()</c>.
    ///   </item>
    ///   <item>
    ///     All options → calls <see cref="SimulationConfig.ApplyRedditEngagementChaos"/>
    ///     to update <c>ChaosFactor</c>, <c>CurrentRedditEngagement</c>, and
    ///     <c>ProceduralEventMeanIntervalYears</c> via the engagement-chaos formula,
    ///     which causes the <see cref="Game.ProceduralEventEngine"/> to shorten its
    ///     next scheduled catastrophe on the very next tick.
    ///   </item>
    /// </list>
    /// This method is intentionally synchronous — it writes to shared simulation
    /// state and must run on the caller's (main/sim-thread) context after
    /// <see cref="ParseAndApplyAsync"/> returns from its background thread.
    /// </summary>
    /// <param name="payload">Payload produced by <see cref="Parse"/>.</param>
    /// <param name="upvotes">Raw upvote count for this engagement window.</param>
    /// <param name="comments">Raw comment count for this engagement window.</param>
    /// <param name="config">The live <see cref="SimulationConfig"/> instance.</param>
    /// <param name="planets">
    ///   Optional planet set whose biome mutation rates are scaled alongside global
    ///   genome mutation probabilities.
    /// </param>
    /// <returns>The chaos multiplier returned by <see cref="SimulationConfig.ApplyRedditEngagementChaos"/>.</returns>
    public double ApplyToConfig(
        RedditVotePayload payload,
        long upvotes,
        long comments,
        SimulationConfig config,
        IEnumerable<CelestialBody>? planets = null)
    {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        if (config is null)  throw new ArgumentNullException(nameof(config));

        // 1. "Bombard with Cosmic Rays" delta → accumulate MutationVolatilityModifier.
        //    Clamped to [0, 1]; TickVolatilityDecay() bleeds it back to 0 over time.
        if (payload.MutationVolatilityDelta > 0)
        {
            config.MutationVolatilityModifier = Math.Clamp(
                config.MutationVolatilityModifier + payload.MutationVolatilityDelta,
                0.0, 1.0);
        }

        // 2. Drive ChaosFactor, CurrentRedditEngagement, and ProceduralEventMeanIntervalYears.
        //    The base formula: interval = baseInterval / (1 + chaosPressure * MaxBoost)
        //    High upvotes/comments → lower interval → more frequent catastrophes.
        return config.ApplyRedditEngagementChaos(upvotes, comments, planets);
    }

    /// <summary>
    /// Async entry point for a weekly Reddit poll cycle.
    /// <para>
    ///   The CPU-bound parse runs on a thread-pool thread via <see cref="Task.Run"/>.
    ///   Config writes and EventBus publication happen on the caller's context after
    ///   the await, keeping shared simulation state thread-safe without locks.
    /// </para>
    /// <para>
    ///   Publishes a <see cref="PollAppliedEvent"/> on the <see cref="EventBus"/>
    ///   so DataMiningAutomator (and any future observer) can log the outcome without
    ///   holding a direct reference to this parser or the config.
    /// </para>
    /// </summary>
    /// <param name="pollResults">Raw rows from the Reddit poll API.</param>
    /// <param name="upvotes">Total upvotes on the associated Reddit post this window.</param>
    /// <param name="comments">Total comments on the associated Reddit post this window.</param>
    /// <param name="config">The live <see cref="SimulationConfig"/> instance to update.</param>
    /// <param name="planets">
    ///   Optional planet set for per-biome mutation scaling inside
    ///   <see cref="SimulationConfig.ApplyRedditEngagementChaos"/>.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the background parse.</param>
    /// <returns>The applied payload and the resulting chaos multiplier.</returns>
    public async Task<(RedditVotePayload Payload, double ChaosMultiplier)> ParseAndApplyAsync(
        IEnumerable<RedditPollResult> pollResults,
        long upvotes,
        long comments,
        SimulationConfig config,
        IEnumerable<CelestialBody>? planets = null,
        CancellationToken cancellationToken = default)
    {
        if (pollResults is null) throw new ArgumentNullException(nameof(pollResults));
        if (config is null)      throw new ArgumentNullException(nameof(config));

        // Materialize once here so the list can be safely captured by the lambda
        // and also inspected for the winning option without double-enumeration.
        var results = pollResults as IReadOnlyList<RedditPollResult>
                      ?? pollResults.ToList();

        // Identify the winning option before handing off to the background thread
        // so we don't need to marshal results back just for logging.
        string winningOption = results.Count > 0
            ? (results.OrderByDescending(r => r.Votes).First().Option ?? string.Empty)
            : string.Empty;

        // Offload parse (potentially large poll datasets) to the thread pool.
        // ConfigureAwait(false) avoids re-entering the Unity main thread (or any
        // synchronisation context) after the parse — ApplyToConfig is called next.
        RedditVotePayload payload = await Task
            .Run(() => Parse(results), cancellationToken)
            .ConfigureAwait(false);

        // Apply config-level effects on the caller's context.
        // Planet/species-level effects (ApplyToPlanet / ApplyToAlphaSpecies) remain
        // the caller's responsibility so they can choose the correct target.
        double chaosMultiplier = ApplyToConfig(payload, upvotes, comments, config, planets);

        // Notify all EventBus subscribers (e.g. DataMiningAutomator's logger, UI).
        // This keeps DataMiningAutomator fully decoupled — it never references the parser.
        EventBus.Publish(new PollAppliedEvent(
            winningOption,
            upvotes,
            comments,
            chaosMultiplier,
            config.MutationVolatilityModifier,
            config.ProceduralEventMeanIntervalYears));

        return (payload, chaosMultiplier);
    }

    private static void EnsurePayload(RedditVotePayload? payload)
    {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
    }
}
