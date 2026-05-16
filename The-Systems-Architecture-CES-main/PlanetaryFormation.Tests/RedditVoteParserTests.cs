using PlanetaryFormation.Models;
using PlanetaryFormation.SimulationCore.Config;
using PlanetaryFormation.SimulationCore.Events;
using PlanetaryFormation.SimulationCore.IO;
using PlanetaryFormation.SimulationCore.Micro;

namespace PlanetaryFormation.Tests;

public class RedditVoteParserTests : IDisposable
{
    public RedditVoteParserTests() => EventBus.Clear();
    public void Dispose() => EventBus.Clear();

    [Fact]
    public void Parse_AggregatesKnownOptions_AndIgnoresUnknownOrNegativeVotes()
    {
        var parser = new RedditVoteParser();
        var input = new[]
        {
            new RedditPollResult(RedditVoteParser.BombardWithCosmicRaysOption, 2),
            new RedditPollResult(RedditVoteParser.SteerCometSwarmOption, 1),
            new RedditPollResult(RedditVoteParser.AggressivePredationDriveOption, 1),
            new RedditPollResult("Unknown", 100),
            new RedditPollResult(RedditVoteParser.TriggerVolcanicActivityOption, -5)
        };

        var payload = parser.Parse(input);

        Assert.Equal(0.16f, payload.MutationVolatilityDelta, 3);
        Assert.Equal(0.08, payload.SurfaceWaterPercentDelta, 3);
        Assert.Equal(0.20, payload.PrebioticChemistryScoreDelta, 3);
        Assert.True(payload.ForceAlphaCarnivore);
        Assert.Equal(0.0, payload.GlobalTemperatureModifierDelta);
    }

    [Fact]
    public void ApplyToPlanet_ClampsAndAppliesPlanetLevelChanges()
    {
        var parser = new RedditVoteParser();
        var planet = new TerrestrialPlanet("Gaia", 1, 1, 0.02, 4.5, 0.5, true);
        var initialTemp = planet.Biomes[0].Temperature;
        var payload = new RedditVotePayload
        {
            GlobalTemperatureModifierDelta = 5.0,
            SurfaceWaterPercentDelta = 0.7,
            PrebioticChemistryScoreDelta = 0.9,
            OrbitalRadiusDeltaAu = -1.0
        };

        parser.ApplyToPlanet(planet, payload);

        Assert.Equal(1.0, planet.SurfaceWaterPercent, 3);
        Assert.Equal(0.9, planet.PrebioticChemistryScore, 3);
        Assert.Equal(0.01, planet.OrbitalRadiusAU, 3);
        Assert.Equal(5.0, parser.GetGlobalTemperatureModifier(planet), 3);
        Assert.Equal(initialTemp + 5.0, planet.Biomes[0].Temperature, 3);
    }

    [Fact]
    public void ApplyToAlphaSpecies_ClampsVolatilityAndForcesCarnivore()
    {
        var parser = new RedditVoteParser();
        var species = new SpeciesData(new Genome
        {
            DietType = DietType.Herbivore,
            MutationVolatility = 0.95f
        }, initialPopulation: 100);
        var payload = new RedditVotePayload
        {
            MutationVolatilityDelta = 0.4f,
            ForceAlphaCarnivore = true
        };

        parser.ApplyToAlphaSpecies(species, payload);

        Assert.Equal(1.0f, species.BaseGenome.MutationVolatility);
        Assert.Equal(DietType.Carnivore, species.BaseGenome.DietType);
    }

    [Fact]
    public async Task ParseAndApplyAsync_PublishesPollAppliedEventWithWinningOption()
    {
        var parser = new RedditVoteParser();
        var config = new SimulationConfig();
        PollAppliedEvent? published = null;
        EventBus.Subscribe<PollAppliedEvent>(e => published = e);

        var (payload, _) = await parser.ParseAndApplyAsync(
            new[]
            {
                new RedditPollResult(RedditVoteParser.TriggerVolcanicActivityOption, 3),
                new RedditPollResult(RedditVoteParser.BombardWithCosmicRaysOption, 1),
            },
            upvotes: 10,
            comments: 4,
            config: config);

        Assert.NotNull(published);
        Assert.Equal(RedditVoteParser.TriggerVolcanicActivityOption, published!.WinningOption);
        Assert.Equal(payload.MutationVolatilityDelta, (float)config.MutationVolatilityModifier, 3);
    }
}
