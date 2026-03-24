using Zoo.Domain.Animals;

namespace Zoo.Tests.Unit;

/// <summary>
/// Unit tests for animal disease rules
/// </summary>
public sealed class DiseaseRulesTests
{
    /// <summary>
    /// Checks that sickness duration stays inside the expected range
    /// </summary>
    [Fact]
    public void ContractSicknessSetsDurationWithinAllowedRange()
    {
        var eagle = new ZooAnimal("Aquila", SexType.Male, SpeciesType.Eagle, ageDays: 4 * 365);

        var started = eagle.ContractSickness(new Random(0));

        Assert.True(started);
        Assert.True(eagle.IsSick);
        Assert.InRange(eagle.DiseaseRemainingDays, 24, 36);
    }

    /// <summary>
    /// Checks that a sick female cannot start gestation
    /// </summary>
    [Fact]
    public void SickAnimalCannotStartGestation()
    {
        var tigress = new ZooAnimal("Nyx", SexType.Female, SpeciesType.Tiger, ageDays: 4 * 365);

        Assert.True(tigress.CanStartGestationToday());

        tigress.ContractSickness(new Random(1));

        Assert.True(tigress.IsSick);
        Assert.False(tigress.CanStartGestationToday());
    }
}
