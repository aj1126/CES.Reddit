namespace PlanetaryFormation.SimulationCore.Behavior;

/// <summary>
/// Describes the limb layout of a spawned creature for the Unity IK system.
/// Passed to IKLocomotionController once at spawn time and updated if the
/// species genome changes significantly.
/// Iteration 5: Physics &amp; Locomotion.
/// </summary>
public struct LimbConfiguration
{
    /// <summary>Number of ground-contact limbs.</summary>
    public int LimbCount;

    /// <summary>
    /// Per-limb length values normalised to body length = 1.0.
    /// Array length equals <see cref="LimbCount"/>.
    /// </summary>
    public float[] LimbLengths;
}
