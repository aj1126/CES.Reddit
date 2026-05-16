using PlanetaryFormation.Models;
using PlanetaryFormation.SimulationCore.Events;
using PlanetaryFormation.SimulationCore.Micro;

namespace PlanetaryFormation.SimulationCore.RenderBridge;

/// <summary>Published when a biome is brought into focus for observation.</summary>
public record BiomeFocusedEvent(Biome Biome, PopulationPool Pool);

/// <summary>Published when observation ends and the focus is cleared.</summary>
public record BiomeFocusClearedEvent();

/// <summary>
/// The single entry point for the Unity camera system into the simulation data layer.
/// Fetches the <see cref="PopulationPool"/> for the observed biome and notifies
/// the spawner via the event bus.
/// Iteration 4: The Render Layer.
/// </summary>
public class ObserverContext
{
    private readonly MicroSimulationManager _micro;

    /// <summary>The biome currently being observed; null when zoomed out.</summary>
    public Biome? FocusedBiome { get; private set; }

    public ObserverContext(MicroSimulationManager microManager)
    {
        _micro = microManager;
    }

    /// <summary>
    /// Called by the Unity camera when it zooms into a biome.
    /// Raises <see cref="BiomeFocusedEvent"/> so the spawner can instantiate creatures.
    /// </summary>
    public void SetFocusBiome(Biome biome)
    {
        FocusedBiome = biome;

        // GetOrCreatePool registers the pool with the manager so it will receive
        // future simulation ticks, rather than creating an orphaned pool here.
        var pool = _micro.GetOrCreatePool(biome.Name);
        EventBus.Publish(new BiomeFocusedEvent(biome, pool));
    }

    /// <summary>
    /// Called when the player zooms out. Signals the spawner to de-instantiate
    /// all active creature GameObjects.
    /// </summary>
    public void ClearFocus()
    {
        FocusedBiome = null;
        EventBus.Publish(new BiomeFocusClearedEvent());
    }
}
