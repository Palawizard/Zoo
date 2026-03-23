using Zoo.Domain.Animals;

namespace Zoo.Domain.Habitats;

/// <summary>
/// Groups the monthly effects produced by a habitat
/// </summary>
public sealed record HabitatMonthlyOutcome(
    IReadOnlyList<Animal> NaturalLosses,
    IReadOnlyList<Animal> OverpopulationLosses,
    IReadOnlyList<Animal> NewlySickAnimals
);
