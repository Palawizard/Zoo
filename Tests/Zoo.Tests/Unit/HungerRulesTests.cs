using Zoo.Domain.Animals;

namespace Zoo.Tests.Unit;

public sealed class HungerRulesTests
{
    [Fact]
    public void TigerBecomesHungryAfterTwoDaysWithoutEnoughFood()
    {
        var tiger = new ZooAnimal("Rajah", SexType.Male, SpeciesType.Tiger);

        tiger.ApplyDailyFeeding(0m);

        Assert.False(tiger.IsHungry);
        Assert.Equal(1, tiger.HungerDebtDays);

        tiger.ApplyDailyFeeding(0m);

        Assert.True(tiger.IsHungry);
        Assert.Equal(2, tiger.HungerDebtDays);
    }

    [Fact]
    public void GestatingFemaleNeedsDoubleFood()
    {
        var tigress = new ZooAnimal("Shereza", SexType.Female, SpeciesType.Tiger, ageDays: 4 * 365);

        Assert.Equal(10m, tigress.GetDailyFoodNeedKg());

        tigress.StartGestation();

        Assert.True(tigress.IsGestating);
        Assert.Equal(20m, tigress.GetDailyFoodNeedKg());
    }
}
