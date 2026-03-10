using System;
using System.Collections.Generic;
using System.Linq;
using Zoo.Domain;
using Zoo.Domain.Animals;
using Zoo.Domain.Feeding;
using Zoo.Domain.Finance;
using Zoo.Domain.Visitors;

namespace Zoo.Application.Simulation;

public sealed class ZooSimulationService
{
    private readonly List<ZooAnimal> _animals = new();
    private readonly ZooState _state;
    private readonly FoodMarket _foodMarket = new();
    private readonly AnimalMarket _animalMarket = new();
    private readonly SubsidyPolicy _subsidyPolicy = new();
    private readonly VisitorPricing _visitorPricing;

    public IReadOnlyList<ZooAnimal> Animals => _animals;

    public decimal MeatStockKg => _state.FoodStock.MeatKg;
    public decimal SeedsStockKg => _state.FoodStock.SeedsKg;
    public decimal Cash => _state.Cash;

    public int CurrentMonth {get; private set;} = 1;

    public ZooSimulationService(
        IEnumerable<ZooAnimal>? animals = null,
        decimal meatStockKg = 0m,
        decimal seedsStockKg = 0m,
        decimal cash = 80000m,
        VisitorPricing? visitorPricing = null)
    {
        if (meatStockKg < 0m)
            throw new ArgumentOutOfRangeException(nameof(meatStockKg), "Stock cannot be negative.");
        if (seedsStockKg < 0m)
            throw new ArgumentOutOfRangeException(nameof(seedsStockKg), "Stock cannot be negative.");
        if (cash < 0m)
            throw new ArgumentOutOfRangeException(nameof(cash), "Cash cannot be negative.");

        _state = new ZooState(
            animals: Enumerable.Empty<ZooAnimal>(),
            foodStock: new FoodStock(meatStockKg, seedsStockKg),
            visitorStats: new VisitorStats(),
            cash: cash);

        _visitorPricing = visitorPricing ?? new VisitorPricing(15m, 8m);

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
        _state.AddAnimal(animal);
    }

    public bool BuyAnimal(ZooAnimal animal)
    {
        ArgumentNullException.ThrowIfNull(animal);

        var cost = _animalMarket.BuyAnimalPrice(animal.Species, animal.AgeDays);
        if (!_state.SpendCash(cost, $"Buy animal: {animal.Species}", "Animal")) return false;

        AddAnimal(animal);
        return true;
    }

    public bool SellAnimal(ZooAnimal animal)
    {
        ArgumentNullException.ThrowIfNull(animal);
        if (!_animals.Remove(animal)) return false;

        _state.RemoveAnimal(animal);
        var revenue = _animalMarket.SellAnimalPrice(animal.Species, animal.AgeDays);
        _state.AddCash(revenue, $"Sell animal: {animal.Species}", "Animal");
        return true;
    }

    public void AddFood(FoodType type, decimal kg)
    {
        _state.FoodStock.Add(type, kg);
    }

    public bool BuyFood(FoodType type, decimal kg)
    {
        if (kg < 0m)
            throw new ArgumentOutOfRangeException(nameof(kg), "Food amount cannot be negative.");

        var cost = _foodMarket.Buy(type, kg);
        if (!_state.SpendCash(cost, $"Buy food: {type} {kg}kg", "Food")) return false;

        _state.FoodStock.Add(type, kg);
        return true;
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
            var consumed = Math.Min(_state.FoodStock.MeatKg, requestedKg);
            _state.FoodStock.Consume(type, consumed);
            return consumed;
        }

        var seedConsumed = Math.Min(_state.FoodStock.SeedsKg, requestedKg);
        _state.FoodStock.Consume(type, seedConsumed);
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

    public decimal CollectMonthlyVisitorRevenue()
    {
        _state.VisitorStats.ExposedAnimalsCount = GetAnimalsExposedToPublic().Count;
        _state.VisitorStats.ComputeVisitors();
        var revenue = _state.VisitorStats.ComputeRevenue(_visitorPricing);
        _state.AddCash(revenue, "Visitor revenue", "Visitors");
        return revenue;
    }

    public decimal ApplyAnnualSubsidies()
    {
        return _subsidyPolicy.ApplyAnnualSubsidies(_state);
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
