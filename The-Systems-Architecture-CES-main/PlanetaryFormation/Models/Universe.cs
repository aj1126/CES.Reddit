namespace PlanetaryFormation.Models;

/// <summary>
/// Top-level container for the entire simulated universe.
/// Holds a set of PhysicalConstants that shape all downstream physics and a
/// collection of Galaxy objects. Pressing "Big Bang" instantiates one Universe.
/// Maps to the Universe block in the GDD.
/// </summary>
public class Universe
{
    /// <summary>Human-readable label (useful for multi-universe comparison runs).</summary>
    public string Name { get; set; }

    /// <summary>The fundamental constants governing this universe's physics.</summary>
    public PhysicalConstants Constants { get; set; }

    /// <summary>All galaxies in this universe.</summary>
    public List<Galaxy> Galaxies { get; } = new();

    public Universe(string name, PhysicalConstants? constants = null)
    {
        Name      = name;
        Constants = constants ?? PhysicalConstants.Standard;
    }

    /// <summary>Adds a galaxy to this universe.</summary>
    public void AddGalaxy(Galaxy galaxy) => Galaxies.Add(galaxy);

    /// <summary>Returns every planet across all galaxies and star systems.</summary>
    public IEnumerable<CelestialBody> AllPlanets() =>
        Galaxies.SelectMany(g => g.AllPlanets());

    /// <summary>Returns every star system across all galaxies.</summary>
    public IEnumerable<SolarSystem> AllStarSystems() =>
        Galaxies.SelectMany(g => g.StarSystems);

    public override string ToString() =>
        $"[Universe] {Name}  galaxies={Galaxies.Count}  " +
        $"constants=({Constants})";
}
