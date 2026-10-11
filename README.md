# BetClaude (working name)

A local Windows desktop tool that collects football data and shows **statistical probability estimates**
for common markets. It is an analysis tool: it does not promise profit, guaranteed predictions or wins.

> **Status: Alpha (0.1).** Everything here is under active development. Probabilities come from a simple
> Poisson / Dixon-Coles model. Before a model is shown as validated, it must beat a plain frequency baseline
> in a walk-forward backtest; otherwise the app says "not validated" or "insufficient data".

## Scope of the Alpha

- .NET 8, WPF, Windows 10/11 only.
- Local SQLite database, no server needed.
- Historical data: CSV files from football-data.co.uk, downloaded to the user's own machine
  (check their terms before redistributing any data).
- Goals-based markets: 1X2, double chance, draw no bet, total goals lines, both teams to score, likely scores.
- Persian (RTL) and English (LTR) UI.
- News: RSS headlines with link back to the source (title, link and short summary only; `robots.txt` is respected).

**Not in the Alpha:** corners and cards markets, API-Football adapter, calibration charts, backups, scheduling.

## Layout

```
src/BetClaude.Domain          core types, independent of everything else
src/BetClaude.Prediction      score matrix, goal model fitting, markets, walk-forward backtest
tests/                        xUnit tests
```

More projects (Services, Infrastructure, Desktop) are added in later commits.

## Build

```
dotnet build BetClaude.sln -c Release
dotnet test BetClaude.sln -c Release
```

API keys must never be committed. Keys entered by a user are meant to be stored encrypted with Windows DPAPI.
