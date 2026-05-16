using PlanetaryFormation.Models;
using PlanetaryFormation.SimulationCore.Behavior;
using PlanetaryFormation.SimulationCore.Config;
using PlanetaryFormation.SimulationCore.Debug;
using PlanetaryFormation.SimulationCore.Events;
using PlanetaryFormation.SimulationCore.Game;
using PlanetaryFormation.SimulationCore.Macro;
using PlanetaryFormation.SimulationCore.Micro;
using PlanetaryFormation.SimulationCore.Persistence;
using PlanetaryFormation.SimulationCore.RenderBridge;
using PlanetaryFormation.SimulationCore.Telemetry;
using PlanetaryFormation.SimulationCore.Time;

// ──────────────────────────────────────────────────────────────────────────────
// Configuration
// ──────────────────────────────────────────────────────────────────────────────
int seed = 42;
var config = new SimulationConfig
{
    AbiogenesisThreshold         = 0.30,  // lowered so demo reaches abiogenesis quickly
    AbiogenesisScoreRateOptimal  = 0.10,
    AbiogenesisScoreRateHabitable= 0.03,
    TelemetryOutputDirectory     = Path.Combine(AppContext.BaseDirectory, "Telemetry")
};
SimulationConfig.SetInstance(config);
// Initialise the clock with scales from config so tuning is centralised.
SimulationClock.Initialize(config);

string saveOutputPath = Path.Combine(AppContext.BaseDirectory, "solar_system.json");

// ──────────────────────────────────────────────────────────────────────────────
// Wire up the event bus with console observers (in Unity these would be UI hooks)
// ──────────────────────────────────────────────────────────────────────────────
EventBus.Subscribe<GameStateChangedEvent>(e =>
    Console.WriteLine($"  [State]     {e.OldState} → {e.NewState}"));

EventBus.Subscribe<AbiogenesisEvent>(e =>
    Console.WriteLine($"  [Abiogenesis] Life emerged on {e.Planet.Name} " +
                      $"at {e.SimulatedYears:N0} simulated years  " +
                      $"(prebiotic score: {e.Planet.PrebioticChemistryScore:F2})  " +
                      $"stage={e.Planet.LifeStage}"));

EventBus.Subscribe<SpeciationEvent>(e =>
    Console.WriteLine($"  [Speciation]  New branch in biome '{e.BiomeName}' " +
                      $"(parent gen {e.Parent.GenerationIndex})"));

EventBus.Subscribe<ExtinctionEvent>(e =>
    Console.WriteLine($"  [Extinction]  Species extinct in biome '{e.BiomeName}'"));

// ──────────────────────────────────────────────────────────────────────────────
// Iteration 6 — Universe / Galaxy hierarchy (Big Bang)
// ──────────────────────────────────────────────────────────────────────────────
Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
Console.WriteLine("║      P R O C E D U R A L   U N I V E R S E   S I M       ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
Console.WriteLine();

// The player presses "Big Bang" — a Universe is born with standard physics constants.
var universe = new Universe("Prime Universe", PhysicalConstants.Standard);

// Universe → Galaxy → StarSystem hierarchy
var milkyWayAnalogue = new Galaxy("Via Lactea", massSolarMasses: 1.5e12, ageGyr: 13.6);
universe.AddGalaxy(milkyWayAnalogue);

// Build the initial solar system (StarSystem in GDD terms)
var system = new SolarSystem(
    starName:      "Sol-Alpha",
    starClass:     "G2V",
    starLuminosity: 1.0
);
milkyWayAnalogue.AddStarSystem(system);

Console.WriteLine($"  {universe}");
Console.WriteLine();

system.AddPlanet(new DesertPlanet(
    name: "Ignis", massEarth: 0.38, radiusEarth: 0.53,
    orbitalRadiusAU: 0.39, ageGyr: 4.5,
    daysideTempK: 700, nightsideTempK: 100, dustOpacity: 0.30));

system.AddPlanet(new TerrestrialPlanet(
    name: "Verdania", massEarth: 0.82, radiusEarth: 0.95,
    orbitalRadiusAU: 0.72, ageGyr: 4.5,
    oceanCoverage: 0.25, hasMagnetosphere: true));

system.AddPlanet(new TerrestrialPlanet(
    name: "Terra Nova", massEarth: 1.0, radiusEarth: 1.0,
    orbitalRadiusAU: 1.0, ageGyr: 4.5,
    oceanCoverage: 0.71, hasMagnetosphere: true));

system.AddPlanet(new DesertPlanet(
    name: "Rubrum", massEarth: 0.11, radiusEarth: 0.53,
    orbitalRadiusAU: 1.52, ageGyr: 4.6,
    daysideTempK: 310, nightsideTempK: 150, dustOpacity: 0.55));

system.AddPlanet(new GasGiant(
    name: "Colossus", massEarth: 317.8, radiusEarth: 11.2,
    orbitalRadiusAU: 5.2, ageGyr: 4.6,
    windSpeedMs: 130, ringCount: 0));

system.AddPlanet(new GasGiant(
    name: "Ringed Titan", massEarth: 95.2, radiusEarth: 9.45,
    orbitalRadiusAU: 9.5, ageGyr: 4.6,
    windSpeedMs: 450, ringCount: 7));

system.AddPlanet(new IcePlanet(
    name: "Glacius", massEarth: 14.5, radiusEarth: 4.0,
    orbitalRadiusAU: 19.8, ageGyr: 4.5,
    surfaceTemperatureK: 76, hasSubglacialOcean: true));

system.AddPlanet(new IcePlanet(
    name: "Terminus", massEarth: 17.1, radiusEarth: 3.9,
    orbitalRadiusAU: 30.1, ageGyr: 4.5,
    surfaceTemperatureK: 55, hasSubglacialOcean: false));

Console.WriteLine("  Universe hierarchy:");
Console.WriteLine($"    {universe.Name}");
foreach (var galaxy in universe.Galaxies)
{
    Console.WriteLine($"    └─ {galaxy}");
    foreach (var starSystem in galaxy.StarSystems)
        Console.WriteLine($"       └─ ★ {starSystem}");
}

// ──────────────────────────────────────────────────────────────────────────────
// Iteration 1 — The Catalyst & Time Framework
// ──────────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── Iteration 1: Triggering the Catalyst ─────────────────────");
var stateManager = new GameStateManager(config);
stateManager.TriggerCatalyst(system, seed);

// ──────────────────────────────────────────────────────────────────────────────
// Iteration 2 — Macro-Simulation (Planetary Formation & Abiogenesis)
// ──────────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── Iteration 2: Macro-Simulation (clock-driven) ─────────────");

var rng          = new Random(seed);
var macroManager = new MacroSimulationManager(config);
var microManager = new MicroSimulationManager(config);
macroManager.Attach(system, seed);

// Bridge: when abiogenesis fires, seed the micro layer
EventBus.Subscribe<AbiogenesisEvent>(e => microManager.SeedLife(e.Planet, new Random(seed)));

// Print habitability diagnostics before running
Console.WriteLine("\n  Goldilocks survey:");
foreach (var planet in system.Planets)
{
    var (score, band) = HabitabilityCalculator.GetGoldilocksScore(planet, system);
    Console.WriteLine($"    {planet.Name,-12} score={score:F2}  band={band}  " +
                      $"water={planet.SurfaceWaterPercent:P0}  atm={planet.AtmosphericDensity:F2}  " +
                      $"stage={planet.LifeStage}");
}

// Run macro ticks (each real tick = one generation step at macro scale)
int macroTicks = 20;
Console.WriteLine($"\n  Running {macroTicks} macro ticks…");
for (int i = 0; i < macroTicks; i++)
    SimulationClock.Tick(1.0f);

Console.WriteLine($"\n  Simulated time: {SimulationClock.SimulatedYears:N0} years  " +
                  $"Generation: {system.Generation}");

// ──────────────────────────────────────────────────────────────────────────────
// Iteration 3 — Micro-Simulation (Genomes, Populations, Mutation)
// ──────────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── Iteration 3: Micro-Simulation ─────────────────────────────");
Console.WriteLine($"  Active population pools: {microManager.Pools.Count}");

if (microManager.Pools.Count == 0)
{
    // Manually seed Terra Nova if abiogenesis didn't trigger yet in the demo
    var terraNova = system.Planets.First(p => p.Name == "Terra Nova");
    microManager.SeedLife(terraNova, rng);
    terraNova.LifeStage = LifeStage.Microbial;
    Console.WriteLine("  (Manually seeded Terra Nova for micro-sim demo)");
}

using var telemetry = new TelemetryLogger(config);

// Tick the micro-sim for each seeded biome
SimulationClock.SetMicroScale();
int microTicks = 30;
Console.WriteLine($"\n  Running {microTicks} micro ticks per biome…");
foreach (var (biomeName, pool) in microManager.Pools)
{
    // Find the biome object and its host planet to pass to Tick
    Biome? biomeObj = system.Planets
        .SelectMany(p => p.Biomes)
        .FirstOrDefault(b => b.Name == biomeName);

    CelestialBody? hostPlanet = system.Planets
        .FirstOrDefault(p => p.Biomes.Any(b => b.Name == biomeName));

    if (biomeObj is null) continue;

    for (int i = 0; i < microTicks; i++)
    {
        if (hostPlanet is not null)
            microManager.Tick(biomeObj, hostPlanet, rng);
        else
            microManager.Tick(biomeObj, rng);
    }
}

Console.WriteLine("\n  Species summary after micro-simulation:");
foreach (var (biomeName, pool) in microManager.Pools)
{
    CelestialBody? hostPlanet = system.Planets
        .FirstOrDefault(p => p.Biomes.Any(b => b.Name == biomeName));
    string lifeStageStr = hostPlanet is not null ? $"  stage={hostPlanet.LifeStage}" : "";

    Console.WriteLine($"    Biome '{biomeName}': {pool.ActiveSpecies.Count} species, " +
                      $"biomass={pool.TotalBiomass:N0}{lifeStageStr}");
    foreach (var species in pool.ActiveSpecies.Take(3))
    {
        var traits = SpeciesTraits.FromGenome(species.BaseGenome);
        Console.WriteLine($"      • Gen {species.GenerationIndex,3}  pop={species.Population,8:N0}  " +
                          $"fitness={species.FitnessScore:F2}  " +
                          $"plan={species.BaseGenome.BodyPlan}  " +
                          $"limbs={species.BaseGenome.LimbCount}  " +
                          $"speed={traits.Speed:F2}  " +
                          $"agg={traits.Aggression:F2}  " +
                          $"diet={species.BaseGenome.DietType}");
    }
}

// ──────────────────────────────────────────────────────────────────────────────
// Iteration 4 — Render Bridge (Observer Effect)
// ──────────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── Iteration 4: Render Bridge ────────────────────────────────");

var observerContext = new ObserverContext(microManager);
var spawnBudget     = new SpawnBudget(config);

EventBus.Subscribe<BiomeFocusedEvent>(e =>
{
    Console.WriteLine($"  Camera zoomed into: '{e.Biome.Name}'");
    var sample = spawnBudget.SelectSample(e.Pool);
    Console.WriteLine($"  Spawn budget selects {sample.Count} species:");
    foreach (var (species, count) in sample)
    {
        var blueprint = CreatureAssembler.BuildBlueprint(species);
        Console.WriteLine($"    → {count}× [plan={blueprint.BodyPlan}  limbs={blueprint.LimbCount}  " +
                          $"scale={blueprint.BodyScale:F2}  " +
                          $"bones=({blueprint.BoneScaleX:F2},{blueprint.BoneScaleY:F2},{blueprint.BoneScaleZ:F2})  " +
                          $"color=HSV({blueprint.ColorH:F2},{blueprint.ColorS:F2},{blueprint.ColorV:F2})  " +
                          $"diet={blueprint.DietType}]");
    }
});

EventBus.Subscribe<BiomeFocusClearedEvent>(_ =>
    Console.WriteLine("  Camera zoomed out — all creatures despawned."));

// Simulate zoom-in/zoom-out on the first seeded biome
if (microManager.Pools.Count > 0)
{
    var firstBiomeName = microManager.Pools.Keys.First();
    var firstBiome = system.Planets.SelectMany(p => p.Biomes)
                           .FirstOrDefault(b => b.Name == firstBiomeName);

    if (firstBiome is not null)
    {
        stateManager.ZoomIntoBiome(firstBiome);
        observerContext.SetFocusBiome(firstBiome);
        stateManager.ZoomOut();
        observerContext.ClearFocus();
    }
}

// ──────────────────────────────────────────────────────────────────────────────
// Iteration 5 — Behavior (pure C# contracts for Unity IK)
// ──────────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── Iteration 5: Behavior Engine (data contracts) ─────────────");

if (microManager.Pools.Values.FirstOrDefault()?.ActiveSpecies.FirstOrDefault() is { } demoSpecies)
{
    var brain = new CreatureBrainData
    {
        Hunger           = 0.8f,
        Fear             = 0.0f,
        ReproductiveDrive= 0.3f,
        Diet             = demoSpecies.BaseGenome.DietType,
        SightRange       = demoSpecies.BaseGenome.SensoryOrgans.HasFlag(SensoryOrganFlags.Eyes) ? 10f : 3f,
        CurrentState     = CreatureState.Idle
    };

    var env = new EnvironmentSnapshot { ThreatNearby = false, FoodDensity = 0.5f, MateAvailable = false };
    var newState = BehaviorEngine.UpdateBrain(ref brain, in env);
    Console.WriteLine($"  Demo creature brain tick: hunger={brain.Hunger:F2}  state={newState}");

    var limbCfg = BehaviorEngine.BuildLimbConfiguration(demoSpecies.BaseGenome);
    Console.WriteLine($"  IK contract: {limbCfg.LimbCount} limbs, " +
                      $"lengths=[{string.Join(", ", limbCfg.LimbLengths.Select(l => l.ToString("F2")))}]");

    // ── Iteration 6 addition: LocomotionController PD contract ────────────────
    var locomotion = LocomotionController.FromMass(demoSpecies.BaseGenome.Mass);
    float exampleTorque = locomotion.ComputeTorque(targetAngle: 0f, currentAngle: 15f, angularVelocity: 5f);
    Console.WriteLine($"  PD controller: Kp={locomotion.Kp:F1}  Kd={locomotion.Kd:F1}  " +
                      $"MaxTorque={locomotion.MaxTorque:F1}");
    Console.WriteLine($"  Example joint torque (target=0°, current=15°, ω=5°/s): {exampleTorque:F2}");
}

// ──────────────────────────────────────────────────────────────────────────────
// Iteration 6 — Universe hierarchy & LifeStage summary
// ──────────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── Iteration 6: Universe Hierarchy & Life Stage Summary ───────");
Console.WriteLine($"  Universe: {universe.Name}");
Console.WriteLine($"  Physical constants: {universe.Constants}");
Console.WriteLine();
Console.WriteLine("  Life stage per planet:");
foreach (var planet in system.Planets)
    Console.WriteLine($"    {planet.Name,-12}  stage={planet.LifeStage}  " +
                      $"habitable={planet.IsHabitable}");

// ──────────────────────────────────────────────────────────────────────────────
// Developer Overrides — force specific simulation outcomes
// ──────────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── Developer Overrides ───────────────────────────────────────");

// 1. Force abiogenesis on a planet that hasn't developed life naturally
var rubrum = system.Planets.First(p => p.Name == "Rubrum");
Console.WriteLine($"\n  Before override — Rubrum: stage={rubrum.LifeStage}  " +
                  $"prebiotic={rubrum.PrebioticChemistryScore:F2}");
DeveloperOverrides.ForceAbiogenesis(rubrum, macroManager.AbiogenesisEngine, microManager, rng);
Console.WriteLine($"  After  override — Rubrum: stage={rubrum.LifeStage}  " +
                  $"prebiotic={rubrum.PrebioticChemistryScore:F2}  " +
                  $"pools={microManager.Pools.Count}");

// 2. Jump a planet straight to a specific life stage
var verdania = system.Planets.First(p => p.Name == "Verdania");
Console.WriteLine($"\n  Before override — Verdania: stage={verdania.LifeStage}");
DeveloperOverrides.ForceLifeStage(verdania, LifeStage.Complex,
    macroManager.AbiogenesisEngine, microManager, rng);
Console.WriteLine($"  After  override — Verdania: stage={verdania.LifeStage}");

// 3. Force a speciation event in an existing biome pool
var (firstBiomeNameDev, firstPoolDev) = microManager.Pools.First();
int speciesCountBefore = firstPoolDev.ActiveSpecies.Count;
var newBranch = DeveloperOverrides.ForceSpeciation(firstPoolDev, firstBiomeNameDev, rng, config);
Console.WriteLine($"\n  Forced speciation in '{firstBiomeNameDev}': " +
                  $"{speciesCountBefore} → {firstPoolDev.ActiveSpecies.Count} species  " +
                  (newBranch is not null
                      ? $"new branch plan={newBranch.BaseGenome.BodyPlan}  limbs={newBranch.BaseGenome.LimbCount}"
                      : "(no eligible parent)"));

// 4. Force a specific genome configuration on a species
if (firstPoolDev.ActiveSpecies.FirstOrDefault() is { } targetSpecies)
{
    Console.WriteLine($"\n  Before trait override — speed={targetSpecies.BaseGenome.Speed:F2}  " +
                      $"limbs={targetSpecies.BaseGenome.LimbCount}  " +
                      $"plan={targetSpecies.BaseGenome.BodyPlan}");
    DeveloperOverrides.ForceGenomeTraits(targetSpecies, g =>
    {
        g.Speed            = 3.5f;           // fast runner
        g.LimbCount        = 6;              // hexapod
        g.BodyPlan         = BodyPlan.Bilateral;
        g.NeuralComplexity = 0.85f;          // near-sapient intelligence
        return g;
    });
    Console.WriteLine($"  After  trait override — speed={targetSpecies.BaseGenome.Speed:F2}  " +
                      $"limbs={targetSpecies.BaseGenome.LimbCount}  " +
                      $"plan={targetSpecies.BaseGenome.BodyPlan}  " +
                      $"neural={targetSpecies.BaseGenome.NeuralComplexity:F2}");
}

// 5. Force population to a large stable value (max carrying capacity)
if (firstPoolDev.ActiveSpecies.FirstOrDefault() is { } popTarget)
{
    long popBefore = popTarget.Population;
    DeveloperOverrides.ForcePopulation(popTarget, 500_000, config);
    Console.WriteLine($"\n  Forced population: {popBefore:N0} → {popTarget.Population:N0}");
}

// 6. Force extinction of the least-fit species in a pool
var weakest = firstPoolDev.ActiveSpecies
    .OrderBy(s => s.FitnessScore)
    .FirstOrDefault(s => s.Population < 200);
if (weakest is not null)
{
    Console.WriteLine($"\n  Forcing extinction of weakest species " +
                      $"(pop={weakest.Population}, fitness={weakest.FitnessScore:F2})...");
    DeveloperOverrides.ForceExtinction(weakest, firstPoolDev, firstBiomeNameDev);
    Console.WriteLine($"  Pool now has {firstPoolDev.ActiveSpecies.Count} species.");
}

// 7. Set atmosphere/water habitability factors on an outer planet
//    (IsHabitable will still be false — orbital radius at 19.8 AU is outside the 0.7–1.8 AU band)
var glacius = system.Planets.First(p => p.Name == "Glacius");
Console.WriteLine($"\n  Before local habitability override — Glacius: " +
                  $"water={glacius.SurfaceWaterPercent:P0}  atm={glacius.AtmosphericDensity:F2}  " +
                  $"isHabitable={glacius.IsHabitable} (depends on orbital radius)");
DeveloperOverrides.ForceMaxHabitability(glacius);
Console.WriteLine($"  After  local habitability override — Glacius: " +
                  $"water={glacius.SurfaceWaterPercent:P0}  atm={glacius.AtmosphericDensity:F2}  " +
                  $"isHabitable={glacius.IsHabitable} (still orbital-radius-dependent)");

// ──────────────────────────────────────────────────────────────────────────────
// Game Layer — PlayerActionSystem, ResourceManager, ProceduralEventEngine, Goals
// ──────────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── Game Layer: Player Agency, Procedural Events & Goals ──────");

// -- ResourceManager earns Cosmic Energy from simulation milestones -----------
using var resourceManager = new ResourceManager();
// Award a starting bonus to reflect milestones that already fired in the demo.
resourceManager.Award(50.0);

var playerActions = new PlayerActionSystem(resourceManager);
Console.WriteLine($"  Starting Cosmic Energy: {playerActions.CosmicEnergy:F0} CE");

// Subscribe to game-layer events for console feedback — store handlers so they
// can be unsubscribed at cleanup to avoid accumulating delegates on the static EventBus.
Action<PlayerActionEvent> onPlayerAction = e =>
    Console.WriteLine($"  [Action ✓]   '{e.Action.Description}'  " +
                      $"cost={e.Action.CosmicEnergyCost:F0} CE  " +
                      $"CE remaining={resourceManager.CosmicEnergy:F0}");

Action<PlayerActionRejectedEvent> onPlayerActionRejected = e =>
    Console.WriteLine($"  [Action ✗]   '{e.Action.Description}'  Reason: {e.Reason}");

Action<AsteroidImpactEvent> onAsteroidImpact = e =>
    Console.WriteLine($"  [Event] 💥 Asteroid impact on {e.Planet.Name}! " +
                      $"{e.SpeciesExtinguished} species lost at {e.SimulatedYears:N0} yr");

Action<SolarFlareEvent> onSolarFlare = e =>
    Console.WriteLine($"  [Event] ☀  Solar flare! Affected: " +
                      $"{string.Join(", ", e.AffectedPlanets.Select(p => p.Name))} " +
                      $"at {e.SimulatedYears:N0} yr");

Action<IceAgeEvent> onIceAge = e =>
    Console.WriteLine($"  [Event] 🧊 Ice age on {e.Planet.Name} at {e.SimulatedYears:N0} yr");

Action<GammaRayBurstEvent> onGammaRayBurst = e =>
    Console.WriteLine($"  [Event] ☢  Gamma-ray burst! {e.AffectedPlanets.Count} planets " +
                      $"affected at {e.SimulatedYears:N0} yr");

Action<CometDeliveryEvent> onCometDelivery = e =>
    Console.WriteLine($"  [Event] ☄  Comet delivers organics to {e.TargetPlanet.Name} " +
                      $"(+{e.ScoreBoost:F2} prebiotic) at {e.SimulatedYears:N0} yr");

Action<LifeStageAdvancedEvent> onLifeStageAdvanced = e =>
    Console.WriteLine($"  [Stage]  {e.Planet.Name}: {e.OldStage} → {e.NewStage}  " +
                      $"(+{ResourceManager.LifeStageBonus:F0} CE bonus)");

Action<ScenarioVictoryEvent> onVictory = e =>
    Console.WriteLine($"\n  ★ VICTORY — {e.Scenario.Name} completed at {e.SimulatedYears:N0} yr!");

Action<ScenarioDefeatEvent> onDefeat = e =>
    Console.WriteLine($"\n  ✗ DEFEAT  — {e.Scenario.Name}: {e.Reason}");

EventBus.Subscribe(onPlayerAction);
EventBus.Subscribe(onPlayerActionRejected);
EventBus.Subscribe(onAsteroidImpact);
EventBus.Subscribe(onSolarFlare);
EventBus.Subscribe(onIceAge);
EventBus.Subscribe(onGammaRayBurst);
EventBus.Subscribe(onCometDelivery);
EventBus.Subscribe(onLifeStageAdvanced);
EventBus.Subscribe(onVictory);
EventBus.Subscribe(onDefeat);

// -- Set up a scenario --------------------------------------------------------
var scenario     = ScenarioDefinition.GuideToCivilisation(timeLimitYears: 100_000_000.0);
var goalsSystem  = new GoalsSystem(system, scenario);
Console.WriteLine($"\n  Scenario: {scenario.Name}");
Console.WriteLine($"  Goal:     {scenario.Description}");

// -- ProceduralEventEngine ticks with the clock --------------------------------
// Use a tighter mean interval so the demo fires at least one event.
var demoConfig = new SimulationConfig
{
    AbiogenesisThreshold         = config.AbiogenesisThreshold,
    AbiogenesisScoreRateOptimal  = config.AbiogenesisScoreRateOptimal,
    AbiogenesisScoreRateHabitable= config.AbiogenesisScoreRateHabitable,
    MacroYearsPerSecond          = config.MacroYearsPerSecond,
    ProceduralEventMeanIntervalYears = 50_000.0,  // short interval for demo
    TelemetryOutputDirectory     = config.TelemetryOutputDirectory,
};
var proceduralEngine = new ProceduralEventEngine(system, microManager, demoConfig, new Random(seed + 1));

SimulationClock.SetMacroScale();
Console.WriteLine("\n  Running 5 clock ticks with procedural events…");
for (int i = 0; i < 5; i++)
    SimulationClock.Tick(1.0f);

// -- Demo player actions -------------------------------------------------------
Console.WriteLine("\n  Demonstrating PlayerActionSystem:");

double now = SimulationClock.SimulatedYears;
var terraNovaPlanet = system.Planets.First(p => p.Name == "Terra Nova");
var rubrumPlanet    = system.Planets.First(p => p.Name == "Rubrum");

// Action 1: Nudge Rubrum's atmosphere (should succeed)
var nudge = new NudgeAtmosphereAction(rubrumPlanet);
playerActions.TryExecute(nudge, now);

// Action 2: Try the same action again immediately (should be rejected — on cooldown)
playerActions.TryExecute(nudge, now);

// Action 3: Seed prebiotics on Terra Nova
if (microManager.Pools.TryGetValue(
    system.Planets.First(p => p.Name == "Terra Nova").Biomes.FirstOrDefault()?.Name ?? "",
    out _))
{
    var prebiotics = new SeedPrebioticsAction(terraNovaPlanet, 0.20);
    playerActions.TryExecute(prebiotics, now);
}

// Action 4: Boost mutation rate in the first available biome
if (microManager.Pools.Count > 0)
{
    var (boostBiomeName, _) = microManager.Pools.First();
    var biomeObj = system.Planets.SelectMany(p => p.Biomes)
                         .FirstOrDefault(b => b.Name == boostBiomeName);
    if (biomeObj is not null)
    {
        var boost = new BoostMutationRateAction(biomeObj, 2.0f);
        playerActions.TryExecute(boost, now);
    }
}

// Action 5: Force abiogenesis — expensive, should drain CE significantly
var abiogenesisAction = new TriggerAbiogenesisAction(
    rubrumPlanet, macroManager.AbiogenesisEngine, microManager, rng);
playerActions.TryExecute(abiogenesisAction, now);

// Action 6: Attempt speciation in a pool
if (microManager.Pools.Count > 0)
{
    var (speciationBiome, speciationPool) = microManager.Pools.First();
    var speciation = new TriggerSpeciationAction(speciationPool, speciationBiome, rng, config);
    playerActions.TryExecute(speciation, now);
}

Console.WriteLine($"\n  Cosmic Energy after actions: {playerActions.CosmicEnergy:F0} CE");
Console.WriteLine($"  Scenario outcome so far: {goalsSystem.Outcome}");

// Clean up game-layer subscriptions
proceduralEngine.Detach();
goalsSystem.Detach();
EventBus.Unsubscribe(onPlayerAction);
EventBus.Unsubscribe(onPlayerActionRejected);
EventBus.Unsubscribe(onAsteroidImpact);
EventBus.Unsubscribe(onSolarFlare);
EventBus.Unsubscribe(onIceAge);
EventBus.Unsubscribe(onGammaRayBurst);
EventBus.Unsubscribe(onCometDelivery);
EventBus.Unsubscribe(onLifeStageAdvanced);
EventBus.Unsubscribe(onVictory);
EventBus.Unsubscribe(onDefeat);
// resourceManager is cleaned up automatically by the `using var` declaration above.

// ──────────────────────────────────────────────────────────────────────────────
// Cross-cutting: Telemetry flush & Save
// ──────────────────────────────────────────────────────────────────────────────
telemetry.Flush();

system.SortByOrbitalRadius();
SaveManager.Save(system, microManager.Pools, saveOutputPath);
Console.WriteLine($"\n✔ Simulation save → {saveOutputPath}");
Console.WriteLine("✔ Telemetry CSV  → " + config.TelemetryOutputDirectory);

