using PlanetaryFormation.SimulationCore.Micro;

namespace PlanetaryFormation.SimulationCore.RenderBridge;

/// <summary>
/// Converts a <see cref="SpeciesData"/> record into a <see cref="CreatureBlueprint"/>
/// the Unity spawner can act on. All translation from simulation-space values to
/// render-space values is centralised here.
/// Iteration 4: The Render Layer.
/// </summary>
public static class CreatureAssembler
{
    /// <summary>
    /// Derives a spawn blueprint from a species' genome.
    /// All translation from simulation-space values to render-space values
    /// is centralised here (Creature Generation pipeline: Genome → Blueprint).
    /// </summary>
    public static CreatureBlueprint BuildBlueprint(SpeciesData species)
    {
        Genome g = species.BaseGenome;

        return new CreatureBlueprint
        {
            SpeciesId     = species.SpeciesId,
            BodyPlan      = g.BodyPlan,
            LimbCount     = g.LimbCount,
            LimbLengthAvg = g.LimbLengthAvg,
            BoneScaleX    = g.BoneScaleX,
            BoneScaleY    = g.BoneScaleY,
            BoneScaleZ    = g.BoneScaleZ,
            ColorH        = g.ColorH,
            ColorS        = g.ColorS,
            ColorV        = g.ColorV,
            SensoryOrgans = g.SensoryOrgans,
            // Mass → body scale: 1.0 mass = base scale (1.0); scales logarithmically
            BodyScale     = (float)(0.3 + Math.Log10(1.0 + g.Mass) * 0.7),
            DietType      = g.DietType,
        };
    }
}
