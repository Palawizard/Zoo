using Zoo.Application.Simulation;
using Zoo.Domain.Animals;

namespace Zoo.Tests.Unit;

/// <summary>
/// Unit tests for reproduction rules
/// </summary>
public sealed class ReproductionRulesTests
{
    /// <summary>
    /// Checks that adult arrival cooldown blocks then unlocks gestation
    /// </summary>
    [Fact]
    public void AdultArrivalBlockPreventsThenAllowsGestation()
    {
        var tigress = new ZooAnimal("Nala", SexType.Female, SpeciesType.Tiger, ageDays: 4 * 365);

        tigress.RegisterArrivalInZoo();

        Assert.False(tigress.CanStartGestationToday());

        for (var day = 0; day < 30; day++)
            tigress.ProgressArrivalReproductionBlockOneDay();

        Assert.True(tigress.CanStartGestationToday());
    }

    /// <summary>
    /// Checks that hunger interrupts an ongoing gestation
    /// </summary>
    [Fact]
    public void HungryFemaleLosesFetus()
    {
        var tigress = new ZooAnimal("Kira", SexType.Female, SpeciesType.Tiger, ageDays: 4 * 365);
        tigress.StartGestation();

        tigress.ApplyDailyFeeding(0m);
        tigress.ApplyDailyFeeding(0m);

        var bornCount = tigress.ProgressGestationOneDay();

        Assert.True(tigress.IsHungry);
        Assert.False(tigress.IsGestating);
        Assert.Equal(0, tigress.GestationRemainingDays);
        Assert.Equal(0, bornCount);
    }

    /// <summary>
    /// Checks that egg-laying species cannot start standard gestation
    /// </summary>
    [Fact]
    public void EggLayerCannotStartGestation()
    {
        var eagle = new ZooAnimal("Aella", SexType.Female, SpeciesType.Eagle, ageDays: 4 * 365);

        UnlockArrivalBlock(eagle);

        Assert.False(eagle.CanStartGestationToday());
    }

    /// <summary>
    /// Checks that tiger pregnancies only start when future litter space exists
    /// </summary>
    [Fact]
    public void TigerPregnancyRequiresEnoughHabitatSpaceForFutureLitter()
    {
        var simulation = new ZooSimulationService(cash: 20000m);
        simulation.BuyHabitat(SpeciesType.Tiger);

        var habitat = Assert.Single(simulation.Habitats);
        var male = new ZooAnimal("Raja", SexType.Male, SpeciesType.Tiger, ageDays: 6 * 365);
        var female = new ZooAnimal("Kali", SexType.Female, SpeciesType.Tiger, ageDays: 4 * 365);

        simulation.AddAnimal(male);
        simulation.AddAnimal(female);
        habitat.AddAnimal(male);
        habitat.AddAnimal(female);
        UnlockArrivalBlock(male, female);

        simulation.TryStartPregnancies();
        Assert.False(female.IsGestating);

        simulation.BuyHabitat(SpeciesType.Tiger);
        simulation.TryStartPregnancies();
        Assert.False(female.IsGestating);

        simulation.BuyHabitat(SpeciesType.Tiger);
        simulation.TryStartPregnancies();
        Assert.True(female.IsGestating);
    }

    /// <summary>
    /// Checks that one male eagle cannot be paired with two females
    /// </summary>
    [Fact]
    public void MonogamousEaglesDoNotLetOneMalePairWithTwoFemales()
    {
        var simulation = new ZooSimulationService(cash: 20000m);
        simulation.BuyHabitat(SpeciesType.Eagle);
        simulation.BuyHabitat(SpeciesType.Eagle);

        var habitat = simulation.Habitats[0];
        var male = new ZooAnimal("Aetos", SexType.Male, SpeciesType.Eagle, ageDays: 4 * 365);
        var firstFemale = new ZooAnimal("Aella", SexType.Female, SpeciesType.Eagle, ageDays: 4 * 365);
        var secondFemale = new ZooAnimal("Nyx", SexType.Female, SpeciesType.Eagle, ageDays: 4 * 365);

        simulation.AddAnimal(male);
        simulation.AddAnimal(firstFemale);
        simulation.AddAnimal(secondFemale);
        habitat.AddAnimal(male);
        habitat.AddAnimal(firstFemale);
        habitat.AddAnimal(secondFemale);
        UnlockArrivalBlock(male, firstFemale, secondFemale);
        simulation.SetCurrentMonth(3);

        simulation.TryEggLayingForCurrentMonth();

        var femalesWithEggs = new[] { firstFemale, secondFemale }.Count(a => a.PendingEggs > 0);
        Assert.Equal(1, femalesWithEggs);
    }

    /// <summary>
    /// Checks that hens need enough habitat space for their egg batch
    /// </summary>
    [Fact]
    public void HensNeedEnoughHabitatSpaceForTheirMonthlyEggBatch()
    {
        var simulation = new ZooSimulationService(cash: 20000m);
        simulation.BuyHabitat(SpeciesType.Rooster);

        var habitat = Assert.Single(simulation.Habitats);
        var rooster = new ZooAnimal("Rocky", SexType.Male, SpeciesType.Rooster, ageDays: 6 * 30);
        var hen = new ZooAnimal("Ruby", SexType.Female, SpeciesType.Rooster, ageDays: 6 * 30);

        simulation.AddAnimal(rooster);
        simulation.AddAnimal(hen);
        habitat.AddAnimal(rooster);
        habitat.AddAnimal(hen);
        UnlockArrivalBlock(rooster, hen);
        simulation.SetCurrentMonth(1);

        simulation.TryEggLayingForCurrentMonth();
        Assert.Equal(0, hen.PendingEggs);

        simulation.BuyHabitat(SpeciesType.Rooster);
        simulation.BuyHabitat(SpeciesType.Rooster);

        simulation.TryEggLayingForCurrentMonth();
        Assert.True(hen.PendingEggs >= 16);
    }

    /// <summary>
    /// Checks that a tigress waits twenty months between litters
    /// </summary>
    [Fact]
    public void TigressMustWaitTwentyMonthsBetweenLitters()
    {
        var tigress = new ZooAnimal("Sura", SexType.Female, SpeciesType.Tiger, ageDays: 4 * 365);
        UnlockArrivalBlock(tigress);

        tigress.StartGestation();
        Assert.True(tigress.IsGestating);

        Assert.NotNull(tigress.Profile.GestationDays);
        var gestationDays = tigress.Profile.GestationDays.Value;
        for (var day = 0; day < gestationDays; day++)
        {
            tigress.ApplyDailyFeeding(tigress.GetDailyFoodNeedKg());
            tigress.ProgressGestationOneDay();
        }

        tigress.RegisterBirthCycleCompleted();

        Assert.False(tigress.CanStartGestationToday());

        for (var month = 0; month < 19; month++)
            tigress.ProgressReproductionOneMonth();

        Assert.False(tigress.CanStartGestationToday());

        tigress.ProgressReproductionOneMonth();

        Assert.True(tigress.CanStartGestationToday());
    }

    /// <summary>
    /// Checks that newborns can be renamed one after another
    /// </summary>
    [Fact]
    public void NewbornsCanBeNamedOneByOneAfterBirth()
    {
        var simulation = new ZooSimulationService(cash: 50000m);
        simulation.BuyHabitat(SpeciesType.Tiger);
        simulation.BuyHabitat(SpeciesType.Tiger);
        simulation.BuyHabitat(SpeciesType.Tiger);

        var tigress = new ZooAnimal("Nala", SexType.Female, SpeciesType.Tiger, ageDays: 4 * 365);
        UnlockArrivalBlock(tigress);
        tigress.StartGestation();
        simulation.AddAnimal(tigress);
        simulation.Habitats[0].AddAnimal(tigress);

        var gestationDays = tigress.Profile.GestationDays!.Value;
        for (var day = 0; day < gestationDays; day++)
        {
            tigress.ApplyDailyFeeding(tigress.GetDailyFoodNeedKg());
            simulation.ProcessGestations();
        }

        var firstNewborn = simulation.PeekNewbornAwaitingName();
        Assert.NotNull(firstNewborn);
        Assert.StartsWith("Cub of Nala ", firstNewborn.Name);

        var assignedNames = new[] { "Asha", "Kito", "Zuri" };
        var renamedCount = 0;

        foreach (var assignedName in assignedNames)
        {
            if (simulation.PeekNewbornAwaitingName() is null)
                break;

            var renamed = simulation.TryFinalizeNextNewbornNaming(assignedName, out var namedAnimal, out var failureReason);
            Assert.True(renamed);
            Assert.Equal(string.Empty, failureReason);
            Assert.Equal(assignedName, namedAnimal!.Name);
            renamedCount++;
        }

        Assert.True(renamedCount >= 1);
    }

    // Arrival cooldown is removed by simulating thirty days
    private static void UnlockArrivalBlock(params ZooAnimal[] animals)
    {
        foreach (var animal in animals)
        {
            for (var day = 0; day < 30; day++)
                animal.ProgressArrivalReproductionBlockOneDay();
        }
    }
}
