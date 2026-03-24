using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Zoo.Desktop.Views;

namespace Zoo.Desktop;

/// <summary>
/// Avalonia application entry point
/// </summary>
public class App : Avalonia.Application
{
    /// <summary>
    /// Loads the application resources
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Creates the main window when the desktop lifetime is ready
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
