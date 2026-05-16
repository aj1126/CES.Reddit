using PlanetaryFormation.SimulationCore.Micro;

namespace PlanetaryFormation.SimulationCore.RenderBridge;

/// <summary>
/// Plain C# struct that carries everything the Unity spawner needs to
/// assemble a creature from modular prefab parts.
/// Iteration 4: The Render Layer.
/// Extended in Iteration 6 with bodyPlan, bone-scale, and colorHSV fields
/// from the GDD Creature Generation pipeline (Genome → Skeleton → Mesh → GameObject).
/// </summary>
public struct CreatureBlueprint
{
    /// <summary>Unique species identifier — links the visual to its data record.</summary>
    public Guid SpeciesId;

    /// <summary>
    /// Morphological body plan driving which prefab skeleton tree to load.
    /// Bilateral skeletons use paired limbs; Radial use radial symmetry rigs.
    /// </summary>
    public BodyPlan BodyPlan;

    /// <summary>Number of limbs to attach.</summary>
    public int LimbCount;

    /// <summary>Average limb scale multiplier relative to the body prefab.</summary>
    public float LimbLengthAvg;

    // ── Bone scales ───────────────────────────────────────────────────────────

    /// <summary>Lateral (X) bone scale applied uniformly along the skeleton.</summary>
    public float BoneScaleX;

    /// <summary>Vertical (Y) bone scale applied uniformly along the skeleton.</summary>
    public float BoneScaleY;

    /// <summary>Depth (Z) bone scale applied uniformly along the skeleton.</summary>
    public float BoneScaleZ;

    // ── Visual material ───────────────────────────────────────────────────────

    /// <summary>Hue channel of the creature's base color (0–1).</summary>
    public float ColorH;

    /// <summary>Saturation channel of the creature's base color (0–1).</summary>
    public float ColorS;

    /// <summary>Value (brightness) channel of the creature's base color (0–1).</summary>
    public float ColorV;

    // ── Other ─────────────────────────────────────────────────────────────────

    /// <summary>Bitmask determining which sensory-organ prefabs to attach.</summary>
    public SensoryOrganFlags SensoryOrgans;

    /// <summary>Uniform body scale derived from mass (larger mass → larger creature).</summary>
    public float BodyScale;

    /// <summary>Nutritional strategy; drives spawn-time AI initialisation.</summary>
    public DietType DietType;
}
