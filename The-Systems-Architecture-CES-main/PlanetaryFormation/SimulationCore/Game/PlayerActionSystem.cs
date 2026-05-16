using PlanetaryFormation.SimulationCore.Events;
using PlanetaryFormation.SimulationCore.Time;

namespace PlanetaryFormation.SimulationCore.Game;

/// <summary>Published whenever the player successfully executes an action.</summary>
public record PlayerActionEvent(IPlayerAction Action, double SimulatedYears);

/// <summary>
/// Published when a player action is rejected (insufficient funds or still on cooldown).
/// </summary>
public record PlayerActionRejectedEvent(IPlayerAction Action, string Reason, double SimulatedYears);

/// <summary>
/// Validates player actions against Cosmic Energy costs and per-action cooldowns,
/// then delegates execution to the underlying action.
///
/// This is the single entry-point for all player-initiated simulation interventions.
/// The simulation layer stays pure — all game-rule enforcement lives here.
/// </summary>
public class PlayerActionSystem
{
    private readonly ResourceManager _resources;

    /// <summary>
    /// Tracks when each action (keyed by <see cref="IPlayerAction.Id"/>) will next be
    /// available, measured in simulated years.
    /// </summary>
    private readonly Dictionary<string, double> _cooldowns = new();

    public PlayerActionSystem(ResourceManager resources)
    {
        _resources = resources;
    }

    /// <summary>Current Cosmic Energy available to the player (delegated to ResourceManager).</summary>
    public double CosmicEnergy => _resources.CosmicEnergy;

    /// <summary>
    /// Attempts to execute <paramref name="action"/> at <paramref name="simulatedYears"/>.
    /// Returns <c>true</c> if the action was executed successfully.
    /// Returns <c>false</c> and publishes <see cref="PlayerActionRejectedEvent"/> if the
    /// action is on cooldown or the player lacks sufficient Cosmic Energy.
    /// </summary>
    public bool TryExecute(IPlayerAction action, double simulatedYears)
    {
        if (_cooldowns.TryGetValue(action.Id, out var readyAt) && simulatedYears < readyAt)
        {
            EventBus.Publish(new PlayerActionRejectedEvent(
                action,
                $"On cooldown for {readyAt - simulatedYears:N0} more simulated years.",
                simulatedYears));
            return false;
        }

        if (!_resources.TrySpend(action.CosmicEnergyCost))
        {
            EventBus.Publish(new PlayerActionRejectedEvent(
                action,
                $"Insufficient Cosmic Energy (need {action.CosmicEnergyCost:F0}, have {_resources.CosmicEnergy:F0}).",
                simulatedYears));
            return false;
        }

        action.Execute();
        _cooldowns[action.Id] = simulatedYears + action.CooldownYears;
        EventBus.Publish(new PlayerActionEvent(action, simulatedYears));
        return true;
    }

    /// <summary>
    /// Returns the simulated-year timestamp at which <paramref name="actionId"/> will
    /// next be available, or 0 if the action has never been used or has no cooldown.
    /// </summary>
    public double GetCooldownReadyAt(string actionId) =>
        _cooldowns.TryGetValue(actionId, out var t) ? t : 0.0;

    /// <summary>
    /// Returns <c>true</c> if <paramref name="actionId"/> is currently off cooldown
    /// at the given <paramref name="simulatedYears"/>.
    /// </summary>
    public bool IsAvailable(string actionId, double simulatedYears) =>
        !_cooldowns.TryGetValue(actionId, out var readyAt) || simulatedYears >= readyAt;
}
