using PlanetaryFormation.SimulationCore.Config;
using PlanetaryFormation.SimulationCore.Events;
using PlanetaryFormation.SimulationCore.Macro;
using PlanetaryFormation.SimulationCore.Micro;

namespace PlanetaryFormation.SimulationCore.Telemetry;

/// <summary>
/// Buffers <see cref="TelemetryEvent"/> records and flushes them to a CSV file
/// at configurable intervals. Auto-subscribes to simulation events so that
/// abiogenesis, speciation, and extinction are recorded without manual wiring.
/// Pillar V: Scientific Telemetry over Assumption.
/// </summary>
public class TelemetryLogger : IDisposable
{
    private readonly SimulationConfig _config;
    private readonly List<TelemetryEvent> _buffer = new();
    private readonly string _outputPath;
    private int  _ticksSinceFlush;
    private bool _headerWritten;
    private bool _disposed;

    private const string CsvHeader = "Timestamp,SystemName,EventType,Key,Value";

    public TelemetryLogger(SimulationConfig? config = null)
    {
        _config = config ?? SimulationConfig.Instance;
        Directory.CreateDirectory(_config.TelemetryOutputDirectory);

        string fileName = $"telemetry_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        _outputPath = Path.Combine(_config.TelemetryOutputDirectory, fileName);

        EventBus.Subscribe<AbiogenesisEvent>(OnAbiogenesis);
        EventBus.Subscribe<SpeciationEvent>(OnSpeciation);
        EventBus.Subscribe<ExtinctionEvent>(OnExtinction);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Buffer a single event; will be flushed to disk on the next interval.</summary>
    public void Log(TelemetryEvent evt)
    {
        _buffer.Add(evt);
        _ticksSinceFlush++;

        if (_ticksSinceFlush >= _config.TelemetryFlushInterval)
            Flush();
    }

    /// <summary>Flush all buffered events to disk immediately.</summary>
    public void Flush()
    {
        if (_buffer.Count == 0) return;

        using var writer = new StreamWriter(_outputPath, append: true);

        if (!_headerWritten)
        {
            writer.WriteLine(CsvHeader);
            _headerWritten = true;
        }

        foreach (var evt in _buffer)
            writer.WriteLine(evt.ToCsvLine());

        _buffer.Clear();
        _ticksSinceFlush = 0;
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void OnAbiogenesis(AbiogenesisEvent e) =>
        Log(TelemetryEvent.Create("AbiogenesisEngine", "Abiogenesis",
            "planet", e.Planet.Name));

    private void OnSpeciation(SpeciationEvent e) =>
        Log(TelemetryEvent.Create("MicroSimulation", "Speciation",
            "parentSpeciesId", e.Parent.SpeciesId.ToString()));

    private void OnExtinction(ExtinctionEvent e) =>
        Log(TelemetryEvent.Create("MicroSimulation", "Extinction",
            "speciesId", e.Species.SpeciesId.ToString()));

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Flush();

        EventBus.Unsubscribe<AbiogenesisEvent>(OnAbiogenesis);
        EventBus.Unsubscribe<SpeciationEvent>(OnSpeciation);
        EventBus.Unsubscribe<ExtinctionEvent>(OnExtinction);
    }
}
