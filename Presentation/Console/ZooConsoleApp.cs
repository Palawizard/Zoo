using Zoo.Application.Simulation;
using Zoo.Domain.Animals;
using Zoo.Domain.Combat;
using Zoo.Domain.Feeding;
using Zoo.Domain.Habitats;

namespace Zoo.Presentation.ConsoleApp;

public sealed class ZooConsoleApp
{
    private readonly ZooSimulationService _simulation;
    private readonly ConsoleInput _input;
    private readonly ZooConsolePrinter _printer;

    public ZooConsoleApp(ZooSimulationService simulation, ConsoleInput input, ZooConsolePrinter printer)
    {
        _simulation = simulation;
        _input = input;
        _printer = printer;
    }

    public void Run()
    {
        _printer.PrintWelcome();
        _printer.PrintStatus(_simulation);

        var running = true;
        while (running)
        {
            _printer.PrintMenu();
            var choice = (MenuOption)_input.ReadInt("Your choice:", 0, 8);

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
                case MenuOption.Fight:
                    HandleFight();
                    break;
                case MenuOption.Quit:
                    running = false;
                    break;
                default:
                    Console.WriteLine("Invalid choice.");
                    break;
            }
        }

        Console.WriteLine("Goodbye!");
    }

    private void AdvanceOneTurn()
    {
        var previousEventCount = _simulation.Events.Count;

        _simulation.NextTurn();

        var newEvents = _simulation.Events.Skip(previousEventCount).ToList();

        Console.WriteLine();
        Console.WriteLine("--- End of turn ---");
        Console.WriteLine($"Turn { _simulation.TurnNumber } | Date { _simulation.CurrentDayOfMonth:00}/{ _simulation.CurrentMonth:00}/Year { _simulation.CurrentYear }");
        Console.WriteLine($"Season: {( _simulation.IsHighSeason ? "High" : "Low" )}");

        _printer.PrintEvents(newEvents);
    }

    private void HandleCanteen()
    {
        var foodType = _input.ReadEnumChoice<FoodType>("Choose food type:");
        var kg = _input.ReadDecimal("Quantity in kg:", 0m, 10000m);

        if (_simulation.BuyFood(foodType, kg))
            Console.WriteLine("Purchase successful.");
        else
            Console.WriteLine("Purchase denied: not enough cash.");
    }

    private void HandleAddAnimal()
    {
        var name = _input.ReadRequiredString("Animal name:");
        var species = _input.ReadEnumChoice<SpeciesType>("Choose species:");
        var sex = _input.ReadEnumChoice<SexType>("Choose sex:");
        var ageDays = _input.ReadInt("Age in days (0 = newborn):", 0, 36500);

        var targetHabitat = SelectHabitatForSpecies(species);
        if (targetHabitat is null)
        {
            Console.WriteLine("No habitat available for this species.");
            return;
        }

        var animal = new ZooAnimal(name, sex, species, ageDays);
        var bought = _simulation.BuyAnimal(animal);

        if (!bought)
        {
            Console.WriteLine("Purchase denied: not enough cash.");
            return;
        }

        try
        {
            targetHabitat.AddAnimal(animal);
            Console.WriteLine("Animal added to the zoo.");
        }
        catch (Exception ex)
        {
            _simulation.SellAnimal(animal);
            Console.WriteLine($"Cannot add animal to habitat: {ex.Message}");
        }
    }

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
                Console.WriteLine("Purchase denied: not enough cash.");
                return null;
            }

            habitats = _simulation.Habitats
                .Where(h => h.Species == species && h.AvailableSlots > 0)
                .ToList();
        }

        if (habitats.Count == 1)
            return habitats[0];

        Console.WriteLine("Choose habitat:");
        for (var i = 0; i < habitats.Count; i++)
        {
            var habitat = habitats[i];
            Console.WriteLine($"{i + 1}. {habitat.Species} | {habitat.Animals.Count}/{habitat.Capacity}");
        }

        var choice = _input.ReadInt("Habitat:", 1, habitats.Count);
        return habitats[choice - 1];
    }

    private void HandleBuyHabitat()
    {
        var species = _input.ReadEnumChoice<SpeciesType>("Choose habitat species:");
        if (_simulation.BuyHabitat(species))
        {
            Console.WriteLine("Habitat purchased.");
            OfferAnimalsForHabitat(species);
        }
        else
            Console.WriteLine("Purchase denied: not enough cash.");
    }

    private void HandleSellAnimal()
    {
        var animals = _simulation.Animals.ToList();
        if (animals.Count == 0)
        {
            Console.WriteLine("No animals to sell.");
            return;
        }

        Console.WriteLine("Choose an animal to sell:");
        for (var i = 0; i < animals.Count; i++)
        {
            var animal = animals[i];
            Console.WriteLine($"{i + 1}. {animal.Name} | {animal.Species} | {animal.Sex} | Age: {animal.AgeDays}d");
        }

        var choice = _input.ReadInt("Animal:", 1, animals.Count);
        var selected = animals[choice - 1];

        if (_simulation.SellAnimal(selected))
            Console.WriteLine("Animal sold.");
        else
            Console.WriteLine("Sale failed.");
    }

    private void HandleSellHabitat()
    {
        var habitats = _simulation.Habitats.ToList();
        if (habitats.Count == 0)
        {
            Console.WriteLine("No habitats to sell.");
            return;
        }

        Console.WriteLine("Choose a habitat to sell:");
        for (var i = 0; i < habitats.Count; i++)
        {
            var habitat = habitats[i];
            Console.WriteLine($"{i + 1}. {habitat.Species} | {habitat.Animals.Count}/{habitat.Capacity}");
        }

        var choice = _input.ReadInt("Habitat:", 1, habitats.Count);
        var selected = habitats[choice - 1];

        if (selected.Animals.Count > 0)
        {
            Console.WriteLine("Cannot sell: habitat not empty.");
            return;
        }

        if (_simulation.SellHabitat(selected))
            Console.WriteLine("Habitat sold.");
        else
            Console.WriteLine("Sale failed.");
    }

    private void HandleFight()
    {
        var animals = _simulation.Animals.Where(a => a.IsAlive).ToList();

        if (animals.Count < 2)
        {
            Console.WriteLine("At least 2 alive animals are required to fight.");
            return;
        }

        PrintFighterList(animals);

        var firstIndex  = _input.ReadInt("Fighter 1:", 1, animals.Count) - 1;
        var secondIndex = _input.ReadInt("Fighter 2:", 1, animals.Count) - 1;

        if (firstIndex == secondIndex)
        {
            Console.WriteLine("An animal cannot fight itself.");
            return;
        }

        var result = CombatService.Fight(animals[firstIndex], animals[secondIndex]);
        _printer.PrintCombatResult(result);
    }

    private void PrintFighterList(List<ZooAnimal> animals)
    {
        Console.WriteLine("Available fighters:");
        for (var i = 0; i < animals.Count; i++)
        {
            var a     = animals[i];
            var stats = CombatStatsCatalog.GetStats(a);
            Console.WriteLine($"{i + 1}. {a.Name} | {a.Species} | Force: {stats.Force} | Vitesse: {stats.Vitesse} | Défense: {stats.Defense}");
        }
    }

    private void OfferAnimalsForHabitat(SpeciesType species)
    {
        var habitat = _simulation.Habitats.LastOrDefault(h => h.Species == species);
        if (habitat is null)
            return;

        if (habitat.AvailableSlots <= 0)
        {
            Console.WriteLine("This habitat has no available slots.");
            return;
        }

        var shouldBuy = _input.ReadYesNo("Buy animals for this habitat?");
        if (!shouldBuy)
            return;

        var maxCount = habitat.AvailableSlots;
        var count = _input.ReadInt($"How many animals (1-{maxCount})?", 1, maxCount);

        for (var i = 0; i < count; i++)
        {
            Console.WriteLine($"Animal {i + 1}/{count}");
            var name = _input.ReadRequiredString("Animal name:");
            var sex = _input.ReadEnumChoice<SexType>("Choose sex:");
            var ageDays = _input.ReadInt("Age in days (0 = newborn):", 0, 36500);

            var animal = new ZooAnimal(name, sex, species, ageDays);
            if (!_simulation.BuyAnimal(animal))
            {
                Console.WriteLine("Purchase denied: not enough cash.");
                break;
            }

            try
            {
                habitat.AddAnimal(animal);
                Console.WriteLine("Animal added to the habitat.");
            }
            catch (Exception ex)
            {
                _simulation.SellAnimal(animal);
                Console.WriteLine($"Cannot add animal to habitat: {ex.Message}");
                break;
            }
        }
    }
}
