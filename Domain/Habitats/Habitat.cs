using System.Net.Quic;
using Zoo.Domain.Animals;

namespace Zoo.Domain.Habitats;

public class Habitat
{
    public Guid Id { get; } = Guid.NewGuid();
    public HabitatProfile Profile { get; }
    public SpeciesType Species { get; }
    public decimal BuyPrice {get; }
    public decimal SellPrice {get; }
    public int Capacity {get; }
    public int MonthlyLossCount {get; }
    public decimal LossProbability {get; }
    public List<Animal> Animals { get; }

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
    public void AddAnimal(Animal animal)
    {
        if (animal.Species != Species)
            throw new InvalidOperationException($"Cannot add animal of species {animal.Species} to habitat for species {Species}.");

        if (Animals.Count >= Capacity)
            throw new InvalidOperationException($"Habitat for species {Species} is at full capacity.");

        Animals.Add(animal);
    }
    public void RemoveAnimal(Animal animal)
    {
        Animals.Remove(animal);
    }
}