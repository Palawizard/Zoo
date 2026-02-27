using System;
using System.Collections.Generic;
using System.Linq;

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
    public int SicknessDaysRemaining { get; private set; } = 0;

    public int AdultArrivalReproductionBlockRemainingDays { get; private set; }

    private const int AdultArrivalReproductionBlockDays = 30;
    private const decimal GestatingFemaleFoodMultiplier = 2m;
    private const decimal SicknessDeathChance = 0.10m;
    private const decimal SicknessDurationVariance = 0.20m;

    // ✅ Système de santé unifié
    public HealthStatus Health { get; private set; } = HealthStatus.Healthy;
    public bool IsAlive => Health != HealthStatus.Dead;
    public bool IsSick  => Health == HealthStatus.Sick;

    public void MakeSick() => Health = HealthStatus.Sick;
    public void Kill()     => Health = HealthStatus.Dead;
    public void Heal()     => Health = HealthStatus.Healthy;

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

        if (isSick) MakeSick();
    }

//alim
    public decimal GetDailyFoodNeedKg()
    {
        if (Sex == SexType.Female && IsGestating)
            return Profile.DailyFoodKg * GestatingFemaleFoodMultiplier;
        return Profile.DailyFoodKg;
    }

    public void ApplyDailyFeeding(decimal kgProvided) => Feed(kgProvided);

    public void Feed(decimal kgProvided)
    {
        if (!IsAlive) return;
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

//temps
    public bool AdvanceOneDay(Random random)
    {
        if (!IsAlive) return false;

        AgeDays++;
        ProgressArrivalReproductionBlockOneDay();

        if (AgeDays >= Profile.LifeExpectancyDays)
        {
            Kill();
            return true;
        }

        if (IsSick)
            return ProgressSicknessOneDay(random);

        return false;
    }

//maladie
    public void ContractSickness(Random random)
    {
        if (!IsAlive || IsSick) return;

        var baseDuration = Profile.SicknessDurationDays;
        var variance = (int)(baseDuration * (double)SicknessDurationVariance);
        var duration = baseDuration + random.Next(-variance, variance + 1);

        SicknessDaysRemaining = Math.Max(1, duration);
        MakeSick();
    }

    public bool ProgressSicknessOneDay(Random random)
    {
        if (!IsSick || SicknessDaysRemaining <= 0) return false;

        SicknessDaysRemaining--;

        if (SicknessDaysRemaining == 0)
        {
            if ((decimal)random.NextDouble() < SicknessDeathChance)
            {
                Kill();
                return true;
            }
            Heal();
        }

        return false;
    }

//reproduction
    public bool CanReproduceToday()
        => IsAlive && !IsHungry && !IsSick && !IsBlockedFromReproductionByArrival();

    public bool HasReachedSexualMaturity()
        => AgeDays >= Profile.SexualMaturityDays;

    public bool HasReachedReproductionEnd()
        => AgeDays >= Profile.ReproductionEndDays;

    public bool CanReproduceByAge()
        => HasReachedSexualMaturity() && !HasReachedReproductionEnd();

    public bool CanStartGestationToday()
        => Sex == SexType.Female
        && IsAlive && !IsHungry && !IsSick
        && CanReproduceByAge()
        && !IsGestating
        && Profile.GestationDays is > 0
        && !IsBlockedFromReproductionByArrival();

    public void StartGestation()
    {
        if (!CanStartGestationToday()) return;
        if (Profile.GestationDays is not int gestationDays || gestationDays <= 0) return;

        IsGestating = true;
        GestationRemainingDays = gestationDays;
    }

    public int ProgressGestationOneDay()
    {
        if (!IsGestating || GestationRemainingDays <= 0) return 0;

        if (IsHungry)
        {
            IsGestating = false;
            GestationRemainingDays = 0;
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
            && IsAlive && !IsHungry && !IsSick
            && CanReproduceByAge()
            && !IsBlockedFromReproductionByArrival();

        if (!canLay) return false;
        if (Profile.EggLayingMonth is int eggMonth) return month == eggMonth;
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

//arrive au zoo
    public void RegisterArrivalInZoo()
    {
        AdultArrivalReproductionBlockRemainingDays =
            HasReachedSexualMaturity() ? AdultArrivalReproductionBlockDays : 0;
    }

    public void ProgressArrivalReproductionBlockOneDay()
    {
        if (AdultArrivalReproductionBlockRemainingDays > 0)
            AdultArrivalReproductionBlockRemainingDays--;
    }

    public bool IsBlockedFromReproductionByArrival()
        => AdultArrivalReproductionBlockRemainingDays > 0;

    public bool IsExposedToPublic()
        => IsAlive && !IsGestating;
}
