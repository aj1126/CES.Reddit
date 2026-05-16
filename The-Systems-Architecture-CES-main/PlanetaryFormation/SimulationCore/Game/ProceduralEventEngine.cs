using PlanetaryFormation.Models;
using PlanetaryFormation.SimulationCore.Config;
using PlanetaryFormation.SimulationCore.Debug;
using PlanetaryFormation.SimulationCore.Events;
using PlanetaryFormation.SimulationCore.Micro;
using PlanetaryFormation.SimulationCore.Time;

namespace PlanetaryFormation.SimulationCore.Game;

// ── Procedural event records ──────────────────────────────────────────────────

/// <summary>Published when an asteroid impact devastates a planet.</summary>
public record AsteroidImpactEvent(CelestialBody Planet, int SpeciesExtinguished, double SimulatedYears);

/// <summary>Published when a solar flare scorches the nearest planets.</summary>
public record SolarFlareEvent(IReadOnlyList<CelestialBody> AffectedPlanets, double SimulatedYears);

/// <summary>Published when a global ice age cripples a planet's biomes.</summary>
public record IceAgeEvent(CelestialBody Planet, double SimulatedYears);

/// <summary>
/// Published when a gamma-ray burst causes mass extinction across the system.
/// </summary>
public record GammaRayBurstEvent(IReadOnlyList<CelestialBody> AffectedPlanets, double SimulatedYears);

/// <summary>
/// Published when a comet delivers organic compounds to a sterile planet
/// (panspermia opportunity).
/// </summary>
public record CometDeliveryEvent(CelestialBody TargetPlanet, double ScoreBoost, double SimulatedYears);

// ── Engine ────────────────────────────────────────────────────────────────────

/// <summary>
/// Listens to <see cref="TickEvent"/> and fires narrative, Poisson-distributed
/// catastrophic and opportunity events.
///
/// Each tick samples an exponential inter-arrival time using the mean interval
/// from <see cref="SimulationConfig.ProceduralEventMeanIntervalYears"/>. When the
/// accumulated simulated time since the last event exceeds the next scheduled
/// arrival, one event type is drawn at random and executed.
///
/// Catastrophic effects delegate to <see cref="DeveloperOverrides"/> so the
/// simulation layer remains pure; the procedural-event engine only adds game
/// narrative on top.
/// </summary>
public class ProceduralEventEngine
{
    private readonly SimulationConfig       _config;
    private readonly SolarSystem            _system;
    private readonly MicroSimulationManager _microManager;
    private readonly Random                 _rng;

    /// <summary>Simulated years at which the next procedural event will fire.</summary>
    private double _nextEventAt;

    // Relative weights for each event category.
    private static readonly (ProceduralEventType Type, int Weight)[] EventWeights =
    {
        (ProceduralEventType.AsteroidImpact,  20),
        (ProceduralEventType.SolarFlare,      15),
        (ProceduralEventType.IceAge,          15),
        (ProceduralEventType.GammaRayBurst,    5),
        (ProceduralEventType.CometDelivery,   45),
    };

    private static readonly int TotalWeight = EventWeights.Sum(e => e.Weight);

    public ProceduralEventEngine(
        SolarSystem system,
        MicroSimulationManager microManager,
        SimulationConfig? config = null,
        Random? rng = null)
    {
        _config       = config ?? SimulationConfig.Instance;
        _system       = system;
        _microManager = microManager;
        _rng          = rng ?? new Random();

        EventBus.Subscribe<TickEvent>(OnTick);
        // Schedule the first event relative to the current simulated time so the
        // engine does not fire immediately if it is created mid-simulation.
        _nextEventAt = SampleNextArrival(SimulationClock.SimulatedYears);
    }

    /// <summary>Removes the TickEvent subscription.</summary>
    public void Detach() => EventBus.Unsubscribe<TickEvent>(OnTick);

    // ── Internal ──────────────────────────────────────────────────────────────

    private void OnTick(TickEvent tick)
    {
        // Process all Poisson arrivals that fall within this tick — large macro
        // time steps can advance past multiple scheduled events at once.
        while (tick.SimulatedYears >= _nextEventAt)
        {
            double eventAt = _nextEventAt;
            FireRandomEvent(eventAt);
            _nextEventAt = SampleNextArrival(eventAt);
        }
    }

    private double SampleNextArrival(double fromYears)
    {
        // Exponential inter-arrival: -mean * ln(U)
        double u = Math.Max(1e-10, _rng.NextDouble());
        return fromYears + (-_config.ProceduralEventMeanIntervalYears * Math.Log(u));
    }

    private void FireRandomEvent(double simulatedYears)
    {
        var type = DrawEventType();
        switch (type)
        {
            case ProceduralEventType.AsteroidImpact:
                FireAsteroidImpact(simulatedYears); break;
            case ProceduralEventType.SolarFlare:
                FireSolarFlare(simulatedYears); break;
            case ProceduralEventType.IceAge:
                FireIceAge(simulatedYears); break;
            case ProceduralEventType.GammaRayBurst:
                FireGammaRayBurst(simulatedYears); break;
            case ProceduralEventType.CometDelivery:
                FireCometDelivery(simulatedYears); break;
        }
    }

    private ProceduralEventType DrawEventType()
    {
        int roll = _rng.Next(TotalWeight);
        int cumulative = 0;
        foreach (var (type, weight) in EventWeights)
        {
            cumulative += weight;
            if (roll < cumulative) return type;
        }
        return ProceduralEventType.CometDelivery;
    }

    // ── Event implementations ─────────────────────────────────────────────────

    private void FireAsteroidImpact(double simulatedYears)
    {
        var target = PickRandomLivingPlanet();
        if (target is null) return;

        int killed = ExtinguishFraction(target, _config.MassExtinctionFraction * 0.5);
        EventBus.Publish(new AsteroidImpactEvent(target, killed, simulatedYears));
    }

    private void FireSolarFlare(double simulatedYears)
    {
        // Affects the two innermost planets
        var affected = _system.Planets
            .OrderBy(p => p.OrbitalRadiusAU)
            .Take(2)
            .ToList();

        foreach (var planet in affected)
            ExtinguishFraction(planet, 0.25);

        EventBus.Publish(new SolarFlareEvent(affected, simulatedYears));
    }

    private void FireIceAge(double simulatedYears)
    {
        var target = PickRandomLivingPlanet();
        if (target is null) return;

        // Reduce mutation rate in all biomes
        foreach (var biome in target.Biomes)
            biome.MutationRate = Math.Max(0f, biome.MutationRate * 0.5f);

        ExtinguishFraction(target, 0.30);
        EventBus.Publish(new IceAgeEvent(target, simulatedYears));
    }

    private void FireGammaRayBurst(double simulatedYears)
    {
        // Affects all planets with life
        var affected = _system.Planets
            .Where(p => p.LifeStage > Models.LifeStage.Sterile)
            .ToList();

        foreach (var planet in affected)
            ExtinguishFraction(planet, _config.MassExtinctionFraction);

        EventBus.Publish(new GammaRayBurstEvent(affected, simulatedYears));
    }

    private void FireCometDelivery(double simulatedYears)
    {
        // Target a sterile planet to give it an organic boost
        var sterile = _system.Planets
            .Where(p => p.LifeStage == Models.LifeStage.Sterile && p.IsHabitable)
            .ToList();

        CelestialBody? target = sterile.Count > 0
            ? sterile[_rng.Next(sterile.Count)]
            : null;

        if (target is null) return;

        DeveloperOverrides.ForcePrebioticScore(
            target,
            Math.Min(1.0, target.PrebioticChemistryScore + _config.PanspermiaScoreBoost));

        EventBus.Publish(new CometDeliveryEvent(target, _config.PanspermiaScoreBoost, simulatedYears));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private CelestialBody? PickRandomLivingPlanet()
    {
        var living = _system.Planets
            .Where(p => p.LifeStage > Models.LifeStage.Sterile)
            .ToList();

        return living.Count > 0 ? living[_rng.Next(living.Count)] : null;
    }

    /// <summary>
    /// Extinguishes a randomly selected fraction of a planet's active species.
    /// Returns the number of species extinguished.
    /// </summary>
    private int ExtinguishFraction(CelestialBody planet, double fraction)
    {
        fraction = Math.Clamp(fraction, 0d, 1d);

        int killed = 0;
        foreach (var biome in planet.Biomes)
        {
            if (!_microManager.Pools.TryGetValue(biome.Name, out var pool)) continue;

            var candidates = pool.ActiveSpecies.ToList();
            if (candidates.Count == 0 || fraction <= 0d) continue;

            // Use floor rounding so a single-species biome is only wiped when
            // fraction == 1.0; for all other fractions at least 1 victim is chosen
            // only when there are enough candidates.
            int count = (int)Math.Floor(candidates.Count * fraction);
            if (count <= 0) continue;

            // Fisher-Yates partial shuffle so victims are chosen at random.
            for (int i = 0; i < count; i++)
            {
                int swapIndex = _rng.Next(i, candidates.Count);
                (candidates[i], candidates[swapIndex]) = (candidates[swapIndex], candidates[i]);

                DeveloperOverrides.ForceExtinction(candidates[i], pool, biome.Name);
                killed++;
            }
        }
        return killed;
    }
}

/// <summary>Internal discriminator for random event selection.</summary>
internal enum ProceduralEventType
{
    AsteroidImpact,
    SolarFlare,
    IceAge,
    GammaRayBurst,
    CometDelivery,
}
