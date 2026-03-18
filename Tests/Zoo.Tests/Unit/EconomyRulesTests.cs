using Zoo.Application.Simulation;
using Zoo.Domain.Animals;
using Zoo.Domain.Feeding;
using Zoo.Domain.Habitats;

namespace Zoo.Tests.Unit;

public sealed class EconomyRulesTests
{
    [Fact]
    public void BuyFoodConsumesCashAddsStockAndLedgerEntry()
    {
        var simulation = new ZooSimulationService(cash: 100m);

        var bought = simulation.BuyFood(FoodType.Meat, 10m);

        Assert.True(bought);
        Assert.Equal(50m, simulation.Cash);
        Assert.Equal(10m, simulation.MeatStockKg);
        Assert.Equal(2, simulation.Ledger.Transactions.Count);
        Assert.Equal(-50m, simulation.Ledger.Transactions[^1].Amount);
        Assert.Equal("Food", simulation.Ledger.Transactions[^1].Category);
        Assert.Equal(50m, simulation.Ledger.Transactions[^1].BalanceAfter);
    }

    [Fact]
    public void BuyHabitatFailsWhenCashIsInsufficient()
    {
        var simulation = new ZooSimulationService(cash: 1000m);

        var bought = simulation.BuyHabitat(SpeciesType.Tiger);

        Assert.False(bought);
        Assert.Empty(simulation.Habitats);
        Assert.Equal(1000m, simulation.Cash);
        Assert.Single(simulation.Ledger.Transactions);
    }

    [Fact]
    public void EagleHabitatUsesSpecifiedSellPrice()
    {
        var habitat = HabitatFactory.Create(SpeciesType.Eagle);

        Assert.Equal(500m, habitat.SellPrice);
    }
}
