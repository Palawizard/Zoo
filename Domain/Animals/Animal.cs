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
    public bool IsGestating {get; protected set;}
    public int GestationRemainingDays {get; protected set;}
    public int PendingEggs {get; protected set;}
    public int EggIncubationRemainingDays {get; protected set;}


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
        IsGestating = false;
        GestationRemainingDays = 0;
        PendingEggs = 0;
        EggIncubationRemainingDays = 0;
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

    public bool HasReachedSexualMaturity()
    {
        return AgeDays >= Profile.SexualMaturityDays;
    }

    public bool HasReachedReproductionEnd()
    {
        return AgeDays >= Profile.ReproductionEndDays;
    }

    public bool CanReproduceByAge()
    {
        return HasReachedSexualMaturity() && !HasReachedReproductionEnd();
    }

    public bool CanStartGestationToday()
    {
        return Sex == SexType.Female && IsAlive && !IsHungry && !IsSick && CanReproduceByAge() && !IsGestating && Profile.GestationDays is > 0;
    }

    public void StartGestation()
    {
        if (!CanStartGestationToday()) return;
        if (Profile.GestationDays is not int gestationDays || gestationDays <= 0) return;

        IsGestating = true;
        GestationRemainingDays = gestationDays;
    }

    public int ProgressGestationOneDay()
    {
        if (!IsGestating || GestationRemainingDays <= 0)
        {
            return 0;
        }

        GestationRemainingDays--;

        if (GestationRemainingDays > 0) return 0;

            IsGestating = false;
            GestationRemainingDays = 0;
            return Profile.LitterSize ?? 0;
    }

    public bool CanLayEggThisMonth(int month)
    {
        var canLay = Sex == SexType.Female
            && IsAlive
            && !IsHungry
            && !IsSick
            && CanReproduceByAge();

        if (!canLay)
            return false;

        if (Profile.EggLayingMonth is int eggMonth)
            return month == eggMonth;

        return Profile.EggsPerYear is > 0;
    }

    public void StartEggIncubation(int eggCount, int month)
    {
        if (eggCount <= 0) 
            throw new ArgumentOutOfRangeException(nameof(eggCount), "Egg count must be positive.");

        if (!CanLayEggThisMonth(month)) return;

        if (Profile.GestationDays is not int incubationDays || incubationDays <= 0) return;

        if (EggIncubationRemainingDays > 0) return;

        PendingEggs = eggCount;
        EggIncubationRemainingDays = incubationDays;
    }

    public int ProgressEggIncubationOneDay()
    {
        if (PendingEggs <= 0 || EggIncubationRemainingDays <= 0) return 0;

        EggIncubationRemainingDays--;

        if (EggIncubationRemainingDays > 0) return 0;

        var hatched = PendingEggs;
        PendingEggs = 0;
        EggIncubationRemainingDays = 0;
        return hatched;
    }
}
