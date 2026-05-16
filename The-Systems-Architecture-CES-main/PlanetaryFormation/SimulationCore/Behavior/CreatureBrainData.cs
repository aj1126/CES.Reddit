using PlanetaryFormation.SimulationCore.Micro;

namespace PlanetaryFormation.SimulationCore.Behavior;

/// <summary>
/// Mutable drive values and current behavioural state for a single spawned creature.
/// Owned by the simulation layer; read by the Unity IKLocomotionController each frame
/// to select animations and movement targets.
/// Iteration 5: Physics &amp; Locomotion.
/// </summary>
public struct CreatureBrainData
{
    /// <summary>Hunger level: 0.0 = satiated, 1.0 = starving.</summary>
    public float Hunger;

    /// <summary>Fear level: 0.0 = calm, 1.0 = maximum fear response.</summary>
    public float Fear;

    /// <summary>Reproductive drive: 0.0 = not ready, 1.0 = strong drive to mate.</summary>
    public float ReproductiveDrive;

    /// <summary>Nutritional strategy, copied from the species' genome at spawn time.</summary>
    public DietType Diet;

    /// <summary>
    /// Visual/detection range in world units.
    /// Derived from <see cref="SensoryOrganFlags"/> at spawn time.
    /// </summary>
    public float SightRange;

    /// <summary>The behaviour the creature should currently be executing.</summary>
    public CreatureState CurrentState;
}
