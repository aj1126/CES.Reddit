namespace PlanetaryFormation.SimulationCore.Micro;

/// <summary>
/// Bit-flags representing the sensory organs a species possesses.
/// Multiple organs can co-exist on the same creature.
/// </summary>
[Flags]
public enum SensoryOrganFlags
{
    None             = 0,
    Eyes             = 1 << 0,
    Ears             = 1 << 1,
    Electroreception = 1 << 2,
    Thermoreception  = 1 << 3,
    Magnetoreception = 1 << 4
}
