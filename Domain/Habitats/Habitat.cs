using System;
using System.Collections.Generic;
using System.Linq;
using Zoo.Domain.Animals;

namespace Zoo.Domain.Habitats;

///Habitat
public class Habitat
{
    /// id unique de l'habitat.
    public Guid Id { get; } = Guid.NewGuid();

    /// Profil de l'habitat
    public HabitatProfile Profile { get; }

    /// Espèce supportée par habitat.
    public SpeciesType Species { get; }

    /// Prix d'achat
    public decimal BuyPrice { get; }

    /// Prix de revente
    public decimal SellPrice { get; }

    /// Capacité maximale
    public int Capacity { get; }

    /// Nombre d'animaux  pouvant etre perdu
    public int MonthlyLossCount { get; }

    /// Probabilité de perte par event
    public decimal LossProbability { get; }

    /// Collection des animaux
    public List<Animal> Animals { get; }

    /// Nombre de places dispo
    public int AvailableSlots => Capacity - Animals.Count;

    /// habitat plein
    public bool IsFull => Animals.Count >= Capacity;

    /// habiat vide
    public bool IsEmpty => Animals.Count == 0;

    /// animaux en bonen santé
    public decimal HealthRatio =>
        Animals.Count == 0 ? 1m
        : (decimal)Animals.Count(a => a.Health == HealthStatus.Healthy) / Animals.Count;

    /// check si reproduction est possible
    public bool CanReproduce() => Animals.Count >= 2 && AvailableSlots >= 1;

    /// Constructeur protégé : initialise l'habitat
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

    /// Ajoute un animal à l'habitat
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

    /// Retire un animal
    public void RemoveAnimal(Animal animal)
    {
        Animals.Remove(animal);
    }

    /// Simule les événements mensuels : pertes naturelles puis surpopulation.
    public HabitatMonthlyOutcome ProcessMonth(Random random)
    {
        var newlySickAnimals = ProcessSickness(random);
        var overpopulationLosses = ProcessOverpopulation(random);

        return new HabitatMonthlyOutcome(Array.Empty<Animal>(), overpopulationLosses, newlySickAnimals);
    }

    /// Pour chaque animal en excès de capacité, 50% de chance qu'il meure.
    private IReadOnlyList<Animal> ProcessOverpopulation(Random random)
    {
        var lost = new List<Animal>();
        const double OverpopulationDeathChance = 0.5;

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
            Animals.Remove(victim);
            lost.Add(victim);
        }

        return lost;
    }

    public override string ToString() =>
        $"[{Species}] Achat: {BuyPrice}€ | Vente: {SellPrice}€ | Animaux: {Animals.Count}/{Capacity}";
    
    /// Tire aléatoirement les nouvelles maladies ce mois (proba annuelle → mensuelle).
    private IReadOnlyList<Animal> ProcessSickness(Random random)
    {
        double monthlyChance = 1.0 - Math.Pow(1.0 - (double)LossProbability, 1.0 / 12.0);
        var newlySickAnimals = new List<Animal>();

        foreach (var animal in Animals.Where(a => a.IsAlive && !a.IsSick))
        {
            if (random.NextDouble() < monthlyChance)
            {
                if (animal.ContractSickness(random))
                    newlySickAnimals.Add(animal);
            }
        }

        return newlySickAnimals;
    }

}
