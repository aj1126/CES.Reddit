using System.Text.Json;
using PlanetaryFormation.Models;
using PlanetaryFormation.SimulationCore.Mutation;

namespace PlanetaryFormation.Simulation;

/// <summary>
/// The core simulation loop. Each tick of the loop:
///   1. Calls <see cref="CelestialBody.Evolve"/> on every planet.
///   2. Applies genetic-style mutations via <see cref="MutationEngine"/>.
///   3. Increments the generation counter on the solar system.
///
/// After all generations have run, the final state is serialised to a
/// formatted JSON file via <see cref="ExportJson"/>.
/// </summary>
public class SimulationLoop
{
    private readonly SolarSystem _system;
    private readonly int _generations;
    private readonly Random _rng;
    private readonly bool _verbose;

    public SimulationLoop(SolarSystem system, int generations, int? seed = null, bool verbose = true)
    {
        _system     = system;
        _generations = generations;
        _rng        = seed.HasValue ? new Random(seed.Value) : new Random();
        _verbose    = verbose;
    }

    /// <summary>Runs the full simulation and then exports JSON.</summary>
    public void Run(string outputPath)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     P L A N E T A R Y   F O R M A T I O N   S I M        ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine(_system);
        Console.WriteLine($"Running {_generations} generation(s)…\n");

        for (int gen = 1; gen <= _generations; gen++)
        {
            // 1. Evolve each planet (planet-specific physical changes)
            foreach (var planet in _system.Planets)
                planet.Evolve(gen, _rng);

            // 2. Apply biome mutations across the whole system
            BiomeMutationEngine.ApplyMutations(_system, _rng);

            _system.Generation = gen;

            if (_verbose || gen % 10 == 0 || gen == _generations)
                PrintGenerationSummary(gen);
        }

        _system.SortByOrbitalRadius();

        Console.WriteLine("\n══════════════ FINAL SOLAR SYSTEM STATE ══════════════");
        Console.WriteLine(_system);
        foreach (var planet in _system.Planets)
        {
            Console.WriteLine($"\n  {planet}");
            foreach (var biome in planet.Biomes)
                Console.WriteLine($"    • {biome}");
        }

        ExportJson(outputPath);

        Console.WriteLine($"\n✔ JSON exported → {outputPath}");
    }

    private void PrintGenerationSummary(int gen)
    {
        int totalBiomes = _system.Planets.Sum(p => p.Biomes.Count);
        Console.WriteLine($"  Gen {gen,3}  planets={_system.Planets.Count}  total biomes={totalBiomes}");
    }

    /// <summary>
    /// Serialises the solar system to a human-readable, indented JSON file.
    /// The shape mirrors the domain model so it can be re-imported by other tools.
    /// </summary>
    public void ExportJson(string outputPath)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        var snapshot = BuildSnapshot();
        string json = JsonSerializer.Serialize(snapshot, options);
        File.WriteAllText(outputPath, json);
    }

    private object BuildSnapshot()
    {
        return new
        {
            starName       = _system.StarName,
            starClass      = _system.StarClass,
            starLuminosity = _system.StarLuminosity,
            generation     = _system.Generation,
            exportedAt     = DateTime.UtcNow.ToString("o"),
            planets        = _system.Planets.Select(p => BuildPlanetSnapshot(p)).ToList()
        };
    }

    private static object BuildPlanetSnapshot(CelestialBody planet)
    {
        var basePart = new
        {
            name            = planet.Name,
            type            = planet.PlanetType,
            massEarth       = planet.MassEarth,
            radiusEarth     = planet.RadiusEarth,
            orbitalRadiusAU = planet.OrbitalRadiusAU,
            ageGyr          = planet.AgeGyr,
            surfaceGravity  = planet.SurfaceGravity,
            biomes          = planet.Biomes.Select(b => new
            {
                name               = b.Name,
                temperatureK       = b.Temperature,
                humidity           = b.Humidity,
                vegetationDensity  = b.VegetationDensity,
                atmosphericPressure= b.AtmosphericPressure,
                mutationRate       = b.MutationRate,
                canGoExtinct       = b.CanGoExtinct,
                generationsEvolved = b.GenerationsEvolved
            }).ToList(),
            typeSpecific = BuildTypeSpecific(planet)
        };
        return basePart;
    }

    private static object BuildTypeSpecific(CelestialBody planet)
    {
        return planet switch
        {
            TerrestrialPlanet t => new
            {
                oceanCoverage    = t.OceanCoverage,
                hasMagnetosphere = t.HasMagnetosphere
            },
            GasGiant g => new
            {
                windSpeedMs = g.WindSpeedMs,
                ringCount   = g.RingCount
            },
            IcePlanet i => new
            {
                surfaceTemperatureK  = i.SurfaceTemperatureK,
                hasSubglacialOcean   = i.HasSubglacialOcean
            },
            DesertPlanet d => new
            {
                daysideTemperatureK  = d.DaysideTemperatureK,
                nightsideTemperatureK= d.NightsideTemperatureK,
                dustOpacity          = d.DustOpacity
            },
            _ => new { }
        };
    }
}
