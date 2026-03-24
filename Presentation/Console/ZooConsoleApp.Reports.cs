using Zoo.Domain.Animals;
using Zoo.Domain.Events;
using Zoo.Domain.Habitats;

namespace Zoo.Presentation.Console;

public sealed partial class ZooConsoleApp
{
    /// <summary>
    /// Prints the full zoo status
    /// </summary>
    private void ShowFullStatus()
    {
        var animals = GetOrderedAnimals();
        var habitats = GetOrderedHabitats();

        _printer.PrintDashboard(_simulation);
        _printer.PrintHabitats(habitats);
        _printer.PrintAnimals(animals, habitats, zooAnimal => DescribeReproductionStatus(zooAnimal, animals, habitats), "Animals");
        _input.WaitForContinue();
    }

    /// <summary>
    /// Prints the recent event list
    /// </summary>
    private void ShowRecentEvents()
    {
        var recentEvents = _simulation.Events
            .Reverse()
            .Take(20)
            .ToList();

        _printer.PrintEvents(recentEvents, title: "Recent events");
        _input.WaitForContinue();
    }

    /// <summary>
    /// Prints the important event list
    /// </summary>
    private void ShowImportantEvents()
    {
        var importantEvents = _simulation.Events
            .Where(IsImportantEvent)
            .Reverse()
            .ToList();

        _printer.PrintEvents(importantEvents, title: "Important events");
        _input.WaitForContinue();
    }

    /// <summary>
    /// Prints the recent financial ledger
    /// </summary>
    private void ShowLedger()
    {
        var ledgerEntries = _simulation.Ledger.Transactions
            .Reverse()
            .Take(12)
            .ToList();

        _printer.PrintLedger(ledgerEntries);
        _input.WaitForContinue();
    }

    /// <summary>
    /// Prints the projected visitor revenue by species
    /// </summary>
    private void ShowProjectedRevenue()
    {
        var visibleAnimals = _simulation.GetAnimalsExposedToPublic();
        var projectedRevenueBySpecies = _simulation.CalculateVisitorRevenueBySpecies(_simulation.IsHighSeason);

        _printer.PrintProjectedRevenue(projectedRevenueBySpecies, visibleAnimals);
        _input.WaitForContinue();
    }

    /// <summary>
    /// Prints the list of habitats
    /// </summary>
    private void HandleBrowseHabitats()
    {
        _printer.PrintHabitats(GetOrderedHabitats());
        _input.WaitForContinue();
    }

    /// <summary>
    /// Prints the list of animals for one selected habitat
    /// </summary>
    private void HandleBrowseAnimalsByHabitat()
    {
        var habitats = GetOrderedHabitats();
        var habitat = SelectHabitat("Choose a habitat to browse:", habitats);
        if (habitat is null)
            return;

        var animals = habitat.Animals
            .OfType<ZooAnimal>()
            .OrderBy(zooAnimal => zooAnimal.Species)
            .ThenBy(zooAnimal => zooAnimal.Name)
            .ToList();

        _printer.PrintAnimals(animals, habitats, zooAnimal => DescribeReproductionStatus(zooAnimal, _simulation.Animals, habitats), $"Animals in {habitat.Species} habitat");
        _input.WaitForContinue();
    }

    /// <summary>
    /// Prints the details of one selected animal
    /// </summary>
    private void HandleInspectAnimal()
    {
        var animals = GetOrderedAnimals();
        var selectedAnimal = SelectAnimal("Choose an animal to inspect:", animals);
        if (selectedAnimal is null)
            return;

        var habitats = GetOrderedHabitats();
        var containingHabitat = FindHabitatContainingAnimal(selectedAnimal, habitats);
        var salePrice = _simulation.EstimateAnimalSalePrice(selectedAnimal);
        var reproductionStatus = DescribeReproductionStatus(selectedAnimal, animals, habitats);

        _printer.PrintAnimalDetails(selectedAnimal, containingHabitat, reproductionStatus, salePrice);
        _input.WaitForContinue();
    }

    /// <summary>
    /// Prints the details of one selected habitat
    /// </summary>
    private void HandleInspectHabitat()
    {
        var habitats = GetOrderedHabitats();
        var selectedHabitat = SelectHabitat("Choose a habitat to inspect:", habitats);
        if (selectedHabitat is null)
            return;

        var occupants = selectedHabitat.Animals
            .OfType<ZooAnimal>()
            .OrderBy(zooAnimal => zooAnimal.Species)
            .ThenBy(zooAnimal => zooAnimal.Name)
            .ToList();

        _printer.PrintHabitatDetails(selectedHabitat, occupants, habitats, zooAnimal => DescribeReproductionStatus(zooAnimal, _simulation.Animals, habitats));
        _input.WaitForContinue();
    }

    // Animals are shown in the same order as the desktop dashboard
    private IReadOnlyList<ZooAnimal> GetOrderedAnimals()
    {
        return _simulation.Animals
            .OrderBy(animal => animal.Species)
            .ThenBy(animal => animal.Name)
            .ToList();
    }

    // Habitats are shown in the same order as the desktop dashboard
    private IReadOnlyList<Habitat> GetOrderedHabitats()
    {
        return _simulation.Habitats
            .OrderBy(habitat => habitat.Species)
            .ThenByDescending(habitat => habitat.AvailableSlots)
            .ToList();
    }

    // The animal selector stays concise because it is used often by the management flows
    private ZooAnimal? SelectAnimal(string title, IReadOnlyList<ZooAnimal> animals)
    {
        if (animals.Count == 0)
        {
            _printer.PrintInfo("No animals are available.");
            return null;
        }

        var items = animals
            .Select(zooAnimal => new ConsoleMenuItem<ZooAnimal>(
                zooAnimal.Id.ToString(),
                $"{zooAnimal.Name} | {zooAnimal.Species} | {zooAnimal.Sex} | {ZooConsoleFormatter.DescribeAnimalMarker(zooAnimal)}",
                ZooConsoleFormatter.FindHabitatLabel(zooAnimal, _simulation.Habitats),
                zooAnimal))
            .ToList();

        return _input.TryReadMenuSelection(title, items, out var selectedAnimal)
            ? selectedAnimal
            : null;
    }

    // Habitat selection is shared by inspection and sale flows
    private Habitat? SelectHabitat(string title, IReadOnlyList<Habitat> habitats)
    {
        if (habitats.Count == 0)
        {
            _printer.PrintInfo("No habitats are available.");
            return null;
        }

        var items = habitats
            .Select(currentHabitat => new ConsoleMenuItem<Habitat>(
                currentHabitat.Id.ToString(),
                $"{currentHabitat.Species} habitat | {currentHabitat.Animals.Count}/{currentHabitat.Capacity} occupied",
                $"{currentHabitat.AvailableSlots} free slot(s) | Health {currentHabitat.HealthRatio:P0}",
                currentHabitat))
            .ToList();

        return _input.TryReadMenuSelection(title, items, out var selectedHabitat)
            ? selectedHabitat
            : null;
    }

    // The important-event list keeps routine entries out of the spotlight
    private static bool IsImportantEvent(ZooEvent zooEvent)
    {
        return zooEvent.Type is not ZooEventType.TurnAdvanced
            and not ZooEventType.VisitorIncome
            and not ZooEventType.SpoiledMeat;
    }

    // The current habitat is resolved from the latest ordered habitat list
    private static Habitat? FindHabitatContainingAnimal(ZooAnimal animal, IReadOnlyList<Habitat> habitats)
    {
        return habitats.FirstOrDefault(candidate => candidate.Animals.Contains(animal));
    }

    // The reproduction note mirrors the richer desktop detail panel
    private string DescribeReproductionStatus(
        ZooAnimal animal,
        IReadOnlyList<ZooAnimal> animals,
        IReadOnlyList<Habitat> habitats)
    {
        var reasons = new List<string>();
        var habitat = habitats.FirstOrDefault(candidate => candidate.Animals.Contains(animal));
        var compatibleMate = habitat is null ? null : GetCompatibleMate(animal, habitat);

        if (!animal.IsAlive)
            return "Reproduction unavailable";

        if (!animal.HasReachedSexualMaturity())
            reasons.Add("too young");
        if (animal.HasReachedReproductionEnd())
            reasons.Add("past reproduction age");
        if (animal.IsHungry)
            reasons.Add("hungry");
        if (animal.IsSick)
            reasons.Add("sick");
        if (animal.IsBlockedFromReproductionByArrival())
            reasons.Add("arrival cooldown");
        if (animal.MonthsUntilNextLitter > 0)
            reasons.Add($"{animal.MonthsUntilNextLitter} month cooldown");
        if (animal.IsGestating)
            reasons.Add("already gestating");
        if (animal.EggIncubationRemainingDays > 0)
            reasons.Add("incubating eggs");
        if (habitat is null)
            reasons.Add("no habitat");
        if (habitat is not null && compatibleMate is null)
            reasons.Add("no compatible mate");

        if (NeedsFreeSpaceForReproduction(animal) &&
            compatibleMate is not null &&
            !HasSpaceForFutureOffspring(animal, compatibleMate, animals, habitats))
        {
            reasons.Add("not enough free space");
        }

        if (animal.Sex == SexType.Female &&
            animal.Profile.EggLayingMonth is { } layingMonth &&
            layingMonth != _simulation.CurrentMonth)
        {
            // Fixed laying month species stay blocked outside their allowed month
            reasons.Add($"lays eggs in month {layingMonth}");
        }

        return reasons.Count == 0
            ? "Reproduction ready"
            : $"Reproduction blocked: {string.Join(", ", reasons.Distinct())}";
    }

    // Compatibility depends on species, sex and the current reproduction state
    private ZooAnimal? GetCompatibleMate(ZooAnimal animal, Habitat habitat)
    {
        return habitat.Animals
            .OfType<ZooAnimal>()
            .Where(candidate => candidate.Id != animal.Id)
            .FirstOrDefault(candidate =>
                candidate.Sex != animal.Sex &&
                candidate.Species == animal.Species &&
                candidate.IsAlive &&
                (candidate.Sex == SexType.Male
                    ? candidate.CanReproduceToday() && candidate.CanReproduceByAge()
                    : candidate.CanStartGestationToday() || candidate.CanLayEggThisMonth(_simulation.CurrentMonth)));
    }

    // Only animals that are otherwise ready to reproduce need the free-space check
    private static bool NeedsFreeSpaceForReproduction(ZooAnimal animal)
    {
        return animal.IsAlive &&
               animal.CanReproduceByAge() &&
               !animal.IsHungry &&
               !animal.IsSick &&
               !animal.IsBlockedFromReproductionByArrival() &&
               animal.MonthsUntilNextLitter == 0 &&
               !animal.IsGestating &&
               animal.EggIncubationRemainingDays == 0;
    }

    // Future offspring reserve habitat slots before they are born
    private bool HasSpaceForFutureOffspring(
        ZooAnimal animal,
        ZooAnimal compatibleMate,
        IReadOnlyList<ZooAnimal> animals,
        IReadOnlyList<Habitat> habitats)
    {
        var availableSlots = habitats
            .Where(currentHabitat => currentHabitat.Species == animal.Species)
            .Sum(currentHabitat => currentHabitat.AvailableSlots);

        var reservedSlots = animals
            .Where(existingAnimal => existingAnimal.IsAlive && existingAnimal.Species == animal.Species)
            .Sum(GetReservedOffspringCount);

        var requiredSlots = GetExpectedOffspringSlots(animal, compatibleMate);
        return availableSlots - reservedSlots >= requiredSlots;
    }

    // The expected slot count depends on gestation or egg-laying rules
    private int GetExpectedOffspringSlots(ZooAnimal animal, ZooAnimal? compatibleMate)
    {
        var female = animal.Sex == SexType.Female ? animal : compatibleMate;
        if (female is null)
            return Math.Max(1, animal.Profile.LitterSize ?? 1);

        if (female.Profile.EggLayingMonth is { } layingMonth)
        {
            // Species with a fixed laying month may need zero slots this month
            if (female.Profile.LitterSize is { } litterSize && litterSize > 0 && layingMonth == _simulation.CurrentMonth)
                return litterSize;

            return 0;
        }

        if (female.Profile.EggsPerYear is { } eggsPerYear && eggsPerYear > 0)
            return GetEggCountForMonth(eggsPerYear, _simulation.CurrentMonth);

        if (female.Profile.LitterSize is { } femaleLitterSize && femaleLitterSize > 0)
            return femaleLitterSize;

        return Math.Max(1, animal.Profile.LitterSize ?? compatibleMate?.Profile.LitterSize ?? 1);
    }

    // Yearly egg counts are spread over the first months of the year
    private static int GetEggCountForMonth(int eggsPerYear, int month)
    {
        var baseEggs = eggsPerYear / 12;
        var remainder = eggsPerYear % 12;
        return baseEggs + (month <= remainder ? 1 : 0);
    }

    // Pregnancies and incubating eggs already consume future space
    private static int GetReservedOffspringCount(ZooAnimal animal)
    {
        if (animal.IsGestating)
            return animal.Profile.LitterSize ?? 0;
        if (animal.EggIncubationRemainingDays > 0)
            return animal.PendingEggs;

        return 0;
    }
}
