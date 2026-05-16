namespace PlanetaryFormation.Models;

/// <summary>
/// A frozen world in the outer solar system. Biomes represent subsurface ocean
/// pockets, glacier fields, and icy plains.
/// </summary>
public class IcePlanet : CelestialBody
{
    public override string PlanetType => "Ice Planet";

    /// <summary>Average surface temperature in Kelvin.</summary>
    public double SurfaceTemperatureK { get; set; }

    /// <summary>Whether a liquid sub-glacial ocean is suspected.</summary>
    public bool HasSubglacialOcean { get; set; }

    public IcePlanet(string name, double massEarth, double radiusEarth,
        double orbitalRadiusAU, double ageGyr, double surfaceTemperatureK, bool hasSubglacialOcean)
        : base(name, massEarth, radiusEarth, orbitalRadiusAU, ageGyr)
    {
        SurfaceTemperatureK = surfaceTemperatureK;
        HasSubglacialOcean  = hasSubglacialOcean;

        // Macro-simulation properties
        AtmosphericDensity  = 0.05;
        SurfaceWaterPercent = hasSubglacialOcean ? 0.30 : 0.0;
        CoreComposition     = CoreComposition.Icy;

        InitializeBiomes();
    }

    private void InitializeBiomes()
    {
        Biomes.Add(new Biome("Nitrogen Ice Plain", temperature: SurfaceTemperatureK,
            humidity: 0.05, vegetationDensity: 0.0, atmosphericPressure: 0.01, mutationRate: 0.02, canGoExtinct: false));
        Biomes.Add(new Biome("Cryo-Geyser Field", temperature: SurfaceTemperatureK + 30,
            humidity: 0.20, vegetationDensity: 0.01, atmosphericPressure: 0.02, mutationRate: 0.07, canGoExtinct: true));
        if (HasSubglacialOcean)
            Biomes.Add(new Biome("Subsurface Briny Ocean", temperature: 270,
                humidity: 1.0, vegetationDensity: 0.05, atmosphericPressure: 10.0, mutationRate: 0.09, canGoExtinct: true));
    }

    public override void Evolve(int generation, Random rng)
    {
        // Slow surface cooling as the planet drifts further from its star
        SurfaceTemperatureK = Math.Max(20, SurfaceTemperatureK - rng.NextDouble() * 0.5);
        AgeGyr += 0.01;

        // Subglacial ocean may freeze over on very cold worlds
        if (HasSubglacialOcean && SurfaceTemperatureK < 40 && rng.NextDouble() < 0.03)
            HasSubglacialOcean = false;
    }
}
