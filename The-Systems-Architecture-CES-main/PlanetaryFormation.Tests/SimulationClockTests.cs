using PlanetaryFormation.SimulationCore.Config;
using PlanetaryFormation.SimulationCore.Events;
using PlanetaryFormation.SimulationCore.Time;

namespace PlanetaryFormation.Tests;

public class SimulationClockTests : IDisposable
{
    public SimulationClockTests()
    {
        EventBus.Clear();
        SimulationClock.Reset();
    }

    public void Dispose()
    {
        EventBus.Clear();
        SimulationClock.Reset();
    }

    [Fact]
    public void Tick_WhenPaused_DoesNotAdvanceTime()
    {
        SimulationClock.Tick(1f);

        Assert.Equal(0d, SimulationClock.SimulatedYears);
    }

    [Fact]
    public void SetMacroScale_ThenTick_AdvancesAndPublishesTick()
    {
        TickEvent? received = null;
        EventBus.Subscribe<TickEvent>(e => received = e);
        SimulationClock.SetMacroScale(10f);

        SimulationClock.Tick(2f);

        Assert.Equal(TimeMode.MacroScale, SimulationClock.Mode);
        Assert.Equal(20d, SimulationClock.SimulatedYears);
        Assert.NotNull(received);
        Assert.Equal(20d, received!.SimulatedYears);
        Assert.Equal(2f, received.DeltaTime);
    }

    [Fact]
    public void Initialize_SetsDefaultMicroScaleFromDaysPerSecond()
    {
        var config = new SimulationConfig { MicroDaysPerSecond = 2.0 };
        SimulationClock.Initialize(config);
        SimulationClock.SetMicroScale();

        Assert.Equal(TimeMode.MicroScale, SimulationClock.Mode);
        Assert.Equal((float)(2.0 / 365.0), SimulationClock.TimeScale, 5);
    }

    [Fact]
    public void Pause_SetsPausedModeAndZeroScaleInEvent()
    {
        TimeScaleChangedEvent? evt = null;
        EventBus.Subscribe<TimeScaleChangedEvent>(e => evt = e);
        SimulationClock.SetMacroScale(1f);

        SimulationClock.Pause();

        Assert.Equal(TimeMode.Paused, SimulationClock.Mode);
        Assert.NotNull(evt);
        Assert.Equal(0f, evt!.NewTimeScale);
    }
}
