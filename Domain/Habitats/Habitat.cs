using System;
using System.Collections.Generic;
using System.Linq;
using Zoo.Domain.Animals;

namespace Zoo.Domain.Habitats;
/// Représente un habitat dédié à une espèce particulière et contenant des animaux.
public class Habitat
{
    ///Identifiant unique de l'habitat.
    public Guid Id { get; } = Guid.NewGuid();

    /// Profil de l'habitat (prix, capacité, etc.)
    public HabitatProfile Profile { get; }

    ///Espèce supportée par cet habitat.
    public SpeciesType Species { get; }

    /// Prix d'achat de l'habitat.
    public decimal BuyPrice { get; }

    /// Prix de revente de l'habitat
    public decimal SellPrice { get; }

    ///Capacité maximale en nombre d'animaux
    public int Capacity { get; }

    ///Nombre d'animaux susceptibles d'être perdus chaque mois.
    public int MonthlyLossCount { get; }

    ///Probabilité de perte par tirage pour chaque événement.
    public decimal LossProbability { get; }

    ///Collection des animaux présents dans l'habitat.
    public List<Animal> Animals { get; }
    // 
public bool CanReproduce() => Animals.Count >= 2 && AvailableSlots >= 1;
    /// Constructeur protégé : initialise l'habitat pour une espèce donnée.
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
            throw new InvalidOperationException($"Cannot add animal of species {animal.Species} to habitat for species {Species}.");

        if (Animals.Count >= Capacity)
            throw new InvalidOperationException($"Habitat for species {Species} is at full capacity.");

        Animals.Add(animal);
    }

    /// Retire un animal de l'habitat.
    public void RemoveAnimal(Animal animal)
    {
        Animals.Remove(animal);
    }
    /// Simule les pertes d'animaux sur un mois et retourne la liste des animaux perdus.
    public IReadOnlyList<Animal> ProcessMonth(Random random)
    {
        var lost = new List<Animal>();

        for (int i = 0; i < MonthlyLossCount; i++)
        {
            // sélectionne les animaux vivants
            var candidates = Animals.Where(a => a.Health != HealthStatus.Dead).ToList();
            if (candidates.Count == 0)
                break;

            if (random.NextDouble() < (double)LossProbability)
            {
                var victim = candidates[random.Next(candidates.Count)];
                victim.Kill();
                Animals.Remove(victim);
                lost.Add(victim);
            }
        }

        return lost;
    }
    public int AvailableSlots  => Capacity - Animals.Count;
    public bool IsFull         => Animals.Count >= Capacity;
    public bool IsEmpty        => Animals.Count == 0;

    public override string ToString() =>
        $"[{Species}] Achat: {BuyPrice}€ | Vente: {SellPrice}€ | " +
        $"Animaux: {Animals.Count}/{Capacity}";

    ///Ratio d'animaux en bonne santé
    public decimal HealthRatio =>
        Animals.Count == 0 ? 1m
        : (decimal)Animals.Count(a => a.Health == HealthStatus.Healthy) / Animals.Count;

}