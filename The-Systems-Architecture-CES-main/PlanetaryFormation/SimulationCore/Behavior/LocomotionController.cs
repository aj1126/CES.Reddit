namespace PlanetaryFormation.SimulationCore.Behavior;

/// <summary>
/// Data contract handed to the Unity physics layer for each spawned creature.
/// Carries PD (proportional-derivative) controller gains used to drive joint-space
/// locomotion via ConfigurableJoints.
///
/// Torque formula per joint (applied each FixedUpdate):
///   torque = Kp × (targetAngle − currentAngle) − Kd × angularVelocity
///
/// Unity wiring:
///   • Attach a ConfigurableJoint per limb segment.
///   • Each FixedUpdate call <see cref="ComputeTorque"/> with current sensor readings.
///   • Clamp result to <see cref="MaxTorque"/> before applying via Rigidbody.AddTorque.
///   • Use <see cref="UprightTorqueMagnitude"/> separately to maintain upright stance.
///
/// Maps to the Physics Creatures / Locomotion section of the GDD.
/// </summary>
public struct LocomotionController
{
    /// <summary>Proportional gain — acts as a spring stiffness coefficient.</summary>
    public float Kp;

    /// <summary>Derivative gain — acts as a damping coefficient to reduce oscillation.</summary>
    public float Kd;

    /// <summary>Maximum torque magnitude (N·m analogue) clamped per physics step.</summary>
    public float MaxTorque;

    /// <summary>
    /// Target upright rotation angle in degrees (0 = fully upright).
    /// Applied as a secondary torque to stabilise the creature's vertical axis.
    /// </summary>
    public float UprightTargetDeg;

    /// <summary>Torque magnitude applied to restore upright stance each tick.</summary>
    public float UprightTorqueMagnitude;

    // ── Core formula ──────────────────────────────────────────────────────────

    /// <summary>
    /// Computes the corrective joint torque for a single degree of freedom
    /// using the PD control law:
    ///   torque = Kp × (targetAngle − currentAngle) − Kd × angularVelocity
    /// The result is clamped to [−MaxTorque, +MaxTorque].
    /// </summary>
    /// <param name="targetAngle">Desired joint angle in degrees.</param>
    /// <param name="currentAngle">Measured joint angle in degrees.</param>
    /// <param name="angularVelocity">Measured angular velocity in degrees per second.</param>
    /// <returns>Torque to apply this physics step.</returns>
    public float ComputeTorque(float targetAngle, float currentAngle, float angularVelocity)
    {
        float torque = Kp * (targetAngle - currentAngle) - Kd * angularVelocity;
        return Math.Clamp(torque, -MaxTorque, MaxTorque);
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a <see cref="LocomotionController"/> scaled to a creature's body mass.
    /// Heavier creatures need stronger springs and more damping.
    /// </summary>
    /// <param name="mass">Body mass in Earth-relative units (Genome.Mass).</param>
    public static LocomotionController FromMass(float mass)
    {
        float scale = Math.Max(0.5f, mass);
        return new LocomotionController
        {
            Kp                     = 80f  * scale,
            Kd                     = 10f  * scale,
            MaxTorque              = 200f * scale,
            UprightTargetDeg       = 0f,
            UprightTorqueMagnitude = 50f  * scale,
        };
    }
}
