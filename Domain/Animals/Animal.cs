namespace Zoo.Domain.Animals;

public abstract class Animal
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; protected set; }
    public SexType Sex { get; }

    public SpeciesType Species { get; }
    public AnimalProfile Profile { get; }

    public int AgeDays { get; protected set; }
    public bool IsAlive { get; protected set; }
    public bool IsHungry { get; protected set; }
    public bool IsSick { get; protected set; }
    public int HungerDebtDays { get; private set; }


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
        HungerDebtDays = isHungry ? Profile.DaysBeforeHungry : 0;
    }

    public void Feed(decimal kgProvided)
    {
        if (!IsAlive)
            return;

        if (kgProvided < 0m)
            throw new ArgumentOutOfRangeException(nameof(kgProvided), "Food amount cannot be negative.");

        if (kgProvided >= Profile.DailyFoodKg)
        {
            HungerDebtDays = 0;
            IsHungry = false;
            return;
        }

        HungerDebtDays++;
        IsHungry = HungerDebtDays >= Profile.DaysBeforeHungry;
    }

    public void AdvanceOneDay()
    {
        if (!IsAlive)
            return;

        AgeDays++;

        if (AgeDays >= Profile.LifeExpectancyDays)
            IsAlive = false;
    }

    public decimal GetDailyFoodNeedKg()
    {
        return Profile.DailyFoodKg;
    }

    public void ApplyDailyFeeding(decimal kgProvided)
    {
        Feed(kgProvided);
    }

    public bool CanReproduceToday()
    {
        return IsAlive && !IsHungry;
    }
}
