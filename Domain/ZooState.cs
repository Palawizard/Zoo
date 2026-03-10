using Zoo.Domain.Animals;
using Zoo.Domain.Feeding;
using Zoo.Domain.Visitors;

namespace Zoo.Domain;

public sealed class ZooState
{
    public List<ZooAnimal> Animals { get; }
    public FoodStock FoodStock { get; }
    public VisitorStats VisitorStats { get; }
    public decimal Cash { get; private set; }

    public ZooState(
        IEnumerable<ZooAnimal>? animals = null,
        FoodStock? foodStock = null,
        VisitorStats? visitorStats = null,
        decimal cash = 0m)
    {
        if (cash < 0m) throw new ArgumentOutOfRangeException(nameof(cash));

        Animals = animals?.ToList() ?? new List<ZooAnimal>();
        FoodStock = foodStock ?? new FoodStock();
        VisitorStats = visitorStats ?? new VisitorStats();
        Cash = cash;
    }

    public void AddAnimal(ZooAnimal animal)
    {
        ArgumentNullException.ThrowIfNull(animal);
        Animals.Add(animal);
    }

    public bool RemoveAnimal(ZooAnimal animal) => Animals.Remove(animal);

    public void AddCash(decimal amount)
    {
        if (amount < 0m) throw new ArgumentOutOfRangeException(nameof(amount));
        Cash += amount;
    }

    public bool SpendCash(decimal amount)
    {
        if (amount < 0m) throw new ArgumentOutOfRangeException(nameof(amount));
        if (Cash < amount) return false;
        Cash -= amount;
        return true;
    }
}
