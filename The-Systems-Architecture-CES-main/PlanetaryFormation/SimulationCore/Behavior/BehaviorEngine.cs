using PlanetaryFormation.SimulationCore.Micro;

namespace PlanetaryFormation.SimulationCore.Behavior;

/// <summary>
/// Pure C# state-machine for spawned creature behaviour.
/// Has zero Unity dependencies — the Unity IKLocomotionController reads the
/// resulting <see cref="CreatureState"/> each frame.
/// Iteration 5: Physics &amp; Locomotion.
/// </summary>
public static class BehaviorEngine
{
    private const float HungerThreshold           = 0.60f;
    private const float FearThreshold             = 0.40f;
    private const float ReproductiveDriveThreshold = 0.70f;
    private const float HungerDecayRate           = 0.01f;  // added to Hunger each tick
    private const float FearDecayRate             = 0.05f;  // subtracted from Fear each tick (no threat)
    private const float FearBuildRate             = 0.20f;  // added to Fear each tick (threat present)

    /// <summary>
    /// Advances <paramref name="brain"/> by one tick using the provided environment
    /// snapshot and returns the new <see cref="CreatureState"/>.
    /// Priority order: safety > hunger > reproduction > idle.
    /// </summary>
    public static CreatureState UpdateBrain(
        ref CreatureBrainData brain,
        in EnvironmentSnapshot env)
    {
        // Drive updates
        brain.Hunger = Math.Clamp(brain.Hunger + HungerDecayRate, 0f, 1f);
        brain.Fear   = env.ThreatNearby
            ? Math.Clamp(brain.Fear + FearBuildRate, 0f, 1f)
            : Math.Clamp(brain.Fear - FearDecayRate, 0f, 1f);

        // State selection by priority
        CreatureState next;
        if (brain.Fear >= FearThreshold)
        {
            next = CreatureState.Fleeing;
        }
        else if (brain.Hunger >= HungerThreshold)
        {
            next = brain.Diet == DietType.Herbivore || brain.Diet == DietType.Omnivore
                ? CreatureState.Foraging
                : CreatureState.Hunting;
        }
        else if (brain.ReproductiveDrive >= ReproductiveDriveThreshold && env.MateAvailable)
        {
            next = CreatureState.Mating;
        }
        else
        {
            next = CreatureState.Idle;
        }

        brain.CurrentState = next;
        return next;
    }

    /// <summary>
    /// Builds a <see cref="LimbConfiguration"/> from a species' genome for
    /// hand-off to the Unity IK layer at spawn time.
    /// </summary>
    public static LimbConfiguration BuildLimbConfiguration(Genome genome)
    {
        var lengths = new float[genome.LimbCount];
        for (int i = 0; i < genome.LimbCount; i++)
            lengths[i] = genome.LimbLengthAvg;  // uniform baseline; IK adds per-step variance

        return new LimbConfiguration
        {
            LimbCount   = genome.LimbCount,
            LimbLengths = lengths
        };
    }
}
