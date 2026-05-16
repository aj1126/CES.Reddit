using PlanetaryFormation.Models;
using PlanetaryFormation.SimulationCore.Config;
using PlanetaryFormation.SimulationCore.Events;
using PlanetaryFormation.SimulationCore.Mutation;
using PlanetaryFormation.SimulationCore.Time;

namespace PlanetaryFormation.SimulationCore.Macro;

/// <summary>
/// Clock-driven replacement for the legacy <c>SimulationLoop</c>.
/// Subscribes to <see cref="TickEvent"/> and advances each planet one step per tick,
/// then delegates to <see cref="AbiogenesisEngine"/> to check for life emergence.
/// Iteration 2: Macro-Simulation — Planetary Formation &amp; Abiogenesis.
/// </summary>
public class MacroSimulationManager
{
    private readonly SimulationConfig _config;
    private SolarSystem? _system;
    private Random _rng = new();

    /// <summary>The abiogenesis engine used by this manager.</summary>
    public AbiogenesisEngine AbiogenesisEngine { get; }

    public MacroSimulationManager(SimulationConfig? config = null)
    {
        _config = config ?? SimulationConfig.Instance;
        AbiogenesisEngine = new AbiogenesisEngine(_config);
        EventBus.Subscribe<TickEvent>(OnTick);
    }

    /// <summary>Attach the solar system this manager should evolve.</summary>
    public void Attach(SolarSystem system, int? seed = null)
    {
        _system = system;
        _rng    = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    /// <summary>Detaches from tick events. Call when tearing down the simulation.</summary>
    public void Detach()
    {
        EventBus.Unsubscribe<TickEvent>(OnTick);
        _system = null;
    }

    private void OnTick(TickEvent tick)
    {
        if (_system is null) return;
        if (SimulationClock.Mode != TimeMode.MacroScale) return;

        // 1. Evolve each planet (planet-specific physical changes)
        foreach (var planet in _system.Planets)
            planet.Evolve(_system.Generation + 1, _rng);

        // 2. Apply biome mutations across the whole system, scaled by current ChaosFactor
        BiomeMutationEngine.ApplyMutations(_system, _rng, _config.ChaosFactor);

        // 3. Advance generation counter
        _system.Generation++;

        // Decay vote-accumulated mutation volatility toward baseline each tick
        _config.TickVolatilityDecay();
    }
}
