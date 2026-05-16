using PlanetaryFormation.Models;
using PlanetaryFormation.SimulationCore.Config;
using PlanetaryFormation.SimulationCore.Debug;
using PlanetaryFormation.SimulationCore.Macro;
using PlanetaryFormation.SimulationCore.Micro;

namespace PlanetaryFormation.SimulationCore.Game;

// ── Interface ─────────────────────────────────────────────────────────────────

/// <summary>
/// Contract for all player-initiated actions.
/// Implementations encapsulate a single intervention on the simulation.
/// </summary>
public interface IPlayerAction
{
    /// <summary>Stable identifier used to track per-action cooldowns.</summary>
    string Id { get; }

    /// <summary>Cosmic Energy cost to execute the action.</summary>
    double CosmicEnergyCost { get; }

    /// <summary>
    /// Simulated years that must elapse after execution before the same
    /// action can be used again (0 = no cooldown).
    /// </summary>
    double CooldownYears { get; }

    /// <summary>Human-readable description shown in the UI.</summary>
    string Description { get; }

    /// <summary>Carries out the action on the simulation data layer.</summary>
    void Execute();
}

// ── Concrete actions ──────────────────────────────────────────────────────────

/// <summary>
/// Nudges a planet's atmosphere and water levels to habitable values,
/// accelerating the path toward abiogenesis.
/// Cost: 40 CE  Cooldown: 500 000 years
/// </summary>
public sealed class NudgeAtmosphereAction : IPlayerAction
{
    public string   Id               => $"nudge_atmosphere_{_planet.Name}";
    public double   CosmicEnergyCost => 40.0;
    public double   CooldownYears    => 500_000.0;
    public string   Description      => $"Nudge {_planet.Name}'s atmosphere toward habitable conditions.";

    private readonly CelestialBody _planet;

    public NudgeAtmosphereAction(CelestialBody planet) => _planet = planet;

    public void Execute() => DeveloperOverrides.ForceMaxHabitability(_planet);
}

/// <summary>
/// Seeds a prebiotic score boost on a planet, pushing it closer to the
/// abiogenesis threshold on the next macro tick.
/// Cost: 30 CE  Cooldown: 250 000 years
/// </summary>
public sealed class SeedPrebioticsAction : IPlayerAction
{
    public string   Id               => $"seed_prebiotics_{_planet.Name}";
    public double   CosmicEnergyCost => 30.0;
    public double   CooldownYears    => 250_000.0;
    public string   Description      => $"Deliver organic compounds to {_planet.Name}.";

    private readonly CelestialBody _planet;
    private readonly double        _scoreBoost;

    public SeedPrebioticsAction(CelestialBody planet, double scoreBoost = 0.30)
    {
        _planet     = planet;
        _scoreBoost = scoreBoost;
    }

    public void Execute() =>
        DeveloperOverrides.ForcePrebioticScore(
            _planet,
            Math.Min(1.0, _planet.PrebioticChemistryScore + _scoreBoost));
}

/// <summary>
/// Boosts the mutation rate in a target biome for the next several ticks,
/// accelerating evolutionary diversification.
/// Cost: 20 CE  Cooldown: 100 000 years
/// </summary>
public sealed class BoostMutationRateAction : IPlayerAction
{
    public string   Id               => $"boost_mutation_{_biome.Name}";
    public double   CosmicEnergyCost => 20.0;
    public double   CooldownYears    => 100_000.0;
    public string   Description      => $"Boost mutation rate in biome '{_biome.Name}'.";

    private readonly Biome  _biome;
    private readonly float  _multiplier;

    public BoostMutationRateAction(Biome biome, float multiplier = 2.0f)
    {
        if (multiplier <= 0.0f)
            throw new ArgumentOutOfRangeException(nameof(multiplier), multiplier, "Multiplier must be greater than 0.");

        _biome      = biome;
        _multiplier = multiplier;
    }

    public void Execute() =>
        _biome.MutationRate = Math.Clamp(_biome.MutationRate * _multiplier, 0.0f, 1.0f);
}

/// <summary>
/// Forces an immediate speciation event in a biome's population pool,
/// introducing new genetic diversity.
/// Cost: 15 CE  Cooldown: 50 000 years
/// </summary>
public sealed class TriggerSpeciationAction : IPlayerAction
{
    public string   Id               => $"trigger_speciation_{_biomeName}";
    public double   CosmicEnergyCost => 15.0;
    public double   CooldownYears    => 50_000.0;
    public string   Description      => $"Trigger a speciation event in biome '{_biomeName}'.";

    private readonly PopulationPool  _pool;
    private readonly string          _biomeName;
    private readonly Random          _rng;
    private readonly SimulationConfig _config;

    public TriggerSpeciationAction(
        PopulationPool pool,
        string biomeName,
        Random rng,
        SimulationConfig? config = null)
    {
        _pool      = pool;
        _biomeName = biomeName;
        _rng       = rng;
        _config    = config ?? SimulationConfig.Instance;
    }

    public void Execute() =>
        DeveloperOverrides.ForceSpeciation(_pool, _biomeName, _rng, _config);
}

/// <summary>
/// Forces abiogenesis on a planet, immediately seeding primitive life.
/// This is the most powerful action and reflects divine-level intervention.
/// Cost: 80 CE  Cooldown: 1 000 000 years
/// </summary>
public sealed class TriggerAbiogenesisAction : IPlayerAction
{
    public string   Id               => $"trigger_abiogenesis_{_planet.Name}";
    public double   CosmicEnergyCost => 80.0;
    public double   CooldownYears    => 1_000_000.0;
    public string   Description      => $"Seed life on {_planet.Name}.";

    private readonly CelestialBody         _planet;
    private readonly AbiogenesisEngine     _abiogenesisEngine;
    private readonly MicroSimulationManager _microManager;
    private readonly Random                _rng;

    public TriggerAbiogenesisAction(
        CelestialBody planet,
        AbiogenesisEngine abiogenesisEngine,
        MicroSimulationManager microManager,
        Random rng)
    {
        _planet            = planet;
        _abiogenesisEngine = abiogenesisEngine;
        _microManager      = microManager;
        _rng               = rng;
    }

    public void Execute() =>
        DeveloperOverrides.ForceAbiogenesis(_planet, _abiogenesisEngine, _microManager, _rng);
}
