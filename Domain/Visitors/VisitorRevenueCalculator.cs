using Zoo.Domain.Animals;

namespace Zoo.Domain.Visitors;

/// <summary>
/// Calculates projected revenue from the animals exposed to visitors
/// </summary>
public sealed class VisitorRevenueCalculator
{
    private readonly VisitorPricing _pricing = new();
    private readonly VisitorFlow _flow = new();
    private readonly Random _random;

    public VisitorRevenueCalculator(Random? random = null)
    {
        _random = random ?? Random.Shared;
    }

    /// <summary>
    /// Calculates projected revenue by species
    /// </summary>
    public IReadOnlyDictionary<SpeciesType, decimal> CalculateBySpecies(
        IEnumerable<ZooAnimal> exposedAnimals,
        bool isHighSeason)
    {
        // Revenue is grouped by species because the UI shows one row per species
        return exposedAnimals
            .GroupBy(a => a.Species)
            .ToDictionary(
                g => g.Key,
                g => g.Count() * _flow.GetVisitorsPerAnimal(g.Key, isHighSeason, _random) * _pricing.GroupRevenue);
    }
}
