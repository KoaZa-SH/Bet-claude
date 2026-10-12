using System.ComponentModel;
using System.Windows;

namespace BetClaude.App.Localization;

/// <summary>
/// Current UI language. XAML binds to the indexer, e.g. {Binding [Settings], Source={x:Static loc:Loc.Instance}},
/// so switching language updates every label at once. Persian is right-to-left, English left-to-right.
/// </summary>
public sealed class Loc : INotifyPropertyChanged
{
    private string _language = "en";

    public static Loc Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Language
    {
        get => _language;
        set
        {
            if (_language == value)
            {
                return;
            }

            _language = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }

    public FlowDirection Flow => _language == "fa" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    public string this[string key] => Strings.Get(_language, key);
}
