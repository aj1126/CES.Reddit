namespace PlanetaryFormation.SimulationCore.Micro;

/// <summary>
/// Lightweight value-type data profile for a species. All fields are value types
/// so arrays of Genomes are cache-friendly and can be trivially ported to
/// Unity ECS NativeArrays.
/// Iteration 3: Micro-Simulation.
/// Extended in Iteration 6 to include the full GenomeComponent fields from the GDD:
/// bodyPlan, boneScales, colorHSV, neuralComplexity, speed, and aggression.
/// </summary>
public struct Genome
{
    // ── Morphology ────────────────────────────────────────────────────────────

    /// <summary>
    /// Primary morphological symmetry plan (Radial / Bilateral / Asymmetric / Sessile).
    /// Drives which prefab skeleton is selected in the Unity render layer.
    /// </summary>
    public BodyPlan BodyPlan;

    /// <summary>Number of locomotion limbs (0 = limbless).</summary>
    public int LimbCount;

    /// <summary>
    /// Average limb length relative to body length
    /// (0.1 = stubby, 2.0 = stilt-like).
    /// </summary>
    public float LimbLengthAvg;

    /// <summary>Bone scale along the X (lateral) axis relative to base prefab.</summary>
    public float BoneScaleX;

    /// <summary>Bone scale along the Y (vertical) axis relative to base prefab.</summary>
    public float BoneScaleY;

    /// <summary>Bone scale along the Z (depth) axis relative to base prefab.</summary>
    public float BoneScaleZ;

    /// <summary>Bit-mask of sensory organs the creature possesses.</summary>
    public SensoryOrganFlags SensoryOrgans;

    // ── Visual material ───────────────────────────────────────────────────────

    /// <summary>Hue channel of the creature's base color (0.0–1.0).</summary>
    public float ColorH;

    /// <summary>Saturation channel of the creature's base color (0.0–1.0).</summary>
    public float ColorS;

    /// <summary>Value (brightness) channel of the creature's base color (0.0–1.0).</summary>
    public float ColorV;

    // ── Neural traits ─────────────────────────────────────────────────────────

    /// <summary>
    /// Neural complexity index (0.0 = reflexive/simple, 1.0 = highly adaptive).
    /// Higher values improve threat recognition and mate-selection accuracy in
    /// <see cref="PlanetaryFormation.SimulationCore.Behavior.BehaviorEngine"/>.
    /// </summary>
    public float NeuralComplexity;

    // ── Locomotion ────────────────────────────────────────────────────────────

    /// <summary>
    /// Maximum movement speed in body-lengths per second.
    /// Feeds into the speedAdvantage component of the fitness function.
    /// </summary>
    public float Speed;

    // ── Behaviour seed ────────────────────────────────────────────────────────

    /// <summary>
    /// Tendency toward inter-species or intra-species conflict (0.0–1.0).
    /// High aggression increases predation success but also energy expenditure.
    /// </summary>
    public float Aggression;

    /// <summary>Primary nutritional strategy of this species.</summary>
    public DietType DietType;

    // ── Thermal biology ───────────────────────────────────────────────────────

    /// <summary>Optimal ambient temperature in Kelvin.</summary>
    public float IdealTempK;

    /// <summary>±Kelvin the creature can tolerate before fitness is penalised.</summary>
    public float TempToleranceK;

    // ── Size &amp; metabolism ──────────────────────────────────────────────────────

    /// <summary>Body mass in Earth-relative units (1.0 ≈ 70 kg analogue).</summary>
    public float Mass;

    /// <summary>Energy consumed per tick. Higher values require more food.</summary>
    public float MetabolismRate;

    // ── Reproduction ──────────────────────────────────────────────────────────

    /// <summary>Offspring produced per tick at ideal conditions (0.001–0.10).</summary>
    public float ReproductionRate;

    /// <summary>
    /// How volatile this genome is; higher values breed rapid diversification
    /// and speciation events.
    /// </summary>
    public float MutationVolatility;

    // ── Crossover ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Produces an offspring genome by blending each parent field with a random
    /// linear-interpolation weight (single alpha per crossover operation).
    /// Discrete fields (BodyPlan, LimbCount, SensoryOrgans, DietType) are
    /// inherited from one parent chosen at random.
    /// </summary>
    public readonly Genome Crossover(Genome other, Random rng)
    {
        float alpha = (float)rng.NextDouble();

        return new Genome
        {
            BodyPlan          = rng.NextDouble() < 0.5 ? BodyPlan          : other.BodyPlan,
            LimbCount         = rng.NextDouble() < 0.5 ? LimbCount         : other.LimbCount,
            LimbLengthAvg     = LimbLengthAvg     * alpha + other.LimbLengthAvg     * (1f - alpha),
            BoneScaleX        = BoneScaleX        * alpha + other.BoneScaleX        * (1f - alpha),
            BoneScaleY        = BoneScaleY        * alpha + other.BoneScaleY        * (1f - alpha),
            BoneScaleZ        = BoneScaleZ        * alpha + other.BoneScaleZ        * (1f - alpha),
            SensoryOrgans     = rng.NextDouble() < 0.5 ? SensoryOrgans     : other.SensoryOrgans,
            ColorH            = ColorH            * alpha + other.ColorH            * (1f - alpha),
            ColorS            = ColorS            * alpha + other.ColorS            * (1f - alpha),
            ColorV            = ColorV            * alpha + other.ColorV            * (1f - alpha),
            NeuralComplexity  = NeuralComplexity  * alpha + other.NeuralComplexity  * (1f - alpha),
            Speed             = Speed             * alpha + other.Speed             * (1f - alpha),
            Aggression        = Aggression        * alpha + other.Aggression        * (1f - alpha),
            DietType          = rng.NextDouble() < 0.5 ? DietType          : other.DietType,
            IdealTempK        = IdealTempK        * alpha + other.IdealTempK        * (1f - alpha),
            TempToleranceK    = TempToleranceK    * alpha + other.TempToleranceK    * (1f - alpha),
            Mass              = Mass              * alpha + other.Mass              * (1f - alpha),
            MetabolismRate    = MetabolismRate    * alpha + other.MetabolismRate    * (1f - alpha),
            ReproductionRate  = ReproductionRate  * alpha + other.ReproductionRate  * (1f - alpha),
            MutationVolatility= MutationVolatility* alpha + other.MutationVolatility* (1f - alpha),
        };
    }
}
