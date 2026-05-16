using PlanetaryFormation.Models;
using PlanetaryFormation.SimulationCore.Config;
using PlanetaryFormation.SimulationCore.Events;

namespace PlanetaryFormation.SimulationCore.Time;

/// <summary>Published when the game state changes.</summary>
public record GameStateChangedEvent(GameState OldState, GameState NewState);

/// <summary>Published the moment the Catalyst fires (Big Bang button pressed).</summary>
public record CatalystFiredEvent(int Seed, SolarSystem System);

/// <summary>Published when the player zooms into a biome.</summary>
public record BiomeObservationStartedEvent(Biome Biome);

/// <summary>Published when the player zooms out of a biome.</summary>
public record BiomeObservationEndedEvent();

/// <summary>
/// Manages top-level game-state transitions and seeds the simulation universe.
/// Acts as the primary entry point for UI interactions (Big Bang button, camera zoom).
/// Iteration 1: The Catalyst &amp; Core Time Framework.
/// </summary>
public class GameStateManager
{
    public GameState CurrentState { get; private set; } = GameState.PreCatalyst;

    private readonly SimulationConfig _config;

    /// <summary>
    /// The non-Observing state that was active when the player last zoomed into a biome.
    /// Used by <see cref="ZoomOut"/> to restore the correct state and clock mode.
    /// </summary>
    private GameState _preObservingState = GameState.MacroSimulation;

    public GameStateManager(SimulationConfig? config = null)
    {
        _config = config ?? SimulationConfig.Instance;
    }

    /// <summary>
    /// The player pressed the Big Bang button. Seeds the RNG, initialises the solar
    /// system placeholder, starts the clock at macro scale, and transitions to
    /// <see cref="GameState.MacroSimulation"/>.
    /// </summary>
    public void TriggerCatalyst(SolarSystem system, int? seed = null)
    {
        if (CurrentState != GameState.PreCatalyst)
            throw new InvalidOperationException("Catalyst has already been triggered.");

        int resolvedSeed = seed ?? Environment.TickCount;

        SimulationClock.Reset();
        // Source the default scale from config so all tuning is in one place.
        SimulationClock.SetMacroScale((float)_config.MacroYearsPerSecond);

        TransitionTo(GameState.MacroSimulation);
        EventBus.Publish(new CatalystFiredEvent(resolvedSeed, system));
    }

    /// <summary>
    /// Player zooms into a biome. Switches clock to micro scale and enters
    /// <see cref="GameState.Observing"/>.
    /// </summary>
    public void ZoomIntoBiome(Biome biome)
    {
        if (CurrentState != GameState.MacroSimulation && CurrentState != GameState.MicroSimulation)
            throw new InvalidOperationException($"Cannot zoom into a biome from state {CurrentState}.");

        // Remember which state we came from so ZoomOut can return to it correctly.
        _preObservingState = CurrentState;

        // Source the default scale from config so all tuning is in one place.
        SimulationClock.SetMicroScale((float)_config.MicroDaysPerSecond);
        TransitionTo(GameState.Observing);
        EventBus.Publish(new BiomeObservationStartedEvent(biome));
    }

    /// <summary>
    /// Player zooms back out. Restores the state and clock mode that were active
    /// before the zoom (either <see cref="GameState.MacroSimulation"/> or
    /// <see cref="GameState.MicroSimulation"/>).
    /// </summary>
    public void ZoomOut()
    {
        if (CurrentState != GameState.Observing)
            throw new InvalidOperationException("Not currently observing a biome.");

        // Return to whichever scale matches the state we came from.
        if (_preObservingState == GameState.MicroSimulation)
            SimulationClock.SetMicroScale((float)_config.MicroDaysPerSecond);
        else
            SimulationClock.SetMacroScale((float)_config.MacroYearsPerSecond);

        TransitionTo(_preObservingState);
        EventBus.Publish(new BiomeObservationEndedEvent());
    }

    /// <summary>
    /// Transitions to <see cref="GameState.MicroSimulation"/> when life has been
    /// seeded on at least one planet but no biome is currently observed.
    /// </summary>
    public void EnterMicroSimulation()
    {
        if (CurrentState != GameState.MacroSimulation) return;
        TransitionTo(GameState.MicroSimulation);
    }

    private void TransitionTo(GameState newState)
    {
        var old = CurrentState;
        CurrentState = newState;
        EventBus.Publish(new GameStateChangedEvent(old, newState));
    }
}
