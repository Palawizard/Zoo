namespace Zoo.Domain.Animals;

public sealed record AnimalDailyOutcome(
    bool DiedOfOldAge = false,
    bool DiedOfDisease = false,
    bool DiedOfHunger = false,
    bool RecoveredFromDisease = false
);
