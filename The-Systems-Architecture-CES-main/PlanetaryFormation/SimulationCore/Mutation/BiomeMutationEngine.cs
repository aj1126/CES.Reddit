using PlanetaryFormation.Models;

namespace PlanetaryFormation.SimulationCore.Mutation;

/// <summary>
/// Applies genetic-style mutations to a planet's biomes each generation.
/// Renamed from <c>MutationEngine</c> to distinguish biome-level environmental
/// mutation from species genome mutation (handled by
/// <c>MicroSimulationManager.MutationStep</c>).
///
/// Mutation logic:
///   1. For each biome, roll against its MutationRate.
///   2. On a hit, one randomly chosen property is nudged by a Gaussian-distributed delta.
///   3. Properties are clamped to physically meaningful ranges after each mutation.
///   4. With a low probability a completely new "mutant" biome may emerge (biome birth).
///   5. Biomes with vegetation near zero may go extinct (biome death).
/// </summary>
public static class BiomeMutationEngine
{
    private const double BiomeBirthChance    = 0.03;   // 3 % per generation per planet
    private const double BiomeDeathThreshold = 0.005;  // vegetation below this → candidate for removal

    /// <summary>
    /// Applies one round of genetic-style mutations to all biomes on every planet
    /// in the solar system.
    /// </summary>
    public static void ApplyMutations(SolarSystem system, Random rng, double chaosFactor = 0.0)
    {
        foreach (var planet in system.Planets)
            MutatePlanet(planet, rng, chaosFactor);
    }

    private static void MutatePlanet(CelestialBody planet, Random rng, double chaosFactor)
    {
        var toRemove = new List<Biome>();

        foreach (var biome in planet.Biomes)
        {
            if (rng.NextDouble() > biome.MutationRate) continue;

            MutateBiome(biome, rng, chaosFactor);
            biome.GenerationsEvolved++;

            // Mark for extinction if vegetation collapses and the biome allows it
            if (biome.VegetationDensity < BiomeDeathThreshold && biome.CanGoExtinct)
                if (rng.NextDouble() < 0.25)
                    toRemove.Add(biome);
        }

        foreach (var dead in toRemove)
            planet.Biomes.Remove(dead);

        // Biome birth: scales with chaos. At ChaosFactor = 0: 3% chance (baseline).
        // At ChaosFactor = 1 (peak Reddit engagement): up to 10% — volcanic upheaval
        // and cosmic bombardment carve new ecological niches out of existing terrain.
        double biomeBirthChance = BiomeBirthChance + chaosFactor * 0.07;
        if (rng.NextDouble() < biomeBirthChance)
            planet.Biomes.Add(GenerateMutantBiome(planet, rng));
    }

    private static void MutateBiome(Biome biome, Random rng, double chaosFactor)
    {
        int property = rng.Next(4);
        // ChaosFactor widens the environmental mutation spread. At ChaosFactor = 0:
        // stdDev = 0.05 (V1 baseline). At ChaosFactor = 1: stdDev = 0.15 (3× wider).
        // This means a "Solar Flare" vote produces more extreme biome shifts per tick,
        // not just more frequent genome mutations.
        double delta = SampleGaussian(rng, mean: 0, stdDev: 0.05 + chaosFactor * 0.10);

        switch (property)
        {
            case 0:
                biome.Temperature = Math.Max(20, biome.Temperature * (1 + delta));
                break;
            case 1:
                biome.Humidity = Math.Clamp(biome.Humidity + delta, 0.0, 1.0);
                break;
            case 2:
                biome.VegetationDensity = Math.Clamp(biome.VegetationDensity + delta * 0.5, 0.0, 1.0);
                break;
            case 3:
                biome.AtmosphericPressure = Math.Max(0.001, biome.AtmosphericPressure * (1 + delta));
                break;
        }
    }

    private static Biome GenerateMutantBiome(CelestialBody planet, Random rng)
    {
        Biome? seed = planet.Biomes.Count > 0
            ? planet.Biomes[rng.Next(planet.Biomes.Count)]
            : null;

        double baseTemp = seed?.Temperature        ?? 300;
        double baseHum  = seed?.Humidity           ?? 0.5;
        double baseVeg  = seed?.VegetationDensity  ?? 0.3;
        double baseAtm  = seed?.AtmosphericPressure ?? 1.0;

        return new Biome(
            name: $"Mutant Biome Gen-{rng.Next(10000)}",
            temperature: Math.Max(20, baseTemp * (0.85 + rng.NextDouble() * 0.30)),
            humidity: Math.Clamp(baseHum + SampleGaussian(rng, 0, 0.15), 0.0, 1.0),
            vegetationDensity: Math.Clamp(baseVeg + SampleGaussian(rng, 0, 0.10), 0.0, 1.0),
            atmosphericPressure: Math.Max(0.001, baseAtm * (0.80 + rng.NextDouble() * 0.40)),
            mutationRate: Math.Clamp(rng.NextDouble() * 0.20, 0.01, 0.20),
            canGoExtinct: true
        );
    }

    /// <summary>
    /// Box-Muller transform to sample from a normal distribution.
    /// Exposed as internal so MicroSimulationManager can reuse it for genome nudges.
    /// </summary>
    internal static double SampleGaussian(Random rng, double mean, double stdDev)
    {
        double u1 = 1.0 - rng.NextDouble();
        double u2 = 1.0 - rng.NextDouble();
        double z  = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        return mean + stdDev * z;
    }
}
