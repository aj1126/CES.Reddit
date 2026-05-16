namespace PlanetaryFormation.SimulationCore.Time;

/// <summary>Top-level states the game can be in.</summary>
public enum GameState
{
    /// <summary>Before the player presses the Big Bang button.</summary>
    PreCatalyst,

    /// <summary>Cosmic evolution running: planetary formation, abiogenesis.</summary>
    MacroSimulation,

    /// <summary>Biological evolution running as data; no biome is currently focused.</summary>
    MicroSimulation,

    /// <summary>Player is zoomed into a biome; the render layer is active.</summary>
    Observing
}
