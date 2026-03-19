# Zoo

Zoo simulator written in C# with a turn engine, economic management, business event tracking, monthly exceptional events and an interactive console application.

## Run

```bash
dotnet run
```

The entry point starts the interactive console application in [Program.cs](./Program.cs).

## Run the desktop UI

```bash
dotnet run --project Zoo.Desktop
```

The desktop dashboard is built with Avalonia and lives in [Zoo.Desktop](./Zoo.Desktop).

## Test

```bash
dotnet test Zoo.sln
```

The solution now includes `Tests/Zoo.Tests` with:
- unit tests for critical hunger, reproduction, disease and economy rules
- one integration scenario covering a 24-month simulation run

## Documentation

PlantUML sources are in [Docs/Diagrams](./Docs/Diagrams).

Main files:
- [Docs/Diagrams/ZooSimulator.puml](./Docs/Diagrams/ZooSimulator.puml)
- [Docs/Diagrams/ZooSimulator.Domain.puml](./Docs/Diagrams/ZooSimulator.Domain.puml)
- [Docs/Diagrams/ZooSimulator.Economy.puml](./Docs/Diagrams/ZooSimulator.Economy.puml)
- [Docs/Diagrams/ZooSimulator.Events.puml](./Docs/Diagrams/ZooSimulator.Events.puml)
- [Docs/Diagrams/ZooSimulator.Rules.puml](./Docs/Diagrams/ZooSimulator.Rules.puml)
- [Docs/Diagrams/ZooSimulator.AppInfraUI.puml](./Docs/Diagrams/ZooSimulator.AppInfraUI.puml)

User guide:
- [Docs/Guide/USER_GUIDE.md](./Docs/Guide/USER_GUIDE.md)
