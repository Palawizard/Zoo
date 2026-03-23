namespace Zoo.Domain.Animals;

/// <summary>
/// Concrete animal type used by the zoo simulation
/// </summary>
public sealed class ZooAnimal : Animal
{
    /// <summary>
    /// Creates a zoo animal with its current state
    /// </summary>
    public ZooAnimal(
        string name,
        SexType sex,
        SpeciesType species,
        int ageDays = 0,
        bool isHungry = false,
        bool isSick = false)
        : base(name, sex, species, ageDays, isHungry, isSick)
    {
    }
}
