# GameDealWatcher

A Windows desktop application that monitors game deals from Steam and Epic Games stores, helping you never miss a discount again.

## Features

- **Multi-store tracking** — Aggregates deals from Steam and Epic Games in one place
- **Automatic refresh** — Configurable background polling (minimum every 5 minutes)
- **Smart notifications** — Windows toast alerts for new deals based on your preferences
- **Price history** — Tracks price changes over time so you know if it's a real deal
- **Cover art caching** — Downloads and caches game images locally for fast browsing
- **Start with Windows** — Optional auto-start to keep monitoring while you're away

## Tech Stack

- **UI**: WinUI 3 (Windows App SDK) with MVVM pattern using CommunityToolkit.Mvvm
- **Architecture**: Clean architecture across 5 projects:
  - `Domain` — Core entities and interfaces
  - `Application` — Business logic and orchestration services
  - `Infrastructure` — SQLite persistence, HTTP providers, notifications, image caching
  - `App` — WinUI views, viewmodels, dependency injection setup
- **Data**: SQLite via Dapper for local deal storage
- **Language**: C# / .NET

## Building

1. Clone the repository
2. Open `GameDealWatcher.sln` in Visual Studio 2022 (or use `dotnet build`)
3. Build and run `GameDealWatcher.App` as the startup project

Requires:
- Windows 10/11
- .NET SDK 8+
- Visual Studio 2022 with "Desktop development with C#" workload + Windows App SDK

## Data Storage

All data is stored in `%LOCALAPPDATA%\GameDealWatcher`:
- `Database\game_deals.db` — SQLite database of deals and price history
- `Images\` — Cached game cover images
- `Logs\` — Application logs (daily rotation)
- `settings.json` — User preferences

## License

MIT
