using Avalonia;

namespace Zoo.Desktop;

/// <summary>
/// Entry point of the desktop application
/// </summary>
class Program
{
    /// <summary>
    /// Starts the Avalonia desktop application
    /// </summary>
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    /// <summary>
    /// Builds the Avalonia application
    /// </summary>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
