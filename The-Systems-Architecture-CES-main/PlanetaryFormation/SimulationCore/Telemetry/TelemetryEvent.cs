using System.Globalization;

namespace PlanetaryFormation.SimulationCore.Telemetry;

/// <summary>
/// Immutable record of a single telemetry measurement.
/// CSV schema (fixed): Timestamp, SystemName, EventType, Key, Value.
/// Pillar V: Scientific Telemetry over Assumption.
/// </summary>
public record TelemetryEvent(
    DateTime Timestamp,
    string   SystemName,
    string   EventType,
    string   Key,
    string   Value)
{
    /// <summary>Factory helper for numeric values.</summary>
    public static TelemetryEvent Create(string system, string eventType, string key, double value)
        => new(DateTime.UtcNow, system, eventType, key,
               value.ToString("G", CultureInfo.InvariantCulture));

    /// <summary>Factory helper for string values.</summary>
    public static TelemetryEvent Create(string system, string eventType, string key, string value)
        => new(DateTime.UtcNow, system, eventType, key, value);

    /// <summary>Renders the event as a single CSV line (no trailing newline).</summary>
    public string ToCsvLine()
        => $"{Timestamp:o},{Escape(SystemName)},{Escape(EventType)},{Escape(Key)},{Escape(Value)}";

    /// <summary>
    /// Wraps a CSV field in double-quotes and escapes internal double-quotes when
    /// the field contains commas, double-quotes, or newline characters.
    /// </summary>
    private static string Escape(string field)
    {
        if (field.Contains(',') || field.Contains('"') ||
            field.Contains('\r') || field.Contains('\n'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }
}
