using Zoo.Domain.Animals;
using Zoo.Domain.Feeding;
using Zoo.Domain.Habitats;

namespace Zoo.Presentation.Console;

public sealed partial class ZooConsoleApp
{
    /// <summary>
    /// Buys food through the console workflow
    /// </summary>
    private void HandleBuyFood()
    {
        var foodType = _input.ReadEnumChoice<FoodType>("Choose food type:");
        var kilograms = _input.ReadPositiveDecimal("Quantity in kg:");
        var label = foodType == FoodType.Meat ? "meat" : "seeds";
        var cost = _foodMarket.Buy(foodType, kilograms);

        if (!_input.ReadYesNo($"Buy {kilograms:0.##} kg of {label} for {cost:0.##} EUR?", defaultValue: true))
        {
            _printer.PrintInfo("Food purchase canceled.");
            return;
        }

        var previousEventCount = _simulation.Events.Count;
        if (_simulation.BuyFood(foodType, kilograms))
        {
            _printer.PrintSuccess($"{kilograms:0.##} kg of {label} purchased.");
            ShowNewEventsAndPendingBirths(previousEventCount);
            return;
        }

        _printer.PrintError("Food purchase denied because the zoo does not have enough cash.");
    }

    /// <summary>
    /// Prints the current food stock
    /// </summary>
    private void ShowFoodStock()
    {
        _printer.PrintInfo($"Food stock: {_simulation.MeatStockKg:0.##} kg meat | {_simulation.SeedsStockKg:0.##} kg seeds");
        _input.WaitForContinue();
    }

    /// <summary>
    /// Buys an animal through the console workflow
    /// </summary>
    private void HandleBuyAnimal()
    {
        var name = _input.ReadRequiredString("Animal name:");
        var species = _input.ReadEnumChoice<SpeciesType>("Choose species:");
        var sex = _input.ReadEnumChoice<SexType>("Choose sex:");
        var ageDays = ReadAnimalAgeDays();
        var autoBuyHabitat = _input.ReadYesNo(
            "Auto-buy a habitat if no free compatible habitat is available?",
            defaultValue: true);

        var animalCost = _animalMarket.BuyAnimalPrice(species, sex, ageDays);
        var habitat = SelectHabitatForSpecies(species);
        var ageLabel = ZooConsoleFormatter.FormatAge(ageDays);
        string confirmationMessage;

        if (habitat is not null)
        {
            if (_simulation.Cash < animalCost)
            {
                _printer.PrintError($"Not enough cash to buy {name}.");
                return;
            }

            confirmationMessage = $"Buy {name} ({species}, {sex}, {ageLabel}) for {animalCost:0.##} EUR?";
        }
        else
        {
            if (!autoBuyHabitat)
            {
                _printer.PrintError($"No free {species} habitat. Enable auto-buy or purchase one first.");
                return;
            }

            var habitatCost = HabitatFactory.Create(species).BuyPrice;
            if (_simulation.Cash < animalCost + habitatCost)
            {
                _printer.PrintError($"Not enough cash to buy {name} and a {species} habitat.");
                return;
            }

            confirmationMessage =
                $"Buy {name} ({species}, {sex}, {ageLabel}) for {animalCost:0.##} EUR and auto-buy one habitat for {habitatCost:0.##} EUR?";
        }

        if (!_input.ReadYesNo(confirmationMessage, defaultValue: true))
        {
            _printer.PrintInfo("Animal purchase canceled.");
            return;
        }

        var previousEventCount = _simulation.Events.Count;

        if (habitat is null)
        {
            if (!_simulation.BuyHabitat(species))
            {
                _printer.PrintError($"No free {species} habitat and not enough cash to auto-buy one.");
                return;
            }

            habitat = SelectHabitatForSpecies(species);
            if (habitat is null)
            {
                _printer.PrintError("Habitat purchase succeeded but no compatible slot was found afterwards.");
                ShowNewEventsAndPendingBirths(previousEventCount);
                return;
            }
        }

        var animal = new ZooAnimal(name, sex, species, ageDays);
        if (!_simulation.BuyAnimal(animal))
        {
            _printer.PrintError($"Not enough cash to buy {name}.");
            return;
        }

        try
        {
            habitat.AddAnimal(animal);
            _printer.PrintSuccess($"{animal.Name} the {animal.Species} has been added to the zoo.");
            ShowNewEventsAndPendingBirths(previousEventCount);
        }
        catch (Exception exception)
        {
            // The animal purchase is rolled back if habitat placement fails
            _simulation.SellAnimal(animal);
            _printer.PrintError($"Animal could not be placed into a habitat: {exception.Message}");
            ShowNewEventsAndPendingBirths(previousEventCount);
        }
    }

    /// <summary>
    /// Sells one selected animal
    /// </summary>
    private void HandleSellAnimal()
    {
        var animals = GetOrderedAnimals();
        var animal = SelectAnimal("Choose an animal to sell:", animals);
        if (animal is null)
            return;

        var revenue = _simulation.EstimateAnimalSalePrice(animal);
        var confirmationMessage = animal.IsAlive
            ? $"Sell {animal.Name} for {revenue:0.##} EUR?"
            : $"Sell {animal.Name}'s remains for {revenue:0.##} EUR?";

        if (!_input.ReadYesNo(confirmationMessage, defaultValue: false))
        {
            _printer.PrintInfo("Animal sale canceled.");
            return;
        }

        var previousEventCount = _simulation.Events.Count;
        if (_simulation.SellAnimal(animal))
        {
            _printer.PrintSuccess($"{animal.Name} was sold.");
            ShowNewEventsAndPendingBirths(previousEventCount);
            return;
        }

        _printer.PrintError($"The sale of {animal.Name} failed.");
    }

    /// <summary>
    /// Buys one habitat through the console workflow
    /// </summary>
    private void HandleBuyHabitat()
    {
        var species = _input.ReadEnumChoice<SpeciesType>("Choose habitat species:");
        var habitat = HabitatFactory.Create(species);

        if (!_input.ReadYesNo($"Buy a {species} habitat for {habitat.BuyPrice:0.##} EUR?", defaultValue: true))
        {
            _printer.PrintInfo("Habitat purchase canceled.");
            return;
        }

        var previousEventCount = _simulation.Events.Count;
        if (_simulation.BuyHabitat(species))
        {
            _printer.PrintSuccess($"{species} habitat purchased.");
            ShowNewEventsAndPendingBirths(previousEventCount);
            return;
        }

        _printer.PrintError($"Not enough cash to buy a {species} habitat.");
    }

    /// <summary>
    /// Sells one selected empty habitat
    /// </summary>
    private void HandleSellHabitat()
    {
        var habitats = GetOrderedHabitats();
        var habitat = SelectHabitat("Choose a habitat to sell:", habitats);
        if (habitat is null)
            return;

        if (habitat.Animals.Count > 0)
        {
            _printer.PrintError("Only empty habitats can be sold.");
            return;
        }

        if (!_input.ReadYesNo($"Sell the {habitat.Species} habitat for {habitat.SellPrice:0.##} EUR?", defaultValue: false))
        {
            _printer.PrintInfo("Habitat sale canceled.");
            return;
        }

        var previousEventCount = _simulation.Events.Count;
        if (_simulation.SellHabitat(habitat))
        {
            _printer.PrintSuccess($"{habitat.Species} habitat sold.");
            ShowNewEventsAndPendingBirths(previousEventCount);
            return;
        }

        _printer.PrintError($"The sale of the {habitat.Species} habitat failed.");
    }

    // Age is entered as years, months and days then converted to days
    private int ReadAnimalAgeDays()
    {
        while (true)
        {
            var years = _input.ReadOptionalNonNegativeInt("Age years (leave empty for 0):");
            var months = _input.ReadOptionalNonNegativeInt("Age months (leave empty for 0):");
            var days = _input.ReadOptionalNonNegativeInt("Age days (leave empty for 0):");

            try
            {
                return checked((years * 365) + (months * 30) + days);
            }
            catch (OverflowException)
            {
                _printer.PrintError("Animal age is too large.");
            }
        }
    }

    // The console follows the same placement rule as the desktop UI
    private Habitat? SelectHabitatForSpecies(SpeciesType species)
    {
        return _simulation.Habitats
            .Where(habitat => habitat.Species == species && habitat.AvailableSlots > 0)
            .OrderByDescending(habitat => habitat.AvailableSlots)
            .FirstOrDefault();
    }
}
