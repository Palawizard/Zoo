using Zoo.Domain.Animals;

namespace Zoo.Domain.Combat;

public static class CombatStatsCatalog
{
    private const int StatVariationRange = 3;

    public static CombatStats GetStats(Animal animal)
    {
        var (baseForce, baseVitesse, baseDefense) = GetBaseStats(animal.Species);
        var rng = new Random(animal.Id.GetHashCode()); //toujours identique
        var force   = baseForce   + rng.Next(-StatVariationRange, StatVariationRange + 1);
        var vitesse = baseVitesse + rng.Next(-StatVariationRange, StatVariationRange + 1);
        var defense = baseDefense + rng.Next(-StatVariationRange, StatVariationRange + 1);

        return new CombatStats(
            Force:   Math.Max(1, force),
            Vitesse: Math.Max(1, vitesse),
            Defense: Math.Max(0, defense));
    }

    private static (int Force, int Vitesse, int Defense) GetBaseStats(SpeciesType species) =>
        species switch
        {
            SpeciesType.Tiger   => (22, 14, 12),
            SpeciesType.Eagle   => (12, 24,  5),
            SpeciesType.Rooster => ( 9, 11,  7),
            _                   => (10, 10,  5)
        };
}
