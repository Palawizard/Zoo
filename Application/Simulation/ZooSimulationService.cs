using System;
using System.Collections.Generic;
using System.Linq;
using Zoo.Domain.Animals;
using Zoo.Domain.Feeding;
using Zoo.Domain.Finance;
using Zoo.Domain.Habitats;
using Zoo.Domain.Visitors;

namespace Zoo.Application.Simulation;

public sealed class ZooSimulationService
{
    private readonly List<ZooAnimal> _animals = new();
    private readonly AnimalMarket _animalMarket = new();
    private readonly FoodMarket _foodMerchant = new();
    private readonly List<Habitat> _habitats = new();
    private int _lastStockLossMonth = -1;
    private readonly VisitorRevenueCalculator _visitorRevenueCalculator = new();

    public IReadOnlyList<Habitat> Habitats => _habitats;

    public IReadOnlyList<ZooAnimal> Animals => _animals;

    public decimal MeatStockKg { get; private set; }
    public decimal SeedsStockKg { get; private set; }
    public decimal Cash { get; private set; }
    public Ledger Ledger { get; } = new();

    public int CurrentDayOfMonth { get; private set; } = 1;
    public int CurrentMonth {get; private set;} = 1;
    public int CurrentYear { get; private set; } = 1;
    public int TurnNumber { get; private set; }
    
    public bool IsHighSeason => CurrentMonth >= 5 && CurrentMonth <= 9;

    public ZooSimulationService(
        IEnumerable<ZooAnimal>? animals = null,
        decimal meatStockKg = 0m,
        decimal seedsStockKg = 0m,
        decimal cash = 80000m)
    {
        if (meatStockKg < 0m)
            throw new ArgumentOutOfRangeException(nameof(meatStockKg), "Stock cannot be negative.");
        if (seedsStockKg < 0m)
            throw new ArgumentOutOfRangeException(nameof(seedsStockKg), "Stock cannot be negative.");
        if (cash < 0m)
            throw new ArgumentOutOfRangeException(nameof(cash), "Cash cannot be negative.");

        MeatStockKg = meatStockKg;
        SeedsStockKg = seedsStockKg;
        Cash = cash;
        Ledger.Add(new Transaction(DateTime.UtcNow, cash, "Initial zoo budget", "Init", Cash));

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

    public bool BuyAnimal(ZooAnimal animal)
    {
        ArgumentNullException.ThrowIfNull(animal);

        var cost = _animalMarket.BuyAnimalPrice(animal.Species, animal.Sex, animal.AgeDays);
        if (!SpendCash(cost, $"Buy animal: {animal.Species}", "Animal")) return false;

        AddAnimal(animal);
        return true;
    }

    public bool SellAnimal(ZooAnimal animal)
    {
        ArgumentNullException.ThrowIfNull(animal);
        if (!_animals.Remove(animal)) return false;

        var revenue = _animalMarket.SellAnimalPrice(animal.Species, animal.Sex, animal.AgeDays);
        AddCash(revenue, $"Sell animal: {animal.Species}", "Animal");
        return true;
    }

    public bool BuyHabitat(SpeciesType species)
    {
        var habitat = HabitatFactory.Create(species);

        if (!SpendCash(habitat.BuyPrice, $"Buy habitat: {species}", "Habitat")) return false;

        _habitats.Add(habitat);
        return true;
    }

    public bool SellHabitat(Habitat habitat)
    {
        ArgumentNullException.ThrowIfNull(habitat);
        if (!_habitats.Remove(habitat)) return false;

        AddCash(habitat.SellPrice, $"Sell habitat: {habitat.Species}", "Habitat");
        return true;
    }

    public bool BuyFood(FoodType type, decimal kg)
    {
        if (kg < 0m)
            throw new ArgumentOutOfRangeException(nameof(kg), "Food amount cannot be negative.");
        if (kg == 0m) return true;

        var cost = _foodMerchant.Buy(type, kg);
        var label = type == FoodType.Meat ? "meat" : "seeds";

        if (!SpendCash(cost, $"Buy food: {label} ({kg:0.##} kg)", "Food")) return false;

        AddFood(type, kg);
        return true;
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

    //check stock
    public (decimal MeatKg, decimal SeedsKg) GetFoodStock()
    {
        return (MeatStockKg, SeedsStockKg);
    }

    //Perte du stock
    public void TryApplyMonthlyStockLoss(int dayOfMonth)
    {
        //securise la date du mois
        var daysInMonth = GetDaysInMonth(CurrentMonth);
        if (dayOfMonth < 1 || dayOfMonth > daysInMonth)
            throw new ArgumentOutOfRangeException(nameof(dayOfMonth), $"Day must be between 1 and {daysInMonth}.");

        //lance le 1er jour
        if (dayOfMonth != 1 || _lastStockLossMonth == CurrentMonth) return;

        _lastStockLossMonth = CurrentMonth;

        //20% de chance: perte de 10% des graines
        if (IsEventTriggered(0.20m))
            SeedsStockKg = ReduceByPercent(SeedsStockKg, 0.10m);

        //10% de chance: perte de 20% de la viande
        if (IsEventTriggered(0.10m))
            MeatStockKg = ReduceByPercent(MeatStockKg, 0.20m);
    }

    //calcule revenu visiteurs
    public IReadOnlyDictionary<SpeciesType, decimal> CalculateVisitorRevenueBySpecies(bool isHighSeason)
    {
        var exposedAnimals = GetAnimalsExposedToPublic();
        return _visitorRevenueCalculator.CalculateBySpecies(exposedAnimals, isHighSeason);
    }

    //ajoute l'argent
    public decimal CollectDailyVisitorRevenue(bool isHighSeason)
    {
        var revenueBySpecies = CalculateVisitorRevenueBySpecies(isHighSeason);
        var total = revenueBySpecies.Values.Sum();

        if (total > 0m)
        {
            var seasonLabel = isHighSeason ? "high" : "low";
            AddCash(total, $"Visitors income ({seasonLabel} season)", "Visitors");
        }

        return total;
    }

    public void ProcessDailyFeeding()
    {
        foreach (var animal in _animals.Where(a => a.IsAlive))
        {
            var requiredKg = animal.GetDailyFoodNeedKg();
            var providedKg = ConsumeFromStock(animal.Profile.FoodType, requiredKg);
            animal.ApplyDailyFeeding(providedKg);
            animal.AdvanceOneDay();
            animal.TryCatchDiseaseToday();
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
                a.CanReproduceByAge());

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

    //nb jours / mois
    private static int GetDaysInMonth(int month)
    {
        return month switch
        {
            1 => 31,
            2 => 28,
            3 => 31,
            4 => 30,
            5 => 31,
            6 => 30,
            7 => 31,
            8 => 31,
            9 => 30,
            10 => 31,
            11 => 30,
            12 => 31,
            _ => throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.")
        };
    }

    //retire un pourcentage d'une quantite
    private static decimal ReduceByPercent(decimal value, decimal percent)
    {
        if (value <= 0m) return 0m;
        if (percent <= 0m) return value;
        if (percent >= 1m) return 0m;

        return value * (1m - percent);
    }

    //fait un tirage aleatoire
    private static bool IsEventTriggered(decimal probability)
    {
        if (probability <= 0m) return false;
        if (probability >= 1m) return true;

        return (decimal)Random.Shared.NextDouble() < probability;
    }

    public IReadOnlyList<ZooAnimal> GetAnimalsExposedToPublic()
    {
        return _animals
            .Where(a => a.IsExposedToPublic())
            .ToList();
    }

    public void AddCash(decimal amount, string description = "Cash in", string category = "Income")
    {
        if (amount < 0m)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

        Cash += amount;
        Ledger.Add(new Transaction(DateTime.UtcNow, amount, description, category, Cash));
    }

    public bool SpendCash(decimal amount, string description = "Cash out", string category = "Expense")
    {
        if (amount < 0m)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

        if (Cash < amount) return false;

        Cash -= amount;
        Ledger.Add(new Transaction(DateTime.UtcNow, -amount, description, category, Cash));
        return true;
    }

    public void NextTurn()
    {
        ProcessDailyTurn();

        if (CurrentDayOfMonth == 1) ProcessMonthlyTurn();
        if (CurrentDayOfMonth == 1 && CurrentMonth == 1) ProcessYearlyTurn();

        AdvanceCalendar();
        TurnNumber++;
    }

    private void ProcessDailyTurn()
    {
        ProcessDailyFeeding();
        ProcessGestations();
        ProcessEggIncubations();
    }

    private void ProcessMonthlyTurn()
    {
        TryApplyMonthlyStockLoss(CurrentDayOfMonth);
        TryEggLayingForCurrentMonth();
        
        foreach (var habitat in _habitats) habitat.ProcessMonth(Random.Shared);

        CollectMonthlyVisitorRevenue();
    }

    private decimal CollectMonthlyVisitorRevenue()
    {
        return CollectDailyVisitorRevenue(IsHighSeason);
    }

    private void ProcessYearlyTurn()
    {
        var tigerCount = _animals.Count(a => a.IsAlive && a.Species == SpeciesType.Tiger);
        var eagleCount = _animals.Count(a => a.IsAlive && a.Species == SpeciesType.Eagle);

        var subsidy = (tigerCount * 43800m) + (eagleCount * 2190m);

        if (subsidy > 0m)
            AddCash(subsidy, "Protected species annual subsidy", "Subsidy");
    }

    private void AdvanceCalendar()
    {
        var daysInMonth = GetDaysInMonth(CurrentMonth);

        if (CurrentDayOfMonth < daysInMonth)
        {
            CurrentDayOfMonth++;
            return;
        }

        CurrentDayOfMonth = 1;

        if (CurrentMonth < 12)
        {
            CurrentMonth++;
            return;
        }
        
        CurrentMonth = 1;
        CurrentYear++;
    }
}   
