using System.Collections.Generic;
using System.Linq;
using Zoo.Domain.Animals;

namespace Zoo.Domain.Visitors;

public sealed class VisitorRevenueCalculator
{
    private readonly VisitorPricing _pricing = new();
    private readonly VisitorFlow _flow = new();

    //calcule les revenus par espece
    public IReadOnlyDictionary<SpeciesType, decimal> CalculateBySpecies(
        IEnumerable<ZooAnimal> exposedAnimals,
        bool isHighSeason)
    {
        return exposedAnimals
            .GroupBy(a => a.Species)
            .ToDictionary(
                g => g.Key,
                g => g.Count() * _flow.GetVisitorsPerAnimal(g.Key, isHighSeason) * _pricing.GroupRevenue);
    }
}
