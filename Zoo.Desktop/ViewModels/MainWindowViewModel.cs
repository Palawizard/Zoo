using System.Collections.ObjectModel;
using Avalonia.Media;
using Zoo.Application.Simulation;
using Zoo.Desktop.Infrastructure;
using Zoo.Desktop.Models;
using Zoo.Desktop.Styling;
using Zoo.Domain.Animals;
using Zoo.Domain.Feeding;
using Zoo.Domain.Finance;

namespace Zoo.Desktop.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
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
    private bool _isRefreshingSnapshot;
    private string? _pendingCashPopupMessage;

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
            if (!SetProperty(ref _selectedHabitatRow, value))
                return;

            UpdateSelectedHabitatDetails();
            RaisePropertyChanged(nameof(AnimalHeader));
            RaisePropertyChanged(nameof(AnimalPanelCaption));

            if (!_isRefreshingSnapshot)
                RefreshAnimalRows(selectedAnimalId: SelectedAnimalRow?.Animal.Id);
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

    public string AnimalHeader => SelectedHabitatRow is null
        ? "Animals (0)"
        : $"Animals in {SelectedHabitatRow.Title} ({AnimalRows.Count})";
    public string AnimalPanelCaption => SelectedHabitatRow is null
        ? "Select a habitat to inspect its animals."
        : "Showing animals from the selected habitat.";
    public string HabitatHeader => $"Habitats ({HabitatRows.Count})";
    public string EventHeader => $"Recent events ({EventRows.Count})";
    public string ImportantEventHeader => $"Important events ({ImportantEventRows.Count})";
    public string LedgerHeader => $"Ledger ({LedgerRows.Count})";
    public int EventCount => _simulation.Events.Count;
    public PendingHabitatEmergency? PendingHabitatEmergency => _simulation.PendingHabitatEmergency;
    public ZooAnimal? PendingNewbornAwaitingName => _simulation.PeekNewbornAwaitingName();
}
