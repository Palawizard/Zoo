namespace Zoo.Domain.Events;

/// <summary>
/// Lists the event kinds that can appear in the simulation log
/// </summary>
public enum ZooEventType
{
    SimulationInitialized,
    Pregnancy,
    EggLaying,
    Birth,
    InfantDeath,
    Disease,
    DiseaseRecovered,
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
