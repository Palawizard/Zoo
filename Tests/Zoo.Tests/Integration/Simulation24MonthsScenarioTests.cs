using Zoo.Application.Simulation;
using Zoo.Domain.Animals;
using Zoo.Domain.Events;
using Zoo.Domain.Feeding;

namespace Zoo.Tests.Integration;

/// <summary>
/// Integration tests that run the simulation over a long scenario
/// </summary>
public sealed class Simulation24MonthsScenarioTests
{
    /// <summary>
    /// Checks that the simulation can run for two full years without breaking its audit trail
    /// </summary>
    [Fact]
    public void SimulationCanRunForTwentyFourMonthsWithConsistentAuditLog()
    {
        var simulation = new ZooSimulationService(cash: 20000m);

        Assert.True(simulation.BuyHabitat(SpeciesType.Eagle));

        var habitat = Assert.Single(simulation.Habitats);
        var male = new ZooAnimal("Aetos", SexType.Male, SpeciesType.Eagle, ageDays: 4 * 365);
        var female = new ZooAnimal("Aella", SexType.Female, SpeciesType.Eagle, ageDays: 4 * 365);

        Assert.True(simulation.BuyAnimal(male));
        Assert.True(simulation.BuyAnimal(female));
        habitat.AddAnimal(male);
        habitat.AddAnimal(female);
        Assert.True(simulation.BuyFood(FoodType.Meat, 500m));

        for (var day = 0; day < 730; day++)
            simulation.NextTurn();

        Assert.Equal(730, simulation.TurnNumber);
        Assert.Equal(1, simulation.CurrentDayOfMonth);
        Assert.Equal(1, simulation.CurrentMonth);
        Assert.Equal(3, simulation.CurrentYear);
        Assert.True(simulation.Cash >= 0m);
        Assert.Equal(simulation.Cash, simulation.Ledger.Transactions[^1].BalanceAfter);
        Assert.Equal(730, simulation.Events.Count(e => e.Type == ZooEventType.TurnAdvanced));
        Assert.Contains(simulation.Events, e => e.Type == ZooEventType.HabitatPurchased);
        Assert.Contains(simulation.Events, e => e.Type == ZooEventType.AnimalPurchased);
        Assert.Contains(simulation.Events, e => e.Type == ZooEventType.FoodPurchased);
    }
}
