namespace Zoo.Domain.Events;

public sealed record ZooEvent(
    int TurnNumber,
    int Year,
    int Month,
    int Day,
    ZooEventType Type,
    string Description
);
