using Zoo.Domain.Animals;
using Zoo.Domain.Visitors;

namespace Zoo.Tests.Unit;

public sealed class VisitorRulesTests
{
    [Fact]
    public void VisitorRevenueAppliesMonthlyVariationWithinTwentyPercentRange()
    {
        var calculator = new VisitorRevenueCalculator(new Random(0));
        var tiger = new ZooAnimal("Rajah", SexType.Male, SpeciesType.Tiger, ageDays: 4 * 365);

        var revenueBySpecies = calculator.CalculateBySpecies(new[] { tiger }, isHighSeason: true);

        var revenue = revenueBySpecies[SpeciesType.Tiger];
        Assert.InRange(revenue, 30m * 0.8m * 60m, 30m * 1.2m * 60m);
    }
}
