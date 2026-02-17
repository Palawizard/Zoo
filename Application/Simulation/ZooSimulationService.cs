using Zoo.Domain.Animals;

namespace Zoo.Application.Simulation;

public sealed class ZooSimulationService
{
    private readonly List<ZooAnimal> _animals = new();

    public IReadOnlyList<ZooAnimal> Animals => _animals;

    public decimal MeatStockKg { get; private set; }
    public decimal SeedsStockKg { get; private set; }

    public ZooSimulationService(
        IEnumerable<ZooAnimal>? animals = null,
        decimal meatStockKg = 0m,
        decimal seedsStockKg = 0m)
    {
        if (meatStockKg < 0m)
            throw new ArgumentOutOfRangeException(nameof(meatStockKg), "Stock cannot be negative.");
        if (seedsStockKg < 0m)
            throw new ArgumentOutOfRangeException(nameof(seedsStockKg), "Stock cannot be negative.");

        MeatStockKg = meatStockKg;
        SeedsStockKg = seedsStockKg;

        if (animals is not null)
            _animals.AddRange(animals);
    }

    public void AddAnimal(ZooAnimal animal)
    {
        ArgumentNullException.ThrowIfNull(animal);
        _animals.Add(animal);
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

    public void ProcessDailyFeeding()
    {
        foreach (var animal in _animals.Where(a => a.IsAlive))
        {
            var requiredKg = animal.GetDailyFoodNeedKg();
            var providedKg = ConsumeFromStock(animal.Profile.FoodType, requiredKg);
            animal.ApplyDailyFeeding(providedKg);
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
}
