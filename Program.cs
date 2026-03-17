using Zoo.Application.Simulation;
using Zoo.Presentation.ConsoleApp;

var simulation = new ZooSimulationService();
var input = new ConsoleInput();
var printer = new ZooConsolePrinter();

var app = new ZooConsoleApp(simulation, input, printer);
app.Run();
