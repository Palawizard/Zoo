using System;
using System.Collections.Generic;
using System.Linq;
using Zoo.Domain.Animals;

namespace Zoo.Domain.Habitats;

/// <summary>
/// Représente un habitat dédié à une espèce particulière et contenant des animaux.
/// </summary>
public class Habitat
{
    /// <summary>Identifiant unique de l'habitat.</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>Profil de l'habitat (prix, capacité, etc.).</summary>
    public HabitatProfile Profile { get; }

    /// <summary>Espèce supportée par cet habitat.</summary>
    public SpeciesType Species { get; }

    /// <summary>Prix d'achat de l'habitat.</summary>
    public decimal BuyPrice { get; }

    /// <summary>Prix de revente de l'habitat.</summary>
    public decimal SellPrice { get; }

    /// <summary>Capacité maximale en nombre d'animaux.</summary>
    public int Capacity { get; }

    /// <summary>Nombre d'animaux susceptibles d'être perdus chaque mois.</summary>
    public int MonthlyLossCount { get; }

    /// <summary>Probabilité de perte par tirage pour chaque événement.</summary>
    public decimal LossProbability { get; }

    /// <summary>Collection des animaux présents dans l'habitat.</summary>
    public List<Animal> Animals { get; }

    /// <summary>
    /// Constructeur protégé : initialise l'habitat pour une espèce donnée.
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
    /// Ajoute un animal à l'habitat si l'espèce correspond et s'il y a de la place.
    /// </summary>
    /// <exception cref="InvalidOperationException">Si l'espèce ne correspond pas ou si l'habitat est plein.</exception>
    public void AddAnimal(Animal animal)
    {
        if (animal.Species != Species)
            throw new InvalidOperationException($"Cannot add animal of species {animal.Species} to habitat for species {Species}.");

        if (Animals.Count >= Capacity)
            throw new InvalidOperationException($"Habitat for species {Species} is at full capacity.");

        Animals.Add(animal);
    }

    /// <summary>
    /// Retire un animal de l'habitat.
    /// </summary>
    public void RemoveAnimal(Animal animal)
    {
        Animals.Remove(animal);
    }

    /// <summary>
    /// Simule les pertes d'animaux sur un mois et retourne la liste des animaux perdus.
    /// </summary>
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

    /// <summary>Nombre de places disponibles.</summary>
    public int AvailableSlots => Capacity - Animals.Count;

    /// <summary>Indique si l'habitat est plein.</summary>
    public bool IsFull => Animals.Count >= Capacity;

    /// <summary>Indique si l'habitat est vide.</summary>
    public bool IsEmpty => Animals.Count == 0;

    /// <summary>Ratio d'animaux en bonne santé (0..1).</summary>
    public decimal HealthRatio =>
        Animals.Count == 0 ? 1m
        : (decimal)Animals.Count(a => a.Health == HealthStatus.Healthy) / Animals.Count;
}