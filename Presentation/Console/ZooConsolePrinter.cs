using Zoo.Application.Simulation;
using Zoo.Domain.Animals;
using Zoo.Domain.Events;
using Zoo.Domain.Finance;
using Zoo.Domain.Habitats;

namespace Zoo.Presentation.Console;

/// <summary>
/// Prints the console UI of the zoo application
/// </summary>
public sealed class ZooConsolePrinter
{
    /// <summary>
    /// Prints the welcome banner
    /// </summary>
    public void PrintWelcome()
    {
        global::System.Console.WriteLine("==================================================");
        global::System.Console.WriteLine("                  Zoo CLI Dashboard");
        global::System.Console.WriteLine("==================================================");
        global::System.Console.WriteLine("Use Up/Down arrows to move, Enter to confirm and Left Arrow or Backspace to cancel when available.");
    }

    /// <summary>
    /// Builds the main menu header from the current zoo state
    /// </summary>
    public IReadOnlyList<string> GetMainMenuHeaderLines(ZooSimulationService simulation)
    {
        var animals = simulation.Animals.OrderBy(animal => animal.Species).ThenBy(animal => animal.Name).ToList();
        var habitats = simulation.Habitats.OrderBy(habitat => habitat.Species).ThenByDescending(habitat => habitat.AvailableSlots).ToList();
        var visibleAnimals = simulation.GetAnimalsExposedToPublic();
        var projectedRevenueBySpecies = simulation.CalculateVisitorRevenueBySpecies(simulation.IsHighSeason);
        var totalProjectedRevenue = projectedRevenueBySpecies.Values.Sum();
        var aliveAnimals = animals.Where(animal => animal.IsAlive).ToList();
        var sickCount = aliveAnimals.Count(animal => animal.IsSick);
        var hungryCount = aliveAnimals.Count(animal => animal.IsHungry);
        var gestatingCount = aliveAnimals.Count(animal => animal.IsGestating || animal.EggIncubationRemainingDays > 0);
        var emptyHabitatCount = habitats.Count(habitat => habitat.Animals.Count == 0);
        var lines = new List<string>
        {
            "Zoo CLI Dashboard",
            "=================",
            $"Date: Day {simulation.CurrentDayOfMonth:00}/{simulation.CurrentMonth:00}/Year {simulation.CurrentYear} | Turn {simulation.TurnNumber}",
            $"Season: {(simulation.IsHighSeason ? "High season" : "Low season")} ({(simulation.IsHighSeason ? "May to September" : "October to April")})",
            $"Cash: {simulation.Cash:0.##} EUR",
            $"Food stock: {simulation.MeatStockKg:0.##} kg meat | {simulation.SeedsStockKg:0.##} kg seeds",
            $"Population: {aliveAnimals.Count} alive / {animals.Count} total",
            $"Exposure: {visibleAnimals.Count} animal(s) visible to visitors",
            $"Projected revenue: {totalProjectedRevenue:0.##} EUR",
            $"Watchlist: {sickCount} sick | {hungryCount} hungry | {gestatingCount} hidden from visitors | {emptyHabitatCount} empty habitat(s)"
        };

        if (simulation.PendingHabitatEmergency is { } emergency)
        {
            lines.Add(
                $"Pending emergency: {emergency.Species} habitat destroyed, {emergency.DisplacedAnimals.Count} displaced animal(s).");
        }

        if (simulation.PeekNewbornAwaitingName() is { } newborn)
            lines.Add($"Pending newborn naming: {newborn.Name} ({newborn.Species}).");

        return lines;
    }

    /// <summary>
    /// Prints the main operational dashboard
    /// </summary>
    public void PrintDashboard(ZooSimulationService simulation)
    {
        var animals = simulation.Animals.OrderBy(animal => animal.Species).ThenBy(animal => animal.Name).ToList();
        var habitats = simulation.Habitats.OrderBy(habitat => habitat.Species).ThenByDescending(habitat => habitat.AvailableSlots).ToList();
        var visibleAnimals = simulation.GetAnimalsExposedToPublic();
        var projectedRevenueBySpecies = simulation.CalculateVisitorRevenueBySpecies(simulation.IsHighSeason);
        var totalProjectedRevenue = projectedRevenueBySpecies.Values.Sum();
        var aliveAnimals = animals.Where(animal => animal.IsAlive).ToList();
        var sickCount = aliveAnimals.Count(animal => animal.IsSick);
        var hungryCount = aliveAnimals.Count(animal => animal.IsHungry);
        var gestatingCount = aliveAnimals.Count(animal => animal.IsGestating || animal.EggIncubationRemainingDays > 0);
        var emptyHabitatCount = habitats.Count(habitat => habitat.Animals.Count == 0);
        var watchCount = sickCount + hungryCount + gestatingCount;

        PrintSection("Dashboard");
        global::System.Console.WriteLine($"Date: Day {simulation.CurrentDayOfMonth:00}/{simulation.CurrentMonth:00}/Year {simulation.CurrentYear} | Turn {simulation.TurnNumber}");
        global::System.Console.WriteLine($"Season: {(simulation.IsHighSeason ? "High season" : "Low season")} ({(simulation.IsHighSeason ? "May to September" : "October to April")})");
        global::System.Console.WriteLine($"Cash: {simulation.Cash:0.##} EUR");
        global::System.Console.WriteLine($"Food stock: {simulation.MeatStockKg:0.##} kg meat | {simulation.SeedsStockKg:0.##} kg seeds");
        global::System.Console.WriteLine($"Population: {aliveAnimals.Count} alive / {animals.Count} total");
        global::System.Console.WriteLine($"Exposure: {visibleAnimals.Count} animal(s) visible to visitors");
        global::System.Console.WriteLine($"Projected revenue: {totalProjectedRevenue:0.##} EUR");
        global::System.Console.WriteLine(watchCount == 0
            ? "Watchlist: No immediate operational alerts"
            : $"Watchlist: {watchCount} issue(s) need attention");
        global::System.Console.WriteLine($"Watchlist details: {sickCount} sick | {hungryCount} hungry | {gestatingCount} hidden from visitors | {emptyHabitatCount} empty habitat(s)");

        if (simulation.PendingHabitatEmergency is { } emergency)
        {
            global::System.Console.WriteLine(
                $"Pending emergency: {emergency.Species} habitat destroyed, {emergency.DisplacedAnimals.Count} displaced animal(s).");
        }

        if (simulation.PeekNewbornAwaitingName() is { } newborn)
            global::System.Console.WriteLine($"Pending newborn naming: {newborn.Name} ({newborn.Species}).");
    }

    /// <summary>
    /// Prints the pending action reminders above the main menu
    /// </summary>
    public void PrintPendingItemsSummary(ZooSimulationService simulation)
    {
        if (simulation.PendingHabitatEmergency is null && simulation.PeekNewbornAwaitingName() is null)
            return;

        PrintSection("Pending items");

        if (simulation.PendingHabitatEmergency is { } emergency)
        {
            global::System.Console.WriteLine(
                $"- Habitat emergency: {emergency.Species} habitat destroyed, {emergency.DisplacedAnimals.Count} displaced animal(s).");
        }

        if (simulation.PeekNewbornAwaitingName() is { } newborn)
            global::System.Console.WriteLine($"- Newborn naming: {newborn.Name} is waiting for a final name.");
    }

    /// <summary>
    /// Prints the full zoo status
    /// </summary>
    public void PrintStatus(ZooSimulationService simulation, Func<ZooAnimal, string> reproductionStatusFactory)
    {
        PrintDashboard(simulation);
        PrintHabitats(simulation.Habitats.OrderBy(habitat => habitat.Species).ThenByDescending(habitat => habitat.AvailableSlots).ToList());
        PrintAnimals(
            simulation.Animals.OrderBy(animal => animal.Species).ThenBy(animal => animal.Name).ToList(),
            simulation.Habitats,
            reproductionStatusFactory,
            title: "Animals");
    }

    /// <summary>
    /// Prints a list of events
    /// </summary>
    public void PrintEvents(IEnumerable<ZooEvent> events, string title = "Events")
    {
        var list = events.ToList();
        if (list.Count == 0)
        {
            PrintSection(title);
            global::System.Console.WriteLine("No events to show.");
            return;
        }

        PrintSection(title);

        foreach (var zooEvent in list)
        {
            global::System.Console.WriteLine(
                $"- [{ZooConsoleFormatter.HumanizeEventType(zooEvent.Type)}] Day {zooEvent.Day:00}/{zooEvent.Month:00}/Y{zooEvent.Year} | Turn {zooEvent.TurnNumber}");
            global::System.Console.WriteLine($"  {zooEvent.Description}");
        }
    }

    /// <summary>
    /// Prints the habitat list
    /// </summary>
    public void PrintHabitats(IReadOnlyList<Habitat> habitats)
    {
        PrintSection("Habitats");

        if (habitats.Count == 0)
        {
            global::System.Console.WriteLine("No habitats.");
            return;
        }

        for (var i = 0; i < habitats.Count; i++)
        {
            var habitat = habitats[i];
            var sellability = habitat.Animals.Count == 0 ? "Ready to sell" : "Occupied";

            global::System.Console.WriteLine(
                $"{i + 1}. {habitat.Species} habitat | {habitat.Animals.Count}/{habitat.Capacity} occupied | {habitat.AvailableSlots} free | Health {habitat.HealthRatio:P0}");
            global::System.Console.WriteLine(
                $"   Buy {habitat.BuyPrice:0.##} EUR | Sell {habitat.SellPrice:0.##} EUR | Loss probability {habitat.LossProbability:P0} | {sellability}");
        }
    }

    /// <summary>
    /// Prints the animal list
    /// </summary>
    public void PrintAnimals(
        IReadOnlyList<ZooAnimal> animals,
        IReadOnlyList<Habitat> habitats,
        Func<ZooAnimal, string> reproductionStatusFactory,
        string title)
    {
        PrintSection(title);

        if (animals.Count == 0)
        {
            global::System.Console.WriteLine("No animals.");
            return;
        }

        for (var i = 0; i < animals.Count; i++)
        {
            var animal = animals[i];
            var habitatLabel = ZooConsoleFormatter.FindHabitatLabel(animal, habitats);

            global::System.Console.WriteLine(
                $"{i + 1}. {animal.Name} | {animal.Species} | {animal.Sex} | {ZooConsoleFormatter.DescribeAnimalMarker(animal)}");
            global::System.Console.WriteLine(
                $"   Age {ZooConsoleFormatter.FormatAge(animal.AgeDays)} | {habitatLabel} | Food {animal.GetDailyFoodNeedKg():0.##} kg/day");
            global::System.Console.WriteLine(
                $"   Health {GetHealthLabel(animal)} | Hunger debt {animal.HungerDebtDays} day(s) | Disease {animal.DiseaseRemainingDays} day(s)");
            global::System.Console.WriteLine($"   {reproductionStatusFactory(animal)}");
        }
    }

    /// <summary>
    /// Prints one animal details block
    /// </summary>
    public void PrintAnimalDetails(
        ZooAnimal animal,
        Habitat? habitat,
        string reproductionStatus,
        decimal salePrice)
    {
        PrintSection("Animal details");

        global::System.Console.WriteLine($"Name: {animal.Name}");
        global::System.Console.WriteLine($"Species: {animal.Species}");
        global::System.Console.WriteLine($"Sex: {animal.Sex}");
        global::System.Console.WriteLine($"Age: {ZooConsoleFormatter.FormatAge(animal.AgeDays)}");
        global::System.Console.WriteLine($"Habitat: {(habitat is null ? "No habitat" : $"{habitat.Species} habitat")}");
        global::System.Console.WriteLine($"Marker: {ZooConsoleFormatter.DescribeAnimalMarker(animal)}");
        global::System.Console.WriteLine($"Health: {GetHealthLabel(animal)}");
        global::System.Console.WriteLine($"Food need: {animal.GetDailyFoodNeedKg():0.##} kg/day");
        global::System.Console.WriteLine($"Sale value: {salePrice:0.##} EUR");
        global::System.Console.WriteLine($"Visible to visitors: {(animal.IsExposedToPublic() ? "Yes" : "No")}");

        if (animal.IsGestating)
            global::System.Console.WriteLine($"Gestation: {animal.GestationRemainingDays} day(s) remaining");

        if (animal.EggIncubationRemainingDays > 0)
            global::System.Console.WriteLine($"Egg incubation: {animal.PendingEggs} egg(s), {animal.EggIncubationRemainingDays} day(s) remaining");

        global::System.Console.WriteLine($"Reproduction: {reproductionStatus}");
    }

    /// <summary>
    /// Prints one habitat details block
    /// </summary>
    public void PrintHabitatDetails(
        Habitat habitat,
        IReadOnlyList<ZooAnimal> occupants,
        IReadOnlyList<Habitat> habitats,
        Func<ZooAnimal, string> reproductionStatusFactory)
    {
        PrintSection("Habitat details");

        global::System.Console.WriteLine($"Species: {habitat.Species}");
        global::System.Console.WriteLine($"Occupancy: {habitat.Animals.Count}/{habitat.Capacity}");
        global::System.Console.WriteLine($"Free slots: {habitat.AvailableSlots}");
        global::System.Console.WriteLine($"Health ratio: {habitat.HealthRatio:P0}");
        global::System.Console.WriteLine($"Buy price: {habitat.BuyPrice:0.##} EUR");
        global::System.Console.WriteLine($"Sell price: {habitat.SellPrice:0.##} EUR");
        global::System.Console.WriteLine($"Loss probability: {habitat.LossProbability:P0}");

        if (occupants.Count == 0)
        {
            global::System.Console.WriteLine("This habitat is empty.");
            return;
        }

        global::System.Console.WriteLine();
        PrintAnimals(occupants, habitats, reproductionStatusFactory, $"Animals in {habitat.Species} habitat");
    }

    /// <summary>
    /// Prints the recent ledger transactions
    /// </summary>
    public void PrintLedger(IReadOnlyList<Transaction> transactions)
    {
        PrintSection("Ledger");

        if (transactions.Count == 0)
        {
            global::System.Console.WriteLine("No transactions.");
            return;
        }

        for (var i = 0; i < transactions.Count; i++)
        {
            var transaction = transactions[i];
            global::System.Console.WriteLine(
                $"{i + 1}. {transaction.Amount:+0.##;-0.##;0} EUR | {transaction.Description}");
            global::System.Console.WriteLine(
                $"   {transaction.Category} | {transaction.TimestampUtc:yyyy-MM-dd HH:mm} UTC | Balance {transaction.BalanceAfter:0.##} EUR");
        }
    }

    /// <summary>
    /// Prints the projected visitor revenue by species
    /// </summary>
    public void PrintProjectedRevenue(
        IReadOnlyDictionary<SpeciesType, decimal> projectedRevenueBySpecies,
        IReadOnlyList<ZooAnimal> visibleAnimals)
    {
        PrintSection("Projected revenue");

        foreach (var species in Enum.GetValues<SpeciesType>())
        {
            var projectedRevenue = projectedRevenueBySpecies.GetValueOrDefault(species);
            var visibleCount = visibleAnimals.Count(animal => animal.Species == species);

            global::System.Console.WriteLine($"{species}: {projectedRevenue:0.##} EUR");
            global::System.Console.WriteLine($"   {visibleCount} animal(s) visible to visitors");
        }
    }

    /// <summary>
    /// Prints the pending habitat emergency details
    /// </summary>
    public void PrintHabitatEmergency(PendingHabitatEmergency emergency, decimal cash)
    {
        PrintSection("Habitat emergency");

        global::System.Console.WriteLine($"Cause: {ZooConsoleFormatter.HumanizeEventType(emergency.CauseType)}");
        global::System.Console.WriteLine($"Description: {emergency.CauseDescription}");
        global::System.Console.WriteLine($"Species: {emergency.Species}");
        global::System.Console.WriteLine($"Displaced animals: {emergency.DisplacedAnimals.Count}");
        global::System.Console.WriteLine($"Replacement habitat cost: {emergency.ReplacementHabitatCost:0.##} EUR");
        global::System.Console.WriteLine($"Current cash: {cash:0.##} EUR");

        foreach (var animal in emergency.DisplacedAnimals)
            global::System.Console.WriteLine($"- {animal.Name} | {animal.Sex} | Age {ZooConsoleFormatter.FormatAge(animal.AgeDays)}");
    }

    /// <summary>
    /// Prints the newborn naming prompt context
    /// </summary>
    public void PrintNewbornAwaitingName(ZooAnimal newborn)
    {
        PrintSection("Newborn naming");

        global::System.Console.WriteLine($"Temporary name: {newborn.Name}");
        global::System.Console.WriteLine($"Species: {newborn.Species}");
        global::System.Console.WriteLine($"Sex: {newborn.Sex}");
        global::System.Console.WriteLine("Press Enter to keep the current temporary name.");
    }

    /// <summary>
    /// Prints a success message
    /// </summary>
    public void PrintSuccess(string message)
    {
        global::System.Console.WriteLine();
        global::System.Console.WriteLine($"[OK] {message}");
    }

    /// <summary>
    /// Prints a warning message
    /// </summary>
    public void PrintWarning(string message)
    {
        global::System.Console.WriteLine();
        global::System.Console.WriteLine($"[Warning] {message}");
    }

    /// <summary>
    /// Prints an error message
    /// </summary>
    public void PrintError(string message)
    {
        global::System.Console.WriteLine();
        global::System.Console.WriteLine($"[Error] {message}");
    }

    /// <summary>
    /// Prints a neutral informational message
    /// </summary>
    public void PrintInfo(string message)
    {
        global::System.Console.WriteLine();
        global::System.Console.WriteLine(message);
    }

    // Section headers keep the console output structured without relying on colors
    private static void PrintSection(string title)
    {
        global::System.Console.WriteLine();
        global::System.Console.WriteLine(title);
        global::System.Console.WriteLine(new string('-', Math.Max(12, title.Length)));
    }

    // The health label treats dead animals differently from the health enum
    private static string GetHealthLabel(ZooAnimal animal)
    {
        return animal.IsAlive ? animal.Health.ToString() : "Dead";
    }
}
