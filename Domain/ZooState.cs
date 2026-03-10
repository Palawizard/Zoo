using Zoo.Domain.Animals;
using Zoo.Domain.Feeding;
using Zoo.Domain.Finance;
using Zoo.Domain.Visitors;

namespace Zoo.Domain;

public sealed class ZooState
{
    public List<ZooAnimal> Animals { get; }
    public FoodStock FoodStock { get; }
    public VisitorStats VisitorStats { get; }
    public Ledger Ledger { get; }
    public decimal Cash { get; private set; }

    public ZooState(
        IEnumerable<ZooAnimal>? animals = null,
        FoodStock? foodStock = null,
        VisitorStats? visitorStats = null,
        Ledger? ledger = null,
        decimal cash = 80000m)
    {
        if (cash < 0m) throw new ArgumentOutOfRangeException(nameof(cash));

        Animals = animals?.ToList() ?? new List<ZooAnimal>();
        FoodStock = foodStock ?? new FoodStock();
        VisitorStats = visitorStats ?? new VisitorStats();
        Ledger = ledger ?? new Ledger();
        Cash = cash;

        Ledger.Add(new Transaction(
            TimestampUtc: DateTime.UtcNow,
            Amount: cash,
            Description: "Initial zoo budget",
            Category: "Init",
            BalanceAfter: Cash));
    }

    public void AddAnimal(ZooAnimal animal)
    {
        ArgumentNullException.ThrowIfNull(animal);
        Animals.Add(animal);
    }

    public bool RemoveAnimal(ZooAnimal animal) => Animals.Remove(animal);

    public void AddCash(decimal amount, string description = "Cash in", string category = "Income")
    {
        if (amount < 0m) throw new ArgumentOutOfRangeException(nameof(amount));
        Cash += amount;

        Ledger.Add(new Transaction(
            TimestampUtc: DateTime.UtcNow,
            Amount: amount,
            Description: description,
            Category: category,
            BalanceAfter: Cash));
    }

    public bool SpendCash(decimal amount, string description = "Cash out", string category = "Expense")
    {
        if (amount < 0m) throw new ArgumentOutOfRangeException(nameof(amount));
        if (Cash < amount) return false;
        Cash -= amount;

        Ledger.Add(new Transaction(
            TimestampUtc: DateTime.UtcNow,
            Amount: -amount,
            Description: description,
            Category: category,
            BalanceAfter: Cash));
        return true;
    }
}
