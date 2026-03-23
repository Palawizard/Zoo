namespace Zoo.Domain.Events;

/// <summary>
/// Represents one event recorded during the simulation
/// </summary>
public sealed record ZooEvent(
    int TurnNumber,
    int Year,
    int Month,
    int Day,
    ZooEventType Type,
    string Description
);
