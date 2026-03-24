using Zoo.Domain.Animals;
using Zoo.Domain.Events;
using Zoo.Domain.Habitats;

namespace Zoo.Presentation.Console;

/// <summary>
/// Formats domain data for the console UI
/// </summary>
public static class ZooConsoleFormatter
{
    /// <summary>
    /// Converts an age in days to a readable label
    /// </summary>
    public static string FormatAge(int ageDays)
    {
        if (ageDays <= 0)
            return "0 days";

        var years = ageDays / 365;
        var remainingDaysAfterYears = ageDays % 365;
        var months = remainingDaysAfterYears / 30;
        var days = remainingDaysAfterYears % 30;

        // Empty units are skipped to keep the label compact
        var parts = new List<string>(3);
        if (years > 0)
            parts.Add($"{years} year{(years == 1 ? string.Empty : "s")}");
        if (months > 0)
            parts.Add($"{months} month{(months == 1 ? string.Empty : "s")}");
        if (days > 0 || parts.Count == 0)
            parts.Add($"{days} day{(days == 1 ? string.Empty : "s")}");

        return string.Join(", ", parts);
    }

    /// <summary>
    /// Splits an event type into readable words
    /// </summary>
    public static string HumanizeEventType(ZooEventType type)
    {
        var value = type.ToString();
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

    /// <summary>
    /// Returns the current habitat label of one animal
    /// </summary>
    public static string FindHabitatLabel(ZooAnimal animal, IEnumerable<Habitat> habitats)
    {
        var habitat = habitats.FirstOrDefault(candidate => candidate.Animals.Contains(animal));
        return habitat is null ? "No habitat" : $"{habitat.Species} habitat";
    }

    /// <summary>
    /// Returns the current top-level marker of one animal
    /// </summary>
    public static string DescribeAnimalMarker(ZooAnimal animal)
    {
        return animal.IsAlive
            ? animal.IsGestating
                ? "Gestating"
                : animal.EggIncubationRemainingDays > 0
                    ? "Incubating"
                    : animal.IsHungry
                        ? "Hungry"
                        : animal.IsSick
                            ? "Sick"
                            : "Stable"
            : "Dead";
    }
}
