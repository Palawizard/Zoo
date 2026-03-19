using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Zoo.Desktop;

public sealed class ConfirmationDialog : Window
{
    private ConfirmationDialog(
        string title,
        string message,
        string confirmLabel,
        string? cancelLabel,
        bool isDangerous)
    {
        Title = title;
        SizeToContent = SizeToContent.WidthAndHeight;
        MinWidth = 420;
        MaxWidth = 720;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush.Parse("#0F1722");
        Foreground = Brush.Parse("#EAF1F6");

        var confirmButton = new Button
        {
            Content = confirmLabel,
            MinWidth = 120,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        if (isDangerous)
        {
            confirmButton.Background = Brush.Parse("#2B171B");
            confirmButton.Foreground = Brush.Parse("#FF6B6B");
            confirmButton.BorderBrush = Brush.Parse("#6A3038");
        }
        else
        {
            confirmButton.Background = Brush.Parse("#2EC4B6");
            confirmButton.Foreground = Brush.Parse("#081418");
            confirmButton.BorderBrush = Brush.Parse("#2EC4B6");
        }

        confirmButton.Click += (_, _) => Close(true);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10
        };

        if (!string.IsNullOrWhiteSpace(cancelLabel))
        {
            var cancelButton = new Button
            {
                Content = cancelLabel,
                MinWidth = 120,
                Background = Brush.Parse("#162433"),
                BorderBrush = Brush.Parse("#27384D"),
                Foreground = Brush.Parse("#EAF1F6")
            };
            cancelButton.Click += (_, _) => Close(false);
            actions.Children.Add(cancelButton);
        }

        actions.Children.Add(confirmButton);

        Content = new Border
        {
            Padding = new Thickness(22),
            Child = new StackPanel
            {
                MaxWidth = 620,
                Spacing = 18,
                Children =
                {
                    new TextBlock
                    {
                        Text = title,
                        FontSize = 24,
                        FontWeight = FontWeight.SemiBold,
                        TextWrapping = TextWrapping.Wrap
                    },
                    new TextBlock
                    {
                        Text = message,
                        FontSize = 15,
                        Foreground = Brush.Parse("#B8C5D3"),
                        MaxWidth = 580,
                        TextWrapping = TextWrapping.Wrap
                    },
                    actions
                }
            }
        };
    }

    public static async Task<bool> ShowAsync(
        Window owner,
        string title,
        string message,
        string confirmLabel = "Confirm",
        string? cancelLabel = "Cancel",
        bool isDangerous = false)
    {
        var dialog = new ConfirmationDialog(title, message, confirmLabel, cancelLabel, isDangerous);
        return await dialog.ShowDialog<bool>(owner);
    }
}
