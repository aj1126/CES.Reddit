namespace PlanetaryFormation.Models;

/// <summary>
/// A rocky, Earth-like planet with a solid surface capable of supporting diverse biomes.
/// Evolves over time by slowly gaining or losing surface water and atmospheric density.
/// </summary>
public class TerrestrialPlanet : CelestialBody
{
    public override string PlanetType => "Terrestrial";

    /// <summary>Fraction of the surface covered by liquid water (0.0–1.0).</summary>
    public double OceanCoverage { get; set; }

    /// <summary>Whether the planet has a magnetosphere that shields surface life.</summary>
    public bool HasMagnetosphere { get; set; }

    public TerrestrialPlanet(string name, double massEarth, double radiusEarth,
        double orbitalRadiusAU, double ageGyr, double oceanCoverage, bool hasMagnetosphere)
        : base(name, massEarth, radiusEarth, orbitalRadiusAU, ageGyr)
    {
        OceanCoverage = Math.Clamp(oceanCoverage, 0.0, 1.0);
        HasMagnetosphere = hasMagnetosphere;

        // Macro-simulation properties
        AtmosphericDensity  = 0.90;
        SurfaceWaterPercent = OceanCoverage;
        CoreComposition     = CoreComposition.Metallic;

        InitializeBiomes();
    }

    private void InitializeBiomes()
    {
        Biomes.Add(new Biome("Tropical Rainforest", temperature: 303, humidity: 0.90,
            vegetationDensity: 0.95, atmosphericPressure: 1.05, mutationRate: 0.12, canGoExtinct: true));
        Biomes.Add(new Biome("Temperate Forest", temperature: 285, humidity: 0.65,
            vegetationDensity: 0.75, atmosphericPressure: 1.0, mutationRate: 0.08, canGoExtinct: true));
        Biomes.Add(new Biome("Arctic Tundra", temperature: 255, humidity: 0.30,
            vegetationDensity: 0.10, atmosphericPressure: 0.98, mutationRate: 0.05, canGoExtinct: true));
        if (OceanCoverage > 0.3)
            Biomes.Add(new Biome("Shallow Ocean", temperature: 290, humidity: 1.0,
                vegetationDensity: 0.60, atmosphericPressure: 1.02, mutationRate: 0.10, canGoExtinct: true));
    }

    public override void Evolve(int generation, Random rng)
    {
        // Gradual tectonic aging: slight radius growth and mass redistribution
        RadiusEarth += rng.NextDouble() * 0.001 - 0.0005;
        AgeGyr += 0.01;

        // Magnetosphere may weaken over time on low-mass planets
        if (MassEarth < 0.5 && rng.NextDouble() < 0.02)
            HasMagnetosphere = false;

        // Ocean coverage drifts slightly each generation
        OceanCoverage = Math.Clamp(OceanCoverage + (rng.NextDouble() * 0.02 - 0.01), 0.0, 1.0);
    }
}
