namespace PlanetaryFormation.Models;

/// <summary>
/// Represents a biome on a planet's surface with properties that can undergo
/// genetic-style mutations across simulation generations.
/// </summary>
public class Biome
{
    public string Name { get; set; }

    /// <summary>Average surface temperature in Kelvin.</summary>
    public double Temperature { get; set; }

    /// <summary>Humidity level from 0.0 (arid) to 1.0 (saturated).</summary>
    public double Humidity { get; set; }

    /// <summary>Vegetation density from 0.0 (barren) to 1.0 (lush).</summary>
    public double VegetationDensity { get; set; }

    /// <summary>Atmospheric pressure in Earth atmospheres (atm).</summary>
    public double AtmosphericPressure { get; set; }

    /// <summary>Probability (0.0–1.0) that this biome mutates in a given generation.</summary>
    public double MutationRate { get; set; }

    public int GenerationsEvolved { get; set; }

    /// <summary>
    /// When <c>true</c>, this biome is eligible for extinction if its vegetation
    /// density collapses below the engine threshold. Abiotic biomes (e.g. gas-giant
    /// cloud layers, dune seas, ice plains) should set this to <c>false</c>.
    /// </summary>
    public bool CanGoExtinct { get; set; }

    public Biome(
        string name,
        double temperature,
        double humidity,
        double vegetationDensity,
        double atmosphericPressure,
        double mutationRate,
        bool canGoExtinct = true)
    {
        Name = name;
        Temperature = temperature;
        Humidity = humidity;
        VegetationDensity = vegetationDensity;
        AtmosphericPressure = atmosphericPressure;
        MutationRate = Math.Clamp(mutationRate, 0.0, 1.0);
        CanGoExtinct = canGoExtinct;
        GenerationsEvolved = 0;
    }

    public override string ToString() =>
        $"{Name} [T={Temperature:F1}K, H={Humidity:P0}, V={VegetationDensity:P0}, P={AtmosphericPressure:F2}atm, MR={MutationRate:P0}]";
}
