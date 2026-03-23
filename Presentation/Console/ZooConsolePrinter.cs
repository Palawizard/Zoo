using Zoo.Application.Simulation;
using Zoo.Domain.Animals;
using Zoo.Domain.Events;
using Zoo.Domain.Habitats;

namespace Zoo.Presentation.ConsoleApp;

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
        Console.WriteLine("====================================");
        Console.WriteLine("        Welcome to the Zoo");
        Console.WriteLine("====================================");
    }

    /// <summary>
    /// Prints the main menu
    /// </summary>
    public void PrintMenu()
    {
        Console.WriteLine();
        Console.WriteLine("Main menu");
        Console.WriteLine("1. Advance one turn (1 day)");
        Console.WriteLine("2. Canteen (buy food)");
        Console.WriteLine("3. Add an animal");
        Console.WriteLine("4. Buy a habitat");
        Console.WriteLine("5. Sell an animal");
        Console.WriteLine("6. Sell a habitat");
        Console.WriteLine("7. Show status");
        Console.WriteLine("0. Quit");
    }

    /// <summary>
    /// Prints the full zoo status
    /// </summary>
    public void PrintStatus(ZooSimulationService simulation)
    {
        Console.WriteLine();
        Console.WriteLine("=== Zoo status ===");
        Console.WriteLine($"Date: {simulation.CurrentDayOfMonth:00}/{simulation.CurrentMonth:00}/Year {simulation.CurrentYear}");
        Console.WriteLine($"Turn: {simulation.TurnNumber}");
        Console.WriteLine($"Cash: {simulation.Cash:0.##} €");
        Console.WriteLine($"Meat stock: {simulation.MeatStockKg:0.##} kg");
        Console.WriteLine($"Seed stock: {simulation.SeedsStockKg:0.##} kg");
        Console.WriteLine($"Season: {(simulation.IsHighSeason ? "High" : "Low")}");

        var totalAnimals = simulation.Animals.Count;
        var aliveAnimals = simulation.Animals.Count(a => a.IsAlive);
        var sickAnimals = simulation.Animals.Count(a => a.IsSick);
        var hungryAnimals = simulation.Animals.Count(a => a.IsHungry);

        Console.WriteLine($"Animals: {totalAnimals} (alive: {aliveAnimals}, sick: {sickAnimals}, hungry: {hungryAnimals})");

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

        Console.WriteLine();
        Console.WriteLine("Events:");
        foreach (var ev in list)
        {
            Console.WriteLine($"- [{ev.Type}] {ev.Description}");
        }
    }

    // Habitats are shown before animals to give quick context
    private void PrintHabitats(IReadOnlyList<Habitat> habitats)
    {
        Console.WriteLine();
        Console.WriteLine("Habitats:");

        if (habitats.Count == 0)
        {
            Console.WriteLine("- No habitats");
            return;
        }

        for (var i = 0; i < habitats.Count; i++)
        {
            var habitat = habitats[i];
            Console.WriteLine($"{i + 1}. {habitat.Species} | {habitat.Animals.Count}/{habitat.Capacity} | Health: {habitat.HealthRatio:P0}");
        }
    }

    // Each animal line includes health, hunger and habitat information
    private void PrintAnimals(IReadOnlyList<ZooAnimal> animals, IReadOnlyList<Habitat> habitats)
    {
        Console.WriteLine();
        Console.WriteLine("Animals:");

        if (animals.Count == 0)
        {
            Console.WriteLine("- No animals");
            return;
        }

        for (var i = 0; i < animals.Count; i++)
        {
            var animal = animals[i];
            var habitatLabel = FindHabitatLabel(animal, habitats);
            var status = animal.IsAlive ? animal.Health.ToString() : "Dead";
            var hungry = animal.IsHungry ? "yes" : "no";

            Console.WriteLine($"{i + 1}. {animal.Name} | {animal.Species} | {animal.Sex} | Age: {animal.AgeDays}d | {status} | Hungry: {hungry} | Habitat: {habitatLabel}");
        }
    }

    // The habitat label is resolved from the habitat collection
    private static string FindHabitatLabel(ZooAnimal animal, IReadOnlyList<Habitat> habitats)
    {
        var habitat = habitats.FirstOrDefault(h => h.Animals.Contains(animal));
        return habitat is null ? "None" : habitat.Species.ToString();
    }
}
