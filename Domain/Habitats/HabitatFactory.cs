namespace Zoo.Domain.Habitats;

public static class HabitatFactory
{
    public static Habitat Create(SpeciesType species) => species switch
    {
        SpeciesType.Tiger   => new TigerHabitat(),
        SpeciesType.Eagle   => new EagleHabitat(),
        SpeciesType.Rooster => new RoosterHabitat(),
        _ => throw new InvalidOperationException($"No habitat defined for species {species}.")
    };
}