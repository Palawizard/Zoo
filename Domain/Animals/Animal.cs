namespace Zoo.Domain.Animals;

public abstract class Animal
{
    // identity
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; protected set; }
    public SexType Sex { get; }

    public SpeciesType Species { get; }
    public AnimalProfile Profile { get; }

    public int AgeDays { get; protected set; }
    public bool IsAlive { get; protected set; }
    // 
    public bool IsHungry { get; protected set; }
    public bool IsSick { get; protected set; }

    public int HungerDebtDays { get; private set; }
    public bool IsPregnant { get; private set; }
    public int PregnancyDays { get; private set; }
    public int? LastBirthDay { get; private set; }


    protected Animal(string name, SexType sex, SpeciesType species, int ageDays, bool isHungry, bool isSick)
    {
        Name = name;
        Sex = sex;
        Species = species;
        Profile = AnimalProfileCatalog.Get(species, sex);
        AgeDays = ageDays;
        IsAlive = true;
        IsHungry = isHungry;
        IsSick = isSick;
    }
}
