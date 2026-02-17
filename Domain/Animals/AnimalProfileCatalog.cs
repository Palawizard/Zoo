public static class AnimalProfileCatalog
{
    private static readonly Dictionary<(SpeciesType species, SexType sex), AnimalProfile> Profiles = new()
    {
        {
            (SpeciesType.Tiger, SexType.Male),
            new AnimalProfile(
                SpeciesType.Tiger,
                SexType.Male,
                FoodType.Meat,
                12m,
                2,
                6 * 365,
                null,
                14 * 365,
                null,
                25 * 365,
                false,
                null,
                null,
                null,
                null)
        },
        {
            (SpeciesType.Tiger, SexType.Female),
            new AnimalProfile(
                SpeciesType.Tiger,
                SexType.Female,
                FoodType.Meat,
                10m,
                2,
                4 * 365,
                3 * 30,
                14 * 365,
                0.33m,
                25 * 365,
                false,
                3,
                20,
                null,
                null)
        },
        {
            (SpeciesType.Eagle, SexType.Male),
            new AnimalProfile(
                SpeciesType.Eagle,
                SexType.Male,
                FoodType.Meat,
                0.25m,
                10,
                4 * 365,
                null,
                14 * 365,
                null,
                25 * 365,
                true,
                null,
                null,
                null,
                null)
        },
        {
            (SpeciesType.Eagle, SexType.Female),
            new AnimalProfile(
                SpeciesType.Eagle,
                SexType.Female,
                FoodType.Meat,
                0.3m,
                10,
                4 * 365,
                45,
                14 * 365,
                0.5m,
                25 * 365,
                true,
                2,
                null,
                2,
                3)
        },
        {
            (SpeciesType.Rooster, SexType.Female),
            new AnimalProfile(
                SpeciesType.Rooster,
                SexType.Female,
                FoodType.Seeds,
                0.15m,
                1,
                6 * 30,
                42,
                8 * 365,
                0.5m,
                15 * 365,
                false,
                null,
                null,
                200,
                null)
        },
        {
            (SpeciesType.Rooster, SexType.Male),
            new AnimalProfile(
                SpeciesType.Rooster,
                SexType.Male,
                FoodType.Seeds,
                0.18m,
                2,
                6 * 30,
                null,
                8 * 365,
                null,
                15 * 365,
                false,
                null,
                null,
                null,
                null)
        }
    };

    public static AnimalProfile Get(SpeciesType species, SexType sex)
    {
        if (Profiles.TryGetValue((species, sex), out var profile))
            return profile;

        throw new InvalidOperationException($"Missing profile for {species}/{sex}.");
    }
}
