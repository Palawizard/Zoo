using System.Globalization;

namespace Zoo.Presentation.ConsoleApp;

public sealed class ConsoleInput
{
    public int ReadInt(string prompt, int min, int max)
    {
        while (true)
        {
            Console.Write($"{prompt} ");
            var input = Console.ReadLine();

            if (int.TryParse(input, out var value) && value >= min && value <= max)
                return value;

            Console.WriteLine($"Please enter a number between {min} and {max}.");
        }
    }

    public decimal ReadDecimal(string prompt, decimal min, decimal max)
    {
        while (true)
        {
            Console.Write($"{prompt} ");
            var input = Console.ReadLine();

            if (TryParseDecimal(input, out var value) && value >= min && value <= max)
                return value;

            Console.WriteLine($"Please enter a number between {min:0.##} and {max:0.##}.");
        }
    }

    public string ReadRequiredString(string prompt)
    {
        while (true)
        {
            Console.Write($"{prompt} ");
            var input = Console.ReadLine()?.Trim();

            if (!string.IsNullOrWhiteSpace(input))
                return input;

            Console.WriteLine("Input required.");
        }
    }

    public bool ReadYesNo(string prompt)
    {
        while (true)
        {
            Console.Write($"{prompt} (y/n) ");
            var input = Console.ReadLine()?.Trim().ToLowerInvariant();

            if (input is "y" or "yes")
                return true;
            if (input is "n" or "no")
                return false;

            Console.WriteLine("Please answer with y/n.");
        }
    }

    public TEnum ReadEnumChoice<TEnum>(string title) where TEnum : struct, Enum
    {
        var values = Enum.GetValues<TEnum>();

        Console.WriteLine(title);
        for (var i = 0; i < values.Length; i++)
        {
            Console.WriteLine($"{i + 1}. {values[i]}");
        }

        var choice = ReadInt("Choice:", 1, values.Length);
        return values[choice - 1];
    }

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
