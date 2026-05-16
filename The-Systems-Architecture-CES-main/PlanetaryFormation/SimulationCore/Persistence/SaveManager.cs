using System.Text.Json;
using PlanetaryFormation.Models;
using PlanetaryFormation.SimulationCore.Micro;

namespace PlanetaryFormation.SimulationCore.Persistence;

/// <summary>
/// Serialises and deserialises full simulation state to/from JSON.
/// Extends the existing ExportJson pattern from SimulationLoop to include
/// all population pool data.
/// </summary>
public static class SaveManager
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented        = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Writes a complete snapshot of the solar system plus all population pools
    /// to <paramref name="outputPath"/>.
    /// </summary>
    public static void Save(
        SolarSystem system,
        IReadOnlyDictionary<string, PopulationPool> pools,
        string outputPath)
    {
        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var    snapshot = BuildSnapshot(system, pools);
        string json     = JsonSerializer.Serialize(snapshot, Options);
        File.WriteAllText(outputPath, json);
    }

    private static object BuildSnapshot(
        SolarSystem system,
        IReadOnlyDictionary<string, PopulationPool> pools)
    {
        return new
        {
            savedAt        = DateTime.UtcNow.ToString("o"),
            starName       = system.StarName,
            starClass      = system.StarClass,
            starLuminosity = system.StarLuminosity,
            generation     = system.Generation,
            planets        = system.Planets.Select(BuildPlanetEntry).ToList(),
            populationPools = pools.Select(kvp => new
            {
                biome   = kvp.Key,
                species = kvp.Value.ActiveSpecies.Select(s => new
                {
                    speciesId      = s.SpeciesId,
                    parentId       = s.ParentSpeciesId,
                    population     = s.Population,
                    fitnessScore   = s.FitnessScore,
                    generationIndex= s.GenerationIndex,
                    genome         = new
                    {
                        limbCount         = s.BaseGenome.LimbCount,
                        limbLengthAvg     = s.BaseGenome.LimbLengthAvg,
                        sensoryOrgans     = s.BaseGenome.SensoryOrgans.ToString(),
                        idealTempK        = s.BaseGenome.IdealTempK,
                        tempToleranceK    = s.BaseGenome.TempToleranceK,
                        mass              = s.BaseGenome.Mass,
                        metabolismRate    = s.BaseGenome.MetabolismRate,
                        reproductionRate  = s.BaseGenome.ReproductionRate,
                        mutationVolatility= s.BaseGenome.MutationVolatility,
                        dietType          = s.BaseGenome.DietType.ToString()
                    }
                }).ToList()
            }).ToList()
        };
    }

    private static object BuildPlanetEntry(CelestialBody planet)
    {
        return new
        {
            name               = planet.Name,
            type               = planet.PlanetType,
            orbitalRadiusAU    = planet.OrbitalRadiusAU,
            atmosphericDensity = planet.AtmosphericDensity,
            surfaceWaterPercent= planet.SurfaceWaterPercent,
            coreComposition    = planet.CoreComposition.ToString(),
            prebioticScore     = planet.PrebioticChemistryScore,
            isHabitable        = planet.IsHabitable,
            biomeCount         = planet.Biomes.Count
        };
    }
}
