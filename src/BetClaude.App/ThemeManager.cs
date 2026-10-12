using System.Windows.Media;

namespace BetClaude.App;

/// <summary>
/// Light and dark palettes. The accent colour is neutral blue on purpose: colour never signals "profitable",
/// so a higher probability is not painted green and a lower one is not painted red.
/// </summary>
public static class ThemeManager
{
    public static void Apply(bool dark)
    {
        var resources = System.Windows.Application.Current.Resources;

        resources["BgBrush"] = Brush(dark ? "#10151C" : "#F3F5F8");
        resources["CardBrush"] = Brush(dark ? "#18202B" : "#FFFFFF");
        resources["InputBrush"] = Brush(dark ? "#0F141B" : "#FFFFFF");
        resources["BorderBrush"] = Brush(dark ? "#2A3544" : "#D5DBE3");
        resources["TextBrush"] = Brush(dark ? "#E8EDF3" : "#18202B");
        resources["MutedBrush"] = Brush(dark ? "#93A1B3" : "#5D6B7C");
        resources["AccentBrush"] = Brush(dark ? "#4C8DFF" : "#2F6FE4");
    }

    private static SolidColorBrush Brush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
