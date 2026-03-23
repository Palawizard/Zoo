using Avalonia.Media;
using Zoo.Desktop.Styling;
using Zoo.Domain.Habitats;

namespace Zoo.Desktop.Models;

/// <summary>
/// View model row used to display one habitat
/// </summary>
public sealed class HabitatRow
{
    /// <summary>
    /// Creates a desktop row for one habitat
    /// </summary>
    public HabitatRow(Habitat habitat)
    {
        Habitat = habitat;
    }

    public Habitat Habitat { get; }

    public string Title => $"{Habitat.Species} habitat";
    public string Occupancy => $"{Habitat.Animals.Count}/{Habitat.Capacity}";
    public string Detail => $"{Habitat.AvailableSlots} free slot(s) | Sell {Habitat.SellPrice:0.##} EUR";
    public string HealthLabel => $"{Habitat.HealthRatio:P0} healthy";
    public string Sellability => Habitat.Animals.Count == 0 ? "Ready to sell" : "Occupied";
    public IBrush HealthBrush => Habitat.HealthRatio switch
    {
        >= 0.8m => UiBrushes.Success,
        >= 0.5m => UiBrushes.Info,
        _ => UiBrushes.Warning
    };
    public IBrush SellabilityBrush => Habitat.Animals.Count == 0 ? UiBrushes.Success : UiBrushes.Warning;
}
