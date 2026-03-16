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
    TurnAdvanced
}
