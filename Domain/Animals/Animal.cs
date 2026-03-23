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

    /// <summary>
    /// Marks the animal as sick
    /// </summary>
    public void MakeSick() => Health = HealthStatus.Sick;

    /// <summary>
    /// Marks the animal as dead and clears temporary states
    /// </summary>
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

    /// <summary>
    /// Renames the animal after trimming the value
    /// </summary>
    public void Rename(string name)
    {
        var trimmedName = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
            throw new ArgumentException("Animal name cannot be empty.", nameof(name));

        Name = trimmedName;
    }

    /// <summary>
    /// Restores the animal to a healthy state
    /// </summary>
    public void Heal() => Health = HealthStatus.Healthy;
    
    private const decimal DiseaseDeathProbability = 0.10m;

    /// <summary>
    /// Creates a new animal with its current state
    /// </summary>
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

    /// <summary>
    /// Returns the food needed for the day
    /// </summary>
    public decimal GetDailyFoodNeedKg()
    {
        // Gestating females need more food
        if (Sex == SexType.Female && IsGestating)
            return Profile.DailyFoodKg * GestatingFemaleFoodMultiplier;

        return Profile.DailyFoodKg;
    }

    /// <summary>
    /// Applies the daily feeding
    /// </summary>
    public void ApplyDailyFeeding(decimal kgProvided) => Feed(kgProvided);

    /// <summary>
    /// Feeds the animal and updates hunger
    /// </summary>
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

        // Not enough food counts as a missed day
        HungerDebtDays++;
        IsHungry = HungerDebtDays >= Profile.DaysBeforeHungry;
    }

    /// <summary>
    /// Advances the animal by one day
    /// </summary>
    public AnimalDailyOutcome AdvanceOneDay()
    {
        if (!IsAlive)
            return new AnimalDailyOutcome();

        AgeDays++;
        ProgressArrivalReproductionBlockOneDay();
        var wasSick = IsSick;

        // Disease is resolved before other death checks
        if (ProgressDiseaseOneDay())
            return new AnimalDailyOutcome(DiedOfDisease: true);

        if (!IsAlive)
            return new AnimalDailyOutcome();

        if (wasSick && !IsSick)
            return new AnimalDailyOutcome(RecoveredFromDisease: true);

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

    /// <summary>
    /// Tries to infect the animal for the day
    /// </summary>
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

    /// <summary>
    /// Forces the animal to become sick
    /// </summary>
    public bool ContractSickness(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        if (!IsAlive || IsSick)
            return false;

        StartDisease(random);
        return true;
    }

    /// <summary>
    /// Returns whether the animal can reproduce today
    /// </summary>
    public bool CanReproduceToday()
    {
        return IsAlive && !IsHungry && !IsSick && !IsBlockedFromReproductionByArrival();
    }

    /// <summary>
    /// Returns whether the animal is sexually mature
    /// </summary>
    public bool HasReachedSexualMaturity()
    {
        return AgeDays >= Profile.SexualMaturityDays;
    }

    /// <summary>
    /// Returns whether the animal is past its reproduction age
    /// </summary>
    public bool HasReachedReproductionEnd()
    {
        return AgeDays >= Profile.ReproductionEndDays;
    }

    /// <summary>
    /// Returns whether the animal can reproduce based on age
    /// </summary>
    public bool CanReproduceByAge()
    {
        return HasReachedSexualMaturity() && !HasReachedReproductionEnd();
    }

    /// <summary>
    /// Returns whether a female can start gestation today
    /// </summary>
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

    /// <summary>
    /// Starts gestation if possible
    /// </summary>
    public void StartGestation()
    {
        if (!CanStartGestationToday())
            return;

        if (Profile.GestationDays is not int gestationDays || gestationDays <= 0)
            return;

        IsGestating = true;
        GestationRemainingDays = gestationDays;
    }

    /// <summary>
    /// Advances gestation by one day
    /// </summary>
    public int ProgressGestationOneDay()
    {
        if (!IsGestating || GestationRemainingDays <= 0)
            return 0;

        if (IsHungry)
        {
            // Hunger interrupts the gestation
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

    /// <summary>
    /// Returns whether the animal can lay eggs this month
    /// </summary>
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

    /// <summary>
    /// Starts egg incubation if possible
    /// </summary>
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

    /// <summary>
    /// Advances egg incubation by one day
    /// </summary>
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

    /// <summary>
    /// Registers the animal arrival in the zoo
    /// </summary>
    public void RegisterArrivalInZoo()
    {
        // Adults cannot reproduce immediately after arrival
        AdultArrivalReproductionBlockRemainingDays =
            HasReachedSexualMaturity() ? AdultArrivalReproductionBlockDays : 0;
    }

    /// <summary>
    /// Advances the reproduction cooldown by one month
    /// </summary>
    public void ProgressReproductionOneMonth()
    {
        if (MonthsUntilNextLitter > 0)
            MonthsUntilNextLitter--;
    }

    /// <summary>
    /// Starts the cooldown before a new litter
    /// </summary>
    public void RegisterBirthCycleCompleted()
    {
        MonthsUntilNextLitter = Math.Max(0, Profile.MinMonthsBetweenLitters ?? 0);
    }

    /// <summary>
    /// Advances the arrival reproduction block by one day
    /// </summary>
    public void ProgressArrivalReproductionBlockOneDay()
    {
        if (AdultArrivalReproductionBlockRemainingDays > 0)
            AdultArrivalReproductionBlockRemainingDays--;
    }

    /// <summary>
    /// Returns whether arrival still blocks reproduction
    /// </summary>
    public bool IsBlockedFromReproductionByArrival()
    {
        return AdultArrivalReproductionBlockRemainingDays > 0;
    }

    /// <summary>
    /// Returns whether the animal can be shown to the public
    /// </summary>
    public bool IsExposedToPublic()
    {
        return IsAlive && !IsGestating;
    }

    // Egg-laying species are identified from the profile
    private bool IsEggLayer()
    {
        return Profile.EggsPerYear is > 0 || Profile.EggLayingMonth is not null;
    }

    // A new litter can start only when the cooldown is over
    private bool CanStartNewLitter()
    {
        return MonthsUntilNextLitter == 0;
    }

    // Uses the shared random source
    private void StartDisease()
    {
        StartDisease(Random.Shared);
    }

    // Disease duration is rolled once at the start
    private void StartDisease(Random random)
    {
        MakeSick();
        DiseaseRemainingDays = GetRandomDiseaseDurationDays(random);
    }

    // Returns true only if the disease kills the animal
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

    // Disease duration slightly varies around the base value
    private int GetRandomDiseaseDurationDays(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        var baseDuration = Math.Max(1, Profile.BaseDiseaseDurationDays);
        var minDuration = Math.Max(1, (int)Math.Round(baseDuration * 0.8, MidpointRounding.AwayFromZero));
        var maxDuration = Math.Max(minDuration, (int)Math.Round(baseDuration * 1.2, MidpointRounding.AwayFromZero));

        return random.Next(minDuration, maxDuration + 1);
    }

    // Converts the annual probability into a daily probability
    private decimal GetDailyDiseaseProbability()
    {
        var annualProbability = Math.Clamp(Profile.AnnualDiseaseProbability, 0m, 1m);
        var dailyProbability = 1d - Math.Pow(1d - (double)annualProbability, 1d / 365d);

        return (decimal)dailyProbability;
    }

    // Hunger becomes lethal after too many missed days
    private bool HasReachedStarvationDeathThreshold()
    {
        var threshold = Math.Max(1, Profile.DaysBeforeHungry * StarvationDeathMultiplier);
        return HungerDebtDays >= threshold;
    }
}
