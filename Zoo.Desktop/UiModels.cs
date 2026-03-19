using Avalonia.Media;
using Zoo.Domain.Animals;
using Zoo.Domain.Events;
using Zoo.Domain.Finance;
using Zoo.Domain.Habitats;

namespace Zoo.Desktop;

public sealed class AnimalRow
{
    public AnimalRow(ZooAnimal animal, string habitatLabel, string reproductionNote)
    {
        Animal = animal;
        HabitatLabel = habitatLabel;
        ReproductionNote = reproductionNote;
    }

    public ZooAnimal Animal { get; }
    public string HabitatLabel { get; }
    public string ReproductionNote { get; }

    public string Name => Animal.Name;
    public string Secondary => $"{Animal.Species} | {Animal.Sex}";
    public string Detail =>
        $"Age {UiTextFormatter.FormatAge(Animal.AgeDays)} | {HabitatLabel} | Food {Animal.GetDailyFoodNeedKg():0.##} kg/day";
    public string Marker => Animal.IsAlive
        ? Animal.IsGestating
            ? "Gestating"
            : Animal.EggIncubationRemainingDays > 0
                ? "Incubating"
                : Animal.IsHungry
                    ? "Hungry"
                    : Animal.IsSick
                        ? "Sick"
                        : "Stable"
        : "Dead";
    public string Status => Animal.IsAlive ? Animal.Health.ToString() : "Dead";
    public string FoodNeed => $"{Animal.GetDailyFoodNeedKg():0.##} kg/day";
    public IBrush MarkerBrush => Animal.IsAlive
        ? Animal.IsGestating || Animal.EggIncubationRemainingDays > 0
            ? UiBrushes.Info
            : Animal.IsHungry
                ? UiBrushes.Hungry
                : Animal.IsSick
                    ? UiBrushes.Warning
                : UiBrushes.Success
        : UiBrushes.Danger;
    public IBrush StatusBrush => Animal.IsAlive
        ? Animal.IsHungry
            ? UiBrushes.Hungry
            : Animal.IsSick
                ? UiBrushes.Warning
                : UiBrushes.Success
        : UiBrushes.Danger;
}

public sealed class HabitatRow
{
    public HabitatRow(Habitat habitat)
    {
        Habitat = habitat;
    }

    public Habitat Habitat { get; }

    public string Title => $"{Habitat.Species} habitat";
    public string Occupancy => $"{Habitat.Animals.Count}/{Habitat.Capacity}";
    public string Detail => $"{Habitat.AvailableSlots} free slot(s) | Sell {Habitat.SellPrice:0.##} EUR";
    public string HealthLabel => $"{Habitat.HealthRatio:P0} healthy";
    public string Sellability => Habitat.Animals.Count == 0 ? "Ready to sell" : "Occupied";
    public IBrush HealthBrush => Habitat.HealthRatio switch
    {
        >= 0.8m => UiBrushes.Success,
        >= 0.5m => UiBrushes.Info,
        _ => UiBrushes.Warning
    };
    public IBrush SellabilityBrush => Habitat.Animals.Count == 0 ? UiBrushes.Success : UiBrushes.Warning;
}

public sealed class EventRow
{
    public EventRow(ZooEvent zooEvent)
    {
        ZooEvent = zooEvent;
    }

    public ZooEvent ZooEvent { get; }

    public string Title => Humanize(ZooEvent.Type.ToString());
    public string DateLabel => $"Day {ZooEvent.Day:00}/{ZooEvent.Month:00}/Y{ZooEvent.Year} | Turn {ZooEvent.TurnNumber}";
    public string Description => ZooEvent.Description;
    public IBrush AccentBrush => ZooEvent.Type switch
    {
        ZooEventType.Fire or
        ZooEventType.Theft or
        ZooEventType.DiseaseDeath or
        ZooEventType.HungerDeath or
        ZooEventType.HabitatAnimalsEuthanized or
        ZooEventType.OverpopulationDeath => UiBrushes.Danger,
        ZooEventType.Pests or
        ZooEventType.SpoiledMeat or
        ZooEventType.Disease or
        ZooEventType.HabitatMonthlyLoss or
        ZooEventType.InfantDeath => UiBrushes.Warning,
        ZooEventType.Pregnancy or ZooEventType.EggLaying or ZooEventType.Birth or ZooEventType.AnnualSubsidy or ZooEventType.DiseaseRecovered => UiBrushes.Success,
        ZooEventType.VisitorIncome or
        ZooEventType.FoodPurchased or
        ZooEventType.AnimalPurchased or
        ZooEventType.HabitatPurchased or
        ZooEventType.HabitatAnimalsRehoused => UiBrushes.Info,
        _ => UiBrushes.Warning
    };

    private static string Humanize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var characters = new List<char>(value.Length + 8);
        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            if (i > 0 && char.IsUpper(current) && char.IsLower(value[i - 1]))
                characters.Add(' ');

            characters.Add(current);
        }

        return new string(characters.ToArray());
    }
}

public sealed class LedgerRow
{
    public LedgerRow(Transaction transaction)
    {
        Transaction = transaction;
    }

    public Transaction Transaction { get; }

    public string Amount => $"{Transaction.Amount:+0.##;-0.##;0} EUR";
    public string Description => Transaction.Description;
    public string Meta => $"{Transaction.Category} | {Transaction.TimestampUtc:yyyy-MM-dd HH:mm} UTC";
    public string Balance => $"Balance {Transaction.BalanceAfter:0.##} EUR";
    public IBrush AmountBrush => Transaction.Amount >= 0m ? UiBrushes.Success : UiBrushes.Danger;
}

public sealed class RevenueRow
{
    public RevenueRow(SpeciesType species, decimal projectedRevenue, int exposedAnimals)
    {
        Species = species;
        ProjectedRevenue = projectedRevenue;
        ExposedAnimals = exposedAnimals;
    }

    public SpeciesType Species { get; }
    public decimal ProjectedRevenue { get; }
    public int ExposedAnimals { get; }

    public string Title => Species.ToString();
    public string Value => $"{ProjectedRevenue:0.##} EUR";
    public string Detail => $"{ExposedAnimals} animal(s) visible to visitors";
    public IBrush AccentBrush => ProjectedRevenue > 0m ? UiBrushes.Success : UiBrushes.Muted;
}

internal static class UiBrushes
{
    public static readonly IBrush Success = Brush.Parse("#2EC4B6");
    public static readonly IBrush Warning = Brush.Parse("#E6A44E");
    public static readonly IBrush Hungry = Brush.Parse("#D3BB63");
    public static readonly IBrush Danger = Brush.Parse("#FF6B6B");
    public static readonly IBrush Info = Brush.Parse("#5AA9FF");
    public static readonly IBrush Muted = Brush.Parse("#93A4B7");
    public static readonly IBrush MessageGoodFill = Brush.Parse("#12332F");
    public static readonly IBrush MessageGoodBorder = Brush.Parse("#235953");
    public static readonly IBrush MessageBadFill = Brush.Parse("#341B20");
    public static readonly IBrush MessageBadBorder = Brush.Parse("#6C3039");
}

internal static class UiTextFormatter
{
    public static string FormatAge(int ageDays)
    {
        if (ageDays <= 0)
            return "0 days";

        var years = ageDays / 365;
        var remainingDaysAfterYears = ageDays % 365;
        var months = remainingDaysAfterYears / 30;
        var days = remainingDaysAfterYears % 30;

        var parts = new List<string>(3);
        if (years > 0)
            parts.Add($"{years} year{(years == 1 ? string.Empty : "s")}");
        if (months > 0)
            parts.Add($"{months} month{(months == 1 ? string.Empty : "s")}");
        if (days > 0 || parts.Count == 0)
            parts.Add($"{days} day{(days == 1 ? string.Empty : "s")}");

        return string.Join(", ", parts);
    }

    public static string DescribeReproduction(Animal animal)
    {
        if (!animal.IsAlive)
            return "Reproduction unavailable";

        var reasons = new List<string>();

        if (!animal.HasReachedSexualMaturity())
            reasons.Add("too young");
        if (animal.HasReachedReproductionEnd())
            reasons.Add("past reproduction age");
        if (animal.IsHungry)
            reasons.Add("hungry");
        if (animal.IsSick)
            reasons.Add("sick");
        if (animal.IsBlockedFromReproductionByArrival())
            reasons.Add("arrival cooldown");
        if (animal.MonthsUntilNextLitter > 0)
            reasons.Add($"{animal.MonthsUntilNextLitter} month cooldown");
        if (animal.IsGestating)
            reasons.Add("already gestating");
        if (animal.EggIncubationRemainingDays > 0)
            reasons.Add("incubating eggs");

        return reasons.Count == 0
            ? "Reproduction ready"
            : $"Reproduction blocked: {string.Join(", ", reasons)}";
    }
}
