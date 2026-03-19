using System;

namespace Zoo.Domain.Animals;

public abstract class Animal
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; protected set; }
    public SexType Sex { get; }
    public SpeciesType Species { get; }
    public AnimalProfile Profile { get; }

    public int AgeDays { get; protected set; }
    public bool IsHungry { get; protected set; }
    public int HungerDebtDays { get; private set; }
    public bool IsGestating { get; protected set; }
    public int GestationRemainingDays { get; protected set; }
    public int PendingEggs { get; protected set; }
    public int EggIncubationRemainingDays { get; protected set; }
    public int DiseaseRemainingDays { get; private set; }
    public int AdultArrivalReproductionBlockRemainingDays { get; private set; }
    public int MonthsUntilNextLitter { get; private set; }

    private const int AdultArrivalReproductionBlockDays = 30;
    private const decimal GestatingFemaleFoodMultiplier = 2m;
    private const int StarvationDeathMultiplier = 2;

    public HealthStatus Health { get; private set; } = HealthStatus.Healthy;
    public bool IsAlive => Health != HealthStatus.Dead;
    public bool IsSick => Health == HealthStatus.Sick;

    public void MakeSick() => Health = HealthStatus.Sick;
    public void Kill()
    {
        Health = HealthStatus.Dead;
        IsHungry = false;
        HungerDebtDays = 0;
        IsGestating = false;
        GestationRemainingDays = 0;
        PendingEggs = 0;
        EggIncubationRemainingDays = 0;
        DiseaseRemainingDays = 0;
    }
    public void Heal() => Health = HealthStatus.Healthy;
    
    private const decimal DiseaseDeathProbability = 0.10m;

    protected Animal(string name, SexType sex, SpeciesType species, int ageDays, bool isHungry, bool isSick)
    {
        Name = name;
        Sex = sex;
        Species = species;
        Profile = AnimalProfileCatalog.Get(species, sex);
        AgeDays = ageDays;
        IsHungry = isHungry;
        HungerDebtDays = isHungry ? Profile.DaysBeforeHungry : 0;
        IsGestating = false;
        GestationRemainingDays = 0;
        PendingEggs = 0;
        EggIncubationRemainingDays = 0;
        DiseaseRemainingDays = 0;
        MonthsUntilNextLitter = 0;

        if (isSick)
            StartDisease();
    }

    public decimal GetDailyFoodNeedKg()
    {
        if (Sex == SexType.Female && IsGestating)
            return Profile.DailyFoodKg * GestatingFemaleFoodMultiplier;

        return Profile.DailyFoodKg;
    }

    public void ApplyDailyFeeding(decimal kgProvided) => Feed(kgProvided);

    public void Feed(decimal kgProvided)
    {
        if (!IsAlive)
            return;

        if (kgProvided < 0m)
            throw new ArgumentOutOfRangeException(nameof(kgProvided), "Food amount cannot be negative.");

        if (kgProvided >= GetDailyFoodNeedKg())
        {
            HungerDebtDays = 0;
            IsHungry = false;
            return;
        }

        HungerDebtDays++;
        IsHungry = HungerDebtDays >= Profile.DaysBeforeHungry;
    }

    public AnimalDailyOutcome AdvanceOneDay()
    {
        if (!IsAlive)
            return new AnimalDailyOutcome();

        AgeDays++;
        ProgressArrivalReproductionBlockOneDay();
        if (ProgressDiseaseOneDay())
            return new AnimalDailyOutcome(DiedOfDisease: true);

        if (!IsAlive)
            return new AnimalDailyOutcome();

        if (HasReachedStarvationDeathThreshold())
        {
            Kill();
            return new AnimalDailyOutcome(DiedOfHunger: true);
        }

        if (AgeDays < Profile.LifeExpectancyDays)
            return new AnimalDailyOutcome();

        Kill();
        return new AnimalDailyOutcome(DiedOfOldAge: true);
    }

    public bool TryCatchDiseaseToday()
    {
        if (!IsAlive || IsSick)
            return false;

        var dailyProbability = GetDailyDiseaseProbability();
        if (dailyProbability <= 0m)
            return false;

        var roll = (decimal)Random.Shared.NextDouble();
        if (roll < dailyProbability)
        {
            StartDisease(Random.Shared);
            return true;
        }

        return false;
    }

    public bool ContractSickness(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        if (!IsAlive || IsSick)
            return false;

        StartDisease(random);
        return true;
    }

    public bool CanReproduceToday()
    {
        return IsAlive && !IsHungry && !IsSick && !IsBlockedFromReproductionByArrival();
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
        return Sex == SexType.Female
            && IsAlive
            && !IsHungry
            && !IsSick
            && CanReproduceByAge()
            && !IsGestating
            && !IsEggLayer()
            && CanStartNewLitter()
            && Profile.GestationDays is > 0
            && !IsBlockedFromReproductionByArrival();
    }

    public void StartGestation()
    {
        if (!CanStartGestationToday())
            return;

        if (Profile.GestationDays is not int gestationDays || gestationDays <= 0)
            return;

        IsGestating = true;
        GestationRemainingDays = gestationDays;
    }

    public int ProgressGestationOneDay()
    {
        if (!IsGestating || GestationRemainingDays <= 0)
            return 0;

        if (IsHungry)
        {
            IsGestating = false;
            GestationRemainingDays = 0;
            return 0;
        }

        GestationRemainingDays--;
        if (GestationRemainingDays > 0)
            return 0;

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
            && CanReproduceByAge()
            && IsEggLayer()
            && CanStartNewLitter()
            && !IsBlockedFromReproductionByArrival();

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

        if (!CanLayEggThisMonth(month))
            return;

        if (Profile.GestationDays is not int incubationDays || incubationDays <= 0)
            return;

        if (EggIncubationRemainingDays > 0)
            return;

        PendingEggs = eggCount;
        EggIncubationRemainingDays = incubationDays;
    }

    public int ProgressEggIncubationOneDay()
    {
        if (PendingEggs <= 0 || EggIncubationRemainingDays <= 0)
            return 0;

        EggIncubationRemainingDays--;
        if (EggIncubationRemainingDays > 0)
            return 0;

        var hatched = PendingEggs;
        PendingEggs = 0;
        EggIncubationRemainingDays = 0;
        return hatched;
    }

    public void RegisterArrivalInZoo()
    {
        AdultArrivalReproductionBlockRemainingDays =
            HasReachedSexualMaturity() ? AdultArrivalReproductionBlockDays : 0;
    }

    public void ProgressReproductionOneMonth()
    {
        if (MonthsUntilNextLitter > 0)
            MonthsUntilNextLitter--;
    }

    public void RegisterBirthCycleCompleted()
    {
        MonthsUntilNextLitter = Math.Max(0, Profile.MinMonthsBetweenLitters ?? 0);
    }

    public void ProgressArrivalReproductionBlockOneDay()
    {
        if (AdultArrivalReproductionBlockRemainingDays > 0)
            AdultArrivalReproductionBlockRemainingDays--;
    }

    public bool IsBlockedFromReproductionByArrival()
    {
        return AdultArrivalReproductionBlockRemainingDays > 0;
    }

    public bool IsExposedToPublic()
    {
        return IsAlive && !IsGestating;
    }

    private bool IsEggLayer()
    {
        return Profile.EggsPerYear is > 0 || Profile.EggLayingMonth is not null;
    }

    private bool CanStartNewLitter()
    {
        return MonthsUntilNextLitter == 0;
    }

    private void StartDisease()
    {
        StartDisease(Random.Shared);
    }

    private void StartDisease(Random random)
    {
        MakeSick();
        DiseaseRemainingDays = GetRandomDiseaseDurationDays(random);
    }

    private bool ProgressDiseaseOneDay()
    {
        if (!IsSick || DiseaseRemainingDays <= 0)
            return false;

        DiseaseRemainingDays--;

        if (DiseaseRemainingDays > 0)
            return false;

        DiseaseRemainingDays = 0;
        
        var roll = (decimal)Random.Shared.NextDouble();
        if (roll < DiseaseDeathProbability)
        {
            Kill();
            return true;
        }
        
        Heal();
        return false;
    }

    private int GetRandomDiseaseDurationDays(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        var baseDuration = Math.Max(1, Profile.BaseDiseaseDurationDays);
        var minDuration = Math.Max(1, (int)Math.Round(baseDuration * 0.8, MidpointRounding.AwayFromZero));
        var maxDuration = Math.Max(minDuration, (int)Math.Round(baseDuration * 1.2, MidpointRounding.AwayFromZero));

        return random.Next(minDuration, maxDuration + 1);
    }

    private decimal GetDailyDiseaseProbability()
    {
        var annualProbability = Math.Clamp(Profile.AnnualDiseaseProbability, 0m, 1m);
        var dailyProbability = 1d - Math.Pow(1d - (double)annualProbability, 1d / 365d);

        return (decimal)dailyProbability;
    }

    private bool HasReachedStarvationDeathThreshold()
    {
        var threshold = Math.Max(1, Profile.DaysBeforeHungry * StarvationDeathMultiplier);
        return HungerDebtDays >= threshold;
    }
}
