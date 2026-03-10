using Zoo.Application.Simulation;
using Zoo.Domain.Animals;

var simulation = new ZooSimulationService();

Console.WriteLine("Zoo simulation initialized.");
Console.WriteLine($"Animals in zoo: {simulation.Animals.Count}");
Console.WriteLine($"Meat stock (kg): {simulation.MeatStockKg}");
Console.WriteLine($"Seeds stock (kg): {simulation.SeedsStockKg}");
Console.WriteLine($"I have {simulation.Cash} $ !"); 

Console.WriteLine("Simulation");
Console.WriteLine("Buying one tiger habitat...");
var boughtTigerHabitat = simulation.BuyHabitat(SpeciesType.Tiger);
Console.WriteLine($"Habitat bought: {boughtTigerHabitat}");
Console.WriteLine($"Habitats in zoo: {simulation.Habitats.Count}");
Console.WriteLine($"Budget after habitat: {simulation.Cash}€");
