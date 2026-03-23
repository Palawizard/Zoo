namespace Zoo.Domain.Animals;

/// <summary>
/// Describes the notable result of one simulated day for an animal
/// </summary>
public sealed record AnimalDailyOutcome(
    bool DiedOfOldAge = false,
    bool DiedOfDisease = false,
    bool DiedOfHunger = false,
    bool RecoveredFromDisease = false
);
