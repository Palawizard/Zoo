using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Zoo.Desktop;

public sealed class EventDialog : Window
{
    private EventDialog(EventRow eventRow)
    {
        Title = eventRow.Title;
        SizeToContent = SizeToContent.WidthAndHeight;
        MinWidth = 420;
        MaxWidth = 720;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush.Parse("#0F1722");
        Foreground = Brush.Parse("#EAF1F6");

        var okButton = new Button
        {
            Content = "OK",
            MinWidth = 110,
            Background = Brush.Parse("#162433"),
            BorderBrush = Brush.Parse("#27384D"),
            Foreground = Brush.Parse("#EAF1F6"),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        okButton.Click += (_, _) => Close();

        Content = new Border
        {
            Padding = new Thickness(22),
            Child = new StackPanel
            {
                MaxWidth = 620,
                Spacing = 16,
                Children =
                {
                    new Border
                    {
                        Background = eventRow.AccentBrush,
                        CornerRadius = new CornerRadius(999),
                        Padding = new Thickness(10, 4),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Child = new TextBlock
                        {
                            Text = eventRow.Title,
                            Foreground = Brushes.White,
                            FontWeight = FontWeight.Bold,
                            TextWrapping = TextWrapping.Wrap
                        }
                    },
                    new TextBlock
                    {
                        Text = eventRow.DateLabel,
                        Foreground = Brush.Parse("#93A4B7"),
                        TextWrapping = TextWrapping.Wrap
                    },
                    new TextBlock
                    {
                        Text = eventRow.Description,
                        FontSize = 15,
                        Foreground = Brush.Parse("#B8C5D3"),
                        MaxWidth = 580,
                        TextWrapping = TextWrapping.Wrap
                    },
                    okButton
                }
            }
        };
    }

    public static async Task ShowAsync(Window owner, EventRow eventRow)
    {
        var dialog = new EventDialog(eventRow);
        await dialog.ShowDialog(owner);
    }
}
