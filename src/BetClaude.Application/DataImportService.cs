namespace BetClaude.Services;

public sealed class DataImportService
{
    private readonly IMatchRepository _repository;

    public DataImportService(IMatchRepository repository)
    {
        _repository = repository;
    }

    /// <summary>Downloads history from one provider and stores it. Returns the number of rows written.</summary>
    public async Task<int> ImportHistoryAsync(
        IHistoricalDataProvider provider,
        string competitionCode,
        string season,
        CancellationToken ct = default)
    {
        var matches = await provider.FetchHistoryAsync(competitionCode, season, ct).ConfigureAwait(false);
        if (matches.Count == 0)
        {
            return 0;
        }

        return await _repository.UpsertMatchesAsync(matches, provider.Id, ct).ConfigureAwait(false);
    }
}
