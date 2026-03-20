using Avalonia.Media;
using Zoo.Desktop.Styling;
using Zoo.Domain.Animals;

namespace Zoo.Desktop.Models;

public sealed class RevenueRow
{
    public RevenueRow(SpeciesType species, decimal projectedRevenue, int exposedAnimals)
    {
        Species = species;
        ProjectedRevenue = projectedRevenue;
        ExposedAnimals = exposedAnimals;
    }

    public SpeciesType Species { get; }
    public decimal ProjectedRevenue { get; }
    public int ExposedAnimals { get; }

    public string Title => Species.ToString();
    public string Value => $"{ProjectedRevenue:0.##} EUR";
    public string Detail => $"{ExposedAnimals} animal(s) visible to visitors";
    public IBrush AccentBrush => ProjectedRevenue > 0m ? UiBrushes.Success : UiBrushes.Muted;
}
