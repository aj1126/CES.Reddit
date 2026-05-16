using PlanetaryFormation.Models;
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
/// Maps external Reddit poll outcomes to simulation-facing variables and effects.
/// </summary>
public sealed class RedditVoteParser
{
    public const string BombardWithCosmicRaysOption   = "Bombard with Cosmic Rays";
    public const string TriggerVolcanicActivityOption = "Trigger Volcanic Activity";
    public const string SteerCometSwarmOption         = "Steer a Comet Swarm";
    public const string AggressivePredationDriveOption= "Aggressive Predation Drive";
    public const string IncreaseSolarProximityOption  = "Increase Solar Proximity";

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
            0.01,
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
        if (alphaSpecies is null || payload is null) return;

        var genome = alphaSpecies.BaseGenome;
        genome.MutationVolatility = Math.Clamp(genome.MutationVolatility + payload.MutationVolatilityDelta, 0f, 1f);

        if (payload.ForceAlphaCarnivore)
            genome.DietType = DietType.Carnivore;

        alphaSpecies.BaseGenome = genome;
    }

    private static void EnsurePayload(RedditVotePayload? payload)
    {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
    }
}
