namespace Zoo.Domain.Events;

public enum ZooEventType
{
    SimulationInitialized,
    Pregnancy,
    EggLaying,
    Birth,
    InfantDeath,
    Disease,
    DiseaseDeath,
    HungerDeath,
    EndOfLife,
    HabitatMonthlyLoss,
    OverpopulationDeath,
    Fire,
    Theft,
    Pests,
    SpoiledMeat,
    AnimalPurchased,
    AnimalSold,
    HabitatPurchased,
    HabitatSold,
    FoodPurchased,
    VisitorIncome,
    AnnualSubsidy,
    HabitatAnimalsRehoused,
    HabitatAnimalsEuthanized,
    TurnAdvanced
}
