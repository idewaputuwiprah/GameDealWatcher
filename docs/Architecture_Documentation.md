# Architecture Documentation

## Overview
GameDealWatcher follows strict MVVM architecture with dependency inversion. The application is organized into five layers that enforce separation of concerns.

## Layers

### Domain Layer (`GameDealWatcher.Domain`)
Contains the core business models and interfaces. This layer has no external dependencies.

**Entities**: `Game`, `GameDeal`, `AppSettings`
**Interfaces**: `IGameDealProvider`, `IGameDealRepository`, `ISettingsRepository`

### Application Layer (`GameDealWatcher.Application`)
Contains business logic and service orchestration. Depends only on the Domain layer.

**Services**: `IGameDealService` (implements `GameDealService`)
- `RefreshAllDealsAsync()`: Coordinates provider execution
- `GetAllDealsAsync()`: Returns cached deals
- `NeedsRefreshAsync()`: Checks if refresh is required

### Infrastructure Layer (`GameDealWatcher.Infrastructure`)
Contains external concerns: data persistence, HTTP clients, providers, image caching, notifications.

**Providers**:
- `SteamProvider`: Fetches discounted games from Steam
- `EpicGamesProvider`: Fetches free games from Epic Games Store

**Database**: `SqliteGameDealRepository`, `DatabaseInitializer`, `JsonSettingsRepository`
**HTTP**: `HttpClientConfiguration` with named clients for each provider
**Images**: `ImageCacheService` — manages local image caching
**Notifications**: `WindowsNotificationService` — toast notifications

### Presentation Layer (`GameDealWatcher.App`)
WPF UI with MVVM pattern. No business logic.

**ViewModels**: `MainViewModel`, `DashboardViewModel`, `EpicViewModel`, `SteamViewModel`, `SettingsViewModel`
**Views**: `DashboardView`, `EpicView`, `SteamView`, `SettingsView`
**Commands**: All UI actions use `RelayCommand`

### Tests Layer (`GameDealWatcher.Tests`)
Unit and integration tests.

## Dependency Injection Configuration
```csharp
services.AddHttpClient(SteamHandlerName, client => { ... });
services.AddHttpClient(EpicHandlerName, client => { ... });
services.AddSingleton<IGameDealRepository, SqliteGameDealRepository>();
services.AddSingleton<IGameDealService, GameDealService>();
services.AddSingleton<IGameDealProvider, SteamProvider>();
services.AddSingleton<IGameDealProvider, EpicGamesProvider>();
services.AddSingleton<ISettingsRepository, JsonSettingsRepository>();
services.AddSingleton<IImageCacheService, ImageCacheService>();
services.AddSingleton<INotificationService, WindowsNotificationService>();
```

## Data Flow
```
User opens application
    ↓
App loads cached SQLite data → UI displays immediately
    ↓
Background check: LastSuccessfulRefresh > 24h?
    ↓ Yes
RefreshService triggers SteamProvider + EpicGamesProvider concurrently
    ↓
Results validated → Saved to SQLite → DealHistory updated
    ↓
New deals detected → Notifications sent
    ↓
UI updated with latest data
```

## Provider Abstraction
Both providers implement `IGameDealProvider`. The application never directly depends on either provider. This allows swapping data sources without modifying the UI or service layers.

## Database Schema
- **GameDeals**: Current state of all deals with full metadata
- **DealHistory**: Price change snapshots for tracking historical deals
- **RefreshHistory**: Audit trail of refresh attempts
- **Settings**: User preferences
- **Stores**: Store metadata

## Concurrency Model
- Steam and Epic providers run on separate tasks via `Task.WhenAll`
- Each provider failure is isolated — one provider failing does not block the other
- Database operations use transaction isolation
- UI never blocks on network or DB operations

## Error Handling
- Failed providers show error messages in UI
- HTTP 429 triggers exponential backoff retry
- Invalid JSON is logged and skipped
- SQLite failures are caught and logged
- Application remains usable when offline (cached data displayed)
