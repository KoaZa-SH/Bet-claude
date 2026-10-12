using System.Windows;

namespace BetClaude.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        try
        {
            _viewModel = new MainViewModel();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "BetClaude", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Shutdown();
            throw;
        }

        DataContext = _viewModel;
        Loaded += async (_, _) => await _viewModel.InitializeAsync();
    }

    private void SaveKey_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SaveApiKey("api-football", ApiKeyBox.Password);
        ApiKeyBox.Clear();
    }
}
