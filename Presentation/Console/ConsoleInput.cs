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

            global::System.Console.WriteLine($"Please enter a whole number between {min} and {max}.");
        }
    }

    /// <summary>
    /// Reads a positive integer
    /// </summary>
    public int ReadPositiveInt(string prompt)
    {
        while (true)
        {
            global::System.Console.Write($"{prompt} ");
            var input = global::System.Console.ReadLine();

            if (int.TryParse(input, out var value) && value > 0)
                return value;

            global::System.Console.WriteLine("Please enter a whole number greater than or equal to 1.");
        }
    }

    /// <summary>
    /// Reads a non-negative integer and treats an empty value as zero
    /// </summary>
    public int ReadOptionalNonNegativeInt(string prompt)
    {
        while (true)
        {
            global::System.Console.Write($"{prompt} ");
            var input = global::System.Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(input))
                return 0;

            if (int.TryParse(input, out var value) && value >= 0)
                return value;

            global::System.Console.WriteLine("Please enter a whole number greater than or equal to 0, or leave the field empty.");
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
    /// Reads a positive decimal number
    /// </summary>
    public decimal ReadPositiveDecimal(string prompt)
    {
        while (true)
        {
            global::System.Console.Write($"{prompt} ");
            var input = global::System.Console.ReadLine();

            if (TryParseDecimal(input, out var value) && value > 0m)
                return value;

            global::System.Console.WriteLine("Please enter a positive number.");
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
    /// Reads an optional string
    /// </summary>
    public string? ReadOptionalString(string prompt)
    {
        global::System.Console.Write($"{prompt} ");
        var input = global::System.Console.ReadLine()?.Trim();

        return string.IsNullOrWhiteSpace(input) ? null : input;
    }

    /// <summary>
    /// Waits for the user before returning to a menu
    /// </summary>
    public void WaitForContinue(string message = "Press Enter to continue...")
    {
        global::System.Console.WriteLine();
        global::System.Console.Write(message);

        while (global::System.Console.ReadKey(intercept: true).Key != ConsoleKey.Enter)
        {
        }

        global::System.Console.WriteLine();
    }

    /// <summary>
    /// Reads a yes or no answer
    /// </summary>
    public bool ReadYesNo(string prompt, bool? defaultValue = null)
    {
        var items = new List<ConsoleMenuItem<bool>>
        {
            new("yes", "Yes", string.Empty, true),
            new("no", "No", string.Empty, false)
        };

        var initialSelectedIndex = defaultValue == false ? 1 : 0;
        return ReadMenuSelection(prompt, items, allowCancel: false, initialSelectedIndex: initialSelectedIndex);
    }

    /// <summary>
    /// Lets the user choose one menu item with arrow keys
    /// </summary>
    public T ReadMenuSelection<T>(
        string title,
        IReadOnlyList<ConsoleMenuItem<T>> items,
        bool allowCancel = false,
        int initialSelectedIndex = 0,
        IReadOnlyList<string>? headerLines = null)
    {
        if (items.Count == 0)
            throw new ArgumentException("At least one menu item is required.", nameof(items));

        if (!TryReadMenuSelection(title, items, out var selection, allowCancel, initialSelectedIndex, headerLines))
            throw new InvalidOperationException("Selection was canceled.");

        return selection;
    }

    /// <summary>
    /// Lets the user choose one optional menu item with arrow keys
    /// </summary>
    public bool TryReadMenuSelection<T>(
        string title,
        IReadOnlyList<ConsoleMenuItem<T>> items,
        out T selection,
        bool allowCancel = true,
        int initialSelectedIndex = 0,
        IReadOnlyList<string>? headerLines = null)
    {
        if (items.Count == 0)
            throw new ArgumentException("At least one menu item is required.", nameof(items));

        selection = default!;
        var selectedIndex = Math.Clamp(initialSelectedIndex, 0, items.Count - 1);

        while (true)
        {
            RenderMenu(title, items, selectedIndex, allowCancel, headerLines);

            var key = global::System.Console.ReadKey(intercept: true).Key;
            switch (key)
            {
                case ConsoleKey.UpArrow:
                    selectedIndex = selectedIndex == 0 ? items.Count - 1 : selectedIndex - 1;
                    break;
                case ConsoleKey.DownArrow:
                    selectedIndex = selectedIndex == items.Count - 1 ? 0 : selectedIndex + 1;
                    break;
                case ConsoleKey.Home:
                    selectedIndex = 0;
                    break;
                case ConsoleKey.End:
                    selectedIndex = items.Count - 1;
                    break;
                case ConsoleKey.Enter:
                    selection = items[selectedIndex].Value;
                    global::System.Console.ResetColor();
                    global::System.Console.Clear();
                    return true;
                case ConsoleKey.LeftArrow when allowCancel:
                case ConsoleKey.Backspace when allowCancel:
                    global::System.Console.ResetColor();
                    global::System.Console.Clear();
                    return false;
            }
        }
    }

    /// <summary>
    /// Lets the user choose one enum value
    /// </summary>
    public TEnum ReadEnumChoice<TEnum>(string title) where TEnum : struct, Enum
    {
        var values = Enum.GetValues<TEnum>();
        var items = values
            .Select(value => new ConsoleMenuItem<TEnum>(
                value.ToString().ToLowerInvariant(),
                value.ToString(),
                string.Empty,
                value))
            .ToList();

        return ReadMenuSelection(title, items);
    }

    // The menu is cleared and fully redrawn after each arrow key
    private static void RenderMenu<T>(
        string title,
        IReadOnlyList<ConsoleMenuItem<T>> items,
        int selectedIndex,
        bool allowCancel,
        IReadOnlyList<string>? headerLines)
    {
        global::System.Console.ResetColor();
        global::System.Console.Clear();

        if (headerLines is not null)
        {
            foreach (var line in headerLines)
                global::System.Console.WriteLine(line);

            if (headerLines.Count > 0)
                global::System.Console.WriteLine();
        }

        global::System.Console.WriteLine(title);
        global::System.Console.WriteLine(new string('-', Math.Max(12, title.Length)));
        global::System.Console.WriteLine(
            allowCancel
                ? "Use Up/Down to move, Enter to confirm, Left Arrow or Backspace to cancel."
                : "Use Up/Down to move and Enter to confirm.");
        global::System.Console.WriteLine();

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var prefix = i == selectedIndex ? "> " : "  ";
            var line = $"{prefix}{item.Label}";

            if (i == selectedIndex)
            {
                WriteHighlightedLine(line);
            }
            else
            {
                global::System.Console.WriteLine(line);
            }

            if (!string.IsNullOrWhiteSpace(item.Description))
            {
                var description = $"    {item.Description}";
                global::System.Console.WriteLine(description);
            }
        }
    }

    // The selected item is highlighted with inverted console colors
    private static void WriteHighlightedLine(string value)
    {
        var previousForeground = global::System.Console.ForegroundColor;
        var previousBackground = global::System.Console.BackgroundColor;

        try
        {
            global::System.Console.ForegroundColor = ConsoleColor.Black;
            global::System.Console.BackgroundColor = ConsoleColor.Gray;
            global::System.Console.WriteLine(value);
        }
        finally
        {
            global::System.Console.ForegroundColor = previousForeground;
            global::System.Console.BackgroundColor = previousBackground;
        }
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
