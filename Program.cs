using System.Diagnostics;
using Zoo.Application.Simulation;
using Zoo.Presentation.Console;

var input = new ConsoleInput();
var launchItems = new List<ConsoleMenuItem<bool>>
{
    new("gui", "Launch GUI instead", "Start the Avalonia desktop application.", true),
    new("cli", "Start CLI interface", "Continue with the console version of the project.", false)
};

while (true)
{
    var launchGui = input.ReadMenuSelection(
        "Are you sure you want to start the project using the CLI interface? (not the best experience!!)",
        launchItems);

    if (!launchGui)
        break;

    if (TryLaunchDesktopApplication())
        return;

    Console.WriteLine("The desktop application could not be launched from here.");
    input.WaitForContinue();
}

// Create the console dependencies
var simulation = new ZooSimulationService(interactiveHabitatEmergencies: true);
var printer = new ZooConsolePrinter();

// Start the console application
var app = new ZooConsoleApp(simulation, input, printer);
app.Run();

// The desktop application is launched as a separate dotnet process
static bool TryLaunchDesktopApplication()
{
    try
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --project Zoo.Desktop/Zoo.Desktop.csproj",
            WorkingDirectory = Directory.GetCurrentDirectory(),
            UseShellExecute = false
        };

        return Process.Start(startInfo) is not null;
    }
    catch
    {
        return false;
    }
}
