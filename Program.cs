using Zoo.Application.Simulation;
using Zoo.Presentation.ConsoleApp;

// Create the console dependencies
var simulation = new ZooSimulationService();
var input = new ConsoleInput();
var printer = new ZooConsolePrinter();

// Start the console application
var app = new ZooConsoleApp(simulation, input, printer);
app.Run();
