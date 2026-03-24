using Zoo.Application.Simulation;
using Zoo.Domain.Animals;
using Zoo.Domain.Events;
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
        global::System.Console.WriteLine("====================================");
        global::System.Console.WriteLine("        Welcome to the Zoo");
        global::System.Console.WriteLine("====================================");
    }

    /// <summary>
    /// Prints the main menu
    /// </summary>
    public void PrintMenu()
    {
        global::System.Console.WriteLine();
        global::System.Console.WriteLine("Main menu");
        global::System.Console.WriteLine("1. Advance one turn (1 day)");
        global::System.Console.WriteLine("2. Canteen (buy food)");
        global::System.Console.WriteLine("3. Add an animal");
        global::System.Console.WriteLine("4. Buy a habitat");
        global::System.Console.WriteLine("5. Sell an animal");
        global::System.Console.WriteLine("6. Sell a habitat");
        global::System.Console.WriteLine("7. Show status");
        global::System.Console.WriteLine("0. Quit");
    }

    /// <summary>
    /// Prints the full zoo status
    /// </summary>
    public void PrintStatus(ZooSimulationService simulation)
    {
        global::System.Console.WriteLine();
        global::System.Console.WriteLine("=== Zoo status ===");
        global::System.Console.WriteLine($"Date: {simulation.CurrentDayOfMonth:00}/{simulation.CurrentMonth:00}/Year {simulation.CurrentYear}");
        global::System.Console.WriteLine($"Turn: {simulation.TurnNumber}");
        global::System.Console.WriteLine($"Cash: {simulation.Cash:0.##} €");
        global::System.Console.WriteLine($"Meat stock: {simulation.MeatStockKg:0.##} kg");
        global::System.Console.WriteLine($"Seed stock: {simulation.SeedsStockKg:0.##} kg");
        global::System.Console.WriteLine($"Season: {(simulation.IsHighSeason ? "High" : "Low")}");

        var totalAnimals = simulation.Animals.Count;
        var aliveAnimals = simulation.Animals.Count(a => a.IsAlive);
        var sickAnimals = simulation.Animals.Count(a => a.IsSick);
        var hungryAnimals = simulation.Animals.Count(a => a.IsHungry);

        global::System.Console.WriteLine($"Animals: {totalAnimals} (alive: {aliveAnimals}, sick: {sickAnimals}, hungry: {hungryAnimals})");

        PrintHabitats(simulation.Habitats);
        PrintAnimals(simulation.Animals, simulation.Habitats);
    }

    /// <summary>
    /// Prints a list of events
    /// </summary>
    public void PrintEvents(IEnumerable<ZooEvent> events)
    {
        var list = events.ToList();
        if (list.Count == 0)
            return;

        global::System.Console.WriteLine();
        global::System.Console.WriteLine("Events:");
        foreach (var ev in list)
        {
            global::System.Console.WriteLine($"- [{ev.Type}] {ev.Description}");
        }
    }

    // Habitats are shown before animals to give quick context
    private void PrintHabitats(IReadOnlyList<Habitat> habitats)
    {
        global::System.Console.WriteLine();
        global::System.Console.WriteLine("Habitats:");

        if (habitats.Count == 0)
        {
            global::System.Console.WriteLine("- No habitats");
            return;
        }

        for (var i = 0; i < habitats.Count; i++)
        {
            var habitat = habitats[i];
            global::System.Console.WriteLine($"{i + 1}. {habitat.Species} | {habitat.Animals.Count}/{habitat.Capacity} | Health: {habitat.HealthRatio:P0}");
        }
    }

    // Each animal line includes health, hunger and habitat information
    private void PrintAnimals(IReadOnlyList<ZooAnimal> animals, IReadOnlyList<Habitat> habitats)
    {
        global::System.Console.WriteLine();
        global::System.Console.WriteLine("Animals:");

        if (animals.Count == 0)
        {
            global::System.Console.WriteLine("- No animals");
            return;
        }

        for (var i = 0; i < animals.Count; i++)
        {
            var animal = animals[i];
            var habitatLabel = FindHabitatLabel(animal, habitats);
            var status = animal.IsAlive ? animal.Health.ToString() : "Dead";
            var hungry = animal.IsHungry ? "yes" : "no";

            global::System.Console.WriteLine($"{i + 1}. {animal.Name} | {animal.Species} | {animal.Sex} | Age: {animal.AgeDays}d | {status} | Hungry: {hungry} | Habitat: {habitatLabel}");
        }
    }

    // The habitat label is resolved from the habitat collection
    private static string FindHabitatLabel(ZooAnimal animal, IReadOnlyList<Habitat> habitats)
    {
        var habitat = habitats.FirstOrDefault(h => h.Animals.Contains(animal));
        return habitat is null ? "None" : habitat.Species.ToString();
    }
}
