# CES.Reddit

CES.Reddit contains a .NET 8/C# simulation prototype for a **Cosmic Evolution Simulator**. The current codebase focuses on a Unity-agnostic back-end that models universe creation, planetary formation, abiogenesis, micro-simulation, behavior systems, telemetry, and related game-loop scaffolding.

The main implementation lives in `The-Systems-Architecture-CES-main/`, where you will find:

- `PlanetaryFormation/` — the console application and simulation code
- `PlanetaryFormation.Tests/` — xUnit tests for deterministic core logic
- `README.md` — deeper subsystem-level documentation for the simulation architecture

## Prerequisites

Before working with the project, make sure you have:

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- A terminal capable of running `dotnet` CLI commands
- Optional: an IDE such as Visual Studio, VS Code, or Rider for editing and debugging

## Setup

1. Open a terminal in the repository.
2. Change into the main project directory:

   ```bash
   cd The-Systems-Architecture-CES-main
   ```

3. Restore and build the application:

   ```bash
   dotnet build PlanetaryFormation/PlanetaryFormation.csproj -nologo
   ```

4. Run the automated tests:

   ```bash
   dotnet test PlanetaryFormation.Tests/PlanetaryFormation.Tests.csproj -nologo
   ```

## Usage

Run the console demo from `The-Systems-Architecture-CES-main`:

```bash
dotnet run --project PlanetaryFormation/PlanetaryFormation.csproj -nologo
```

Example output includes:

- Universe and galaxy hierarchy creation
- Macro-simulation and habitability survey output
- Micro-simulation/speciation events
- Behavior, telemetry, and other simulation summaries

If you want more implementation detail, see:

- `The-Systems-Architecture-CES-main/README.md`

## Contributing

Contributions are welcome. To keep changes easy to review:

1. Make focused changes.
2. Build the app before opening a pull request:

   ```bash
   dotnet build PlanetaryFormation/PlanetaryFormation.csproj -nologo
   ```

3. Run the test suite:

   ```bash
   dotnet test PlanetaryFormation.Tests/PlanetaryFormation.Tests.csproj -nologo
   ```

4. Update documentation when behavior or setup steps change.

## License

This repository does not currently include a license file. Until one is added, assume the license is **not specified**.
