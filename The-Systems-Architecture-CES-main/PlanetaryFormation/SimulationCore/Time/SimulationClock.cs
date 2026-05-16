using PlanetaryFormation.SimulationCore.Config;
using PlanetaryFormation.SimulationCore.Events;

namespace PlanetaryFormation.SimulationCore.Time;

/// <summary>Published whenever the simulation clock advances one tick.</summary>
public record TickEvent(double SimulatedYears, float DeltaTime);

/// <summary>Published when the time-scale mode switches.</summary>
public record TimeScaleChangedEvent(TimeMode OldMode, TimeMode NewMode, float NewTimeScale);

/// <summary>
/// Singleton simulation clock. Advances in-simulation time and fires tick events
/// so all downstream systems stay in sync without polling.
/// Default scale values are sourced from <see cref="SimulationConfig"/> via
/// <see cref="Initialize"/>; they can also be overridden per-call in
/// <see cref="SetMacroScale"/> / <see cref="SetMicroScale"/>.
/// Iteration 1: The Catalyst &amp; Core Time Framework.
/// </summary>
public static class SimulationClock
{
    /// <summary>Total simulated time that has elapsed (in years).</summary>
    public static double SimulatedYears { get; private set; }

    /// <summary>Current time multiplier (simulated years per real second).</summary>
    public static float TimeScale { get; private set; }

    /// <summary>Current operating mode of the clock.</summary>
    public static TimeMode Mode { get; private set; } = TimeMode.Paused;

    // Default multipliers — overridden by Initialize() or per Set*Scale() call.
    private static float _macroScale = 1_000_000f;  // 1 M simulated years / real second
    private static float _microScale = 1f / 365f;   // 1 simulated day / real second

    /// <summary>
    /// Sources the default macro and micro scales from <paramref name="config"/>.
    /// Call once at application start (before <see cref="SetMacroScale"/> /
    /// <see cref="SetMicroScale"/>) so all time-scale tuning lives in one place.
    /// </summary>
    public static void Initialize(SimulationConfig config)
    {
        _macroScale = (float)config.MacroYearsPerSecond;
        // Config stores micro scale as "days per second"; convert to years/sec.
        _microScale = (float)(config.MicroDaysPerSecond / 365.0);
    }

    /// <summary>
    /// Advances the clock by <paramref name="deltaTime"/> real-time seconds,
    /// publishing a <see cref="TickEvent"/> if the clock is not paused.
    /// </summary>
    public static void Tick(float deltaTime)
    {
        if (Mode == TimeMode.Paused) return;

        SimulatedYears += deltaTime * TimeScale;
        EventBus.Publish(new TickEvent(SimulatedYears, deltaTime));
    }

    /// <summary>
    /// Switch to macro-scale (cosmic) time.
    /// Fires <see cref="TimeScaleChangedEvent"/>.
    /// </summary>
    public static void SetMacroScale(float? overrideScale = null)
    {
        var old = Mode;
        _macroScale = overrideScale ?? _macroScale;
        TimeScale = _macroScale;
        Mode = TimeMode.MacroScale;
        EventBus.Publish(new TimeScaleChangedEvent(old, Mode, TimeScale));
    }

    /// <summary>
    /// Switch to micro-scale (biological) time.
    /// Fires <see cref="TimeScaleChangedEvent"/>.
    /// </summary>
    public static void SetMicroScale(float? overrideScale = null)
    {
        var old = Mode;
        _microScale = overrideScale ?? _microScale;
        TimeScale = _microScale;
        Mode = TimeMode.MicroScale;
        EventBus.Publish(new TimeScaleChangedEvent(old, Mode, TimeScale));
    }

    /// <summary>Pause the clock.</summary>
    public static void Pause()
    {
        var old = Mode;
        Mode = TimeMode.Paused;
        EventBus.Publish(new TimeScaleChangedEvent(old, Mode, 0f));
    }

    /// <summary>Resets the clock to zero. Used when triggering a new Catalyst.</summary>
    public static void Reset()
    {
        SimulatedYears = 0;
        TimeScale = 0;
        Mode = TimeMode.Paused;
    }
}
