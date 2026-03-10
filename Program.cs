using Zoo.Application.Simulation;

var simulation = new ZooSimulationService();

Console.WriteLine("Zoo simulation initialized.");
Console.WriteLine($"Animals in zoo: {simulation.Animals.Count}");
Console.WriteLine($"Meat stock (kg): {simulation.MeatStockKg}");
Console.WriteLine($"Seeds stock (kg): {simulation.SeedsStockKg}");
Console.WriteLine($"I have {simulation.Cash} $ !"); 
