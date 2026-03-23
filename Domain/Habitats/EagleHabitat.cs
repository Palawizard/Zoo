namespace Zoo.Domain.Habitats;

/// <summary>
/// Habitat dedicated to eagles
/// </summary>
public sealed class EagleHabitat : Habitat
{
    /// <summary>
    /// Creates an eagle habitat
    /// </summary>
    public EagleHabitat() : base(SpeciesType.Eagle) { }
}
