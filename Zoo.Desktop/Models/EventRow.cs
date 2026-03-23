using Avalonia.Media;
using Zoo.Desktop.Styling;
using Zoo.Domain.Events;

namespace Zoo.Desktop.Models;

/// <summary>
/// View model row used to display one event
/// </summary>
public sealed class EventRow
{
    /// <summary>
    /// Creates a desktop row for one event
    /// </summary>
    public EventRow(ZooEvent zooEvent)
    {
        ZooEvent = zooEvent;
    }

    public ZooEvent ZooEvent { get; }

    public string Title => Humanize(ZooEvent.Type.ToString());
    public string DateLabel => $"Day {ZooEvent.Day:00}/{ZooEvent.Month:00}/Y{ZooEvent.Year} | Turn {ZooEvent.TurnNumber}";
    public string Description => ZooEvent.Description;

    // Event colors roughly separate danger, warning, success and neutral actions
    public IBrush AccentBrush => ZooEvent.Type switch
    {
        ZooEventType.Fire or
        ZooEventType.Theft or
        ZooEventType.DiseaseDeath or
        ZooEventType.HungerDeath or
        ZooEventType.HabitatAnimalsEuthanized or
        ZooEventType.OverpopulationDeath => UiBrushes.Danger,
        ZooEventType.Pests or
        ZooEventType.SpoiledMeat or
        ZooEventType.Disease or
        ZooEventType.HabitatMonthlyLoss or
        ZooEventType.InfantDeath => UiBrushes.Warning,
        ZooEventType.Pregnancy or
        ZooEventType.EggLaying or
        ZooEventType.Birth or
        ZooEventType.AnnualSubsidy or
        ZooEventType.DiseaseRecovered => UiBrushes.Success,
        ZooEventType.VisitorIncome or
        ZooEventType.FoodPurchased or
        ZooEventType.AnimalPurchased or
        ZooEventType.HabitatPurchased or
        ZooEventType.HabitatAnimalsRehoused => UiBrushes.Info,
        _ => UiBrushes.Warning
    };

    // Event type names are split into readable words for the UI
    private static string Humanize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var characters = new List<char>(value.Length + 8);
        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            if (i > 0 && char.IsUpper(current) && char.IsLower(value[i - 1]))
                characters.Add(' ');

            characters.Add(current);
        }

        return new string(characters.ToArray());
    }
}
