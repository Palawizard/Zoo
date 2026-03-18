# Zoo Simulator - User Guide

## Prerequisites

- .NET 10 SDK installed

## Start the application

```bash
dotnet run
```

The application opens the interactive console menu.

## Main actions

The console exposes these actions:

1. Advance one turn: runs one simulation day
2. Canteen: buy meat or seeds
3. Add an animal: buy an animal and place it in a compatible habitat
4. Buy a habitat
5. Sell an animal
6. Sell a habitat
7. Show status
0. Quit

## Simulation model

- 1 turn = 1 day
- high season = May to September
- monthly events are processed on the first day of each month
- yearly subsidy is processed on the first day of January

## What happens during a turn

Each turn can trigger:

- feeding and hunger progression
- disease progression and new disease cases
- gestation progression and hatching
- pregnancy start
- monthly exceptional events
- visitor revenue
- annual protected-species subsidy
- business audit events

## Event log

The simulation keeps an audit log in memory through `ZooSimulationService.Events`.

It records:

- management actions
- business events such as pregnancy or birth
- exceptional monthly events
- financial inflows
- turn progression

## Run the automated tests

```bash
dotnet test Zoo.sln
```

## PlantUML diagrams

PlantUML sources are stored in `Docs/Diagrams`.

If PlantUML is installed locally, diagrams can be regenerated from the `.puml` files.
