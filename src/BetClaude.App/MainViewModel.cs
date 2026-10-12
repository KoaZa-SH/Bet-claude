using System.Collections.ObjectModel;
using System.Globalization;
using BetClaude.App.Localization;
using BetClaude.Domain;
using BetClaude.Infrastructure.Providers;
using BetClaude.Infrastructure.Security;
using BetClaude.Infrastructure.Storage;
using BetClaude.Services;

namespace BetClaude.App;

public sealed record FixtureItem(Fixture Fixture)
{
    public string Display =>
        $"{Fixture.HomeTeam} – {Fixture.AwayTeam}   ·   {Fixture.Competition}   ·   {Fixture.KickoffUtc.ToLocalTime():yyyy-MM-dd HH:mm}";
}

/// <summary>One probability line. <see cref="Percent"/> is 0-100 and only drives the bar length.</summary>
public sealed record ProbabilityRow(string Label, string Text, double Percent);

public sealed class MainViewModel : ObservableObject
{
    private readonly HttpClient _http;
    private readonly SqliteMatchRepository _repository;
    private readonly AnalysisService _analysis;
    private readonly DataImportService _import;
    private readonly FootballDataProvider _provider;
    private readonly ISecretStore _secrets;

    private string _page = "Dashboard";
    private string _status;
    private bool _busy;
    private bool _isDark = true;
    private string _storedCompetitions = string.Empty;
    private string _importCompetition = "E0";
    private string _importSeason = "2425";
    private FixtureItem? _selectedFixture;
    private Fixture? _lastFixture;
    private AnalysisResult? _lastResult;
    private string _analysisHeader = string.Empty;
    private string _qualityText = string.Empty;
    private string _validationText = string.Empty;
    private string _warningsText = string.Empty;
    private string _noteText = string.Empty;

    public MainViewModel()
    {
        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BetClaude");
        Directory.CreateDirectory(dir);
        DbPath = Path.Combine(dir, "betclaude.db");

        var database = new SqliteDatabase(DbPath);
        database.Migrate();

        _repository = new SqliteMatchRepository(database);
        _analysis = new AnalysisService(_repository);
        _import = new DataImportService(_repository);
        _secrets = new DpapiSecretStore(Path.Combine(dir, "secrets"));

        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("BetClaude/0.1 (personal statistics tool)");
        _provider = new FootballDataProvider(_http);

        _status = L("Ready");

        NavigateCommand = new RelayCommand(p =>
        {
            Page = p as string ?? "Dashboard";
            return Task.CompletedTask;
        });
        ImportCommand = new RelayCommand(_ => RunAsync(ImportAsync));
        LoadFixturesCommand = new RelayCommand(_ => RunAsync(LoadFixturesAsync));

        Loc.Instance.PropertyChanged += (_, _) =>
        {
            Raise(nameof(IsPersian));
            Raise(nameof(IsEnglish));
            Rebuild();
        };
    }

    public string DbPath { get; }

    public Loc Localizer => Loc.Instance;

    public ObservableCollection<FixtureItem> Fixtures { get; } = new();

    public ObservableCollection<ProbabilityRow> Rows { get; } = new();

    public RelayCommand NavigateCommand { get; }

    public RelayCommand ImportCommand { get; }

    public RelayCommand LoadFixturesCommand { get; }

    public string Page
    {
        get => _page;
        set
        {
            if (Set(ref _page, value))
            {
                Raise(nameof(IsDashboardPage));
                Raise(nameof(IsMatchesPage));
                Raise(nameof(IsDataPage));
                Raise(nameof(IsSettingsPage));
            }
        }
    }

    public bool IsDashboardPage => _page == "Dashboard";

    public bool IsMatchesPage => _page == "Matches";

    public bool IsDataPage => _page == "Data";

    public bool IsSettingsPage => _page == "Settings";

    public string Status
    {
        get => _status;
        set => Set(ref _status, value);
    }

    public bool Busy
    {
        get => _busy;
        set => Set(ref _busy, value);
    }

    public bool IsDark
    {
        get => _isDark;
        set
        {
            if (Set(ref _isDark, value))
            {
                ThemeManager.Apply(value);
            }
        }
    }

    public bool IsPersian
    {
        get => Loc.Instance.Language == "fa";
        set
        {
            if (value)
            {
                Loc.Instance.Language = "fa";
            }
        }
    }

    public bool IsEnglish
    {
        get => Loc.Instance.Language == "en";
        set
        {
            if (value)
            {
                Loc.Instance.Language = "en";
            }
        }
    }

    public string StoredCompetitions
    {
        get => _storedCompetitions;
        set => Set(ref _storedCompetitions, value);
    }

    public string ImportCompetition
    {
        get => _importCompetition;
        set => Set(ref _importCompetition, value);
    }

    public string ImportSeason
    {
        get => _importSeason;
        set => Set(ref _importSeason, value);
    }

    public FixtureItem? SelectedFixture
    {
        get => _selectedFixture;
        set
        {
            if (Set(ref _selectedFixture, value) && value is not null)
            {
                _ = RunAsync(() => AnalyzeAsync(value.Fixture));
            }
        }
    }

    public bool HasAnalysis => _lastResult is not null && _lastFixture is not null;

    public bool HasNoAnalysis => !HasAnalysis;

    public string AnalysisHeader
    {
        get => _analysisHeader;
        private set => Set(ref _analysisHeader, value);
    }

    public string QualityText
    {
        get => _qualityText;
        private set => Set(ref _qualityText, value);
    }

    public string ValidationText
    {
        get => _validationText;
        private set => Set(ref _validationText, value);
    }

    public string WarningsText
    {
        get => _warningsText;
        private set => Set(ref _warningsText, value);
    }

    public string NoteText
    {
        get => _noteText;
        private set => Set(ref _noteText, value);
    }

    public async Task InitializeAsync()
    {
        await RunAsync(RefreshStoredAsync);
    }

    public void SaveApiKey(string provider, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            Status = L("KeyEmpty");
            return;
        }

        try
        {
            _secrets.Save(provider, key.Trim());
            Status = L("KeySaved");
        }
        catch (Exception ex)
        {
            Status = $"{L("Error")}: {ex.Message}";
        }
    }

    private static string L(string key) => Loc.Instance[key];

    private async Task RunAsync(Func<Task> work)
    {
        Busy = true;
        Status = L("Loading");
        try
        {
            await work();
        }
        catch (OperationCanceledException)
        {
            Status = $"{L("Error")}: timeout";
        }
        catch (Exception ex)
        {
            Status = $"{L("Error")}: {ex.Message}";
        }
        finally
        {
            Busy = false;
        }
    }

    private async Task RefreshStoredAsync()
    {
        var competitions = await _repository.GetCompetitionsAsync();
        StoredCompetitions = competitions.Count == 0 ? L("NoneStored") : string.Join(", ", competitions);
        Status = L("Ready");
    }

    private async Task ImportAsync()
    {
        int written = await _import.ImportHistoryAsync(_provider, ImportCompetition.Trim(), ImportSeason.Trim());
        await RefreshStoredAsync();
        Status = string.Format(CultureInfo.InvariantCulture, L("ImportedRows"), written);
    }

    private async Task LoadFixturesAsync()
    {
        var all = await _provider.FetchFixturesAsync();
        DateTime today = DateTime.UtcNow.Date;
        var upcoming = all.Where(f => f.KickoffUtc >= today).OrderBy(f => f.KickoffUtc).ToList();

        Fixtures.Clear();
        foreach (var fixture in upcoming)
        {
            Fixtures.Add(new FixtureItem(fixture));
        }

        Status = upcoming.Count == 0
            ? L("NoFixtures")
            : string.Format(CultureInfo.InvariantCulture, L("FixturesLoaded"), upcoming.Count);
    }

    private async Task AnalyzeAsync(Fixture fixture)
    {
        var result = await _analysis.AnalyzeAsync(fixture.Competition, fixture.HomeTeam, fixture.AwayTeam, DateTime.UtcNow);
        _lastFixture = fixture;
        _lastResult = result;
        Rebuild();
        Status = L("Ready");
    }

    /// <summary>Rebuilds all analysis text from the last result, so it follows the current language.</summary>
    private void Rebuild()
    {
        Rows.Clear();
        Raise(nameof(HasAnalysis));
        Raise(nameof(HasNoAnalysis));

        var fixture = _lastFixture;
        var result = _lastResult;
        if (fixture is null || result is null)
        {
            return;
        }

        AnalysisHeader = $"{fixture.HomeTeam} – {fixture.AwayTeam}";
        QualityText = $"{L("Quality")}: {L("Quality" + result.Quality)} ({result.HomeMatches}/{result.AwayMatches} {L("MatchesUsed")})";
        ValidationText = $"{L("Validation")}: {L("Validation" + result.Validation)}";
        WarningsText = string.Join("\n", result.WarningKeys.Select(L));

        var p = result.Probabilities;
        if (p is null)
        {
            NoteText = L("InsufficientData");
            return;
        }

        NoteText = string.Empty;

        Add("HomeWin", p.HomeWin);
        Add("Draw", p.Draw);
        Add("AwayWin", p.AwayWin);
        Add("DoubleChance1X", p.HomeOrDraw);
        Add("DoubleChance12", p.HomeOrAway);
        Add("DoubleChanceX2", p.DrawOrAway);
        Add("DnbHome", p.DrawNoBetHome);
        Add("DnbAway", p.DrawNoBetAway);
        Add("Btts", p.BothTeamsScore);

        foreach (var line in p.OverLines.OrderBy(kv => kv.Key))
        {
            string number = line.Key.ToString("0.0#", CultureInfo.InvariantCulture);
            Rows.Add(Row($"{L("Over")} {number}", line.Value));
            Rows.Add(Row($"{L("Under")} {number}", 1.0 - line.Value));
        }

        foreach (var score in p.TopScores.Take(3))
        {
            Rows.Add(Row($"{L("Score")} {score.HomeGoals}-{score.AwayGoals}", score.Probability));
        }

        string home = p.ExpectedHomeGoals.ToString("0.00", CultureInfo.InvariantCulture);
        string away = p.ExpectedAwayGoals.ToString("0.00", CultureInfo.InvariantCulture);
        Rows.Add(new ProbabilityRow($"{L("ExpectedGoals")}: {home} – {away}", string.Empty, 0));
    }

    private void Add(string labelKey, double probability) => Rows.Add(Row(L(labelKey), probability));

    private static ProbabilityRow Row(string label, double probability)
        => new(label, probability.ToString("P1", CultureInfo.InvariantCulture), probability * 100.0);
}
