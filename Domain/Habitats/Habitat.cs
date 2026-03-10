using System;
using System.Collections.Generic;
using System.Linq;
using Zoo.Domain.Animals;

namespace Zoo.Domain.Habitats;

/// Représente un habitat dédié à une espèce particulière et contenant des animaux.
public class Habitat
{
    /// Identifiant unique de l'habitat.
    public Guid Id { get; } = Guid.NewGuid();

    /// Profil de l'habitat (prix, capacité, etc.)
    public HabitatProfile Profile { get; }

    /// Espèce supportée par cet habitat.
    public SpeciesType Species { get; }

    /// Prix d'achat de l'habitat.
    public decimal BuyPrice { get; }

    /// Prix de revente de l'habitat.
    public decimal SellPrice { get; }

    /// Capacité maximale en nombre d'animaux.
    public int Capacity { get; }

    /// Nombre d'animaux susceptibles d'être perdus chaque mois.
    public int MonthlyLossCount { get; }

    /// Probabilité de perte par tirage pour chaque événement.
    public decimal LossProbability { get; }

    /// Collection des animaux présents dans l'habitat.
    public List<Animal> Animals { get; }

    /// Nombre de places disponibles.
    public int AvailableSlots => Capacity - Animals.Count;

    /// Indique si l'habitat est plein.
    public bool IsFull => Animals.Count >= Capacity;

    /// Indique si l'habitat est vide.
    public bool IsEmpty => Animals.Count == 0;

    /// Ratio d'animaux en bonne santé (0..1).
    public decimal HealthRatio =>
        Animals.Count == 0 ? 1m
        : (decimal)Animals.Count(a => a.Health == HealthStatus.Healthy) / Animals.Count;

    /// check reproduction
    public bool CanReproduce() => Animals.Count >= 2 && AvailableSlots >= 1;

    /// Constructeur
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

    /// Ajoute un animal à l'habitat si l'espèce correspond et s'il y a de la place.
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

    /// Retire un animal de l'habitat.
    public void RemoveAnimal(Animal animal)
    {
        Animals.Remove(animal);
    }

    /// Simule les événements mensuels : pertes naturelles puis surpopulation.
    public IReadOnlyList<Animal> ProcessMonth(Random random)
{
    var lost = new List<Animal>();

    //new maladies
    ProcessSickness(random); // les animaux malades bloquent la reproduction

    //perte naturelle
    for (int i = 0; i < MonthlyLossCount; i++)
    {
        var candidates = Animals.Where(a => a.IsAlive).ToList();
        if (candidates.Count == 0) break;

        if (random.NextDouble() < (double)LossProbability)
        {
            var victim = candidates[random.Next(candidates.Count)];
            victim.Kill();
            Animals.Remove(victim);
            lost.Add(victim);
        }
    }

    //surpopulation
    lost.AddRange(ProcessOverpopulation(random));

    return lost;
}

    /// Pour chaque animal en excès de capacité, 50% de chance qu'il meure.
    private IReadOnlyList<Animal> ProcessOverpopulation(Random random)
    {
        var lost = new List<Animal>();
        const double OverpopulationDeathChance = 0.5;

        while (Animals.Count > Capacity)
        {
            var excess = Animals.Where(a => a.IsAlive).ToList();
            if (excess.Count == 0) break;

            var victim = excess[random.Next(excess.Count)];

            if (random.NextDouble() < OverpopulationDeathChance)
            {
                victim.Kill();
                Animals.Remove(victim);
                lost.Add(victim);
            }
            else
            {
                break; // Survie ce mois, on retente le mois suivant
            }
        }

        return lost;
    }

    public override string ToString() =>
        $"[{Species}] Achat: {BuyPrice}€ | Vente: {SellPrice}€ | Animaux: {Animals.Count}/{Capacity}";
    
    /// Tire aléatoirement les nouvelles maladies ce mois (proba annuelle → mensuelle).
    private void ProcessSickness(Random random)
    {
        double monthlyChance = 1.0 - Math.Pow(1.0 - (double)LossProbability, 1.0 / 12.0);

        foreach (var animal in Animals.Where(a => a.IsAlive && !a.IsSick))
        {
            if (random.NextDouble() < monthlyChance)
                animal.ContractSickness(random);
        }
    }

}