namespace PlanetaryFormation.SimulationCore.Time;

/// <summary>Operating modes of the simulation clock.</summary>
public enum TimeMode
{
    /// <summary>Clock is stopped; no ticks are published.</summary>
    Paused,

    /// <summary>Cosmic scale — millions of years per real second.</summary>
    MacroScale,

    /// <summary>Biological scale — days per real second.</summary>
    MicroScale
}
