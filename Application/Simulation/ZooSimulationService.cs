using Zoo.Domain.Animals;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Zoo.Application.Simulation;

public sealed class ZooSimulationService
{
    private readonly List<ZooAnimal> _animals = new();

    public IReadOnlyList<ZooAnimal> Animals => _animals;

    public decimal MeatStockKg { get; private set; }
    public decimal SeedsStockKg { get; private set; }

    public int CurrentMonth {get; private set;} = 1;

    public ZooSimulationService(
        IEnumerable<ZooAnimal>? animals = null,
        decimal meatStockKg = 0m,
        decimal seedsStockKg = 0m)
    {
        if (meatStockKg < 0m)
            throw new ArgumentOutOfRangeException(nameof(meatStockKg), "Stock cannot be negative.");
        if (seedsStockKg < 0m)
            throw new ArgumentOutOfRangeException(nameof(seedsStockKg), "Stock cannot be negative.");

        MeatStockKg = meatStockKg;
        SeedsStockKg = seedsStockKg;

        if (animals is not null)
        {
            foreach(var animal in animals)
            {
                AddAnimal(animal);
            }
        }
    }

    public void AddAnimal(ZooAnimal animal)
    {
        ArgumentNullException.ThrowIfNull(animal);
        animal.RegisterArrivalInZoo();
        _animals.Add(animal);
    }

    public void AddFood(FoodType type, decimal kg)
    {
        if (kg < 0m)
            throw new ArgumentOutOfRangeException(nameof(kg), "Food amount cannot be negative.");

        if (type == FoodType.Meat)
            MeatStockKg += kg;
        else
            SeedsStockKg += kg;
    }

    public void ProcessDailyFeeding()
    {
        foreach (var animal in _animals.Where(a => a.IsAlive))
        {
            var requiredKg = animal.GetDailyFoodNeedKg();
            var providedKg = ConsumeFromStock(animal.Profile.FoodType, requiredKg);
            animal.ApplyDailyFeeding(providedKg);
            animal.AdvanceOneDay(Random.Shared);
        }
    }

    public bool CanReproduceToday(Animal animal)
    {
        ArgumentNullException.ThrowIfNull(animal);
        return animal.CanReproduceToday();
    }

    public bool CanReproduceToday(Animal first, Animal second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        return first.CanReproduceToday() && second.CanReproduceToday();
    }

    private decimal ConsumeFromStock(FoodType type, decimal requestedKg)
    {
        if (requestedKg <= 0m)
            return 0m;

        if (type == FoodType.Meat)
        {
            var consumed = Math.Min(MeatStockKg, requestedKg);
            MeatStockKg -= consumed;
            return consumed;
        }

        var seedConsumed = Math.Min(SeedsStockKg, requestedKg);
        SeedsStockKg -= seedConsumed;
        return seedConsumed;
    }

    public void SetCurrentMonth(int month)
    {
        if (month < 1 || month > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");

        CurrentMonth = month;
    }

public void ProcessGestations()
{
    var newborns = new List<ZooAnimal>();

    foreach (var female in _animals.Where(a => a.IsAlive && a.Sex == SexType.Female && a.IsGestating))
        {
            var bornCount = female.ProgressGestationOneDay();
            if (bornCount <= 0) continue;

            newborns.AddRange(CreateOffspringBatch(female.Species,bornCount,female.Profile.InfantMortalityRate));
        }

        if (newborns.Count > 0)
            _animals.AddRange(newborns);
}

public void ProcessEggIncubations()
{
    var newborns = new List<ZooAnimal>();

    foreach (var female in _animals.Where(a => a.IsAlive && a.Sex == SexType.Female && a.EggIncubationRemainingDays > 0))
        {
            var hatchedCount = female.ProgressEggIncubationOneDay();
            if (hatchedCount <= 0) continue;

            newborns.AddRange(CreateOffspringBatch(female.Species, hatchedCount, female.Profile.InfantMortalityRate));
        }

        if (newborns.Count > 0)
            _animals.AddRange(newborns);
}

public void TryStartPregnancies()
{
    var aliveBySpecies = _animals
        .Where(a => a.IsAlive)
        .GroupBy(a => a.Species);

    foreach (var speciesGroup in aliveBySpecies)
        {
            var hasEligibleMale = speciesGroup.Any(a =>
                a.Sex == SexType.Male &&
                a.CanReproduceToday() &&
                a.CanReproduceByAge() &&
                !a.IsSick);

            if (!hasEligibleMale) continue;

            foreach (var female in speciesGroup.Where(a => a.Sex == SexType.Female))
            {
                if (female.CanStartGestationToday())
                    female.StartGestation();
            }
        }
}

public void TryEggLayingForCurrentMonth()
{
    foreach (var female in _animals.Where(a => a.IsAlive && a.Sex == SexType.Female))
        {
            if (!female.CanLayEggThisMonth(CurrentMonth)) continue;

            var eggsToIncubate = GetEggCountForMonth(female, CurrentMonth);
            if (eggsToIncubate <= 0) continue;

            female.StartEggIncubation(eggsToIncubate, CurrentMonth);
        }
}

    private static IEnumerable<ZooAnimal> CreateOffspringBatch(SpeciesType species, int count, decimal? infantMortalityRate)
        {
            var survivorCount = ComputeSurvivorsAfterInfantMortality(count, infantMortalityRate);

            var newborns = new List<ZooAnimal>(survivorCount);

            for (var i = 0; i<survivorCount; i++)
            {
                var sex = Random.Shared.Next(0,2) == 0 ? SexType.Male : SexType.Female;
                var name = $"{species}_{Guid.NewGuid():N}";
                newborns.Add(new ZooAnimal(name, sex, species, ageDays : 0, isHungry: false, isSick: false));

            }
            return newborns;
        }

    private static int ComputeSurvivorsAfterInfantMortality(int newbornCount, decimal? infantMortalityRate)
    {
        if (newbornCount <= 0) return 0;

        var rate = NormalizeInfantMortalityRate(infantMortalityRate);

        if (rate <= 0m) return newbornCount;
        if (rate >= 1m) return 0;

        var survivors = 0;

        for (var i = 0; i < newbornCount; i++)
        {
            var roll = (decimal)Random.Shared.NextDouble();

            if (roll >= rate) survivors++;
        }

        return survivors;
    }

    private static decimal NormalizeInfantMortalityRate(decimal? infantMortalityRate)
    {
        if (!infantMortalityRate.HasValue) return 0m;

        return Math.Clamp(infantMortalityRate.Value, 0m, 1m);
    }

    private static int GetEggCountForMonth(Animal female, int month)
    {
        if (female.Profile.EggLayingMonth is int layingMonth &&
            layingMonth == month &&
            female.Profile.LitterSize is int litterSize &&
            litterSize > 0)
        {
            return litterSize;
        }

        if (female.Profile.EggsPerYear is int eggsPerYear && eggsPerYear > 0)
        {
            return Math.Max(1, (int)Math.Round(eggsPerYear / 12.0, MidpointRounding.AwayFromZero));
        }
        
        return 0;
    }

    public IReadOnlyList<ZooAnimal> GetAnimalsExposedToPublic()
    {
        return _animals
            .Where(a => a.IsExposedToPublic())
            .ToList();
    }

}