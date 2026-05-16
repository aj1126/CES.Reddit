using PlanetaryFormation.SimulationCore.Events;

namespace PlanetaryFormation.Tests;

public class EventBusTests : IDisposable
{
    public EventBusTests() => EventBus.Clear();
    public void Dispose() => EventBus.Clear();

    [Fact]
    public void Publish_InvokesSubscribedHandler()
    {
        int received = 0;
        EventBus.Subscribe<int>(value => received = value);

        EventBus.Publish(7);

        Assert.Equal(7, received);
    }

    [Fact]
    public void Unsubscribe_StopsFutureEvents()
    {
        int count = 0;
        Action<int> handler = _ => count++;
        EventBus.Subscribe(handler);
        EventBus.Unsubscribe(handler);

        EventBus.Publish(1);

        Assert.Equal(0, count);
    }

    [Fact]
    public void Clear_RemovesAllHandlers()
    {
        int count = 0;
        EventBus.Subscribe<int>(_ => count++);
        EventBus.Clear();

        EventBus.Publish(1);

        Assert.Equal(0, count);
    }

    [Fact]
    public void Publish_AllowsHandlerToUnsubscribeDuringDispatch()
    {
        int selfCount = 0;
        int otherCount = 0;

        Action<int>? self = null;
        self = _ =>
        {
            selfCount++;
            EventBus.Unsubscribe(self!);
        };

        EventBus.Subscribe(self);
        EventBus.Subscribe<int>(_ => otherCount++);

        EventBus.Publish(1);
        EventBus.Publish(2);

        Assert.Equal(1, selfCount);
        Assert.Equal(2, otherCount);
    }
}
