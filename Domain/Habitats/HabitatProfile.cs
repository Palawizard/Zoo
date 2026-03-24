using Zoo.Domain.Animals;

namespace Zoo.Domain.Habitats;

/// <summary>
/// Stores the fixed configuration of one habitat type
/// </summary>
public sealed record HabitatProfile(
    SpeciesType Species,
    decimal BuyPrice,
    decimal SellPrice,
    int Capacity,
    int MonthlyLossCount,
    decimal LossProbability = 0.5m
);
