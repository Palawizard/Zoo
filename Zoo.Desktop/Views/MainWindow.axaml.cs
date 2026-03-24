using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Zoo.Application.Simulation;
using Zoo.Desktop.Dialogs;
using Zoo.Desktop.ViewModels;

namespace Zoo.Desktop.Views;

/// <summary>
/// Code-behind for the desktop main window
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Creates the main window and its view model
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    /// <summary>
    /// Loads the XAML view
    /// </summary>
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

    /// <summary>
    /// Handles the generic advance action
    /// </summary>
    private async void HandleAdvanceTurns(object? sender, RoutedEventArgs e)
    {
        await AdvanceTurnsAsync();
    }

    /// <summary>
    /// Handles preset advance buttons
    /// </summary>
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

    /// <summary>
    /// Handles the buy habitat action
    /// </summary>
    private async void HandleBuyHabitat(object? sender, RoutedEventArgs e)
    {
        var confirmation = ViewModel.GetBuyHabitatConfirmationMessage();
        if (confirmation is null)
        {
            await ShowPendingCashPopupAsync();
            return;
        }

        var previousEventCount = ViewModel.EventCount;
        if (await ConfirmationDialog.ShowAsync(this, "Buy habitat", confirmation, confirmLabel: "Buy"))
        {
            ViewModel.BuyHabitat();
            await ShowNewEventDialogsAsync(previousEventCount);
            await ShowPendingCashPopupAsync();
        }
    }

    /// <summary>
    /// Handles the buy food action
    /// </summary>
    private async void HandleBuyFood(object? sender, RoutedEventArgs e)
    {
        var confirmation = ViewModel.GetBuyFoodConfirmationMessage();
        if (confirmation is null)
        {
            await ShowPendingCashPopupAsync();
            return;
        }

        var previousEventCount = ViewModel.EventCount;
        if (await ConfirmationDialog.ShowAsync(this, "Buy food", confirmation, confirmLabel: "Buy"))
        {
            ViewModel.BuyFood();
            await ShowNewEventDialogsAsync(previousEventCount);
            await ShowPendingCashPopupAsync();
        }
    }

    /// <summary>
    /// Handles the buy animal action
    /// </summary>
    private async void HandleBuyAnimal(object? sender, RoutedEventArgs e)
    {
        var confirmation = ViewModel.GetBuyAnimalConfirmationMessage();
        if (confirmation is null)
        {
            await ShowPendingCashPopupAsync();
            return;
        }

        var previousEventCount = ViewModel.EventCount;
        if (await ConfirmationDialog.ShowAsync(this, "Buy animal", confirmation, confirmLabel: "Buy"))
        {
            ViewModel.BuyAnimal();
            await ShowNewEventDialogsAsync(previousEventCount);
            await ShowPendingCashPopupAsync();
        }
    }

    /// <summary>
    /// Handles the sell animal action
    /// </summary>
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

    /// <summary>
    /// Handles the sell habitat action
    /// </summary>
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

    // Turn advancement may pause on habitat emergencies
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

            // Dialogs are shown after each turn so the user sees events in order
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

    // The emergency dialog loops until the situation is resolved or the user pauses
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

    // New events are displayed one by one after each action
    private async Task ShowNewEventDialogsAsync(int previousEventCount)
    {
        var newEvents = ViewModel.GetNewEventRows(previousEventCount);
        foreach (var eventRow in newEvents)
            // Events are shown one by one instead of in a grouped popup
            await EventDialog.ShowAsync(this, eventRow);

        await ShowPendingNewbornNamingDialogsAsync();
    }

    // Newborn naming happens after the event popups
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

    // Cash errors are shown through a dedicated confirmation popup
    private async Task ShowPendingCashPopupAsync()
    {
        if (!ViewModel.TryTakePendingCashPopupMessage(out var message))
            return;

        await ConfirmationDialog.ShowAsync(
            this,
            "Not enough cash",
            message,
            confirmLabel: "OK",
            cancelLabel: null,
            isDangerous: true);
    }
}
