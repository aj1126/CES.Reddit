using PlanetaryFormation.Models;

namespace PlanetaryFormation.SimulationCore.Macro;

/// <summary>Qualitative stability bands returned by <see cref="HabitabilityCalculator"/>.</summary>
public enum HabitabilityBand
{
    Hostile,
    Marginal,
    Habitable,
    Optimal
}

/// <summary>
/// Pure static calculator for planetary habitability.
/// All thresholds are based on simplified astronomical models tuned for gameplay balance.
/// </summary>
public static class HabitabilityCalculator
{
    // ── Classical habitable-zone boundaries (AU, scaled by √luminosity) ───────
    private const double InnerEdgeAU  = 0.95;
    private const double OuterEdgeAU  = 1.67;
    private const double OptimalInner = 0.97;
    private const double OptimalOuter = 1.40;

    /// <summary>
    /// Returns a 0–1 Goldilocks score and the corresponding
    /// <see cref="HabitabilityBand"/> for the given celestial body orbiting
    /// the supplied star.
    /// </summary>
    public static (float Score, HabitabilityBand Band) GetGoldilocksScore(
        CelestialBody body, SolarSystem system)
    {
        // Scale habitable zone by stellar luminosity (classical HZ scaling: HZ ∝ √L)
        double sqrtL = Math.Sqrt(system.StarLuminosity);
        double inner = InnerEdgeAU  * sqrtL;
        double outer = OuterEdgeAU  * sqrtL;
        double optIn = OptimalInner * sqrtL;
        double optOut= OptimalOuter * sqrtL;

        double orbit = body.OrbitalRadiusAU;

        // Outside the broad HZ → Hostile
        if (orbit < inner || orbit > outer)
            return (0f, HabitabilityBand.Hostile);

        // Orbital score: 1.0 inside the Optimal band, tapering to the HZ edges
        float orbitalScore;
        if (orbit >= optIn && orbit <= optOut)
        {
            orbitalScore = 1.0f;
        }
        else
        {
            double zoneHalfWidth = (outer - inner) / 2.0;
            double zoneCentre    = (optIn + optOut) / 2.0;
            orbitalScore = (float)(1.0 - Math.Abs(orbit - zoneCentre) / zoneHalfWidth);
        }

        orbitalScore = Math.Clamp(orbitalScore, 0f, 1f);

        // Atmospheric modifier — thin or absent atmosphere penalises liquid water
        float atmScore   = (float)Math.Clamp(body.AtmosphericDensity * 2.0, 0.0, 1.0);

        // Surface water bonus
        float waterScore = (float)Math.Clamp(body.SurfaceWaterPercent, 0.0, 1.0);

        // Core heat proxy: metallic cores retain heat longer, supporting a geodynamo
        float coreScore = body.CoreComposition switch
        {
            CoreComposition.Metallic => 0.9f,
            CoreComposition.Rocky    => 0.7f,
            CoreComposition.Icy      => 0.4f,
            CoreComposition.Gaseous  => 0.1f,
            _                        => 0.5f
        };

        float composite = orbitalScore * 0.45f
                        + atmScore     * 0.25f
                        + waterScore   * 0.20f
                        + coreScore    * 0.10f;

        composite = Math.Clamp(composite, 0f, 1f);

        HabitabilityBand band = composite switch
        {
            >= 0.80f => HabitabilityBand.Optimal,
            >= 0.55f => HabitabilityBand.Habitable,
            >= 0.25f => HabitabilityBand.Marginal,
            _        => HabitabilityBand.Hostile
        };

        return (composite, band);
    }
}
