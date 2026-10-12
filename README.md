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
src/BetClaude.Application     services and interfaces (namespace BetClaude.Services)
src/BetClaude.Infrastructure  SQLite storage, football-data.co.uk provider, RSS news, DPAPI secret store
src/BetClaude.App             WPF desktop app (Persian/English, RTL/LTR, light/dark theme)
tests/                        xUnit tests
```

## Running the app

CI publishes a `win-x64` build as the **BetClaude-win-x64** artifact of each run (GitHub → Actions → a run → Artifacts).
It needs the **.NET 8 Desktop Runtime** installed. In the app: *Data Collection* → import a league and season
(for example `E0`, `2425`), then *Matches* → load upcoming matches → pick one.

The data folder is `%LOCALAPPDATA%\BetClaude`. The UI has not been visually reviewed on a real Windows screen yet.

## Build

```
dotnet build BetClaude.sln -c Release
dotnet test BetClaude.sln -c Release
```

API keys must never be committed. Keys entered by a user are meant to be stored encrypted with Windows DPAPI.
