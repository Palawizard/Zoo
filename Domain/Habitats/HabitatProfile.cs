namespace Zoo.Domain.Habitats;

public sealed record HabitatProfile(
    SpeciesType Species,
    decimal BuyPrice,
    decimal SellPrice,
    int Capacity,
    int MonthlyLossCount,
    decimal LossProbability = 0.5m
);
