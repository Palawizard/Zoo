using Zoo.Application.Simulation;
using Zoo.Domain.Animals;

namespace Zoo.Tests.Unit;

public sealed class ReproductionRulesTests
{
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

    [Fact]
    public void EggLayerCannotStartGestation()
    {
        var eagle = new ZooAnimal("Aella", SexType.Female, SpeciesType.Eagle, ageDays: 4 * 365);

        UnlockArrivalBlock(eagle);

        Assert.False(eagle.CanStartGestationToday());
    }

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

    [Fact]
    public void TigressMustWaitTwentyMonthsBetweenLitters()
    {
        var tigress = new ZooAnimal("Sura", SexType.Female, SpeciesType.Tiger, ageDays: 4 * 365);
        UnlockArrivalBlock(tigress);

        tigress.StartGestation();
        Assert.True(tigress.IsGestating);

        var gestationDays = tigress.Profile.GestationDays!.Value;
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

    private static void UnlockArrivalBlock(params ZooAnimal[] animals)
    {
        foreach (var animal in animals)
        {
            for (var day = 0; day < 30; day++)
                animal.ProgressArrivalReproductionBlockOneDay();
        }
    }
}
