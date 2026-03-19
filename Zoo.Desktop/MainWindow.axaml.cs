using Avalonia.Controls;
using Avalonia.Interactivity;
using Zoo.Application.Simulation;

namespace Zoo.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

    private async void HandleAdvanceTurns(object? sender, RoutedEventArgs e)
    {
        await AdvanceTurnsAsync();
    }

    private async void HandleAdvancePreset(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: int days })
        {
            await AdvanceTurnsAsync(days);
            return;
        }

        if (sender is Button { Tag: string rawDays } && int.TryParse(rawDays, out var parsedDays))
            await AdvanceTurnsAsync(parsedDays);
    }

    private async void HandleBuyHabitat(object? sender, RoutedEventArgs e)
    {
        var confirmation = ViewModel.GetBuyHabitatConfirmationMessage();
        if (confirmation is null)
            return;

        var previousEventCount = ViewModel.EventCount;
        if (await ConfirmationDialog.ShowAsync(this, "Buy habitat", confirmation, confirmLabel: "Buy"))
        {
            ViewModel.BuyHabitat();
            await ShowNewEventDialogsAsync(previousEventCount);
        }
    }

    private async void HandleBuyFood(object? sender, RoutedEventArgs e)
    {
        var confirmation = ViewModel.GetBuyFoodConfirmationMessage();
        if (confirmation is null)
            return;

        var previousEventCount = ViewModel.EventCount;
        if (await ConfirmationDialog.ShowAsync(this, "Buy food", confirmation, confirmLabel: "Buy"))
        {
            ViewModel.BuyFood();
            await ShowNewEventDialogsAsync(previousEventCount);
        }
    }

    private async void HandleBuyAnimal(object? sender, RoutedEventArgs e)
    {
        var confirmation = ViewModel.GetBuyAnimalConfirmationMessage();
        if (confirmation is null)
            return;

        var previousEventCount = ViewModel.EventCount;
        if (await ConfirmationDialog.ShowAsync(this, "Buy animal", confirmation, confirmLabel: "Buy"))
        {
            ViewModel.BuyAnimal();
            await ShowNewEventDialogsAsync(previousEventCount);
        }
    }

    private async void HandleSellAnimal(object? sender, RoutedEventArgs e)
    {
        var confirmation = ViewModel.GetSellAnimalConfirmationMessage();
        if (confirmation is null)
            return;

        var previousEventCount = ViewModel.EventCount;
        if (await ConfirmationDialog.ShowAsync(this, "Sell animal", confirmation, confirmLabel: "Sell", isDangerous: true))
        {
            ViewModel.SellSelectedAnimal();
            await ShowNewEventDialogsAsync(previousEventCount);
        }
    }

    private async void HandleSellHabitat(object? sender, RoutedEventArgs e)
    {
        var confirmation = ViewModel.GetSellHabitatConfirmationMessage();
        if (confirmation is null)
            return;

        var previousEventCount = ViewModel.EventCount;
        if (await ConfirmationDialog.ShowAsync(this, "Sell habitat", confirmation, confirmLabel: "Sell", isDangerous: true))
        {
            ViewModel.SellSelectedHabitat();
            await ShowNewEventDialogsAsync(previousEventCount);
        }
    }

    private async Task AdvanceTurnsAsync(int? overrideDays = null)
    {
        if (!ViewModel.TryGetAdvanceDays(overrideDays, out var days))
            return;

        var previousEventCount = ViewModel.EventCount;
        var completedDays = 0;

        while (completedDays < days)
        {
            var eventCountBeforeTurn = ViewModel.EventCount;
            var state = ViewModel.AdvanceSingleTurn();
            completedDays++;

            await ShowNewEventDialogsAsync(eventCountBeforeTurn);

            if (state != TurnAdvanceState.AwaitingHabitatEmergencyDecision)
                continue;

            var resolved = await ResolvePendingHabitatEmergencyAsync();
            if (!resolved)
            {
                ViewModel.ShowAdvanceSummary(completedDays, previousEventCount, paused: true);
                return;
            }
        }

        ViewModel.ShowAdvanceSummary(completedDays, previousEventCount, paused: false);
    }

    private async Task<bool> ResolvePendingHabitatEmergencyAsync()
    {
        while (ViewModel.PendingHabitatEmergency is { } emergency)
        {
            var resolution = await HabitatEmergencyDialog.ShowAsync(this, emergency);
            if (resolution is null)
            {
                ViewModel.ShowStatus("Simulation paused until the habitat emergency is resolved.", isError: true);
                return false;
            }

            var previousEventCount = ViewModel.EventCount;
            if (ViewModel.TryResolvePendingHabitatEmergency(resolution.Value, out var failureReason))
            {
                await ShowNewEventDialogsAsync(previousEventCount);
                continue;
            }

            await ConfirmationDialog.ShowAsync(
                this,
                "Action unavailable",
                failureReason,
                confirmLabel: "OK",
                cancelLabel: null,
                isDangerous: true);
        }

        return true;
    }

    private async Task ShowNewEventDialogsAsync(int previousEventCount)
    {
        var newEvents = ViewModel.GetNewEventRows(previousEventCount);
        foreach (var eventRow in newEvents)
            await EventDialog.ShowAsync(this, eventRow);

        await ShowPendingNewbornNamingDialogsAsync();
    }

    private async Task ShowPendingNewbornNamingDialogsAsync()
    {
        while (ViewModel.PendingNewbornAwaitingName is { } newborn)
        {
            var chosenName = await AnimalNamingDialog.ShowAsync(this, newborn);
            if (!ViewModel.TryFinalizePendingNewbornNaming(chosenName, out var failureReason))
            {
                await ConfirmationDialog.ShowAsync(
                    this,
                    "Naming unavailable",
                    failureReason,
                    confirmLabel: "OK",
                    cancelLabel: null,
                    isDangerous: true);
                return;
            }
        }
    }
}
