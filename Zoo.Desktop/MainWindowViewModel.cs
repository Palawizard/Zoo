using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using Zoo.Application.Simulation;
using Zoo.Domain.Animals;
using Zoo.Domain.Events;
using Zoo.Domain.Feeding;
using Zoo.Domain.Finance;
using Zoo.Domain.Habitats;

namespace Zoo.Desktop;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly ZooSimulationService _simulation = new(cash: 80000m, interactiveHabitatEmergencies: true);
    private readonly AnimalMarket _animalMarket = new();
    private readonly FoodMarket _foodMarket = new();

    private string _advanceDaysInput = "7";
    private string _foodKgInput = string.Empty;
    private string _animalNameInput = string.Empty;
    private string _animalAgeYearsInput = string.Empty;
    private string _animalAgeMonthsInput = string.Empty;
    private string _animalAgeDaysInput = string.Empty;
    private bool _autoBuyHabitatForAnimal = true;
    private SpeciesType _selectedHabitatSpecies = SpeciesType.Tiger;
    private FoodType _selectedFoodType = FoodType.Meat;
    private SpeciesType _selectedAnimalSpecies = SpeciesType.Tiger;
    private SexType _selectedAnimalSex = SexType.Female;
    private AnimalRow? _selectedAnimalRow;
    private HabitatRow? _selectedHabitatRow;
    private string _headerDate = string.Empty;
    private string _seasonLabel = string.Empty;
    private string _seasonDetail = string.Empty;
    private string _cashMetric = string.Empty;
    private string _cashCaption = string.Empty;
    private string _foodMetric = string.Empty;
    private string _foodCaption = string.Empty;
    private string _populationMetric = string.Empty;
    private string _populationCaption = string.Empty;
    private string _exposureMetric = string.Empty;
    private string _exposureCaption = string.Empty;
    private string _revenueMetric = string.Empty;
    private string _revenueCaption = string.Empty;
    private string _watchlistTitle = string.Empty;
    private string _watchlistSummary = string.Empty;
    private string _selectedAnimalTitle = string.Empty;
    private string _selectedAnimalSummary = string.Empty;
    private string _selectedAnimalDetail = string.Empty;
    private string _selectedHabitatTitle = string.Empty;
    private string _selectedHabitatSummary = string.Empty;
    private string _selectedHabitatDetail = string.Empty;
    private string _statusMessage = string.Empty;
    private IBrush _messageBackground = UiBrushes.MessageGoodFill;
    private IBrush _messageBorderBrush = UiBrushes.MessageGoodBorder;

    public MainWindowViewModel()
    {
        RefreshSnapshot();
        SetMessage("Zoo ready. Initial budget: 80 000 EUR.", isError: false);
    }

    public IReadOnlyList<SpeciesType> SpeciesOptions { get; } = Enum.GetValues<SpeciesType>();
    public IReadOnlyList<SexType> SexOptions { get; } = Enum.GetValues<SexType>();
    public IReadOnlyList<FoodType> FoodOptions { get; } = Enum.GetValues<FoodType>();

    public ObservableCollection<AnimalRow> AnimalRows { get; } = new();
    public ObservableCollection<HabitatRow> HabitatRows { get; } = new();
    public ObservableCollection<EventRow> EventRows { get; } = new();
    public ObservableCollection<EventRow> ImportantEventRows { get; } = new();
    public ObservableCollection<LedgerRow> LedgerRows { get; } = new();
    public ObservableCollection<RevenueRow> RevenueRows { get; } = new();

    public string AdvanceDaysInput
    {
        get => _advanceDaysInput;
        set => SetProperty(ref _advanceDaysInput, value);
    }

    public string FoodKgInput
    {
        get => _foodKgInput;
        set => SetProperty(ref _foodKgInput, value);
    }

    public string AnimalNameInput
    {
        get => _animalNameInput;
        set => SetProperty(ref _animalNameInput, value);
    }

    public string AnimalAgeYearsInput
    {
        get => _animalAgeYearsInput;
        set => SetProperty(ref _animalAgeYearsInput, value);
    }

    public string AnimalAgeMonthsInput
    {
        get => _animalAgeMonthsInput;
        set => SetProperty(ref _animalAgeMonthsInput, value);
    }

    public string AnimalAgeDaysInput
    {
        get => _animalAgeDaysInput;
        set => SetProperty(ref _animalAgeDaysInput, value);
    }

    public bool AutoBuyHabitatForAnimal
    {
        get => _autoBuyHabitatForAnimal;
        set => SetProperty(ref _autoBuyHabitatForAnimal, value);
    }

    public SpeciesType SelectedHabitatSpecies
    {
        get => _selectedHabitatSpecies;
        set => SetProperty(ref _selectedHabitatSpecies, value);
    }

    public FoodType SelectedFoodType
    {
        get => _selectedFoodType;
        set => SetProperty(ref _selectedFoodType, value);
    }

    public SpeciesType SelectedAnimalSpecies
    {
        get => _selectedAnimalSpecies;
        set => SetProperty(ref _selectedAnimalSpecies, value);
    }

    public SexType SelectedAnimalSex
    {
        get => _selectedAnimalSex;
        set => SetProperty(ref _selectedAnimalSex, value);
    }

    public AnimalRow? SelectedAnimalRow
    {
        get => _selectedAnimalRow;
        set
        {
            if (SetProperty(ref _selectedAnimalRow, value))
                UpdateSelectedAnimalDetails();
        }
    }

    public HabitatRow? SelectedHabitatRow
    {
        get => _selectedHabitatRow;
        set
        {
            if (SetProperty(ref _selectedHabitatRow, value))
                UpdateSelectedHabitatDetails();
        }
    }

    public string HeaderDate
    {
        get => _headerDate;
        private set => SetProperty(ref _headerDate, value);
    }

    public string SeasonLabel
    {
        get => _seasonLabel;
        private set => SetProperty(ref _seasonLabel, value);
    }

    public string SeasonDetail
    {
        get => _seasonDetail;
        private set => SetProperty(ref _seasonDetail, value);
    }

    public string CashMetric
    {
        get => _cashMetric;
        private set => SetProperty(ref _cashMetric, value);
    }

    public string CashCaption
    {
        get => _cashCaption;
        private set => SetProperty(ref _cashCaption, value);
    }

    public string FoodMetric
    {
        get => _foodMetric;
        private set => SetProperty(ref _foodMetric, value);
    }

    public string FoodCaption
    {
        get => _foodCaption;
        private set => SetProperty(ref _foodCaption, value);
    }

    public string PopulationMetric
    {
        get => _populationMetric;
        private set => SetProperty(ref _populationMetric, value);
    }

    public string PopulationCaption
    {
        get => _populationCaption;
        private set => SetProperty(ref _populationCaption, value);
    }

    public string ExposureMetric
    {
        get => _exposureMetric;
        private set => SetProperty(ref _exposureMetric, value);
    }

    public string ExposureCaption
    {
        get => _exposureCaption;
        private set => SetProperty(ref _exposureCaption, value);
    }

    public string RevenueMetric
    {
        get => _revenueMetric;
        private set => SetProperty(ref _revenueMetric, value);
    }

    public string RevenueCaption
    {
        get => _revenueCaption;
        private set => SetProperty(ref _revenueCaption, value);
    }

    public string WatchlistTitle
    {
        get => _watchlistTitle;
        private set => SetProperty(ref _watchlistTitle, value);
    }

    public string WatchlistSummary
    {
        get => _watchlistSummary;
        private set => SetProperty(ref _watchlistSummary, value);
    }

    public string SelectedAnimalTitle
    {
        get => _selectedAnimalTitle;
        private set => SetProperty(ref _selectedAnimalTitle, value);
    }

    public string SelectedAnimalSummary
    {
        get => _selectedAnimalSummary;
        private set => SetProperty(ref _selectedAnimalSummary, value);
    }

    public string SelectedAnimalDetail
    {
        get => _selectedAnimalDetail;
        private set => SetProperty(ref _selectedAnimalDetail, value);
    }

    public string SelectedHabitatTitle
    {
        get => _selectedHabitatTitle;
        private set => SetProperty(ref _selectedHabitatTitle, value);
    }

    public string SelectedHabitatSummary
    {
        get => _selectedHabitatSummary;
        private set => SetProperty(ref _selectedHabitatSummary, value);
    }

    public string SelectedHabitatDetail
    {
        get => _selectedHabitatDetail;
        private set => SetProperty(ref _selectedHabitatDetail, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public IBrush MessageBackground
    {
        get => _messageBackground;
        private set => SetProperty(ref _messageBackground, value);
    }

    public IBrush MessageBorderBrush
    {
        get => _messageBorderBrush;
        private set => SetProperty(ref _messageBorderBrush, value);
    }

    public string AnimalHeader => $"Animals ({AnimalRows.Count})";
    public string HabitatHeader => $"Habitats ({HabitatRows.Count})";
    public string EventHeader => $"Recent events ({EventRows.Count})";
    public string ImportantEventHeader => $"Important events ({ImportantEventRows.Count})";
    public string LedgerHeader => $"Ledger ({LedgerRows.Count})";
    public int EventCount => _simulation.Events.Count;
    public PendingHabitatEmergency? PendingHabitatEmergency => _simulation.PendingHabitatEmergency;
    public ZooAnimal? PendingNewbornAwaitingName => _simulation.PeekNewbornAwaitingName();
    public IReadOnlyList<EventRow> GetNewEventRows(int previousEventCount)
    {
        if (previousEventCount < 0)
            previousEventCount = 0;

        return _simulation.Events
            .Skip(previousEventCount)
            .Where(ShouldShowPopupForEvent)
            .Select(zooEvent => new EventRow(zooEvent))
            .ToList();
    }

    public void AdvanceTurns(int? overrideDays = null)
    {
        if (!TryReadPositiveInt(overrideDays?.ToString() ?? AdvanceDaysInput, "Advance days", out var days))
            return;

        var previousEventCount = _simulation.Events.Count;
        var completedDays = 0;

        for (; completedDays < days; completedDays++)
        {
            var state = _simulation.AdvanceTurnWithInterruptions();
            if (state == TurnAdvanceState.AwaitingHabitatEmergencyDecision)
            {
                RefreshSnapshot();
                SetMessage($"Simulation paused after {completedDays + 1} day(s). Resolve the habitat emergency to continue.", isError: true);
                return;
            }
        }

        RefreshSnapshot();

        var newEvents = _simulation.Events.Count - previousEventCount;
        SetMessage($"{completedDays} day(s) simulated. {newEvents} new event(s) logged.", isError: false);
    }

    public bool TryGetAdvanceDays(int? overrideDays, out int days)
    {
        return TryReadPositiveInt(overrideDays?.ToString() ?? AdvanceDaysInput, "Advance days", out days);
    }

    public TurnAdvanceState AdvanceSingleTurn()
    {
        var state = _simulation.AdvanceTurnWithInterruptions();
        RefreshSnapshot();

        if (state == TurnAdvanceState.AwaitingHabitatEmergencyDecision)
        {
            SetMessage("Habitat emergency pending. Choose whether to rehouse or euthanize the animals.", isError: true);
            return state;
        }

        return TurnAdvanceState.Completed;
    }

    public bool TryResolvePendingHabitatEmergency(HabitatEmergencyResolution resolution, out string failureReason)
    {
        var success = _simulation.TryResolvePendingHabitatEmergency(resolution, out failureReason);
        RefreshSnapshot();

        if (success)
        {
            var verb = resolution == HabitatEmergencyResolution.RehouseAnimals ? "rehoused" : "euthanized";
            SetMessage($"Habitat emergency resolved. Animals were {verb}.", isError: false);
            return true;
        }

        SetMessage(failureReason, isError: true);
        return false;
    }

    public bool TryFinalizePendingNewbornNaming(string? chosenName, out string failureReason)
    {
        var success = _simulation.TryFinalizeNextNewbornNaming(chosenName, out var newborn, out failureReason);
        RefreshSnapshot(selectedAnimalId: newborn?.Id);

        if (success && newborn is not null)
        {
            SetMessage($"{newborn.Name} is ready in the zoo.", isError: false);
            return true;
        }

        SetMessage(failureReason, isError: true);
        return false;
    }

    public void ShowAdvanceSummary(int completedDays, int previousEventCount, bool paused)
    {
        var newEvents = _simulation.Events.Count - previousEventCount;
        var message = paused
            ? $"Simulation paused after {completedDays} day(s). {newEvents} new event(s) logged."
            : $"{completedDays} day(s) simulated. {newEvents} new event(s) logged.";
        SetMessage(message, isError: paused);
    }

    public void ShowStatus(string message, bool isError = false)
    {
        SetMessage(message, isError);
    }

    public string? GetBuyHabitatConfirmationMessage()
    {
        var habitat = HabitatFactory.Create(SelectedHabitatSpecies);
        return $"Buy a {SelectedHabitatSpecies} habitat for {habitat.BuyPrice:0.##} EUR?";
    }

    public string? GetBuyFoodConfirmationMessage()
    {
        if (!TryReadPositiveDecimal(FoodKgInput, "Food quantity", out var kilograms))
            return null;

        var cost = _foodMarket.Buy(SelectedFoodType, kilograms);
        var label = SelectedFoodType == FoodType.Meat ? "meat" : "seeds";
        return $"Buy {kilograms:0.##} kg of {label} for {cost:0.##} EUR?";
    }

    public string? GetBuyAnimalConfirmationMessage()
    {
        var name = AnimalNameInput.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            SetMessage("Animal name is required.", isError: true);
            return null;
        }

        if (!TryReadAnimalAge(out var ageDays))
            return null;

        var cost = _animalMarket.BuyAnimalPrice(SelectedAnimalSpecies, SelectedAnimalSex, ageDays);
        var hasHabitat = SelectHabitatForSpecies(SelectedAnimalSpecies) is not null;
        var ageLabel = UiTextFormatter.FormatAge(ageDays);

        if (hasHabitat)
            return $"Buy {name} ({SelectedAnimalSpecies}, {SelectedAnimalSex}, {ageLabel}) for {cost:0.##} EUR?";

        if (!AutoBuyHabitatForAnimal)
        {
            SetMessage($"No free {SelectedAnimalSpecies} habitat. Enable auto-buy or purchase one first.", isError: true);
            return null;
        }

        var habitatCost = HabitatFactory.Create(SelectedAnimalSpecies).BuyPrice;
        return
            $"Buy {name} ({SelectedAnimalSpecies}, {SelectedAnimalSex}, {ageLabel}) for {cost:0.##} EUR and auto-buy one habitat for {habitatCost:0.##} EUR?";
    }

    public string? GetSellAnimalConfirmationMessage()
    {
        if (SelectedAnimalRow is null)
        {
            SetMessage("Select an animal to sell.", isError: true);
            return null;
        }

        var animal = SelectedAnimalRow.Animal;
        var revenue = _simulation.EstimateAnimalSalePrice(animal);
        return animal.IsAlive
            ? $"Sell {animal.Name} for {revenue:0.##} EUR?"
            : $"Sell {animal.Name}'s remains for {revenue:0.##} EUR?";
    }

    public string? GetSellHabitatConfirmationMessage()
    {
        if (SelectedHabitatRow is null)
        {
            SetMessage("Select a habitat to sell.", isError: true);
            return null;
        }

        var habitat = SelectedHabitatRow.Habitat;
        if (habitat.Animals.Count > 0)
        {
            SetMessage("Only empty habitats can be sold.", isError: true);
            return null;
        }

        return $"Sell the {habitat.Species} habitat for {habitat.SellPrice:0.##} EUR?";
    }

    public void BuyHabitat()
    {
        var selectedAnimalId = SelectedAnimalRow?.Animal.Id;

        if (_simulation.BuyHabitat(SelectedHabitatSpecies))
        {
            RefreshSnapshot(
                selectedAnimalId: selectedAnimalId,
                selectedHabitatId: _simulation.Habitats.Last().Id);
            SetMessage($"{SelectedHabitatSpecies} habitat purchased.", isError: false);
            return;
        }

        SetMessage($"Not enough cash to buy a {SelectedHabitatSpecies} habitat.", isError: true);
    }

    public void BuyFood()
    {
        if (!TryReadPositiveDecimal(FoodKgInput, "Food quantity", out var kilograms))
            return;

        if (_simulation.BuyFood(SelectedFoodType, kilograms))
        {
            RefreshSnapshot();
            SetMessage($"{kilograms:0.##} kg of {SelectedFoodType} purchased.", isError: false);
            return;
        }

        SetMessage("Food purchase denied because the zoo does not have enough cash.", isError: true);
    }

    public void BuyAnimal()
    {
        var name = AnimalNameInput.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            SetMessage("Animal name is required.", isError: true);
            return;
        }

        if (!TryReadAnimalAge(out var ageDays))
            return;

        var habitat = SelectHabitatForSpecies(SelectedAnimalSpecies);
        if (habitat is null)
        {
            if (!AutoBuyHabitatForAnimal)
            {
                SetMessage($"No free {SelectedAnimalSpecies} habitat. Enable auto-buy or purchase one first.", isError: true);
                return;
            }

            if (!_simulation.BuyHabitat(SelectedAnimalSpecies))
            {
                SetMessage($"No free {SelectedAnimalSpecies} habitat and not enough cash to auto-buy one.", isError: true);
                return;
            }

            habitat = SelectHabitatForSpecies(SelectedAnimalSpecies);
            if (habitat is null)
            {
                SetMessage("Habitat purchase succeeded but no compatible slot was found afterwards.", isError: true);
                return;
            }
        }

        var animal = new ZooAnimal(name, SelectedAnimalSex, SelectedAnimalSpecies, ageDays);
        if (!_simulation.BuyAnimal(animal))
        {
            SetMessage("Animal purchase denied because the zoo does not have enough cash.", isError: true);
            return;
        }

        try
        {
            habitat.AddAnimal(animal);
            AnimalNameInput = string.Empty;
            AnimalAgeYearsInput = string.Empty;
            AnimalAgeMonthsInput = string.Empty;
            AnimalAgeDaysInput = string.Empty;
            RefreshSnapshot(selectedAnimalId: animal.Id, selectedHabitatId: habitat.Id);
            SetMessage($"{animal.Name} the {animal.Species} has been added to the zoo.", isError: false);
        }
        catch (Exception exception)
        {
            _simulation.SellAnimal(animal);
            RefreshSnapshot();
            SetMessage($"Animal could not be placed into a habitat: {exception.Message}", isError: true);
        }
    }

    public void SellSelectedAnimal()
    {
        if (SelectedAnimalRow is null)
        {
            SetMessage("Select an animal to sell.", isError: true);
            return;
        }

        var animal = SelectedAnimalRow.Animal;
        var animalName = animal.Name;
        if (_simulation.SellAnimal(animal))
        {
            RefreshSnapshot();
            SetMessage($"{animalName} was sold.", isError: false);
            return;
        }

        SetMessage($"The sale of {animalName} failed.", isError: true);
    }

    public void SellSelectedHabitat()
    {
        if (SelectedHabitatRow is null)
        {
            SetMessage("Select a habitat to sell.", isError: true);
            return;
        }

        var habitat = SelectedHabitatRow.Habitat;
        if (habitat.Animals.Count > 0)
        {
            SetMessage("Only empty habitats can be sold.", isError: true);
            return;
        }

        if (_simulation.SellHabitat(habitat))
        {
            RefreshSnapshot();
            SetMessage($"{habitat.Species} habitat sold.", isError: false);
            return;
        }

        SetMessage($"The sale of the {habitat.Species} habitat failed.", isError: true);
    }

    private void RefreshSnapshot(Guid? selectedAnimalId = null, Guid? selectedHabitatId = null)
    {
        var animals = _simulation.Animals.OrderBy(a => a.Species).ThenBy(a => a.Name).ToList();
        var habitats = _simulation.Habitats.OrderBy(h => h.Species).ThenByDescending(h => h.AvailableSlots).ToList();
        var visibleAnimals = _simulation.GetAnimalsExposedToPublic();
        var visibleCount = visibleAnimals.Count;
        var projectedRevenueBySpecies = _simulation.CalculateVisitorRevenueBySpecies(_simulation.IsHighSeason);
        var totalProjectedRevenue = projectedRevenueBySpecies.Values.Sum();
        var aliveAnimals = animals.Where(a => a.IsAlive).ToList();

        HeaderDate = $"Day {_simulation.CurrentDayOfMonth:00}/{_simulation.CurrentMonth:00}/Year {_simulation.CurrentYear} | Turn {_simulation.TurnNumber}";
        SeasonLabel = _simulation.IsHighSeason ? "High season" : "Low season";
        SeasonDetail = _simulation.IsHighSeason ? "May to September" : "October to April";

        CashMetric = $"{_simulation.Cash:0.##} EUR";
        CashCaption = $"Last balance recorded in {_simulation.Ledger.Transactions.Count} ledger entries.";
        FoodMetric = $"{_simulation.MeatStockKg:0.##} kg meat | {_simulation.SeedsStockKg:0.##} kg seeds";
        FoodCaption = "Food inventory available for the next feeding cycles.";
        PopulationMetric = $"{aliveAnimals.Count} alive / {animals.Count} total";
        PopulationCaption = $"{aliveAnimals.Count(a => a.IsSick)} sick | {aliveAnimals.Count(a => a.IsHungry)} hungry";
        ExposureMetric = $"{visibleCount} on show";
        ExposureCaption = $"{habitats.Sum(h => h.Animals.Count)}/{Math.Max(1, habitats.Sum(h => h.Capacity))} occupied slots across habitats";
        RevenueMetric = $"{totalProjectedRevenue:0.##} EUR";
        RevenueCaption = "Current estimate.";
        UpdateWatchlist(animals, habitats, visibleCount);

        SyncCollection(
            AnimalRows,
            animals.Select(animal =>
                new AnimalRow(
                    animal,
                    FindHabitatLabel(animal, habitats),
                    DescribeReproductionStatus(animal, habitats))));

        SyncCollection(
            HabitatRows,
            habitats.Select(habitat => new HabitatRow(habitat)));

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

        SelectedAnimalRow = AnimalRows.FirstOrDefault(row =>
            row.Animal.Id == (selectedAnimalId ?? SelectedAnimalRow?.Animal.Id));
        SelectedHabitatRow = HabitatRows.FirstOrDefault(row =>
            row.Habitat.Id == (selectedHabitatId ?? SelectedHabitatRow?.Habitat.Id));

        UpdateSelectedAnimalDetails();
        UpdateSelectedHabitatDetails();

        RaisePropertyChanged(nameof(AnimalHeader));
        RaisePropertyChanged(nameof(HabitatHeader));
        RaisePropertyChanged(nameof(EventHeader));
        RaisePropertyChanged(nameof(ImportantEventHeader));
        RaisePropertyChanged(nameof(LedgerHeader));
    }

    private Habitat? SelectHabitatForSpecies(SpeciesType species)
    {
        return _simulation.Habitats
            .Where(habitat => habitat.Species == species && habitat.AvailableSlots > 0)
            .OrderByDescending(habitat => habitat.AvailableSlots)
            .FirstOrDefault();
    }

    private void SetMessage(string message, bool isError)
    {
        StatusMessage = message;
        MessageBackground = isError ? UiBrushes.MessageBadFill : UiBrushes.MessageGoodFill;
        MessageBorderBrush = isError ? UiBrushes.MessageBadBorder : UiBrushes.MessageGoodBorder;
    }

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
        SelectedAnimalDetail =
            animal switch
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
        SelectedHabitatDetail =
            $"Buy {habitat.BuyPrice:0.##} EUR | Sell {habitat.SellPrice:0.##} EUR | Loss probability {habitat.LossProbability:P0}";
    }

    private void UpdateWatchlist(IReadOnlyList<ZooAnimal> animals, IReadOnlyList<Habitat> habitats, int visibleCount)
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
        WatchlistSummary =
            $"{sickCount} sick | {hungryCount} hungry | {gestatingCount} hidden from visitors | {emptyHabitatCount} empty habitat(s)";
    }

    private static string FindHabitatLabel(ZooAnimal animal, IEnumerable<Habitat> habitats)
    {
        var habitat = habitats.FirstOrDefault(candidate => candidate.Animals.Contains(animal));
        return habitat is null ? "No habitat" : $"{habitat.Species} habitat";
    }

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
            reasons.Add("not enough free space");

        if (animal.Sex == SexType.Female &&
            animal.Profile.EggLayingMonth is int layingMonth &&
            layingMonth != _simulation.CurrentMonth)
        {
            reasons.Add($"lays eggs in month {layingMonth}");
        }

        return reasons.Count == 0
            ? "Reproduction ready"
            : $"Reproduction blocked: {string.Join(", ", reasons.Distinct())}";
    }

    private bool HasCompatibleMate(ZooAnimal animal, Habitat habitat)
    {
        return GetCompatibleMate(animal, habitat) is not null;
    }

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

    private int GetExpectedOffspringSlots(ZooAnimal animal, ZooAnimal? compatibleMate)
    {
        var female = animal.Sex == SexType.Female ? animal : compatibleMate;
        if (female is null)
            return Math.Max(1, animal.Profile.LitterSize ?? 1);

        if (female.Profile.EggLayingMonth is int layingMonth)
        {
            if (female.Profile.LitterSize is int litterSize && litterSize > 0 && layingMonth == _simulation.CurrentMonth)
                return litterSize;

            return 0;
        }

        if (female.Profile.EggsPerYear is int eggsPerYear && eggsPerYear > 0)
            return GetEggCountForMonth(eggsPerYear, _simulation.CurrentMonth);

        if (female.Profile.LitterSize is int femaleLitterSize && femaleLitterSize > 0)
            return femaleLitterSize;

        return Math.Max(1, animal.Profile.LitterSize ?? compatibleMate?.Profile.LitterSize ?? 1);
    }

    private static int GetEggCountForMonth(int eggsPerYear, int month)
    {
        var baseEggs = eggsPerYear / 12;
        var remainder = eggsPerYear % 12;
        return baseEggs + (month <= remainder ? 1 : 0);
    }

    private static int GetReservedOffspringCount(ZooAnimal animal)
    {
        if (animal.IsGestating)
            return animal.Profile.LitterSize ?? 0;

        if (animal.EggIncubationRemainingDays > 0)
            return animal.PendingEggs;

        return 0;
    }

    private static bool IsImportantEvent(Domain.Events.ZooEvent zooEvent)
    {
        return zooEvent.Type is not Domain.Events.ZooEventType.TurnAdvanced
            and not Domain.Events.ZooEventType.VisitorIncome
            and not Domain.Events.ZooEventType.SpoiledMeat;
    }

    private static bool ShouldShowPopupForEvent(ZooEvent zooEvent)
    {
        return zooEvent.Type is not ZooEventType.TurnAdvanced;
    }

    private void SyncCollection<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items)
            target.Add(item);
    }

    private bool TryReadPositiveInt(string rawValue, string label, out int value, bool allowZero = false)
    {
        if (!int.TryParse(rawValue, out value) || (allowZero ? value < 0 : value <= 0))
        {
            var minimum = allowZero ? "0" : "1";
            SetMessage($"{label} must be a whole number greater than or equal to {minimum}.", isError: true);
            return false;
        }

        return true;
    }

    private bool TryReadOptionalNonNegativeInt(string rawValue, string label, out int value)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            value = 0;
            return true;
        }

        if (!int.TryParse(rawValue, out value) || value < 0)
        {
            SetMessage($"{label} must be a whole number greater than or equal to 0.", isError: true);
            return false;
        }

        return true;
    }

    private bool TryReadAnimalAge(out int ageDays)
    {
        ageDays = 0;

        if (!TryReadOptionalNonNegativeInt(AnimalAgeYearsInput, "Animal age years", out var years) ||
            !TryReadOptionalNonNegativeInt(AnimalAgeMonthsInput, "Animal age months", out var months) ||
            !TryReadOptionalNonNegativeInt(AnimalAgeDaysInput, "Animal age days", out var days))
        {
            return false;
        }

        try
        {
            ageDays = checked((years * 365) + (months * 30) + days);
            return true;
        }
        catch (OverflowException)
        {
            SetMessage("Animal age is too large.", isError: true);
            return false;
        }
    }

    private bool TryReadPositiveDecimal(string rawValue, string label, out decimal value)
    {
        var normalized = rawValue.Trim().Replace(',', '.');
        if (!decimal.TryParse(normalized, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out value) ||
            value <= 0m)
        {
            SetMessage($"{label} must be a positive number.", isError: true);
            return false;
        }

        return true;
    }
}
