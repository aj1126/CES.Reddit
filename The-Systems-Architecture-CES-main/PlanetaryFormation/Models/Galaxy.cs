namespace PlanetaryFormation.Models;

/// <summary>
/// Represents a galaxy containing one or more star systems.
/// Sits between Universe and SolarSystem in the celestial hierarchy.
/// Maps to the Galaxy block in the GDD (mass, age, StarSystems[]).
/// </summary>
public class Galaxy
{
    /// <summary>Human-readable identifier for this galaxy.</summary>
    public string Name { get; set; }

    /// <summary>Total galactic mass expressed in solar masses.</summary>
    public double MassSolarMasses { get; set; }

    /// <summary>Galaxy age in billions of years.</summary>
    public double AgeGyr { get; set; }

    /// <summary>All star systems (SolarSystems) contained within this galaxy.</summary>
    public List<SolarSystem> StarSystems { get; } = new();

    public Galaxy(string name, double massSolarMasses, double ageGyr)
    {
        Name            = name;
        MassSolarMasses = massSolarMasses;
        AgeGyr          = ageGyr;
    }

    /// <summary>Adds a star system to this galaxy.</summary>
    public void AddStarSystem(SolarSystem system) => StarSystems.Add(system);

    /// <summary>Returns every planet across all star systems in this galaxy.</summary>
    public IEnumerable<CelestialBody> AllPlanets() =>
        StarSystems.SelectMany(s => s.Planets);

    public override string ToString() =>
        $"[Galaxy] {Name}  mass={MassSolarMasses:E2}M☉  age={AgeGyr:F2}Gyr  " +
        $"systems={StarSystems.Count}";
}
