using System.Globalization;

namespace Zoo.Presentation.Console;

/// <summary>
/// Handles validated console input
/// </summary>
public sealed class ConsoleInput
{
    /// <summary>
    /// Reads an integer inside the given range
    /// </summary>
    public int ReadInt(string prompt, int min, int max)
    {
        while (true)
        {
            global::System.Console.Write($"{prompt} ");
            var input = global::System.Console.ReadLine();

            if (int.TryParse(input, out var value) && value >= min && value <= max)
                return value;

            global::System.Console.WriteLine($"Please enter a number between {min} and {max}.");
        }
    }

    /// <summary>
    /// Reads a decimal number inside the given range
    /// </summary>
    public decimal ReadDecimal(string prompt, decimal min, decimal max)
    {
        while (true)
        {
            global::System.Console.Write($"{prompt} ");
            var input = global::System.Console.ReadLine();

            if (TryParseDecimal(input, out var value) && value >= min && value <= max)
                return value;

            global::System.Console.WriteLine($"Please enter a number between {min:0.##} and {max:0.##}.");
        }
    }

    /// <summary>
    /// Reads a non-empty string
    /// </summary>
    public string ReadRequiredString(string prompt)
    {
        while (true)
        {
            global::System.Console.Write($"{prompt} ");
            var input = global::System.Console.ReadLine()?.Trim();

            if (!string.IsNullOrWhiteSpace(input))
                return input;

            global::System.Console.WriteLine("Input required.");
        }
    }

    /// <summary>
    /// Reads a yes or no answer
    /// </summary>
    public bool ReadYesNo(string prompt)
    {
        while (true)
        {
            global::System.Console.Write($"{prompt} (y/n) ");
            var input = global::System.Console.ReadLine()?.Trim().ToLowerInvariant();

            if (input is "y" or "yes")
                return true;
            if (input is "n" or "no")
                return false;

            global::System.Console.WriteLine("Please answer with y/n.");
        }
    }

    /// <summary>
    /// Lets the user choose one enum value
    /// </summary>
    public TEnum ReadEnumChoice<TEnum>(string title) where TEnum : struct, Enum
    {
        var values = Enum.GetValues<TEnum>();

        global::System.Console.WriteLine(title);
        for (var i = 0; i < values.Length; i++)
        {
            global::System.Console.WriteLine($"{i + 1}. {values[i]}");
        }

        var choice = ReadInt("Choice:", 1, values.Length);

        // Menu choices are displayed from 1, so the array index is shifted back by one
        return values[choice - 1];
    }

    // Both dot and comma are accepted for decimal input
    private static bool TryParseDecimal(string? input, out decimal value)
    {
        if (decimal.TryParse(input, out value))
            return true;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        var normalized = input.Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }
}
