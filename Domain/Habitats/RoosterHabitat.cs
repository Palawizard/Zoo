namespace Zoo.Domain.Habitats;

/// <summary>
/// Habitat dedicated to roosters
/// </summary>
public sealed class RoosterHabitat : Habitat
{
    /// <summary>
    /// Creates a rooster habitat
    /// </summary>
    public RoosterHabitat() : base(SpeciesType.Rooster) { }
}
