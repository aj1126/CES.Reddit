namespace PlanetaryFormation.SimulationCore.Micro;

/// <summary>
/// Reference type that owns a <see cref="Genome"/> and tracks a species'
/// population and evolutionary lineage.
/// Forms nodes in the phylogenetic tree via <see cref="ParentSpeciesId"/>
/// and <see cref="Branches"/>.
/// Iteration 3: Micro-Simulation.
/// </summary>
public class SpeciesData
{
    /// <summary>Unique identifier for this species.</summary>
    public Guid SpeciesId { get; } = Guid.NewGuid();

    /// <summary>Identifier of the ancestral species; null for primordial life.</summary>
    public Guid? ParentSpeciesId { get; init; }

    /// <summary>The underlying genetic profile of this species.</summary>
    public Genome BaseGenome { get; set; }

    /// <summary>Current live individual count for this species.</summary>
    public long Population { get; set; }

    /// <summary>
    /// Fitness score (0–1) computed during the last environmental pressure evaluation.
    /// </summary>
    public double FitnessScore { get; set; }

    /// <summary>How many reproduction cycles this species has survived.</summary>
    public int GenerationIndex { get; set; }

    /// <summary>Child species that diverged from this one via speciation events.</summary>
    public List<SpeciesData> Branches { get; } = new();

    /// <summary>Returns true when the species has no remaining individuals.</summary>
    public bool IsExtinct => Population <= 0;

    public SpeciesData(Genome genome, long initialPopulation, Guid? parentSpeciesId = null)
    {
        BaseGenome      = genome;
        Population      = initialPopulation;
        ParentSpeciesId = parentSpeciesId;
    }
}
