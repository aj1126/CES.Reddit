using PlanetaryFormation.SimulationCore.Config;
using PlanetaryFormation.SimulationCore.Micro;
using PlanetaryFormation.SimulationCore.RenderBridge;

namespace PlanetaryFormation.Tests;

public class SpawnBudgetTests
{
    [Fact]
    public void SelectSample_RespectsMaxActiveBudgetAndSortOrder()
    {
        var config = new SimulationConfig { MaxActiveCreatures = 5 };
        var budget = new SpawnBudget(config);
        var pool = new PopulationPool();
        pool.AddSpecies(new SpeciesData(new Genome(), 90));
        pool.AddSpecies(new SpeciesData(new Genome(), 9));
        pool.AddSpecies(new SpeciesData(new Genome(), 1));

        var sample = budget.SelectSample(pool);

        Assert.True(sample.Sum(x => x.SpawnCount) <= 5);
        Assert.Equal(sample.OrderByDescending(x => x.Species.Population).Select(x => x.Species), sample.Select(x => x.Species));
        Assert.All(sample, x => Assert.True(x.SpawnCount >= 1));
    }

    [Fact]
    public void SelectSample_StopsWhenBudgetExhausted()
    {
        var config = new SimulationConfig { MaxActiveCreatures = 2 };
        var budget = new SpawnBudget(config);
        var pool = new PopulationPool();
        pool.AddSpecies(new SpeciesData(new Genome(), 100));
        pool.AddSpecies(new SpeciesData(new Genome(), 50));
        pool.AddSpecies(new SpeciesData(new Genome(), 25));

        var sample = budget.SelectSample(pool);

        Assert.True(sample.Count <= 2);
        Assert.Equal(2, sample.Sum(x => x.SpawnCount));
    }

    [Fact]
    public void SelectSample_EmptyPool_ReturnsEmptySelection()
    {
        var budget = new SpawnBudget(new SimulationConfig { MaxActiveCreatures = 10 });
        var pool = new PopulationPool();

        var sample = budget.SelectSample(pool);

        Assert.Empty(sample);
    }
}
