using Zoo.Application.Simulation;
using Zoo.Domain.Events;

namespace Zoo.Presentation.Console;

public sealed partial class ZooConsoleApp
{
    private const int TurnPassageDelayMilliseconds = 20;

    /// <summary>
    /// Advances the simulation for the requested number of days
    /// </summary>
    private void AdvanceSimulation(int days)
    {
        if (days <= 0)
        {
            _printer.PrintError("Advance days must be greater than or equal to 1.");
            return;
        }

        if (_simulation.PendingHabitatEmergency is not null && !ResolvePendingHabitatEmergency())
            return;

        var previousEventCount = _simulation.Events.Count;
        var completedDays = 0;

        while (completedDays < days)
        {
            var turnNumberBeforeAdvance = _simulation.TurnNumber;
            var eventCountBeforeTurn = _simulation.Events.Count;
            var state = _simulation.AdvanceTurnWithInterruptions();

            if (_simulation.TurnNumber == turnNumberBeforeAdvance &&
                state == TurnAdvanceState.AwaitingHabitatEmergencyDecision)
            {
                _printer.PrintWarning("The simulation is paused until the habitat emergency is resolved.");

                if (!ResolvePendingHabitatEmergency())
                {
                    PrintAdvanceSummary(completedDays, previousEventCount, paused: true);
                    return;
                }

                continue;
            }

            completedDays++;
            _printer.PrintInfo(
                $"Turn {_simulation.TurnNumber} completed. Date: Day {_simulation.CurrentDayOfMonth:00}/{_simulation.CurrentMonth:00}/Year {_simulation.CurrentYear} | Season: {(_simulation.IsHighSeason ? "High" : "Low")} season");

            ShowNewEventsAndPendingBirths(eventCountBeforeTurn);

            if (state != TurnAdvanceState.AwaitingHabitatEmergencyDecision)
            {
                Thread.Sleep(TurnPassageDelayMilliseconds);
                continue;
            }

            if (!ResolvePendingHabitatEmergency())
            {
                PrintAdvanceSummary(completedDays, previousEventCount, paused: true);
                return;
            }

            Thread.Sleep(TurnPassageDelayMilliseconds);
        }

        PrintAdvanceSummary(completedDays, previousEventCount, paused: false);
    }

    /// <summary>
    /// Resolves the current habitat emergency from the console
    /// </summary>
    private bool ResolvePendingHabitatEmergency()
    {
        if (_simulation.PendingHabitatEmergency is null)
        {
            _printer.PrintInfo("No habitat emergency is pending.");
            return true;
        }

        while (_simulation.PendingHabitatEmergency is { } emergency)
        {
            _printer.PrintHabitatEmergency(emergency, _simulation.Cash);
            var choice = _input.ReadMenuSelection("Emergency resolution", EmergencyMenuItems);
            if (choice == EmergencyMenuOption.Pause)
            {
                _printer.PrintWarning("Simulation paused until the habitat emergency is resolved.");
                return false;
            }

            var resolution = choice == EmergencyMenuOption.RehouseAnimals
                ? HabitatEmergencyResolution.RehouseAnimals
                : HabitatEmergencyResolution.EuthanizeAnimals;

            var previousEventCount = _simulation.Events.Count;
            if (_simulation.TryResolvePendingHabitatEmergency(resolution, out var failureReason))
            {
                var successMessage = resolution == HabitatEmergencyResolution.RehouseAnimals
                    ? "Habitat emergency resolved. Animals were rehoused."
                    : "Habitat emergency resolved. Animals were euthanized.";

                _printer.PrintSuccess(successMessage);
                ShowNewEventsAndPendingBirths(previousEventCount);
                continue;
            }

            _printer.PrintError(failureReason);
        }

        return true;
    }

    /// <summary>
    /// Prints the newly created events and finalizes newborn names
    /// </summary>
    private void ShowNewEventsAndPendingBirths(int previousEventCount)
    {
        var newEvents = _simulation.Events
            .Skip(previousEventCount)
            .Where(ShouldDisplayActionEvent)
            .ToList();

        if (newEvents.Count > 0)
        {
            _printer.PrintEvents(newEvents, title: "New events");
            _input.WaitForContinue();
        }

        FinalizePendingNewbornNames(showEmptyMessage: false);
    }

    /// <summary>
    /// Finalizes all newborns currently waiting for a name
    /// </summary>
    private void FinalizePendingNewbornNames(bool showEmptyMessage)
    {
        if (_simulation.PeekNewbornAwaitingName() is null)
        {
            if (showEmptyMessage)
                _printer.PrintInfo("No newborn is waiting for a name.");

            return;
        }

        while (_simulation.PeekNewbornAwaitingName() is { } newborn)
        {
            _printer.PrintNewbornAwaitingName(newborn);
            var chosenName = _input.ReadOptionalString("Final name:");

            if (_simulation.TryFinalizeNextNewbornNaming(chosenName, out var finalizedNewborn, out var failureReason))
            {
                _printer.PrintSuccess($"{finalizedNewborn!.Name} is ready in the zoo.");
                continue;
            }

            _printer.PrintError(failureReason);
            return;
        }
    }

    // The turn-completed event is already summarized separately in the console flow
    private static bool ShouldDisplayActionEvent(ZooEvent zooEvent)
    {
        return zooEvent.Type is not ZooEventType.TurnAdvanced;
    }

    // The end-of-advance summary mirrors the desktop status message
    private void PrintAdvanceSummary(int completedDays, int previousEventCount, bool paused)
    {
        var newEventCount = _simulation.Events.Count - previousEventCount;
        var message = paused
            ? $"Simulation paused after {completedDays} day(s). {newEventCount} new event(s) logged."
            : $"{completedDays} day(s) simulated. {newEventCount} new event(s) logged.";

        if (paused)
            _printer.PrintWarning(message);
        else
            _printer.PrintSuccess(message);
    }
}
