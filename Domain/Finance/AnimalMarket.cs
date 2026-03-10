using Zoo.Domain.Animals;

namespace Zoo.Domain.Finance;

public sealed class AnimalMarket
{
    public decimal BuyAnimalPrice(SpeciesType species, int ageDays)
    {
        if (ageDays < 0) throw new ArgumentOutOfRangeException(nameof(ageDays));

        var basePrice = GetBasePrice(species);
        var ageFactor = GetAgeFactor(ageDays);

        return basePrice * ageFactor;
    }

    public decimal SellAnimalPrice(SpeciesType species, int ageDays)
    {
        if (ageDays < 0) throw new ArgumentOutOfRangeException(nameof(ageDays));

        var basePrice = GetBasePrice(species);
        var ageFactor = GetAgeFactor(ageDays);

        return basePrice * ageFactor * 0.6m;
    }

    private static decimal GetBasePrice(SpeciesType species) => species switch
    {
        SpeciesType.Tiger => 4000m,
        SpeciesType.Eagle => 800m,
        SpeciesType.Rooster => 50m,
        _ => 100m
    };

    private static decimal GetAgeFactor(int ageDays)
    {
        if (ageDays < 180) return 0.8m;
        if (ageDays < 365) return 1.0m;
        if (ageDays < 365 * 5) return 0.7m;
        return 0.4m;
    }
}
