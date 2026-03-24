using Zoo.Application.Simulation;
using Zoo.Desktop.Models;
using Zoo.Desktop.Styling;
using Zoo.Desktop.Utilities;
using Zoo.Domain.Animals;
using Zoo.Domain.Feeding;
using Zoo.Domain.Habitats;

namespace Zoo.Desktop.ViewModels;

public sealed partial class MainWindowViewModel
{
    /// <summary>
    /// Returns the new events created after a given event count
    /// </summary>
    public IReadOnlyList<EventRow> GetNewEventRows(int previousEventCount)
    {
        if (previousEventCount < 0)
            previousEventCount = 0;

        return _simulation.Events
            .Skip(previousEventCount)
            .Where(ShouldShowPopupForEvent)
            .Select(zooEvent => new EventRow(zooEvent))
            .ToList();
    }

    /// <summary>
    /// Advances the simulation for several days
    /// </summary>
    public void AdvanceTurns(int? overrideDays = null)
    {
        if (!TryReadPositiveInt(overrideDays?.ToString() ?? AdvanceDaysInput, "Advance days", out var days))
            return;

        var previousEventCount = _simulation.Events.Count;
        var completedDays = 0;

        // The loop can stop early if a habitat emergency interrupts the simulation
        for (; completedDays < days; completedDays++)
        {
            var state = _simulation.AdvanceTurnWithInterruptions();
            if (state == TurnAdvanceState.AwaitingHabitatEmergencyDecision)
            {
                RefreshSnapshot();
                SetMessage($"Simulation paused after {completedDays + 1} day(s). Resolve the habitat emergency to continue.", isError: true);
                return;
            }
        }

        RefreshSnapshot();

        var newEvents = _simulation.Events.Count - previousEventCount;
        SetMessage($"{completedDays} day(s) simulated. {newEvents} new event(s) logged.", isError: false);
    }

    /// <summary>
    /// Reads and validates the requested number of days to advance
    /// </summary>
    public bool TryGetAdvanceDays(int? overrideDays, out int days)
    {
        return TryReadPositiveInt(overrideDays?.ToString() ?? AdvanceDaysInput, "Advance days", out days);
    }

    /// <summary>
    /// Advances the simulation by one turn
    /// </summary>
    public TurnAdvanceState AdvanceSingleTurn()
    {
        var state = _simulation.AdvanceTurnWithInterruptions();
        RefreshSnapshot();

        if (state == TurnAdvanceState.AwaitingHabitatEmergencyDecision)
        {
            SetMessage("Habitat emergency pending. Choose whether to rehouse or euthanize the animals.", isError: true);
            return state;
        }

        return TurnAdvanceState.Completed;
    }

    /// <summary>
    /// Resolves the current habitat emergency from the UI
    /// </summary>
    public bool TryResolvePendingHabitatEmergency(HabitatEmergencyResolution resolution, out string failureReason)
    {
        var success = _simulation.TryResolvePendingHabitatEmergency(resolution, out failureReason);
        RefreshSnapshot();

        if (success)
        {
            var verb = resolution == HabitatEmergencyResolution.RehouseAnimals ? "rehoused" : "euthanized";
            SetMessage($"Habitat emergency resolved. Animals were {verb}.", isError: false);
            return true;
        }

        SetMessage(failureReason, isError: true);
        return false;
    }

    /// <summary>
    /// Finalizes the next newborn name from the UI
    /// </summary>
    public bool TryFinalizePendingNewbornNaming(string? chosenName, out string failureReason)
    {
        var success = _simulation.TryFinalizeNextNewbornNaming(chosenName, out var newborn, out failureReason);
        RefreshSnapshot(selectedAnimalId: newborn?.Id);

        if (success && newborn is not null)
        {
            SetMessage($"{newborn.Name} is ready in the zoo.", isError: false);
            return true;
        }

        SetMessage(failureReason, isError: true);
        return false;
    }

    /// <summary>
    /// Shows the final advance summary in the status area
    /// </summary>
    public void ShowAdvanceSummary(int completedDays, int previousEventCount, bool paused)
    {
        var newEvents = _simulation.Events.Count - previousEventCount;
        var message = paused
            ? $"Simulation paused after {completedDays} day(s). {newEvents} new event(s) logged."
            : $"{completedDays} day(s) simulated. {newEvents} new event(s) logged.";
        SetMessage(message, isError: paused);
    }

    /// <summary>
    /// Sets a status message from the UI layer
    /// </summary>
    public void ShowStatus(string message, bool isError = false)
    {
        SetMessage(message, isError);
    }

    /// <summary>
    /// Returns the confirmation message for buying a habitat
    /// </summary>
    public string? GetBuyHabitatConfirmationMessage()
    {
        var habitat = HabitatFactory.Create(SelectedHabitatSpecies);
        if (_simulation.Cash < habitat.BuyPrice)
        {
            SetCashError($"Not enough cash to buy a {SelectedHabitatSpecies} habitat.");
            return null;
        }

        return $"Buy a {SelectedHabitatSpecies} habitat for {habitat.BuyPrice:0.##} EUR?";
    }

    /// <summary>
    /// Returns the confirmation message for buying food
    /// </summary>
    public string? GetBuyFoodConfirmationMessage()
    {
        if (!TryReadPositiveDecimal(FoodKgInput, "Food quantity", out var kilograms))
            return null;

        var cost = _foodMarket.Buy(SelectedFoodType, kilograms);
        var label = SelectedFoodType == FoodType.Meat ? "meat" : "seeds";

        if (_simulation.Cash < cost)
        {
            SetCashError("Food purchase denied because the zoo does not have enough cash.");
            return null;
        }

        return $"Buy {kilograms:0.##} kg of {label} for {cost:0.##} EUR?";
    }

    /// <summary>
    /// Returns the confirmation message for buying an animal
    /// </summary>
    public string? GetBuyAnimalConfirmationMessage()
    {
        var name = AnimalNameInput.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            SetMessage("Animal name is required.", isError: true);
            return null;
        }

        if (!TryReadAnimalAge(out var ageDays))
            return null;

        var cost = _animalMarket.BuyAnimalPrice(SelectedAnimalSpecies, SelectedAnimalSex, ageDays);
        var hasHabitat = SelectHabitatForSpecies(SelectedAnimalSpecies) is not null;
        var ageLabel = UiTextFormatter.FormatAge(ageDays);

        if (hasHabitat)
        {
            if (_simulation.Cash < cost)
            {
                SetCashError($"Not enough cash to buy {name}.");
                return null;
            }

            return $"Buy {name} ({SelectedAnimalSpecies}, {SelectedAnimalSex}, {ageLabel}) for {cost:0.##} EUR?";
        }

        if (!AutoBuyHabitatForAnimal)
        {
            SetMessage($"No free {SelectedAnimalSpecies} habitat. Enable auto-buy or purchase one first.", isError: true);
            return null;
        }

        // When no habitat is free, the confirmation includes the forced habitat purchase
        var habitatCost = HabitatFactory.Create(SelectedAnimalSpecies).BuyPrice;
        if (_simulation.Cash < cost + habitatCost)
        {
            SetCashError($"Not enough cash to buy {name} and a {SelectedAnimalSpecies} habitat.");
            return null;
        }

        return $"Buy {name} ({SelectedAnimalSpecies}, {SelectedAnimalSex}, {ageLabel}) for {cost:0.##} EUR and auto-buy one habitat for {habitatCost:0.##} EUR?";
    }

    /// <summary>
    /// Returns the confirmation message for selling the selected animal
    /// </summary>
    public string? GetSellAnimalConfirmationMessage()
    {
        if (SelectedAnimalRow is null)
        {
            SetMessage("Select an animal to sell.", isError: true);
            return null;
        }

        var animal = SelectedAnimalRow.Animal;
        var revenue = _simulation.EstimateAnimalSalePrice(animal);
        return animal.IsAlive
            ? $"Sell {animal.Name} for {revenue:0.##} EUR?"
            : $"Sell {animal.Name}'s remains for {revenue:0.##} EUR?";
    }

    /// <summary>
    /// Returns the confirmation message for selling the selected habitat
    /// </summary>
    public string? GetSellHabitatConfirmationMessage()
    {
        if (SelectedHabitatRow is null)
        {
            SetMessage("Select a habitat to sell.", isError: true);
            return null;
        }

        var habitat = SelectedHabitatRow.Habitat;
        if (habitat.Animals.Count > 0)
        {
            SetMessage("Only empty habitats can be sold.", isError: true);
            return null;
        }

        return $"Sell the {habitat.Species} habitat for {habitat.SellPrice:0.##} EUR?";
    }

    /// <summary>
    /// Buys a habitat from the UI state
    /// </summary>
    public void BuyHabitat()
    {
        var selectedAnimalId = SelectedAnimalRow?.Animal.Id;
        var habitat = HabitatFactory.Create(SelectedHabitatSpecies);
        if (_simulation.Cash < habitat.BuyPrice)
        {
            SetCashError($"Not enough cash to buy a {SelectedHabitatSpecies} habitat.");
            return;
        }

        if (_simulation.BuyHabitat(SelectedHabitatSpecies))
        {
            RefreshSnapshot(
                selectedAnimalId: selectedAnimalId,
                selectedHabitatId: _simulation.Habitats.Last().Id);
            SetMessage($"{SelectedHabitatSpecies} habitat purchased.", isError: false);
            return;
        }

        SetCashError($"Not enough cash to buy a {SelectedHabitatSpecies} habitat.");
    }

    /// <summary>
    /// Buys food from the UI state
    /// </summary>
    public void BuyFood()
    {
        if (!TryReadPositiveDecimal(FoodKgInput, "Food quantity", out var kilograms))
            return;

        var cost = _foodMarket.Buy(SelectedFoodType, kilograms);
        if (_simulation.Cash < cost)
        {
            SetCashError("Food purchase denied because the zoo does not have enough cash.");
            return;
        }

        if (_simulation.BuyFood(SelectedFoodType, kilograms))
        {
            RefreshSnapshot();
            SetMessage($"{kilograms:0.##} kg of {SelectedFoodType} purchased.", isError: false);
            return;
        }

        SetCashError("Food purchase denied because the zoo does not have enough cash.");
    }

    /// <summary>
    /// Buys an animal from the UI state
    /// </summary>
    public void BuyAnimal()
    {
        var name = AnimalNameInput.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            SetMessage("Animal name is required.", isError: true);
            return;
        }

        if (!TryReadAnimalAge(out var ageDays))
            return;

        var animalCost = _animalMarket.BuyAnimalPrice(SelectedAnimalSpecies, SelectedAnimalSex, ageDays);
        var habitat = SelectHabitatForSpecies(SelectedAnimalSpecies);

        if (habitat is null)
        {
            if (!AutoBuyHabitatForAnimal)
            {
                SetMessage($"No free {SelectedAnimalSpecies} habitat. Enable auto-buy or purchase one first.", isError: true);
                return;
            }

            var habitatCost = HabitatFactory.Create(SelectedAnimalSpecies).BuyPrice;
            if (_simulation.Cash < animalCost + habitatCost)
            {
                SetCashError($"Not enough cash to buy {name} and a {SelectedAnimalSpecies} habitat.");
                return;
            }

            if (!_simulation.BuyHabitat(SelectedAnimalSpecies))
            {
                SetCashError($"No free {SelectedAnimalSpecies} habitat and not enough cash to auto-buy one.");
                return;
            }

            // The habitat reference is looked up again because the collection just changed
            habitat = SelectHabitatForSpecies(SelectedAnimalSpecies);
            if (habitat is null)
            {
                SetMessage("Habitat purchase succeeded but no compatible slot was found afterwards.", isError: true);
                return;
            }
        }
        else if (_simulation.Cash < animalCost)
        {
            SetCashError($"Not enough cash to buy {name}.");
            return;
        }

        var animal = new ZooAnimal(name, SelectedAnimalSex, SelectedAnimalSpecies, ageDays);
        if (!_simulation.BuyAnimal(animal))
        {
            SetCashError($"Not enough cash to buy {name}.");
            return;
        }

        try
        {
            habitat.AddAnimal(animal);
            ResetAnimalInputs();
            RefreshSnapshot(selectedAnimalId: animal.Id, selectedHabitatId: habitat.Id);
            SetMessage($"{animal.Name} the {animal.Species} has been added to the zoo.", isError: false);
        }
        catch (Exception exception)
        {
            // The animal purchase is rolled back if habitat placement fails
            _simulation.SellAnimal(animal);
            RefreshSnapshot();
            SetMessage($"Animal could not be placed into a habitat: {exception.Message}", isError: true);
        }
    }

    /// <summary>
    /// Sells the selected animal
    /// </summary>
    public void SellSelectedAnimal()
    {
        if (SelectedAnimalRow is null)
        {
            SetMessage("Select an animal to sell.", isError: true);
            return;
        }

        var animal = SelectedAnimalRow.Animal;
        var animalName = animal.Name;
        if (_simulation.SellAnimal(animal))
        {
            RefreshSnapshot();
            SetMessage($"{animalName} was sold.", isError: false);
            return;
        }

        SetMessage($"The sale of {animalName} failed.", isError: true);
    }

    /// <summary>
    /// Sells the selected habitat
    /// </summary>
    public void SellSelectedHabitat()
    {
        if (SelectedHabitatRow is null)
        {
            SetMessage("Select a habitat to sell.", isError: true);
            return;
        }

        var habitat = SelectedHabitatRow.Habitat;
        if (habitat.Animals.Count > 0)
        {
            SetMessage("Only empty habitats can be sold.", isError: true);
            return;
        }

        if (_simulation.SellHabitat(habitat))
        {
            RefreshSnapshot();
            SetMessage($"{habitat.Species} habitat sold.", isError: false);
            return;
        }

        SetMessage($"The sale of the {habitat.Species} habitat failed.", isError: true);
    }

    /// <summary>
    /// Returns the pending cash popup message once
    /// </summary>
    public bool TryTakePendingCashPopupMessage(out string message)
    {
        if (string.IsNullOrWhiteSpace(_pendingCashPopupMessage))
        {
            message = string.Empty;
            return false;
        }

        message = _pendingCashPopupMessage;
        _pendingCashPopupMessage = null;
        return true;
    }

    // The UI picks the habitat with the most free space
    private Habitat? SelectHabitatForSpecies(SpeciesType species)
    {
        return _simulation.Habitats
            .Where(habitat => habitat.Species == species && habitat.AvailableSlots > 0)
            .OrderByDescending(habitat => habitat.AvailableSlots)
            .FirstOrDefault();
    }

    // Inputs are cleared after a successful animal purchase
    private void ResetAnimalInputs()
    {
        AnimalNameInput = string.Empty;
        AnimalAgeYearsInput = string.Empty;
        AnimalAgeMonthsInput = string.Empty;
        AnimalAgeDaysInput = string.Empty;
    }

    // The generic status message also clears the cash popup queue
    private void SetMessage(string message, bool isError)
    {
        StatusMessage = message;
        MessageBackground = isError ? UiBrushes.MessageBadFill : UiBrushes.MessageGoodFill;
        MessageBorderBrush = isError ? UiBrushes.MessageBadBorder : UiBrushes.MessageGoodBorder;
        _pendingCashPopupMessage = null;
    }

    // Cash errors are also stored for the dedicated popup
    private void SetCashError(string message)
    {
        StatusMessage = message;
        MessageBackground = UiBrushes.MessageBadFill;
        MessageBorderBrush = UiBrushes.MessageBadBorder;
        _pendingCashPopupMessage = message;
    }
}
