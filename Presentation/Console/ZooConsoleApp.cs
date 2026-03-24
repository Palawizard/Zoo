using Zoo.Application.Simulation;
using Zoo.Domain.Finance;
using Zoo.Domain.Feeding;

namespace Zoo.Presentation.Console;

/// <summary>
/// Runs the console version of the zoo application
/// </summary>
public sealed partial class ZooConsoleApp
{
    private static readonly IReadOnlyList<ConsoleMenuItem<MainMenuOption>> MainMenuItems =
    [
        new("sim", "Simulation", "Advance time and resolve habitat emergencies.", MainMenuOption.Simulation),
        new("food", "Food", "Buy food and inspect the current stock.", MainMenuOption.Food),
        new("animal", "Animals", "Buy, sell and inspect animals.", MainMenuOption.Animals),
        new("hab", "Habitats", "Buy, sell and inspect habitats.", MainMenuOption.Habitats),
        new("report", "Reports", "Browse events, ledger and revenue projections.", MainMenuOption.Reports),
        new("quit", "Quit", "Leave the console application.", MainMenuOption.Quit)
    ];

    private static readonly IReadOnlyList<ConsoleMenuItem<SimulationMenuOption>> SimulationMenuItems =
    [
        new("day", "Advance 1 day", "Run one simulation turn.", SimulationMenuOption.AdvanceOneDay),
        new("week", "Advance 1 week", "Run 7 simulation turns.", SimulationMenuOption.AdvanceOneWeek),
        new("month", "Advance 1 month", "Run 30 simulation turns.", SimulationMenuOption.AdvanceOneMonth),
        new("custom", "Advance custom days", "Run an arbitrary number of days.", SimulationMenuOption.AdvanceCustomDays),
        new("back", "Back", "Return to the main menu.", SimulationMenuOption.Back)
    ];

    private static readonly IReadOnlyList<ConsoleMenuItem<FoodMenuOption>> FoodMenuItems =
    [
        new("buy", "Buy food", "Purchase meat or seeds for the stock.", FoodMenuOption.BuyFood),
        new("stock", "Show food stock", "Display the current meat and seed inventory.", FoodMenuOption.ShowFoodStock),
        new("back", "Back", "Return to the main menu.", FoodMenuOption.Back)
    ];

    private static readonly IReadOnlyList<ConsoleMenuItem<AnimalMenuOption>> AnimalMenuItems =
    [
        new("buy", "Buy animal", "Purchase and place a new animal in the zoo.", AnimalMenuOption.BuyAnimal),
        new("sell", "Sell animal", "Sell one selected animal or its remains.", AnimalMenuOption.SellAnimal),
        new("inspect", "Inspect animal", "Display the full details of one animal.", AnimalMenuOption.InspectAnimal),
        new("habitat", "Browse animals by habitat", "Inspect the animals currently living in one habitat.", AnimalMenuOption.BrowseAnimalsByHabitat),
        new("back", "Back", "Return to the main menu.", AnimalMenuOption.Back)
    ];

    private static readonly IReadOnlyList<ConsoleMenuItem<HabitatMenuOption>> HabitatMenuItems =
    [
        new("buy", "Buy habitat", "Purchase a new habitat for one species.", HabitatMenuOption.BuyHabitat),
        new("sell", "Sell habitat", "Sell one empty habitat.", HabitatMenuOption.SellHabitat),
        new("inspect", "Inspect habitat", "Display the full details of one habitat.", HabitatMenuOption.InspectHabitat),
        new("list", "Browse habitats", "Display the current list of habitats.", HabitatMenuOption.BrowseHabitats),
        new("back", "Back", "Return to the main menu.", HabitatMenuOption.Back)
    ];

    private static readonly IReadOnlyList<ConsoleMenuItem<ReportMenuOption>> ReportMenuItems =
    [
        new("events", "Recent events", "Show the 20 most recent simulation events.", ReportMenuOption.ShowRecentEvents),
        new("important", "Important events", "Show the major non-routine simulation events.", ReportMenuOption.ShowImportantEvents),
        new("ledger", "Ledger", "Show the 12 latest financial transactions.", ReportMenuOption.ShowLedger),
        new("revenue", "Projected revenue", "Show the current visitor revenue estimate by species.", ReportMenuOption.ShowProjectedRevenue),
        new("status", "Full zoo status", "Show the dashboard, habitats and animals in one report.", ReportMenuOption.ShowFullStatus),
        new("back", "Back", "Return to the main menu.", ReportMenuOption.Back)
    ];

    private static readonly IReadOnlyList<ConsoleMenuItem<EmergencyMenuOption>> EmergencyMenuItems =
    [
        new("rehouse", "Rehouse animals", "Buy replacement capacity if needed and move the displaced animals.", EmergencyMenuOption.RehouseAnimals),
        new("euth", "Euthanize animals", "Kill the displaced animals and close the emergency.", EmergencyMenuOption.EuthanizeAnimals),
        new("pause", "Pause", "Leave the emergency pending and return to the menu.", EmergencyMenuOption.Pause)
    ];

    private readonly ZooSimulationService _simulation;
    private readonly ConsoleInput _input;
    private readonly ZooConsolePrinter _printer;
    private readonly AnimalMarket _animalMarket = new();
    private readonly FoodMarket _foodMarket = new();

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

        var running = true;
        while (running)
        {
            var headerLines = _printer.GetMainMenuHeaderLines(_simulation);
            var choice = _input.ReadMenuSelection("Main menu", MainMenuItems, headerLines: headerLines);
            switch (choice)
            {
                case MainMenuOption.Simulation:
                    HandleSimulationMenu();
                    break;
                case MainMenuOption.Food:
                    HandleFoodMenu();
                    break;
                case MainMenuOption.Animals:
                    HandleAnimalMenu();
                    break;
                case MainMenuOption.Habitats:
                    HandleHabitatMenu();
                    break;
                case MainMenuOption.Reports:
                    HandleReportsMenu();
                    break;
                case MainMenuOption.Quit:
                    running = false;
                    break;
            }
        }

        _printer.PrintInfo("Goodbye!");
    }

    // Each submenu loops until the user explicitly goes back
    private void HandleSimulationMenu()
    {
        var inSimulationMenu = true;

        while (inSimulationMenu)
        {
            var choice = _input.ReadMenuSelection("Simulation menu", SimulationMenuItems);

            switch (choice)
            {
                case SimulationMenuOption.AdvanceOneDay:
                    AdvanceSimulation(1);
                    break;
                case SimulationMenuOption.AdvanceOneWeek:
                    AdvanceSimulation(7);
                    break;
                case SimulationMenuOption.AdvanceOneMonth:
                    AdvanceSimulation(30);
                    break;
                case SimulationMenuOption.AdvanceCustomDays:
                    AdvanceSimulation(_input.ReadPositiveInt("How many days should be simulated?"));
                    break;
                case SimulationMenuOption.Back:
                    inSimulationMenu = false;
                    break;
            }
        }
    }

    // Food actions stay grouped because they share the same stock context
    private void HandleFoodMenu()
    {
        var inFoodMenu = true;

        while (inFoodMenu)
        {
            var choice = _input.ReadMenuSelection("Food menu", FoodMenuItems);

            switch (choice)
            {
                case FoodMenuOption.BuyFood:
                    HandleBuyFood();
                    break;
                case FoodMenuOption.ShowFoodStock:
                    ShowFoodStock();
                    break;
                case FoodMenuOption.Back:
                    inFoodMenu = false;
                    break;
            }
        }
    }

    // Animal actions combine market operations and inspection workflows
    private void HandleAnimalMenu()
    {
        var inAnimalMenu = true;

        while (inAnimalMenu)
        {
            var choice = _input.ReadMenuSelection("Animal menu", AnimalMenuItems);

            switch (choice)
            {
                case AnimalMenuOption.BuyAnimal:
                    HandleBuyAnimal();
                    break;
                case AnimalMenuOption.SellAnimal:
                    HandleSellAnimal();
                    break;
                case AnimalMenuOption.InspectAnimal:
                    HandleInspectAnimal();
                    break;
                case AnimalMenuOption.BrowseAnimalsByHabitat:
                    HandleBrowseAnimalsByHabitat();
                    break;
                case AnimalMenuOption.Back:
                    inAnimalMenu = false;
                    break;
            }
        }
    }

    // Habitat actions keep purchases, sales and browsing in the same area
    private void HandleHabitatMenu()
    {
        var inHabitatMenu = true;

        while (inHabitatMenu)
        {
            var choice = _input.ReadMenuSelection("Habitat menu", HabitatMenuItems);

            switch (choice)
            {
                case HabitatMenuOption.BuyHabitat:
                    HandleBuyHabitat();
                    break;
                case HabitatMenuOption.SellHabitat:
                    HandleSellHabitat();
                    break;
                case HabitatMenuOption.InspectHabitat:
                    HandleInspectHabitat();
                    break;
                case HabitatMenuOption.BrowseHabitats:
                    HandleBrowseHabitats();
                    break;
                case HabitatMenuOption.Back:
                    inHabitatMenu = false;
                    break;
            }
        }
    }

    // Reports expose the richer desktop-style read-only views
    private void HandleReportsMenu()
    {
        var inReportsMenu = true;

        while (inReportsMenu)
        {
            var choice = _input.ReadMenuSelection("Reports menu", ReportMenuItems);

            switch (choice)
            {
                case ReportMenuOption.ShowRecentEvents:
                    ShowRecentEvents();
                    break;
                case ReportMenuOption.ShowImportantEvents:
                    ShowImportantEvents();
                    break;
                case ReportMenuOption.ShowLedger:
                    ShowLedger();
                    break;
                case ReportMenuOption.ShowProjectedRevenue:
                    ShowProjectedRevenue();
                    break;
                case ReportMenuOption.ShowFullStatus:
                    ShowFullStatus();
                    break;
                case ReportMenuOption.Back:
                    inReportsMenu = false;
                    break;
            }
        }
    }
}
