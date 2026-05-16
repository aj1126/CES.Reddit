using PlanetaryFormation.Models;
using PlanetaryFormation.SimulationCore.Events;
using PlanetaryFormation.SimulationCore.Macro;
using PlanetaryFormation.SimulationCore.Micro;

namespace PlanetaryFormation.SimulationCore.Game;

/// <summary>
/// Manages the player's Cosmic Energy resource.
/// Earns energy by subscribing to simulation milestones via <see cref="EventBus"/>;
/// no changes to simulation code are required.
/// </summary>
public class ResourceManager : IDisposable
{
    // ── Configuration constants ───────────────────────────────────────────────

    /// <summary>Energy awarded when abiogenesis occurs on any planet.</summary>
    public const double AbiogenesisBonus = 25.0;

    /// <summary>Energy awarded whenever a new species branches off.</summary>
    public const double SpeciationBonus = 5.0;

    /// <summary>Energy awarded when a planet advances to a new <see cref="LifeStage"/>.</summary>
    public const double LifeStageBonus = 50.0;

    // ── State ─────────────────────────────────────────────────────────────────

    /// <summary>Current Cosmic Energy available to the player.</summary>
    public double CosmicEnergy { get; private set; } = 100.0;

    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Subscribes to EventBus milestones. Unsubscribe by calling <see cref="Dispose"/>.
    /// </summary>
    public ResourceManager()
    {
        EventBus.Subscribe<AbiogenesisEvent>(OnAbiogenesis);
        EventBus.Subscribe<SpeciationEvent>(OnSpeciation);
        EventBus.Subscribe<LifeStageAdvancedEvent>(OnLifeStageAdvanced);
    }

    /// <summary>
    /// Attempts to spend <paramref name="amount"/> Cosmic Energy.
    /// Returns <c>true</c> and deducts the cost if sufficient funds exist;
    /// returns <c>false</c> without changing the balance otherwise.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="amount"/> is negative.
    /// </exception>
    public bool TrySpend(double amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount to spend must be non-negative.");

        if (CosmicEnergy < amount)
            return false;

        CosmicEnergy -= amount;
        return true;
    }

    /// <summary>Adds Cosmic Energy directly (for testing or scenario setup).</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="amount"/> is negative.
    /// </exception>
    public void Award(double amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount to award must be non-negative.");

        CosmicEnergy += amount;
    }

    /// <summary>Removes EventBus subscriptions.</summary>
    public void Dispose()
    {
        EventBus.Unsubscribe<AbiogenesisEvent>(OnAbiogenesis);
        EventBus.Unsubscribe<SpeciationEvent>(OnSpeciation);
        EventBus.Unsubscribe<LifeStageAdvancedEvent>(OnLifeStageAdvanced);
    }

    // ── Handlers ─────────────────────────────────────────────────────────────

    private void OnAbiogenesis(AbiogenesisEvent _)   => CosmicEnergy += AbiogenesisBonus;
    private void OnSpeciation(SpeciationEvent _)      => CosmicEnergy += SpeciationBonus;
    private void OnLifeStageAdvanced(LifeStageAdvancedEvent _) => CosmicEnergy += LifeStageBonus;
}
