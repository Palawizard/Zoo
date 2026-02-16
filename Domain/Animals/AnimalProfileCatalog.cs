public static class AnimalProfileCatalog
{
    private static readonly Dictionary<(SpeciesType species, SexType sex), AnimalProfile> Profiles = new()
    {
        {
            (SpeciesType.Tigre, SexType.Male),
            new AnimalProfile(
                SpeciesType.Tigre, SexType.Male, FoodType.Meat,
                12m, 2, 6 * 365, null, 14 * 365, null, 25 * 365,
                false, null, null, null, null)
        },
        {
            (SpeciesType.Tigre, SexType.Female),
            new AnimalProfile(
                SpeciesType.Tigre, SexType.Female, FoodType.Meat, 
                10m, 2, 4 * 365, 3, 14 * 365, 0.33m, 25 * 365,
                false, 3, 20, null, null) 
        },
    };

    public static AnimalProfile Get(SpeciesType species, SexType sex)
    {
        if (Profiles.TryGetValue((species, sex), out var profile))
            return profile;

        throw new InvalidOperationException($"Missing profile for {species}/{sex}.");
    }
}