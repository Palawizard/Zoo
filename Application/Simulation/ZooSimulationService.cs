using System;
using System.Collections.Generic;
using System.Linq;
using Zoo.Domain.Animals;
using Zoo.Domain.Events;
using Zoo.Domain.Feeding;
using Zoo.Domain.Finance;
using Zoo.Domain.Habitats;
using Zoo.Domain.Visitors;

namespace Zoo.Application.Simulation;

/// <summary>
/// Main application service that drives the zoo simulation
/// </summary>
public sealed partial class ZooSimulationService
{
    private const decimal DeadAnimalSaleMultiplier = 0.05m;

    private readonly List<ZooAnimal> _animals = new();
    private readonly AnimalMarket _animalMarket = new();
    private readonly FoodMarket _foodMerchant = new();
    private readonly List<Habitat> _habitats = new();
    private readonly List<ZooEvent> _events = new();
    private readonly Dictionary<Guid, Guid> _monogamousPairs = new();
    private readonly Queue<Guid> _pendingNewbornNaming = new();
    private readonly VisitorRevenueCalculator _visitorRevenueCalculator = new();
    private readonly bool _interactiveHabitatEmergencies;
    private int _lastExceptionalEventsMonth = -1;
    private bool _pendingTurnAwaitingCompletion;
    private bool _pendingTurnRequiresYearlyProcessing;

    public IReadOnlyList<Habitat> Habitats => _habitats;
    public IReadOnlyList<ZooAnimal> Animals => _animals;
    public IReadOnlyList<ZooEvent> Events => _events;
    public PendingHabitatEmergency? PendingHabitatEmergency { get; private set; }

    public decimal MeatStockKg { get; private set; }
    public decimal SeedsStockKg { get; private set; }
    public decimal Cash { get; private set; }
    public Ledger Ledger { get; } = new();

    public int CurrentDayOfMonth { get; private set; } = 1;
    public int CurrentMonth { get; private set; } = 1;
    public int CurrentYear { get; private set; } = 1;
    public int TurnNumber { get; private set; }

    public bool IsHighSeason => CurrentMonth >= 5 && CurrentMonth <= 9;

    /// <summary>
    /// Creates a simulation with optional starting state
    /// </summary>
    public ZooSimulationService(
        IEnumerable<ZooAnimal>? animals = null,
        decimal meatStockKg = 0m,
        decimal seedsStockKg = 0m,
        decimal cash = 80000m,
        bool interactiveHabitatEmergencies = false)
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
        _interactiveHabitatEmergencies = interactiveHabitatEmergencies;

        Ledger.Add(new Transaction(DateTime.UtcNow, cash, "Initial zoo budget", "Init", Cash));
        AddEvent(
            ZooEventType.SimulationInitialized,
            $"Simulation initialized with {Cash:0.##}€ cash, {MeatStockKg:0.##} kg meat and {SeedsStockKg:0.##} kg seeds.");

        if (animals is null)
            return;

        foreach (var animal in animals)
            AddAnimal(animal);
    }

    /// <summary>
    /// Adds an animal to the simulation
    /// </summary>
    public void AddAnimal(ZooAnimal animal)
    {
        ArgumentNullException.ThrowIfNull(animal);

        animal.RegisterArrivalInZoo();
        _animals.Add(animal);
    }

    /// <summary>
    /// Buys an animal and adds it to the zoo if enough cash is available
    /// </summary>
    public bool BuyAnimal(ZooAnimal animal)
    {
        ArgumentNullException.ThrowIfNull(animal);

        var cost = _animalMarket.BuyAnimalPrice(animal.Species, animal.Sex, animal.AgeDays);
        if (!SpendCash(cost, $"Buy animal: {animal.Species}", "Animal"))
            return false;

        AddAnimal(animal);
        AddEvent(
            ZooEventType.AnimalPurchased,
            $"{animal.Name} ({animal.Species}) was bought for {cost:0.##}€.");
        return true;
    }

    /// <summary>
    /// Sells an animal currently owned by the zoo
    /// </summary>
    public bool SellAnimal(ZooAnimal animal)
    {
        ArgumentNullException.ThrowIfNull(animal);
        if (!_animals.Contains(animal))
            return false;

        var revenue = EstimateAnimalSalePrice(animal);
        RemoveAnimalFromZoo(animal);

        AddCash(revenue, $"Sell animal: {animal.Species}", "Animal");
        AddEvent(
            ZooEventType.AnimalSold,
            $"{animal.Name} ({animal.Species}) was sold for {revenue:0.##}€.");
        return true;
    }

    /// <summary>
    /// Estimates the sale price of an animal
    /// </summary>
    public decimal EstimateAnimalSalePrice(ZooAnimal animal)
    {
        ArgumentNullException.ThrowIfNull(animal);

        var basePrice = _animalMarket.SellAnimalPrice(animal.Species, animal.Sex, animal.AgeDays);
        return animal.IsAlive ? basePrice : decimal.Round(basePrice * DeadAnimalSaleMultiplier, 2);
    }

    /// <summary>
    /// Buys a new habitat for the given species
    /// </summary>
    public bool BuyHabitat(SpeciesType species)
    {
        var habitat = HabitatFactory.Create(species);

        if (!SpendCash(habitat.BuyPrice, $"Buy habitat: {species}", "Habitat"))
            return false;

        _habitats.Add(habitat);
        AddEvent(
            ZooEventType.HabitatPurchased,
            $"{species} habitat bought for {habitat.BuyPrice:0.##}€.");
        return true;
    }

    /// <summary>
    /// Sells an empty habitat
    /// </summary>
    public bool SellHabitat(Habitat habitat)
    {
        ArgumentNullException.ThrowIfNull(habitat);
        if (habitat.Animals.Count > 0)
            return false;
        if (!_habitats.Remove(habitat))
            return false;

        AddCash(habitat.SellPrice, $"Sell habitat: {habitat.Species}", "Habitat");
        AddEvent(
            ZooEventType.HabitatSold,
            $"{habitat.Species} habitat sold for {habitat.SellPrice:0.##}€.");
        return true;
    }

    /// <summary>
    /// Buys food and adds it to the stock
    /// </summary>
    public bool BuyFood(FoodType type, decimal kg)
    {
        if (kg < 0m)
            throw new ArgumentOutOfRangeException(nameof(kg), "Food amount cannot be negative.");
        if (kg == 0m)
            return true;

        var cost = _foodMerchant.Buy(type, kg);
        var label = type == FoodType.Meat ? "meat" : "seeds";

        if (!SpendCash(cost, $"Buy food: {label} ({kg:0.##} kg)", "Food"))
            return false;

        AddFood(type, kg);
        AddEvent(
            ZooEventType.FoodPurchased,
            $"{kg:0.##} kg of {label} bought for {cost:0.##}€.");
        return true;
    }

    /// <summary>
    /// Adds food directly to the stock
    /// </summary>
    public void AddFood(FoodType type, decimal kg)
    {
        if (kg < 0m)
            throw new ArgumentOutOfRangeException(nameof(kg), "Food amount cannot be negative.");

        if (type == FoodType.Meat)
            MeatStockKg += kg;
        else
            SeedsStockKg += kg;
    }

    /// <summary>
    /// Returns the current food stock
    /// </summary>
    public (decimal MeatKg, decimal SeedsKg) GetFoodStock()
    {
        return (MeatStockKg, SeedsStockKg);
    }

    /// <summary>
    /// Calculates projected visitor revenue by species
    /// </summary>
    public IReadOnlyDictionary<SpeciesType, decimal> CalculateVisitorRevenueBySpecies(bool isHighSeason)
    {
        var exposedAnimals = GetAnimalsExposedToPublic();
        return _visitorRevenueCalculator.CalculateBySpecies(exposedAnimals, isHighSeason);
    }

    /// <summary>
    /// Collects the visitor revenue for the current season
    /// </summary>
    public decimal CollectVisitorRevenue(bool isHighSeason)
    {
        var revenueBySpecies = CalculateVisitorRevenueBySpecies(isHighSeason);
        var total = revenueBySpecies.Values.Sum();

        if (total > 0m)
        {
            // Visitor revenue is logged only when the amount is positive
            var seasonLabel = isHighSeason ? "high" : "low";
            AddCash(total, $"Visitors income ({seasonLabel} season)", "Visitors");
            AddEvent(
                ZooEventType.VisitorIncome,
                $"Visitors generated {total:0.##}€ during {seasonLabel} season.");
        }

        return total;
    }

    /// <summary>
    /// Overrides the current month
    /// </summary>
    public void SetCurrentMonth(int month)
    {
        if (month < 1 || month > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");

        CurrentMonth = month;
    }

    /// <summary>
    /// Returns the animals currently visible to visitors
    /// </summary>
    public IReadOnlyList<ZooAnimal> GetAnimalsExposedToPublic()
    {
        var housedAnimals = _habitats
            .SelectMany(h => h.Animals)
            .OfType<ZooAnimal>()
            .Distinct();

        return housedAnimals
            .Where(animal => animal.IsExposedToPublic())
            .ToList();
    }

    /// <summary>
    /// Adds cash and records it in the ledger
    /// </summary>
    public void AddCash(decimal amount, string description = "Cash in", string category = "Income")
    {
        if (amount < 0m)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

        Cash += amount;
        Ledger.Add(new Transaction(DateTime.UtcNow, amount, description, category, Cash));
    }

    /// <summary>
    /// Spends cash if enough balance is available
    /// </summary>
    public bool SpendCash(decimal amount, string description = "Cash out", string category = "Expense")
    {
        if (amount < 0m)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");
        if (Cash < amount)
            return false;

        Cash -= amount;
        Ledger.Add(new Transaction(DateTime.UtcNow, -amount, description, category, Cash));
        return true;
    }

    // Stock consumption never goes below zero
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

        var consumedSeeds = Math.Min(SeedsStockKg, requestedKg);
        SeedsStockKg -= consumedSeeds;
        return consumedSeeds;
    }

    // Selling or removing an animal must also clean pairings and habitat occupancy
    private void RemoveAnimalFromZoo(ZooAnimal animal)
    {
        ArgumentNullException.ThrowIfNull(animal);

        RemovePairing(animal.Id);
        _animals.Remove(animal);
        RemoveAnimalFromHabitats(animal);
    }

    // One animal can only be in one habitat at a time
    private void RemoveAnimalFromHabitats(ZooAnimal animal)
    {
        foreach (var habitat in _habitats)
            habitat.RemoveAnimal(animal);
    }

    // New animals are placed in the habitat with the most room left
    private bool TryPlaceAnimalInHabitat(ZooAnimal animal)
    {
        var habitat = _habitats
            .Where(h => h.Species == animal.Species && h.AvailableSlots > 0)
            .OrderByDescending(h => h.AvailableSlots)
            .FirstOrDefault();

        // Placement can fail when the zoo owns no suitable free habitat
        if (habitat is null)
            return false;

        habitat.AddAnimal(animal);
        return true;
    }

    // This sums the free slots across all habitats of one species
    private int GetAvailableHabitatSlots(SpeciesType species)
    {
        return _habitats
            .Where(habitat => habitat.Species == species)
            .Sum(habitat => habitat.AvailableSlots);
    }

    // Events always use the current simulation date
    private void AddEvent(ZooEventType type, string description)
    {
        _events.Add(new ZooEvent(
            TurnNumber,
            CurrentYear,
            CurrentMonth,
            CurrentDayOfMonth,
            type,
            description));
    }
}
