using System.Collections.ObjectModel;
using Zoo.Desktop.Models;
using Zoo.Desktop.Utilities;
using Zoo.Domain.Animals;
using Zoo.Domain.Events;
using Zoo.Domain.Habitats;

namespace Zoo.Desktop.ViewModels;

public sealed partial class MainWindowViewModel
{
    /// <summary>
    /// Refreshes the whole desktop snapshot from the simulation state
    /// </summary>
    private void RefreshSnapshot(Guid? selectedAnimalId = null, Guid? selectedHabitatId = null)
    {
        var animals = _simulation.Animals.OrderBy(animal => animal.Species).ThenBy(animal => animal.Name).ToList();
        var habitats = _simulation.Habitats.OrderBy(habitat => habitat.Species).ThenByDescending(habitat => habitat.AvailableSlots).ToList();
        var visibleAnimals = _simulation.GetAnimalsExposedToPublic();
        var visibleCount = visibleAnimals.Count;
        var projectedRevenueBySpecies = _simulation.CalculateVisitorRevenueBySpecies(_simulation.IsHighSeason);
        var totalProjectedRevenue = projectedRevenueBySpecies.Values.Sum();
        var aliveAnimals = animals.Where(animal => animal.IsAlive).ToList();

        HeaderDate = $"Day {_simulation.CurrentDayOfMonth:00}/{_simulation.CurrentMonth:00}/Year {_simulation.CurrentYear} | Turn {_simulation.TurnNumber}";
        SeasonLabel = _simulation.IsHighSeason ? "High season" : "Low season";
        SeasonDetail = _simulation.IsHighSeason ? "May to September" : "October to April";

        CashMetric = $"{_simulation.Cash:0.##} EUR";
        CashCaption = $"Last balance recorded in {_simulation.Ledger.Transactions.Count} ledger entries.";
        FoodMetric = $"{_simulation.MeatStockKg:0.##} kg meat | {_simulation.SeedsStockKg:0.##} kg seeds";
        FoodCaption = "Food inventory available for the next feeding cycles.";
        PopulationMetric = $"{aliveAnimals.Count} alive / {animals.Count} total";
        PopulationCaption = $"{aliveAnimals.Count(animal => animal.IsSick)} sick | {aliveAnimals.Count(animal => animal.IsHungry)} hungry";
        ExposureMetric = $"{visibleCount} on show";
        ExposureCaption = $"{habitats.Sum(habitat => habitat.Animals.Count)}/{Math.Max(1, habitats.Sum(habitat => habitat.Capacity))} occupied slots across habitats";
        RevenueMetric = $"{totalProjectedRevenue:0.##} EUR";
        RevenueCaption = "Current estimate.";
        UpdateWatchlist(animals, habitats);

        // Refreshing the selected habitat would normally trigger extra UI updates, so it is muted here
        _isRefreshingSnapshot = true;
        try
        {
            SyncCollection(HabitatRows, habitats.Select(habitat => new HabitatRow(habitat)));

            var targetHabitatId = selectedHabitatId ?? SelectedHabitatRow?.Habitat.Id;
            SelectedHabitatRow = HabitatRows.FirstOrDefault(row => row.Habitat.Id == targetHabitatId)
                ?? HabitatRows.FirstOrDefault();

            RefreshAnimalRows(animals, habitats, selectedAnimalId);
        }
        finally
        {
            _isRefreshingSnapshot = false;
        }

        SyncCollection(
            EventRows,
            _simulation.Events
                .Reverse()
                .Take(20)
                .Select(zooEvent => new EventRow(zooEvent)));

        SyncCollection(
            ImportantEventRows,
            _simulation.Events
                .Where(IsImportantEvent)
                .Reverse()
                .Select(zooEvent => new EventRow(zooEvent)));

        SyncCollection(
            LedgerRows,
            _simulation.Ledger.Transactions
                .Reverse()
                .Take(12)
                .Select(transaction => new LedgerRow(transaction)));

        SyncCollection(
            RevenueRows,
            SpeciesOptions.Select(species =>
                new RevenueRow(
                    species,
                    projectedRevenueBySpecies.GetValueOrDefault(species),
                    visibleAnimals.Count(animal => animal.Species == species))));

        UpdateSelectedAnimalDetails();
        UpdateSelectedHabitatDetails();

        RaisePropertyChanged(nameof(AnimalHeader));
        RaisePropertyChanged(nameof(AnimalPanelCaption));
        RaisePropertyChanged(nameof(HabitatHeader));
        RaisePropertyChanged(nameof(EventHeader));
        RaisePropertyChanged(nameof(ImportantEventHeader));
        RaisePropertyChanged(nameof(LedgerHeader));
    }

    /// <summary>
    /// Refreshes only the animal rows for the selected habitat
    /// </summary>
    private void RefreshAnimalRows(
        IReadOnlyList<ZooAnimal>? orderedAnimals = null,
        IReadOnlyList<Habitat>? habitats = null,
        Guid? selectedAnimalId = null)
    {
        orderedAnimals ??= _simulation.Animals.OrderBy(animal => animal.Species).ThenBy(animal => animal.Name).ToList();
        habitats ??= _simulation.Habitats.OrderBy(habitat => habitat.Species).ThenByDescending(habitat => habitat.AvailableSlots).ToList();

        IEnumerable<ZooAnimal> animalsInSelectedHabitat = SelectedHabitatRow is null
            ? Enumerable.Empty<ZooAnimal>()
            : orderedAnimals.Where(animal => SelectedHabitatRow.Habitat.Animals.Contains(animal));

        // Animal rows are filtered by the selected habitat before being mapped for the UI
        SyncCollection(
            AnimalRows,
            animalsInSelectedHabitat.Select(animal =>
                new AnimalRow(
                    animal,
                    FindHabitatLabel(animal, habitats),
                    DescribeReproductionStatus(animal, habitats))));

        SelectedAnimalRow = AnimalRows.FirstOrDefault(row =>
            row.Animal.Id == (selectedAnimalId ?? SelectedAnimalRow?.Animal.Id));

        RaisePropertyChanged(nameof(AnimalHeader));
        RaisePropertyChanged(nameof(AnimalPanelCaption));
    }

    /// <summary>
    /// Updates the detail panel for the selected animal
    /// </summary>
    private void UpdateSelectedAnimalDetails()
    {
        if (SelectedAnimalRow is null)
        {
            SelectedAnimalTitle = "No animal selected";
            SelectedAnimalSummary = "Select an animal.";
            SelectedAnimalDetail = string.Empty;
            return;
        }

        var animal = SelectedAnimalRow.Animal;
        SelectedAnimalTitle = SelectedAnimalRow.Name;
        SelectedAnimalSummary = $"{animal.Species} | {animal.Sex} | {SelectedAnimalRow.Status}";

        var ageLabel = UiTextFormatter.FormatAge(animal.AgeDays);
        var habitats = _simulation.Habitats.ToList();
        var reproductionLabel = DescribeReproductionStatus(animal, habitats);

        SelectedAnimalDetail = animal switch
        {
            _ when !animal.IsAlive =>
                $"Age {ageLabel} | {SelectedAnimalRow.HabitatLabel} | Dead | {reproductionLabel}",
            _ when animal.IsGestating =>
                $"Age {ageLabel} | {SelectedAnimalRow.HabitatLabel} | Gestation {animal.GestationRemainingDays} day(s) | {reproductionLabel}",
            _ when animal.EggIncubationRemainingDays > 0 =>
                $"Age {ageLabel} | {SelectedAnimalRow.HabitatLabel} | {animal.PendingEggs} egg(s), {animal.EggIncubationRemainingDays} day(s) | {reproductionLabel}",
            _ =>
                $"Age {ageLabel} | {SelectedAnimalRow.HabitatLabel} | Hunger {animal.HungerDebtDays} day(s) | Disease {animal.DiseaseRemainingDays} day(s) | {reproductionLabel}"
        };
    }

    /// <summary>
    /// Updates the detail panel for the selected habitat
    /// </summary>
    private void UpdateSelectedHabitatDetails()
    {
        if (SelectedHabitatRow is null)
        {
            SelectedHabitatTitle = "No habitat selected";
            SelectedHabitatSummary = "Select a habitat.";
            SelectedHabitatDetail = string.Empty;
            return;
        }

        var habitat = SelectedHabitatRow.Habitat;
        SelectedHabitatTitle = $"{habitat.Species} habitat";
        SelectedHabitatSummary = $"{habitat.Animals.Count}/{habitat.Capacity} occupied | Health {habitat.HealthRatio:P0}";
        SelectedHabitatDetail = $"Buy {habitat.BuyPrice:0.##} EUR | Sell {habitat.SellPrice:0.##} EUR | Loss probability {habitat.LossProbability:P0}";
    }

    /// <summary>
    /// Updates the watchlist summary shown on the dashboard
    /// </summary>
    private void UpdateWatchlist(IReadOnlyList<ZooAnimal> animals, IReadOnlyList<Habitat> habitats)
    {
        var aliveAnimals = animals.Where(animal => animal.IsAlive).ToList();
        var sickCount = aliveAnimals.Count(animal => animal.IsSick);
        var hungryCount = aliveAnimals.Count(animal => animal.IsHungry);
        var gestatingCount = aliveAnimals.Count(animal => animal.IsGestating || animal.EggIncubationRemainingDays > 0);
        var emptyHabitatCount = habitats.Count(habitat => habitat.Animals.Count == 0);
        var watchCount = sickCount + hungryCount + gestatingCount;

        WatchlistTitle = watchCount == 0
            ? "No immediate operational alerts"
            : watchCount == 1
                ? "1 issue needs attention"
                : $"{watchCount} issues need attention";
        WatchlistSummary = $"{sickCount} sick | {hungryCount} hungry | {gestatingCount} hidden from visitors | {emptyHabitatCount} empty habitat(s)";
    }

    // The habitat label is derived from the current habitat collection
    private static string FindHabitatLabel(ZooAnimal animal, IEnumerable<Habitat> habitats)
    {
        var habitat = habitats.FirstOrDefault(candidate => candidate.Animals.Contains(animal));
        return habitat is null ? "No habitat" : $"{habitat.Species} habitat";
    }

    // The reproduction note tries to explain why reproduction is blocked
    private string DescribeReproductionStatus(ZooAnimal animal, IReadOnlyList<Habitat> habitats)
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
            !HasSpaceForFutureOffspring(animal, compatibleMate, habitats))
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
    private bool NeedsFreeSpaceForReproduction(ZooAnimal animal)
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
    private bool HasSpaceForFutureOffspring(ZooAnimal animal, ZooAnimal compatibleMate, IReadOnlyList<Habitat> habitats)
    {
        var availableSlots = habitats
            .Where(habitat => habitat.Species == animal.Species)
            .Sum(habitat => habitat.AvailableSlots);

        var reservedSlots = _simulation.Animals
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

    // Turn advanced and routine income stay out of the important list
    private static bool IsImportantEvent(ZooEvent zooEvent)
    {
        return zooEvent.Type is not ZooEventType.TurnAdvanced
            and not ZooEventType.VisitorIncome
            and not ZooEventType.SpoiledMeat;
    }

    // Turn advanced is the only event hidden from popup dialogs
    private static bool ShouldShowPopupForEvent(ZooEvent zooEvent)
    {
        return zooEvent.Type is not ZooEventType.TurnAdvanced;
    }

    // Collections are fully rebuilt from the latest snapshot
    private static void SyncCollection<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();

        foreach (var item in items)
            target.Add(item);
    }
}
