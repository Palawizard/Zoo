using Zoo.Domain.Animals;

namespace Zoo.Domain.Habitats;

/// <summary>
/// Creates the correct habitat type for a species
/// </summary>
public static class HabitatFactory
{
    /// <summary>
    /// Creates a habitat instance for the given species
    /// </summary>
    public static Habitat Create(SpeciesType species) => species switch
    {
        SpeciesType.Tiger => new TigerHabitat(),
        SpeciesType.Eagle => new EagleHabitat(),
        SpeciesType.Rooster => new RoosterHabitat(),
        _ => throw new InvalidOperationException($"No habitat defined for species {species}.")
    };
}
