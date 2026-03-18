using Zoo.Domain.Animals;

namespace Zoo.Domain.Visitors;

public sealed class VisitorFlow
{
    public decimal GetVisitorsPerAnimal(SpeciesType species, bool isHighSeason, Random? random = null)
    {
        var baseline = species switch
        {
            SpeciesType.Tiger => isHighSeason ? 30m : 5m,
            SpeciesType.Rooster => isHighSeason ? 2m : 0.5m,
            SpeciesType.Eagle => isHighSeason ? 15m : 7m,
            _ => 0m
        };

        if (baseline == 0m)
            return 0m;

        var source = random ?? Random.Shared;
        var multiplier = 0.8m + ((decimal)source.NextDouble() * 0.4m);
        return baseline * multiplier;
    }
}
