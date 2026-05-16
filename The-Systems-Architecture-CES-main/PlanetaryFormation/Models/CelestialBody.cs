namespace PlanetaryFormation.Models;

/// <summary>
/// Abstract base class for all celestial bodies in the solar system.
/// Defines the shared physical properties and lifecycle interface.
/// </summary>
public abstract class CelestialBody
{
    public string Name { get; set; }

    /// <summary>Mass relative to Earth (Earth = 1.0).</summary>
    public double MassEarth { get; set; }

    /// <summary>Radius relative to Earth (Earth = 1.0).</summary>
    public double RadiusEarth { get; set; }

    /// <summary>Distance from the host star in Astronomical Units (AU).</summary>
    public double OrbitalRadiusAU { get; set; }

    /// <summary>Age of the body in billions of years.</summary>
    public double AgeGyr { get; set; }

    /// <summary>Surface gravity relative to Earth (Earth = 1.0).</summary>
    public double SurfaceGravity => MassEarth / (RadiusEarth * RadiusEarth);

    /// <summary>Atmospheric density from 0.0 (vacuum) to 1.0 (dense).</summary>
    public double AtmosphericDensity { get; set; }

    /// <summary>Fraction of the surface covered by liquid or surface water (0.0–1.0).</summary>
    public double SurfaceWaterPercent { get; set; }

    /// <summary>Primary composition of the planet's core.</summary>
    public CoreComposition CoreComposition { get; set; }

    /// <summary>
    /// Accumulated prebiotic chemistry score (0.0–1.0).
    /// Advances each macro tick on planets that meet habitability criteria.
    /// Crossing the configured threshold triggers abiogenesis.
    /// </summary>
    public double PrebioticChemistryScore { get; set; }

    /// <summary>
    /// Simplified Goldilocks check. Returns true when orbital radius, atmospheric
    /// density, and surface water all meet baseline habitability criteria
    /// (assumes solar luminosity ≈ 1.0; use HabitabilityCalculator for star-specific math).
    /// </summary>
    public bool IsHabitable =>
        OrbitalRadiusAU >= 0.70 && OrbitalRadiusAU <= 1.80 &&
        AtmosphericDensity > 0.10 &&
        SurfaceWaterPercent > 0.0;

    /// <summary>
    /// Collection of biomes present on or within this body.
    /// Gas giants may have atmospheric layer biomes; rocky planets have surface biomes.
    /// </summary>
    public List<Biome> Biomes { get; protected set; } = new();

    /// <summary>
    /// The highest stage of biological complexity that has evolved on this planet.
    /// Starts at <see cref="LifeStage.Sterile"/> and is advanced by
    /// <c>AbiogenesisEngine</c> and <c>MicroSimulationManager</c>.
    /// Maps to the LifeStage field of PlanetComponent (IComponentData) in the GDD.
    /// </summary>
    public LifeStage LifeStage { get; set; } = LifeStage.Sterile;

    protected CelestialBody(string name, double massEarth, double radiusEarth, double orbitalRadiusAU, double ageGyr)
    {
        Name = name;
        MassEarth = massEarth;
        RadiusEarth = radiusEarth;
        OrbitalRadiusAU = orbitalRadiusAU;
        AgeGyr = ageGyr;
    }

    /// <summary>
    /// Called once per simulation generation. Subclasses implement planet-specific
    /// formation and aging logic here.
    /// </summary>
    public abstract void Evolve(int generation, Random rng);

    /// <summary>Returns the planet's classification type as a human-readable string.</summary>
    public abstract string PlanetType { get; }

    public override string ToString() =>
        $"[{PlanetType}] {Name}  mass={MassEarth:F2}M⊕  r={RadiusEarth:F2}R⊕  " +
        $"orbit={OrbitalRadiusAU:F2}AU  age={AgeGyr:F2}Gyr  g={SurfaceGravity:F2}g⊕";
}
