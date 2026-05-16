using PlanetaryFormation.Models;
using PlanetaryFormation.SimulationCore.Macro;

namespace PlanetaryFormation.Tests;

public class HabitabilityCalculatorTests
{
    private static SolarSystem CreateSystem(double luminosity = 1.0) =>
        new("Sol", "G2V", luminosity);

    private static TerrestrialPlanet CreatePlanet(
        double orbitAu,
        double atmosphere = 0.9,
        double water = 0.7,
        CoreComposition core = CoreComposition.Metallic)
    {
        var planet = new TerrestrialPlanet("Test", 1.0, 1.0, orbitAu, 4.5, 0.7, true)
        {
            AtmosphericDensity = atmosphere,
            SurfaceWaterPercent = water,
            CoreComposition = core
        };
        return planet;
    }

    [Fact]
    public void GetGoldilocksScore_OutsideHabitableZone_ReturnsHostileZero()
    {
        var system = CreateSystem();
        var planet = CreatePlanet(orbitAu: 3.0);

        var (score, band) = HabitabilityCalculator.GetGoldilocksScore(planet, system);

        Assert.Equal(0f, score);
        Assert.Equal(HabitabilityBand.Hostile, band);
    }

    [Fact]
    public void GetGoldilocksScore_EarthLikePlanet_ReturnsOptimal()
    {
        var system = CreateSystem();
        var planet = CreatePlanet(orbitAu: 1.0, atmosphere: 0.9, water: 0.71, core: CoreComposition.Metallic);

        var (score, band) = HabitabilityCalculator.GetGoldilocksScore(planet, system);

        Assert.True(score >= 0.80f);
        Assert.Equal(HabitabilityBand.Optimal, band);
    }

    [Fact]
    public void GetGoldilocksScore_PoorAtmosphereAndWater_ReturnsMarginal()
    {
        var system = CreateSystem();
        var planet = CreatePlanet(orbitAu: 1.0, atmosphere: 0.05, water: 0.0, core: CoreComposition.Icy);

        var (_, band) = HabitabilityCalculator.GetGoldilocksScore(planet, system);

        Assert.Equal(HabitabilityBand.Marginal, band);
    }
}
