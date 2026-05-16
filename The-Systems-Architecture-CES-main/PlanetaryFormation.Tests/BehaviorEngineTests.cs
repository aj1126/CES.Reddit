using PlanetaryFormation.SimulationCore.Behavior;
using PlanetaryFormation.SimulationCore.Micro;

namespace PlanetaryFormation.Tests;

public class BehaviorEngineTests
{
    [Fact]
    public void UpdateBrain_ThreatNearby_PrioritizesFleeing()
    {
        var brain = new CreatureBrainData
        {
            Hunger = 0.95f,
            Fear = 0.30f,
            ReproductiveDrive = 1.0f,
            Diet = DietType.Herbivore
        };
        var env = new EnvironmentSnapshot { ThreatNearby = true, MateAvailable = true };

        var state = BehaviorEngine.UpdateBrain(ref brain, in env);

        Assert.Equal(CreatureState.Fleeing, state);
        Assert.Equal(CreatureState.Fleeing, brain.CurrentState);
    }

    [Fact]
    public void UpdateBrain_HungryHerbivore_Forages()
    {
        var brain = new CreatureBrainData { Hunger = 0.60f, Fear = 0f, Diet = DietType.Herbivore };
        var env = new EnvironmentSnapshot { ThreatNearby = false };

        var state = BehaviorEngine.UpdateBrain(ref brain, in env);

        Assert.Equal(CreatureState.Foraging, state);
    }

    [Fact]
    public void UpdateBrain_HungryCarnivore_Hunts()
    {
        var brain = new CreatureBrainData { Hunger = 0.60f, Fear = 0f, Diet = DietType.Carnivore };
        var env = new EnvironmentSnapshot { ThreatNearby = false };

        var state = BehaviorEngine.UpdateBrain(ref brain, in env);

        Assert.Equal(CreatureState.Hunting, state);
    }

    [Fact]
    public void UpdateBrain_ReproductiveDriveWithMate_Mates()
    {
        var brain = new CreatureBrainData
        {
            Hunger = 0.1f,
            Fear = 0.1f,
            ReproductiveDrive = 0.9f,
            Diet = DietType.Omnivore
        };
        var env = new EnvironmentSnapshot { ThreatNearby = false, MateAvailable = true };

        var state = BehaviorEngine.UpdateBrain(ref brain, in env);

        Assert.Equal(CreatureState.Mating, state);
    }

    [Fact]
    public void UpdateBrain_NoDrivers_RemainsIdleAndFearDecays()
    {
        var brain = new CreatureBrainData
        {
            Hunger = 0f,
            Fear = 0.02f,
            ReproductiveDrive = 0f,
            Diet = DietType.Herbivore
        };
        var env = new EnvironmentSnapshot { ThreatNearby = false, MateAvailable = false };

        var state = BehaviorEngine.UpdateBrain(ref brain, in env);

        Assert.Equal(CreatureState.Idle, state);
        Assert.Equal(0f, brain.Fear);
        Assert.Equal(0.01f, brain.Hunger, 3);
    }

    [Fact]
    public void BuildLimbConfiguration_ReturnsCorrectLimbCountAndLengths()
    {
        var genome = new Genome { LimbCount = 4, LimbLengthAvg = 1.75f };

        var cfg = BehaviorEngine.BuildLimbConfiguration(genome);

        Assert.Equal(4, cfg.LimbCount);
        Assert.Equal(4, cfg.LimbLengths.Length);
        Assert.All(cfg.LimbLengths, length => Assert.Equal(1.75f, length, 3));
    }
}
