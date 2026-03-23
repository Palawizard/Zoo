namespace Zoo.Domain.Habitats;

/// <summary>
/// Central catalog for habitat profiles
/// </summary>
public static class HabitatProfileCatalog
{
    // Each species has one shared habitat profile
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
            new HabitatProfile(
                SpeciesType.Eagle,
                2000m,
                500m,
                4,
                1,
                0.1m)
        },
        {
            SpeciesType.Rooster,
            new HabitatProfile(
                SpeciesType.Rooster,
                300m,
                50m,
                10,
                4,
                0.05m)
        }
    };

    /// <summary>
    /// Returns the profile for the requested species
    /// </summary>
    public static HabitatProfile Get(SpeciesType species)
    {
        if (Profiles.TryGetValue(species, out var profile))
            return profile;

        throw new InvalidOperationException($"Missing profile for {species}.");
    }
}
