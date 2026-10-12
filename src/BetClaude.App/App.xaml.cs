using System.Windows;

namespace BetClaude.App;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Brushes must exist before the first window is created, because the XAML refers to them dynamically.
        ThemeManager.Apply(dark: true);
        base.OnStartup(e);
    }
}
