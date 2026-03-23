using Avalonia.Media;

namespace Zoo.Desktop.Styling;

/// <summary>
/// Central palette used by the desktop UI
/// </summary>
internal static class UiBrushes
{
    public static readonly IBrush Success = Brush.Parse("#2EC4B6");
    public static readonly IBrush Warning = Brush.Parse("#E6A44E");
    public static readonly IBrush Hungry = Brush.Parse("#D3BB63");
    public static readonly IBrush Danger = Brush.Parse("#FF6B6B");
    public static readonly IBrush Info = Brush.Parse("#5AA9FF");
    public static readonly IBrush Muted = Brush.Parse("#93A4B7");
    public static readonly IBrush MessageGoodFill = Brush.Parse("#12332F");
    public static readonly IBrush MessageGoodBorder = Brush.Parse("#235953");
    public static readonly IBrush MessageBadFill = Brush.Parse("#341B20");
    public static readonly IBrush MessageBadBorder = Brush.Parse("#6C3039");
}
