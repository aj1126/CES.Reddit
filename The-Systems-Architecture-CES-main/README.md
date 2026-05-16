# Cosmic Evolution Simulator — Systems Architecture Demo

Scaffolds the complete Unity-agnostic back-end for the Procedural Universe Evolution Simulator per the Game Design Document (GDD), covering all six iteration sprints. The simulation runs as pure data from Big Bang through speciation, with a clean render/data boundary, CSV telemetry, and a full developer override toolkit.

## New: `PlanetaryFormation/SimulationCore/`

### Iteration 1 — Time Framework
- **`SimulationConfig`** — singleton holding every tunable float (Pillar V); injectable for tests
- **`EventBus`** — typed `Subscribe<T>` / `Publish<T>`; decouples all layers with no direct cross-boundary references
- **`SimulationClock`** — static clock with macro (1 M years/sec) and micro (1 day/sec) modes; publishes `TickEvent` on every advance
- **`GameStateManager`** — `PreCatalyst → MacroSimulation → Observing` state machine driven by `TriggerCatalyst()` / `ZoomIntoBiome()` / `ZoomOut()`

### Iteration 2 — Macro-Simulation
- **`HabitabilityCalculator`** — Goldilocks zone scoring (orbital radius × luminosity, atm density, surface water, core composition); returns `HabitabilityBand` enum
- **`AbiogenesisEngine`** — accumulates `PrebioticChemistryScore` per tick on qualifying planets; fires `AbiogenesisEvent` when threshold crossed; sets `LifeStage = Microbial` automatically
- **`MacroSimulationManager`** — clock-driven replacement for the old `SimulationLoop` `for`-loop; subscribes to `TickEvent`

`CelestialBody` extended with `AtmosphericDensity`, `SurfaceWaterPercent`, `CoreComposition`, `PrebioticChemistryScore`, `IsHabitable`, `LifeStage`; all four planet subtypes initialize their new fields.

### Iteration 3 — Micro-Simulation
- **`Genome`** (struct) — all value-type fields; ECS/NativeArray-portable; includes `Crossover(Genome, Random)`; extended with `BodyPlan`, `BoneScaleX/Y/Z`, `ColorH/S/V`, `NeuralComplexity`, `Speed`, `Aggression`
- **`SpeciesData`** — owns a `Genome`, tracks population, fitness, generation index, and `Branches` (phylogenetic tree)
- **`SpeciesTraits`** (struct) — phenotype summary derived from `Genome` (size, speed, metabolism, aggression, reproductionRate); ECS `NativeArray`-portable; maps to `SpeciesTraits (IComponentData)` in GDD
- **`EnvironmentalPressureEngine`** — full GDD fitness formula: `environmentalMatchScore + speedAdvantage + energyEfficiency − instabilityPenalty`
- **`MicroSimulationManager`** — seeds primordial life per biome, ticks pressure → typed genome mutation → speciation branch → extinction check; `Tick(Biome, CelestialBody, rng)` overload also advances `LifeStage` automatically

```csharp
// Speciation fires when a single-field delta exceeds SpeciationThreshold
EventBus.Subscribe<SpeciationEvent>(e =>
    Console.WriteLine($"New branch in '{e.BiomeName}' from gen {e.Parent.GenerationIndex}"));
```

### Iteration 4 — Render Bridge
- **`ICreatureSpawner`** — interface Unity's `MonoBehaviour` implements; simulation holds only the interface
- **`CreatureAssembler.BuildBlueprint(SpeciesData)`** — converts genome to `CreatureBlueprint` struct (body plan, limb count/length, bone scales, color HSV, sensory organ flags, log-scaled body size)
- **`ObserverContext`** — single camera entry point; publishes `BiomeFocusedEvent` with the live `PopulationPool`
- **`SpawnBudget`** — population-weighted sample capped at `MaxActiveCreatures` (default 50)

### Iteration 5 — Behavior (data contracts)
- **`BehaviorEngine.UpdateBrain(ref CreatureBrainData, in EnvironmentSnapshot)`** — pure-C# state machine: Flee > Hunger > Mate > Idle priority
- **`BehaviorEngine.BuildLimbConfiguration(Genome)`** — produces `LimbConfiguration` struct handed to Unity IK layer at spawn
- **`LocomotionController`** — PD controller data contract for Unity joint-space locomotion:
  `torque = Kp × (targetAngle − currentAngle) − Kd × angularVelocity`
  `LocomotionController.FromMass(float)` scales gains to creature body mass; `ComputeTorque(target, current, ω)` returns clamped torque for a single joint DOF

### Iteration 6 — Universe Hierarchy & Extended ECS Model
- **`PhysicalConstants`** (struct) — `gravityStrength`, `electromagneticStrength`, `entropyRate`, `expansionRate`; `PhysicalConstants.Standard` is our universe
- **`Universe`** — top-level container; holds `PhysicalConstants` + `List<Galaxy>`; `AllPlanets()` / `AllStarSystems()` traverse the full hierarchy
- **`Galaxy`** — `mass`, `age`, `List<SolarSystem>` (StarSystem in GDD terms)
- **`LifeStage`** enum — `Sterile → Microbial → Multicellular → Complex → Sapient`; advanced automatically by `MicroSimulationManager.Tick` and `AbiogenesisEngine`
- **`BodyPlan`** enum — `Radial / Bilateral / Asymmetric / Sessile`; stored in `Genome`, drives prefab skeleton selection
- **`MutationType`** enum — `Scalar / Structural / Material`; classifies every genome mutation for telemetry and tuning
- `SimulationConfig` extended with **LOD distance thresholds** (`NearLodDistance`, `CloseLodDistance`, `MaxPhysicsCreatures`) matching the GDD's 3-level simulation scaling

### Developer Overrides (`SimulationCore/Debug/DeveloperOverrides.cs`)

Static toolkit for forcing specific simulation outcomes during development and QA — bypasses all normal threshold checks:

| Method | What it forces |
|---|---|
| `ForceAbiogenesis(planet, engine, micro, rng)` | Instantly triggers life on any planet regardless of prebiotic score |
| `ForceLifeStage(planet, stage, ...)` | Jumps a planet directly to any `LifeStage`; auto-seeds life if needed |
| `ForceSpeciation(pool, biome, rng)` | Immediately branches a new species from the most populous parent |
| `ForceExtinction(species, pool, biome)` | Drives a species to immediate extinction and fires `ExtinctionEvent` |
| `ForceGenomeTraits(species, g => { … return g; })` | Directly sets any genome field values on a live species |
| `ForcePopulation(species, count)` | Pins a species' population to any value within carrying capacity |
| `ForceMaxHabitability(planet)` | Sets atmospheric density and surface water to Goldilocks-optimal values |
| `ForcePrebioticScore(planet, score)` | Sets prebiotic chemistry score so the next macro tick triggers abiogenesis naturally |

```csharp
// Example: instantly give Rubrum life and jump it to Complex stage
DeveloperOverrides.ForceAbiogenesis(rubrum, macroManager.AbiogenesisEngine, microManager, rng);
DeveloperOverrides.ForceLifeStage(rubrum, LifeStage.Complex);

// Example: pin a test species to specific traits
DeveloperOverrides.ForceGenomeTraits(species, g => {
    g.Speed     = 3.5f;
    g.LimbCount = 6;
    g.BodyPlan  = BodyPlan.Bilateral;
    return g;
});
```

### Cross-cutting
- **`BiomeMutationEngine`** — full rename of `MutationEngine`; `SampleGaussian` is `internal` so `MicroSimulationManager` can reuse it. Legacy `MutationEngine` retained as an `[Obsolete]` thin wrapper; `SimulationLoop` updated to call `BiomeMutationEngine` directly.
- **`TelemetryLogger`** — auto-subscribes to `AbiogenesisEvent`, `SpeciationEvent`, `ExtinctionEvent`; buffers and flushes to a fixed-schema CSV at `TelemetryFlushInterval`
- **`SaveManager`** — JSON snapshot of `SolarSystem` + all `PopulationPool`s (species genomes, populations, lineage IDs)
- **`Program.cs`** — updated to exercise all six iterations as a headless console harness, including full Universe → Galaxy → StarSystem hierarchy demo and Developer Overrides showcase

## Repository Structure (quick map)

- `PlanetaryFormation/Models/` — domain entities (`Universe`, `Galaxy`, `SolarSystem`, `CelestialBody` + planet types, `Biome`, enums).
- `PlanetaryFormation/SimulationCore/` — simulation logic by subsystem:
  - `Time/` state machine + simulation clock
  - `Macro/` habitability + abiogenesis
  - `Micro/` genome/species/population evolution
  - `Mutation/` biome mutation engine and mutation typing
  - `RenderBridge/` data-only contracts used by the Unity-facing render layer
  - `Behavior/` locomotion and creature-state contracts
  - `Game/` player actions, resources, goals, procedural events
  - `IO/` Reddit poll parsing and engagement integration
  - `Telemetry/`, `Persistence/`, `Debug/`, `Events/`, `Config/` cross-cutting infrastructure
- `PlanetaryFormation/Program.cs` — executable demo wiring all systems together.
- `PlanetaryFormation.Tests/` — unit tests for deterministic core logic.

## Key Technologies

- .NET 8 / C#
- Event-driven architecture via a typed in-process `EventBus`
- Data-oriented simulation contracts (struct-heavy micro/behavior models)
- xUnit test suite (`PlanetaryFormation.Tests`) for core logic validation

## Build & Test

From `The-Systems-Architecture-CES-main`:

- Build app: `dotnet build PlanetaryFormation/PlanetaryFormation.csproj -nologo`
- Run tests: `dotnet test PlanetaryFormation.Tests/PlanetaryFormation.Tests.csproj -nologo`

## Current Development Status

Implemented and demonstrable in the current codebase:
- Iterations 1–6 architecture (time framework, macro/micro simulation, render bridge contracts, behavior contracts, universe hierarchy)
- Reddit poll integration + chaos coupling (`SimulationConfig`, `RedditVoteParser`)
- Developer overrides, telemetry logging, save/load support, and scenario/game-loop scaffolding

Still remaining for production readiness:
- Unity runtime integration for render/physics contracts (currently data contracts + console harness)
- End-to-end integration/performance/load testing at scale
- CI quality gates and release-hardening workflows (coverage gates, regression suites, packaging/deployment)
