using PlanetaryFormation.Models;
using PlanetaryFormation.SimulationCore.Events;
using PlanetaryFormation.SimulationCore.Micro;
using PlanetaryFormation.SimulationCore.Time;

namespace PlanetaryFormation.SimulationCore.Game;

// ── Scenario definition ───────────────────────────────────────────────────────

/// <summary>Possible outcomes of an active scenario.</summary>
public enum ScenarioOutcome { InProgress, Victory, Defeat }

/// <summary>
/// Defines the win and lose conditions for a single playthrough.
/// All built-in factories follow the same pattern: pick a goal, set a time limit.
/// </summary>
public class ScenarioDefinition
{
    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>Display name shown to the player.</summary>
    public string Name { get; init; } = "Unnamed Scenario";

    /// <summary>Short description of what the player must achieve.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Simulated-year budget the player has to meet the win condition.
    /// Set to <see cref="double.MaxValue"/> for an unlimited scenario.
    /// </summary>
    public double TimeLimitYears { get; init; } = 50_000_000.0;

    /// <summary>
    /// The win condition: a function that receives the solar system and returns
    /// <c>true</c> when the player has succeeded.
    /// </summary>
    public required Func<SolarSystem, bool> WinCondition { get; init; }

    /// <summary>
    /// The lose condition: a function that receives the solar system and returns
    /// <c>true</c> when the player has irreversibly failed.
    /// Evaluated only if the win condition has not yet been met.
    /// </summary>
    public required Func<SolarSystem, bool> LoseCondition { get; init; }

    /// <summary>
    /// When <c>true</c>, reaching the time limit with at least one living planet
    /// counts as victory (survival scenarios). When <c>false</c> (default),
    /// reaching the time limit without meeting the win condition is a defeat.
    /// </summary>
    public bool TimeLimitIsVictory { get; init; } = false;

    // ── Built-in factories ────────────────────────────────────────────────────

    /// <summary>
    /// Classic scenario: guide any planet to <see cref="LifeStage.Sapient"/> within
    /// <paramref name="timeLimitYears"/> simulated years.
    /// Lose if all habitable planets become sterile.
    /// </summary>
    public static ScenarioDefinition GuideToCivilisation(double timeLimitYears = 50_000_000.0) => new()
    {
        Name        = "Cradle of Civilisation",
        Description = $"Guide any planet to Sapient life within {timeLimitYears:N0} simulated years.",
        TimeLimitYears = timeLimitYears,
        WinCondition  = system => system.Planets.Any(p => p.LifeStage == LifeStage.Sapient),
        LoseCondition = AllHabitableSterilesLoseCondition,
    };

    /// <summary>
    /// Seed life on every habitable planet in the system within the time limit.
    /// Lose if the time limit elapses without achieving this.
    /// </summary>
    public static ScenarioDefinition SeedAllPlanets(double timeLimitYears = 30_000_000.0) => new()
    {
        Name        = "Genesis Protocol",
        Description = $"Seed life on every habitable planet within {timeLimitYears:N0} simulated years.",
        TimeLimitYears = timeLimitYears,
        WinCondition  = system =>
        {
            var habitable = system.Planets.Where(p => p.IsHabitable).ToList();
            return habitable.Count > 0 && habitable.All(p => p.LifeStage > LifeStage.Sterile);
        },
        LoseCondition = (system) => false, // only time limit applies
    };

    /// <summary>
    /// Survive a catastrophic scenario: keep at least one living planet after
    /// <paramref name="timeLimitYears"/> of Poisson-distributed disasters.
    /// Win by having any planet still alive at the time limit.
    /// </summary>
    public static ScenarioDefinition Survive(double timeLimitYears = 10_000_000.0) => new()
    {
        Name        = "The Great Filter",
        Description = $"Keep at least one planet alive for {timeLimitYears:N0} simulated years.",
        TimeLimitYears    = timeLimitYears,
        TimeLimitIsVictory = true,
        WinCondition  = (system) => false, // win is checked at time-limit by GoalsSystem
        LoseCondition = AllHabitableSterilesLoseCondition,
    };

    // ── Shared helpers ────────────────────────────────────────────────────────

    private static bool AllHabitableSterilesLoseCondition(SolarSystem system)
    {
        var habitable = system.Planets.Where(p => p.IsHabitable).ToList();
        return habitable.Count > 0 && habitable.All(p => p.LifeStage == LifeStage.Sterile);
    }
}

// ── Goals system ──────────────────────────────────────────────────────────────

/// <summary>Published when the scenario ends with a victory.</summary>
public record ScenarioVictoryEvent(ScenarioDefinition Scenario, double SimulatedYears);

/// <summary>Published when the scenario ends with a defeat.</summary>
public record ScenarioDefeatEvent(ScenarioDefinition Scenario, string Reason, double SimulatedYears);

/// <summary>
/// Evaluates win and lose conditions every tick against the active
/// <see cref="ScenarioDefinition"/>.
///
/// When the outcome is determined, the appropriate event is published and
/// the <see cref="Outcome"/> property is updated.  All further ticks are
/// no-ops so the event fires exactly once.
/// </summary>
public class GoalsSystem
{
    private readonly SolarSystem        _system;
    private readonly ScenarioDefinition _scenario;

    /// <summary>The current outcome of the active scenario.</summary>
    public ScenarioOutcome Outcome { get; private set; } = ScenarioOutcome.InProgress;

    public GoalsSystem(SolarSystem system, ScenarioDefinition scenario)
    {
        _system   = system;
        _scenario = scenario;
        EventBus.Subscribe<TickEvent>(OnTick);
    }

    /// <summary>Removes the TickEvent subscription.</summary>
    public void Detach() => EventBus.Unsubscribe<TickEvent>(OnTick);

    // ── Internal ──────────────────────────────────────────────────────────────

    private void OnTick(TickEvent tick)
    {
        if (Outcome != ScenarioOutcome.InProgress) return;

        // Win check first
        if (_scenario.WinCondition(_system))
        {
            Resolve(ScenarioOutcome.Victory, tick.SimulatedYears, "win condition met");
            return;
        }

        // Lose check
        if (_scenario.LoseCondition(_system))
        {
            Resolve(ScenarioOutcome.Defeat, tick.SimulatedYears, "all habitable planets went sterile");
            return;
        }

        // Time-limit check
        if (tick.SimulatedYears >= _scenario.TimeLimitYears)
        {
            if (_scenario.TimeLimitIsVictory && _system.Planets.Any(p => p.LifeStage > LifeStage.Sterile))
                Resolve(ScenarioOutcome.Victory, tick.SimulatedYears, "survived to time limit");
            else
                Resolve(ScenarioOutcome.Defeat, tick.SimulatedYears,
                    _scenario.TimeLimitIsVictory
                        ? "all planets went sterile before time limit"
                        : "time limit reached without meeting win condition");
        }
    }

    private void Resolve(ScenarioOutcome outcome, double simulatedYears, string reason)
    {
        Outcome = outcome;
        if (outcome == ScenarioOutcome.Victory)
            EventBus.Publish(new ScenarioVictoryEvent(_scenario, simulatedYears));
        else
            EventBus.Publish(new ScenarioDefeatEvent(_scenario, reason, simulatedYears));

        EventBus.Unsubscribe<TickEvent>(OnTick);
    }
}
