using PlanetaryFormation.SimulationCore.Micro;

namespace PlanetaryFormation.SimulationCore.RenderBridge;

/// <summary>
/// Contract between the pure-data simulation and the Unity render layer.
/// Unity implements this with a MonoBehaviour; the simulation layer only holds
/// this interface, keeping the data/render boundary strictly one-directional.
/// Iteration 4: The Render Layer.
/// </summary>
public interface ICreatureSpawner
{
    /// <summary>Instantiate a creature according to the provided blueprint.</summary>
    void SpawnCreature(CreatureBlueprint blueprint);

    /// <summary>De-instantiate all active creature GameObjects.</summary>
    void DespawnAll();
}
