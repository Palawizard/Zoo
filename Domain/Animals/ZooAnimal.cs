namespace Zoo.Domain.Animals;

public sealed class ZooAnimal : Animal
{
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
