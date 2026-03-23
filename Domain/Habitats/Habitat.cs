using System;
using System.Collections.Generic;
using System.Linq;
using Zoo.Domain.Animals;

namespace Zoo.Domain.Habitats;

/// <summary>
/// Represents a habitat that can host animals of one species
/// </summary>
public class Habitat
{
    /// <summary>
    /// Unique identifier of the habitat
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Fixed profile used to configure this habitat
    /// </summary>
    public HabitatProfile Profile { get; }

    /// <summary>
    /// Species accepted by this habitat
    /// </summary>
    public SpeciesType Species { get; }

    /// <summary>
    /// Purchase price of the habitat
    /// </summary>
    public decimal BuyPrice { get; }

    /// <summary>
    /// Resale price of the habitat
    /// </summary>
    public decimal SellPrice { get; }

    /// <summary>
    /// Maximum number of animals supported by the habitat
    /// </summary>
    public int Capacity { get; }

    /// <summary>
    /// Maximum number of monthly overpopulation losses to evaluate
    /// </summary>
    public int MonthlyLossCount { get; }

    /// <summary>
    /// Base probability used for habitat monthly sickness rolls
    /// </summary>
    public decimal LossProbability { get; }

    /// <summary>
    /// Animals currently living in the habitat
    /// </summary>
    public List<Animal> Animals { get; }

    /// <summary>
    /// Free slots left in the habitat
    /// </summary>
    public int AvailableSlots => Capacity - Animals.Count;

    /// <summary>
    /// Returns whether the habitat is full
    /// </summary>
    public bool IsFull => Animals.Count >= Capacity;

    /// <summary>
    /// Returns whether the habitat is empty
    /// </summary>
    public bool IsEmpty => Animals.Count == 0;

    /// <summary>
    /// Ratio of healthy animals currently in the habitat
    /// </summary>
    public decimal HealthRatio =>
        Animals.Count == 0 ? 1m
        : (decimal)Animals.Count(a => a.Health == HealthStatus.Healthy) / Animals.Count;

    /// <summary>
    /// Returns whether the habitat has enough animals and space for reproduction
    /// </summary>
    public bool CanReproduce() => Animals.Count >= 2 && AvailableSlots >= 1;

    /// <summary>
    /// Creates a habitat for one species
    /// </summary>
    protected Habitat(SpeciesType species)
    {
        Profile = HabitatProfileCatalog.Get(species);
        Species = species;
        BuyPrice = Profile.BuyPrice;
        SellPrice = Profile.SellPrice;
        Capacity = Profile.Capacity;
        MonthlyLossCount = Profile.MonthlyLossCount;
        LossProbability = Profile.LossProbability;
        Animals = new List<Animal>();
    }

    /// <summary>
    /// Adds an animal to the habitat
    /// </summary>
    public void AddAnimal(Animal animal)
    {
        if (animal.Species != Species)
            throw new InvalidOperationException(
                $"Cannot add animal of species {animal.Species} to habitat for species {Species}.");

        if (Animals.Count >= Capacity)
            throw new InvalidOperationException(
                $"Habitat for species {Species} is at full capacity.");

        Animals.Add(animal);
    }

    /// <summary>
    /// Removes an animal from the habitat
    /// </summary>
    public void RemoveAnimal(Animal animal)
    {
        Animals.Remove(animal);
    }

    /// <summary>
    /// Processes the monthly habitat effects
    /// </summary>
    public HabitatMonthlyOutcome ProcessMonth(Random random)
    {
        var newlySickAnimals = ProcessSickness(random);
        var overpopulationLosses = ProcessOverpopulation(random);

        // Natural losses are not modeled yet, so this list stays empty for now
        return new HabitatMonthlyOutcome(Array.Empty<Animal>(), overpopulationLosses, newlySickAnimals);
    }

    /// <summary>
    /// Returns a short readable summary of the habitat
    /// </summary>
    public override string ToString() =>
        $"[{Species}] Achat: {BuyPrice}€ | Vente: {SellPrice}€ | Animaux: {Animals.Count}/{Capacity}";
    
    // Each monthly attempt can kill one living animal while the habitat stays over capacity
    private IReadOnlyList<Animal> ProcessOverpopulation(Random random)
    {
        var lost = new List<Animal>();
        const double OverpopulationDeathChance = 0.5;

        // Nothing happens if the habitat is still inside its capacity
        if (Animals.Count <= Capacity)
            return lost;

        for (var attempt = 0; attempt < MonthlyLossCount && Animals.Count > Capacity; attempt++)
        {
            var candidates = Animals.Where(a => a.IsAlive).ToList();
            if (candidates.Count == 0)
                break;

            if (random.NextDouble() >= OverpopulationDeathChance)
                continue;

            var victim = candidates[random.Next(candidates.Count)];
            victim.Kill();
            lost.Add(victim);
        }

        return lost;
    }
    
    // The yearly probability is converted to a lighter monthly roll
    private IReadOnlyList<Animal> ProcessSickness(Random random)
    {
        double monthlyChance = 1.0 - Math.Pow(1.0 - (double)LossProbability, 1.0 / 12.0);
        var newlySickAnimals = new List<Animal>();

        foreach (var animal in Animals.Where(a => a.IsAlive && !a.IsSick))
        {
            // Each healthy animal gets its own monthly sickness roll
            if (random.NextDouble() < monthlyChance)
            {
                if (animal.ContractSickness(random))
                    newlySickAnimals.Add(animal);
            }
        }

        return newlySickAnimals;
    }
}
