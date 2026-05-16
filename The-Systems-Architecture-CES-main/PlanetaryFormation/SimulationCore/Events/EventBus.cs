namespace PlanetaryFormation.SimulationCore.Events;

/// <summary>
/// A lightweight, typed publish/subscribe event bus.
/// Decouples simulation systems and the Unity render layer — subscribers hold
/// no direct references across the simulation/render boundary.
/// </summary>
public static class EventBus
{
    private static readonly Dictionary<Type, List<Delegate>> _handlers = new();

    /// <summary>Subscribe <paramref name="handler"/> to events of type <typeparamref name="T"/>.</summary>
    public static void Subscribe<T>(Action<T> handler)
    {
        var type = typeof(T);
        if (!_handlers.TryGetValue(type, out var list))
        {
            list = new List<Delegate>();
            _handlers[type] = list;
        }
        list.Add(handler);
    }

    /// <summary>Unsubscribe a previously registered handler.</summary>
    public static void Unsubscribe<T>(Action<T> handler)
    {
        if (_handlers.TryGetValue(typeof(T), out var list))
            list.Remove(handler);
    }

    /// <summary>Publish an event to all subscribers of type <typeparamref name="T"/>.</summary>
    public static void Publish<T>(T eventData)
    {
        if (!_handlers.TryGetValue(typeof(T), out var list))
            return;

        // Snapshot the list to tolerate handlers that unsubscribe during dispatch.
        foreach (var handler in list.ToArray())
            ((Action<T>)handler)(eventData);
    }

    /// <summary>Removes all subscriptions. Useful for tests and scene reloads.</summary>
    public static void Clear() => _handlers.Clear();
}
