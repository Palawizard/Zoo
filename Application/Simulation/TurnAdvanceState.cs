namespace Zoo.Application.Simulation;

/// <summary>
/// Describes whether a turn finished or stopped for a decision
/// </summary>
public enum TurnAdvanceState
{
    Completed,
    AwaitingHabitatEmergencyDecision
}
