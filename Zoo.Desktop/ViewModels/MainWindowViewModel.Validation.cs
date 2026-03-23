namespace Zoo.Desktop.ViewModels;

public sealed partial class MainWindowViewModel
{
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
        if (!decimal.TryParse(
                normalized,
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out value) ||
            value <= 0m)
        {
            SetMessage($"{label} must be a positive number.", isError: true);
            return false;
        }

        return true;
    }
}
