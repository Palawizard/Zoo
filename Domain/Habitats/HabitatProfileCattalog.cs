using System.Linq.Expressions;

public static class HabitatProfileCatalog
{
    private static readonly Dictionary<SpeciesType, HabitatProfile> Profiles = new()
    {
        {
            SpeciesType.Tiger,
            new HabitatProfile(
                SpeciesType.Tiger,
                2000m,
                500m,
                2,
                1,
                0.2m)
        },
        {
            SpeciesType.Eagle,
            new HabitatProfile(SpeciesType.Eagle, 2000m, 1000m, 4, 1, 0.1m)
        },
        {
            SpeciesType.Rooster,
            new HabitatProfile(SpeciesType.Rooster, 300m, 50m, 10, 4, 0.05m)
        }
    };
}