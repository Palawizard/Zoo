using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Zoo.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

    private void HandleAdvanceTurns(object? sender, RoutedEventArgs e) => ViewModel.AdvanceTurns();

    private void HandleAdvancePreset(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: int days })
        {
            ViewModel.AdvanceTurns(days);
            return;
        }

        if (sender is Button { Tag: string rawDays } && int.TryParse(rawDays, out var parsedDays))
            ViewModel.AdvanceTurns(parsedDays);
    }

    private void HandleBuyHabitat(object? sender, RoutedEventArgs e) => ViewModel.BuyHabitat();

    private void HandleBuyFood(object? sender, RoutedEventArgs e) => ViewModel.BuyFood();

    private void HandleBuyAnimal(object? sender, RoutedEventArgs e) => ViewModel.BuyAnimal();

    private void HandleSellAnimal(object? sender, RoutedEventArgs e) => ViewModel.SellSelectedAnimal();

    private void HandleSellHabitat(object? sender, RoutedEventArgs e) => ViewModel.SellSelectedHabitat();
}
