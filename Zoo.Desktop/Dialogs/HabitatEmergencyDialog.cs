using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Zoo.Application.Simulation;

namespace Zoo.Desktop.Dialogs;

public sealed class HabitatEmergencyDialog : Window
{
    private HabitatEmergencyDialog(PendingHabitatEmergency emergency)
    {
        Title = "Habitat emergency";
        SizeToContent = SizeToContent.WidthAndHeight;
        MinWidth = 460;
        MaxWidth = 760;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush.Parse("#0F1722");
        Foreground = Brush.Parse("#EAF1F6");

        var rehouseButton = new Button
        {
            Content = "Rehouse animals",
            MinWidth = 150,
            Background = Brush.Parse("#2EC4B6"),
            Foreground = Brush.Parse("#081418"),
            BorderBrush = Brush.Parse("#2EC4B6")
        };
        rehouseButton.Click += (_, _) => Close(HabitatEmergencyResolution.RehouseAnimals);

        var euthanizeButton = new Button
        {
            Content = "Euthanize animals",
            MinWidth = 150,
            Background = Brush.Parse("#2B171B"),
            Foreground = Brush.Parse("#FF6B6B"),
            BorderBrush = Brush.Parse("#6A3038")
        };
        euthanizeButton.Click += (_, _) => Close(HabitatEmergencyResolution.EuthanizeAnimals);

        var pauseButton = new Button
        {
            Content = "Pause",
            MinWidth = 110,
            Background = Brush.Parse("#162433"),
            BorderBrush = Brush.Parse("#27384D"),
            Foreground = Brush.Parse("#EAF1F6")
        };
        pauseButton.Click += (_, _) => Close(null);

        Content = new Border
        {
            Padding = new Thickness(22),
            Child = new StackPanel
            {
                MaxWidth = 640,
                Spacing = 16,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Habitat emergency",
                        FontSize = 24,
                        FontWeight = FontWeight.SemiBold
                    },
                    new TextBlock
                    {
                        Text = emergency.CauseDescription,
                        Foreground = Brush.Parse("#B8C5D3"),
                        TextWrapping = TextWrapping.Wrap
                    },
                    new Border
                    {
                        Background = Brush.Parse("#162433"),
                        BorderBrush = Brush.Parse("#27384D"),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(16),
                        Padding = new Thickness(16),
                        Child = new StackPanel
                        {
                            Spacing = 8,
                            Children =
                            {
                                new TextBlock
                                {
                                    Text = $"{emergency.DisplacedAnimals.Count} {emergency.Species} animal(s) are displaced.",
                                    FontWeight = FontWeight.SemiBold,
                                    TextWrapping = TextWrapping.Wrap
                                },
                                new TextBlock
                                {
                                    Text = $"Replacement habitat cost: {emergency.ReplacementHabitatCost:0.##} EUR",
                                    Foreground = Brush.Parse("#B8C5D3"),
                                    TextWrapping = TextWrapping.Wrap
                                },
                                new TextBlock
                                {
                                    Text = "Choose whether to rehouse the animals or euthanize them. The simulation stays paused until the decision is resolved.",
                                    Foreground = Brush.Parse("#B8C5D3"),
                                    TextWrapping = TextWrapping.Wrap
                                }
                            }
                        }
                    },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 10,
                        Children =
                        {
                            pauseButton,
                            euthanizeButton,
                            rehouseButton
                        }
                    }
                }
            }
        };
    }

    public static async Task<HabitatEmergencyResolution?> ShowAsync(Window owner, PendingHabitatEmergency emergency)
    {
        var dialog = new HabitatEmergencyDialog(emergency);
        return await dialog.ShowDialog<HabitatEmergencyResolution?>(owner);
    }
}
