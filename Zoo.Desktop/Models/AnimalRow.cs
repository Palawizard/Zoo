using Avalonia.Media;
using Zoo.Desktop.Styling;
using Zoo.Desktop.Utilities;
using Zoo.Domain.Animals;

namespace Zoo.Desktop.Models;

public sealed class AnimalRow
{
    public AnimalRow(ZooAnimal animal, string habitatLabel, string reproductionNote)
    {
        Animal = animal;
        HabitatLabel = habitatLabel;
        ReproductionNote = reproductionNote;
    }

    public ZooAnimal Animal { get; }
    public string HabitatLabel { get; }
    public string ReproductionNote { get; }

    public string Name => Animal.Name;
    public string Secondary => $"{Animal.Species} | {Animal.Sex}";
    public string Detail =>
        $"Age {UiTextFormatter.FormatAge(Animal.AgeDays)} | {HabitatLabel} | Food {Animal.GetDailyFoodNeedKg():0.##} kg/day";
    public string Marker => Animal.IsAlive
        ? Animal.IsGestating
            ? "Gestating"
            : Animal.EggIncubationRemainingDays > 0
                ? "Incubating"
                : Animal.IsHungry
                    ? "Hungry"
                    : Animal.IsSick
                        ? "Sick"
                        : "Stable"
        : "Dead";
    public string Status => Animal.IsAlive ? Animal.Health.ToString() : "Dead";
    public string FoodNeed => $"{Animal.GetDailyFoodNeedKg():0.##} kg/day";
    public IBrush MarkerBrush => Animal.IsAlive
        ? Animal.IsGestating || Animal.EggIncubationRemainingDays > 0
            ? UiBrushes.Info
            : Animal.IsHungry
                ? UiBrushes.Hungry
                : Animal.IsSick
                    ? UiBrushes.Warning
                    : UiBrushes.Success
        : UiBrushes.Danger;
    public IBrush StatusBrush => Animal.IsAlive
        ? Animal.IsHungry
            ? UiBrushes.Hungry
            : Animal.IsSick
                ? UiBrushes.Warning
                : UiBrushes.Success
        : UiBrushes.Danger;
}
