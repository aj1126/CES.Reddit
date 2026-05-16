namespace PlanetaryFormation.Models;

/// <summary>
/// A dry, arid world with extreme temperature swings and silicate dust biomes.
/// Desert planets are common in the inner habitable zone boundaries.
/// </summary>
public class DesertPlanet : CelestialBody
{
    public override string PlanetType => "Desert Planet";

    /// <summary>Dayside surface temperature in Kelvin.</summary>
    public double DaysideTemperatureK { get; set; }

    /// <summary>Nightside surface temperature in Kelvin.</summary>
    public double NightsideTemperatureK { get; set; }

    /// <summary>Atmospheric dust opacity (0.0 = clear, 1.0 = fully opaque).</summary>
    public double DustOpacity { get; set; }

    public DesertPlanet(string name, double massEarth, double radiusEarth,
        double orbitalRadiusAU, double ageGyr, double daysideTempK, double nightsideTempK, double dustOpacity)
        : base(name, massEarth, radiusEarth, orbitalRadiusAU, ageGyr)
    {
        DaysideTemperatureK  = daysideTempK;
        NightsideTemperatureK = nightsideTempK;
        DustOpacity = Math.Clamp(dustOpacity, 0.0, 1.0);

        // Macro-simulation properties
        AtmosphericDensity  = 0.08;
        SurfaceWaterPercent = 0.0;
        CoreComposition     = CoreComposition.Rocky;

        InitializeBiomes();
    }

    private void InitializeBiomes()
    {
        Biomes.Add(new Biome("Silicate Dune Sea", temperature: DaysideTemperatureK,
            humidity: 0.01, vegetationDensity: 0.0, atmosphericPressure: 0.08, mutationRate: 0.04, canGoExtinct: false));
        Biomes.Add(new Biome("Subsurface Cave Network", temperature: (DaysideTemperatureK + NightsideTemperatureK) / 2,
            humidity: 0.10, vegetationDensity: 0.02, atmosphericPressure: 0.10, mutationRate: 0.06, canGoExtinct: true));
        Biomes.Add(new Biome("Polar Frost Cap", temperature: NightsideTemperatureK - 30,
            humidity: 0.15, vegetationDensity: 0.0, atmosphericPressure: 0.09, mutationRate: 0.03, canGoExtinct: false));
    }

    public override void Evolve(int generation, Random rng)
    {
        // Dust storms increase opacity then clear; temperature swings follow
        DustOpacity = Math.Clamp(DustOpacity + (rng.NextDouble() * 0.10 - 0.05), 0.0, 1.0);
        DaysideTemperatureK += (rng.NextDouble() * 4 - 2) * (1 - DustOpacity);
        AgeGyr += 0.01;
    }
}
