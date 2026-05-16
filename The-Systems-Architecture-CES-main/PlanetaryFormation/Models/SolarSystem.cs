namespace PlanetaryFormation.Models;

/// <summary>
/// Represents a complete star system containing an ordered collection of planets.
/// </summary>
public class SolarSystem
{
    public string StarName { get; set; }

    /// <summary>Stellar classification (e.g. "G2V", "K1V").</summary>
    public string StarClass { get; set; }

    /// <summary>Stellar luminosity relative to the Sun (Sun = 1.0).</summary>
    public double StarLuminosity { get; set; }

    /// <summary>Current simulation generation count.</summary>
    public int Generation { get; set; }

    /// <summary>Ordered list of planets by orbital radius.</summary>
    public List<CelestialBody> Planets { get; } = new();

    public SolarSystem(string starName, string starClass, double starLuminosity)
    {
        StarName = starName;
        StarClass = starClass;
        StarLuminosity = starLuminosity;
        Generation = 0;
    }

    public void AddPlanet(CelestialBody planet) => Planets.Add(planet);

    /// <summary>
    /// Sorts planets in-place by ascending orbital radius so output is always
    /// presented in natural order from the star outward.
    /// </summary>
    public void SortByOrbitalRadius() =>
        Planets.Sort((a, b) => a.OrbitalRadiusAU.CompareTo(b.OrbitalRadiusAU));

    public override string ToString() =>
        $"★ {StarName} [{StarClass}]  L={StarLuminosity:F2}L☉  " +
        $"Planets={Planets.Count}  Generation={Generation}";
}
