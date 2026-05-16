namespace PlanetaryFormation.Models;

/// <summary>
/// A massive hydrogen/helium gas giant. Biomes represent distinct atmospheric
/// band layers rather than surface regions.
/// </summary>
public class GasGiant : CelestialBody
{
    public override string PlanetType => "Gas Giant";

    /// <summary>Wind speed in the upper atmosphere in m/s.</summary>
    public double WindSpeedMs { get; set; }

    /// <summary>Number of confirmed ring bands around the planet.</summary>
    public int RingCount { get; set; }

    public GasGiant(string name, double massEarth, double radiusEarth,
        double orbitalRadiusAU, double ageGyr, double windSpeedMs, int ringCount)
        : base(name, massEarth, radiusEarth, orbitalRadiusAU, ageGyr)
    {
        WindSpeedMs = windSpeedMs;
        RingCount   = ringCount;

        // Macro-simulation properties
        AtmosphericDensity  = 1.0;
        SurfaceWaterPercent = 0.0;
        CoreComposition     = CoreComposition.Gaseous;

        InitializeBiomes();
    }

    private void InitializeBiomes()
    {
        Biomes.Add(new Biome("Upper Ammonia Cloud Layer", temperature: 120, humidity: 0.85,
            vegetationDensity: 0.0, atmosphericPressure: 0.5, mutationRate: 0.20, canGoExtinct: false));
        Biomes.Add(new Biome("Water Cloud Band", temperature: 270, humidity: 1.0,
            vegetationDensity: 0.0, atmosphericPressure: 5.0, mutationRate: 0.15, canGoExtinct: false));
        Biomes.Add(new Biome("Deep Metallic Hydrogen Zone", temperature: 8000, humidity: 0.0,
            vegetationDensity: 0.0, atmosphericPressure: 200.0, mutationRate: 0.03, canGoExtinct: false));
    }

    public override void Evolve(int generation, Random rng)
    {
        // Wind speeds fluctuate each generation
        WindSpeedMs = Math.Max(0, WindSpeedMs + rng.NextDouble() * 20 - 10);

        // Rarely, ring material is gained or lost
        if (rng.NextDouble() < 0.05)
            RingCount = Math.Max(0, RingCount + (rng.NextDouble() < 0.5 ? 1 : -1));

        AgeGyr += 0.01;
    }
}
