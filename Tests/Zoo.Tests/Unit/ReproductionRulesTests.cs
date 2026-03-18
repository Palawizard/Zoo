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
}
