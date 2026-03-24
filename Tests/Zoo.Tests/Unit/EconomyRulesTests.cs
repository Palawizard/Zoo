using Zoo.Application.Simulation;
using Zoo.Domain.Animals;
using Zoo.Domain.Feeding;
using Zoo.Domain.Habitats;

namespace Zoo.Tests.Unit;

/// <summary>
/// Unit tests for economy and habitat emergency rules
/// </summary>
public sealed class EconomyRulesTests
{
    /// <summary>
    /// Checks that buying food updates cash, stock and the ledger
    /// </summary>
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

    /// <summary>
    /// Checks that habitat purchase fails when the zoo is too poor
    /// </summary>
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

    /// <summary>
    /// Checks that eagle habitats use the configured sell price
    /// </summary>
    [Fact]
    public void EagleHabitatUsesSpecifiedSellPrice()
    {
        var habitat = HabitatFactory.Create(SpeciesType.Eagle);

        Assert.Equal(500m, habitat.SellPrice);
    }

    /// <summary>
    /// Checks that interactive habitat destruction creates a pending decision
    /// </summary>
    [Fact]
    public void InteractiveHabitatDestructionCreatesPendingEmergency()
    {
        var simulation = new ZooSimulationService(cash: 10000m, interactiveHabitatEmergencies: true);
        Assert.True(simulation.BuyHabitat(SpeciesType.Tiger));

        var habitat = Assert.Single(simulation.Habitats);
        var tiger = new ZooAnimal("Khan", SexType.Male, SpeciesType.Tiger, ageDays: 365);
        simulation.AddAnimal(tiger);
        habitat.AddAnimal(tiger);

        simulation.DestroyHabitat(habitat, Domain.Events.ZooEventType.Fire, "Test fire.");

        Assert.NotNull(simulation.PendingHabitatEmergency);
        var pending = simulation.PendingHabitatEmergency!;
        Assert.Single(pending.DisplacedAnimals);
        Assert.Equal(SpeciesType.Tiger, pending.Species);
        Assert.Empty(simulation.Habitats);
    }

    /// <summary>
    /// Checks that displaced animals can be rehoused after buying a replacement habitat
    /// </summary>
    [Fact]
    public void ResolvingPendingEmergencyCanRehouseAnimalsByBuyingReplacementHabitat()
    {
        var simulation = new ZooSimulationService(cash: 10000m, interactiveHabitatEmergencies: true);
        Assert.True(simulation.BuyHabitat(SpeciesType.Tiger));

        var habitat = Assert.Single(simulation.Habitats);
        var tiger = new ZooAnimal("Khan", SexType.Male, SpeciesType.Tiger, ageDays: 365);
        simulation.AddAnimal(tiger);
        habitat.AddAnimal(tiger);

        simulation.DestroyHabitat(habitat, Domain.Events.ZooEventType.Fire, "Test fire.");

        var resolved = simulation.TryResolvePendingHabitatEmergency(HabitatEmergencyResolution.RehouseAnimals, out var failureReason);

        Assert.True(resolved);
        Assert.Equal(string.Empty, failureReason);
        Assert.Null(simulation.PendingHabitatEmergency);
        Assert.Single(simulation.Habitats);
        Assert.Contains(tiger, simulation.Habitats[0].Animals);
    }

    /// <summary>
    /// Checks that dead animals sell for a reduced price
    /// </summary>
    [Fact]
    public void DeadAnimalsSellForADeepDiscount()
    {
        var simulation = new ZooSimulationService(cash: 10000m);
        var tiger = new ZooAnimal("Khan", SexType.Male, SpeciesType.Tiger, ageDays: 365);
        simulation.AddAnimal(tiger);
        tiger.Kill();

        var expectedRevenue = simulation.EstimateAnimalSalePrice(tiger);
        var sold = simulation.SellAnimal(tiger);

        Assert.True(sold);
        Assert.DoesNotContain(tiger, simulation.Animals);
        Assert.True(expectedRevenue > 0m);
        Assert.Equal(10000m + expectedRevenue, simulation.Cash);
        Assert.Contains(simulation.Events, zooEvent => zooEvent.Type == Domain.Events.ZooEventType.AnimalSold);
    }

    /// <summary>
    /// Checks that a dead animal still occupies its habitat slot until it is sold
    /// </summary>
    [Fact]
    public void DeadAnimalsKeepHabitatSlotsUntilTheyAreSold()
    {
        var simulation = new ZooSimulationService(cash: 10000m);
        Assert.True(simulation.BuyHabitat(SpeciesType.Tiger));

        var habitat = Assert.Single(simulation.Habitats);
        var tiger = new ZooAnimal("Khan", SexType.Male, SpeciesType.Tiger, ageDays: 365);
        simulation.AddAnimal(tiger);
        habitat.AddAnimal(tiger);
        tiger.Kill();

        Assert.Contains(tiger, habitat.Animals);
        Assert.Equal(habitat.Capacity - 1, habitat.AvailableSlots);

        Assert.True(simulation.SellAnimal(tiger));
        Assert.DoesNotContain(tiger, habitat.Animals);
        Assert.Equal(habitat.Capacity, habitat.AvailableSlots);
    }
}
