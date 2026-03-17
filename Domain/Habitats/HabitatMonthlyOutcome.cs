using Zoo.Domain.Animals;

namespace Zoo.Domain.Habitats;

public sealed record HabitatMonthlyOutcome(
    IReadOnlyList<Animal> NaturalLosses,
    IReadOnlyList<Animal> OverpopulationLosses,
    IReadOnlyList<Animal> NewlySickAnimals
);
