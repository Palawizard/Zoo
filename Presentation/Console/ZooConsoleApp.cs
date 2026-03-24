using Zoo.Application.Simulation;
using Zoo.Domain.Animals;
using Zoo.Domain.Feeding;
using Zoo.Domain.Habitats;

namespace Zoo.Presentation.Console;

/// <summary>
/// Runs the console version of the zoo application
/// </summary>
public sealed class ZooConsoleApp
{
    private readonly ZooSimulationService _simulation;
    private readonly ConsoleInput _input;
    private readonly ZooConsolePrinter _printer;

    /// <summary>
    /// Creates the console application shell
    /// </summary>
    public ZooConsoleApp(ZooSimulationService simulation, ConsoleInput input, ZooConsolePrinter printer)
    {
        _simulation = simulation;
        _input = input;
        _printer = printer;
    }

    /// <summary>
    /// Starts the main console loop
    /// </summary>
    public void Run()
    {
        _printer.PrintWelcome();
        _printer.PrintStatus(_simulation);

        var running = true;
        while (running)
        {
            _printer.PrintMenu();
            var choice = (MenuOption)_input.ReadInt("Your choice:", 0, 7);

            switch (choice)
            {
                case MenuOption.AdvanceTurn:
                    AdvanceOneTurn();
                    break;
                case MenuOption.Canteen:
                    HandleCanteen();
                    break;
                case MenuOption.AddAnimal:
                    HandleAddAnimal();
                    break;
                case MenuOption.BuyHabitat:
                    HandleBuyHabitat();
                    break;
                case MenuOption.SellAnimal:
                    HandleSellAnimal();
                    break;
                case MenuOption.SellHabitat:
                    HandleSellHabitat();
                    break;
                case MenuOption.ShowStatus:
                    _printer.PrintStatus(_simulation);
                    break;
                case MenuOption.Quit:
                    running = false;
                    break;
                default:
                    global::System.Console.WriteLine("Invalid choice.");
                    break;
            }
        }

        global::System.Console.WriteLine("Goodbye!");
    }

    // One turn is advanced first, then only the new events are printed
    private void AdvanceOneTurn()
    {
        var previousEventCount = _simulation.Events.Count;

        _simulation.NextTurn();

        var newEvents = _simulation.Events.Skip(previousEventCount).ToList();

        global::System.Console.WriteLine();
        global::System.Console.WriteLine("--- End of turn ---");
        global::System.Console.WriteLine($"Turn {_simulation.TurnNumber} | Date {_simulation.CurrentDayOfMonth:00}/{_simulation.CurrentMonth:00}/Year {_simulation.CurrentYear}");
        global::System.Console.WriteLine($"Season: {(_simulation.IsHighSeason ? "High" : "Low")}");

        _printer.PrintEvents(newEvents);
    }

    // The canteen only buys food for the stock
    private void HandleCanteen()
    {
        var foodType = _input.ReadEnumChoice<FoodType>("Choose food type:");
        var kg = _input.ReadDecimal("Quantity in kg:", 0m, 10000m);

        if (_simulation.BuyFood(foodType, kg))
            global::System.Console.WriteLine("Purchase successful.");
        else
            global::System.Console.WriteLine("Purchase denied: not enough cash.");
    }

    // Adding an animal requires both a purchase and a valid habitat placement
    private void HandleAddAnimal()
    {
        var name = _input.ReadRequiredString("Animal name:");
        var species = _input.ReadEnumChoice<SpeciesType>("Choose species:");
        var sex = _input.ReadEnumChoice<SexType>("Choose sex:");
        var ageDays = _input.ReadInt("Age in days (0 = newborn):", 0, 36500);

        var targetHabitat = SelectHabitatForSpecies(species);
        if (targetHabitat is null)
        {
            global::System.Console.WriteLine("No habitat available for this species.");
            return;
        }

        var animal = new ZooAnimal(name, sex, species, ageDays);
        var bought = _simulation.BuyAnimal(animal);

        if (!bought)
        {
            global::System.Console.WriteLine("Purchase denied: not enough cash.");
            return;
        }

        try
        {
            targetHabitat.AddAnimal(animal);
            global::System.Console.WriteLine("Animal added to the zoo.");
        }
        catch (Exception ex)
        {
            _simulation.SellAnimal(animal);
            global::System.Console.WriteLine($"Cannot add animal to habitat: {ex.Message}");
        }
    }

    // If no habitat is available, the console can offer to buy one on the spot
    private Habitat? SelectHabitatForSpecies(SpeciesType species)
    {
        var habitats = _simulation.Habitats
            .Where(h => h.Species == species && h.AvailableSlots > 0)
            .ToList();

        if (habitats.Count == 0)
        {
            var shouldBuy = _input.ReadYesNo("No habitat available. Buy a habitat?");
            if (!shouldBuy)
                return null;

            if (!_simulation.BuyHabitat(species))
            {
                global::System.Console.WriteLine("Purchase denied: not enough cash.");
                return null;
            }

            habitats = _simulation.Habitats
                .Where(h => h.Species == species && h.AvailableSlots > 0)
                .ToList();
        }

        if (habitats.Count == 1)
            return habitats[0];

        global::System.Console.WriteLine("Choose habitat:");
        for (var i = 0; i < habitats.Count; i++)
        {
            var habitat = habitats[i];
            global::System.Console.WriteLine($"{i + 1}. {habitat.Species} | {habitat.Animals.Count}/{habitat.Capacity}");
        }

        var choice = _input.ReadInt("Habitat:", 1, habitats.Count);
        return habitats[choice - 1];
    }

    // Buying a habitat can immediately continue with animal purchases
    private void HandleBuyHabitat()
    {
        var species = _input.ReadEnumChoice<SpeciesType>("Choose habitat species:");
        if (_simulation.BuyHabitat(species))
        {
            global::System.Console.WriteLine("Habitat purchased.");
            OfferAnimalsForHabitat(species);
        }
        else
            global::System.Console.WriteLine("Purchase denied: not enough cash.");
    }

    // Animals are selected from the full zoo list
    private void HandleSellAnimal()
    {
        var animals = _simulation.Animals.ToList();
        if (animals.Count == 0)
        {
            global::System.Console.WriteLine("No animals to sell.");
            return;
        }

        global::System.Console.WriteLine("Choose an animal to sell:");
        for (var i = 0; i < animals.Count; i++)
        {
            var animal = animals[i];
            global::System.Console.WriteLine($"{i + 1}. {animal.Name} | {animal.Species} | {animal.Sex} | Age: {animal.AgeDays}d");
        }

        var choice = _input.ReadInt("Animal:", 1, animals.Count);
        var selected = animals[choice - 1];

        if (_simulation.SellAnimal(selected))
            global::System.Console.WriteLine("Animal sold.");
        else
            global::System.Console.WriteLine("Sale failed.");
    }

    // Only empty habitats can be sold
    private void HandleSellHabitat()
    {
        var habitats = _simulation.Habitats.ToList();
        if (habitats.Count == 0)
        {
            global::System.Console.WriteLine("No habitats to sell.");
            return;
        }

        global::System.Console.WriteLine("Choose a habitat to sell:");
        for (var i = 0; i < habitats.Count; i++)
        {
            var habitat = habitats[i];
            global::System.Console.WriteLine($"{i + 1}. {habitat.Species} | {habitat.Animals.Count}/{habitat.Capacity}");
        }

        var choice = _input.ReadInt("Habitat:", 1, habitats.Count);
        var selected = habitats[choice - 1];

        if (selected.Animals.Count > 0)
        {
            global::System.Console.WriteLine("Cannot sell: habitat not empty.");
            return;
        }

        if (_simulation.SellHabitat(selected))
            global::System.Console.WriteLine("Habitat sold.");
        else
            global::System.Console.WriteLine("Sale failed.");
    }

    // This is a small onboarding flow after buying a new habitat
    private void OfferAnimalsForHabitat(SpeciesType species)
    {
        var habitat = _simulation.Habitats.LastOrDefault(h => h.Species == species);
        if (habitat is null)
            return;

        if (habitat.AvailableSlots <= 0)
        {
            global::System.Console.WriteLine("This habitat has no available slots.");
            return;
        }

        var shouldBuy = _input.ReadYesNo("Buy animals for this habitat?");
        if (!shouldBuy)
            return;

        var maxCount = habitat.AvailableSlots;
        var count = _input.ReadInt($"How many animals (1-{maxCount})?", 1, maxCount);

        for (var i = 0; i < count; i++)
        {
            global::System.Console.WriteLine($"Animal {i + 1}/{count}");
            var name = _input.ReadRequiredString("Animal name:");
            var sex = _input.ReadEnumChoice<SexType>("Choose sex:");
            var ageDays = _input.ReadInt("Age in days (0 = newborn):", 0, 36500);

            var animal = new ZooAnimal(name, sex, species, ageDays);
            if (!_simulation.BuyAnimal(animal))
            {
                global::System.Console.WriteLine("Purchase denied: not enough cash.");
                break;
            }

            try
            {
                habitat.AddAnimal(animal);
                global::System.Console.WriteLine("Animal added to the habitat.");
            }
            catch (Exception ex)
            {
                _simulation.SellAnimal(animal);
                global::System.Console.WriteLine($"Cannot add animal to habitat: {ex.Message}");
                break;
            }
        }
    }
}
