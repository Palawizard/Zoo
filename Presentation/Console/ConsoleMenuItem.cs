namespace Zoo.Presentation.Console;

/// <summary>
/// Represents one selectable console menu item
/// </summary>
public sealed record ConsoleMenuItem<TValue>(
    string Key,
    string Label,
    string Description,
    TValue Value
);
