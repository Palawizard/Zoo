using Zoo.Domain.Animals;

namespace Zoo.Domain.Habitats;

/// <summary>
/// Habitat dedicated to tigers
/// </summary>
public sealed class TigerHabitat : Habitat
{
    /// <summary>
    /// Creates a tiger habitat
    /// </summary>
    public TigerHabitat() : base(SpeciesType.Tiger) { }
}
