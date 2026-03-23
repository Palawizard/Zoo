using Zoo.Domain.Animals;

namespace Zoo.Desktop.Utilities;

/// <summary>
/// Formats domain data for the desktop UI
/// </summary>
internal static class UiTextFormatter
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
    /// Returns a readable reproduction status for one animal
    /// </summary>
    public static string DescribeReproduction(Animal animal)
    {
        if (!animal.IsAlive)
            return "Reproduction unavailable";

        var reasons = new List<string>();

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

        // Unlike the richer ViewModel version, this helper stays generic and habitat-agnostic
        return reasons.Count == 0
            ? "Reproduction ready"
            : $"Reproduction blocked: {string.Join(", ", reasons)}";
    }
}
