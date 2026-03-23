using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Zoo.Domain.Animals;

namespace Zoo.Desktop.Dialogs;

/// <summary>
/// Dialog used to rename a newborn
/// </summary>
public sealed class AnimalNamingDialog : Window
{
    // The constructor stays private because the dialog is created through ShowAsync
    private AnimalNamingDialog(ZooAnimal newborn)
    {
        Title = "Name newborn";
        SizeToContent = SizeToContent.WidthAndHeight;
        MinWidth = 420;
        MaxWidth = 720;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush.Parse("#0F1722");
        Foreground = Brush.Parse("#EAF1F6");

        var nameBox = new TextBox
        {
            Text = newborn.Name,
            MinWidth = 320
        };

        var keepButton = new Button
        {
            Content = "Keep current",
            MinWidth = 130,
            Background = Brush.Parse("#162433"),
            BorderBrush = Brush.Parse("#27384D"),
            Foreground = Brush.Parse("#EAF1F6")
        };
        keepButton.Click += (_, _) => Close(newborn.Name);

        var saveButton = new Button
        {
            Content = "Save name",
            MinWidth = 130,
            Background = Brush.Parse("#2EC4B6"),
            Foreground = Brush.Parse("#081418"),
            BorderBrush = Brush.Parse("#2EC4B6")
        };
        saveButton.Click += (_, _) => Close(string.IsNullOrWhiteSpace(nameBox.Text) ? newborn.Name : nameBox.Text.Trim());

        Content = new Border
        {
            Padding = new Thickness(22),
            Child = new StackPanel
            {
                MaxWidth = 620,
                Spacing = 16,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Name the newborn",
                        FontSize = 24,
                        FontWeight = FontWeight.SemiBold,
                        TextWrapping = TextWrapping.Wrap
                    },
                    new TextBlock
                    {
                        Text = $"{newborn.Species} newborn ready to be named.",
                        Foreground = Brush.Parse("#B8C5D3"),
                        TextWrapping = TextWrapping.Wrap
                    },
                    nameBox,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 10,
                        Children =
                        {
                            keepButton,
                            saveButton
                        }
                    }
                }
            }
        };
    }

    /// <summary>
    /// Shows the newborn naming dialog
    /// </summary>
    public static async Task<string?> ShowAsync(Window owner, ZooAnimal newborn)
    {
        var dialog = new AnimalNamingDialog(newborn);
        return await dialog.ShowDialog<string?>(owner);
    }
}
