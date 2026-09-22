# Changelog

## Code Review Fixes — 2026-09-22

### Phase 1: Data Integrity & Correctness (Critical)

- **Stale deal cleanup** — Added `DeleteDealsNotSeenAsync` to `IGameDealRepository` and `SqliteGameDealRepository`. `GameDealService.RefreshAllDealsAsync` now deletes deals not returned by providers in the latest refresh, preventing stale data from lingering for 30 days.
- **Atomic image cache writes** — `ImageCacheService` now downloads to a `.tmp` file and atomically renames to the final path, preventing corrupt partial files from being served after a crash.
- **Atomic settings file writes** — `JsonSettingsRepository` writes to a `.tmp` file then renames. `GetSettingsAsync` now catches `JsonException`/`IOException` and returns defaults instead of crashing on corruption.
- **Startup error handling** — `App.OnLaunched` writes a crash log to `LocalApplicationData/GameDealWatcher/Logs/crash.log` and calls `Environment.Exit(1)` on fatal startup errors, instead of silently swallowing exceptions.

### Phase 2: Error Handling & Resilience (High)

- **Transaction partial-commit tracking** — `SqliteGameDealRepository.InsertDealsAsync` now tracks failed deal IDs and logs a warning with success/failure counts via `ILogger`.
- **HTTP retry policy** — Added `Microsoft.Extensions.Http.Polly` package. Steam and Epic HTTP clients now use a retry policy (2 retries, exponential backoff: 2s, 4s) for transient failures.
- **ViewModel initialization** — Removed fire-and-forget `_ = LoadDealsAsync()` from all 4 ViewModel constructors. Added `InitializeAsync()` method and `IsLoading` observable property to each. Views call `InitializeAsync()` from their `Loaded` event with a guard flag.

### Phase 3: Resource Leaks & Validation (Medium)

- **CancellationTokenSource disposal** — `EpicViewModel` and `SteamViewModel` now dispose the previous CTS before creating a new one.
- **AppSettings validation** — `RefreshInterval` now clamps to a 5-minute minimum, preventing infinite refresh loops.
- **Provider name deduplication** — Created `ProviderNames` static class in `Domain.Entities`. Replaced all hardcoded `"Epic"`/`"Steam"` strings across `GameDealService`, `EpicGamesProvider`, `SteamProvider`, `EpicViewModel`, and `SteamViewModel`.
- **DB schema migration support** — `DatabaseInitializer` now tracks a `SchemaVersion` setting and includes a `MigrateAsync` method with version checking and a migration runner for future schema changes.

### Phase 4: Test Coverage

- **GameDealService tests** — 8 new tests covering: Epic free game notification, notifications disabled, Steam discount threshold (above/below), provider failure isolation, no-deals case, price change recording, unseen deal deletion. Added Moq and `Microsoft.Extensions.Logging.Abstractions` packages.
- **JsonSettingsRepository tests** — 4 new tests: no-file defaults, round-trip, corrupted file returns defaults, no temp file left after save.
- **DeleteDealsNotSeenAsync tests** — 2 new tests: removes unseen deals, empty list deletes all.
- **Sync-over-async fix** — `CreateTestRepo` converted to `CreateTestRepoAsync` in `GameDealRepositoryTests`.

### Phase 5: Polish (Low)

- **Epic discount calculation** — Simplified in `EpicGamesProvider.ParseEpicElement`. Removed confusing `discount` field branch; discount % now computed from dollar values only. Added clarifying comments.
- **FileLogger documentation** — Added XML doc comment on `BeginScope` explaining scopes are not supported.

### Files Modified (18)

| File | Changes |
|------|---------|
| `Domain/Entities/Models.cs` | Added `ProviderNames` static class |
| `Domain/Entities/AppSettings.cs` | `RefreshInterval` validation (5-min minimum) |
| `Domain/Interfaces/IGameDealRepository.cs` | Added `DeleteDealsNotSeenAsync` |
| `Application/Services/GameDealService.cs` | Stale deal cleanup, `ProviderNames` usage |
| `Infrastructure/Database/GameDealRepository.cs` | `DeleteDealsNotSeenAsync` impl, failed deal logging |
| `Infrastructure/Database/JsonSettingsRepository.cs` | Atomic writes, corruption recovery |
| `Infrastructure/Database/DatabaseInitializer.cs` | Schema migration infrastructure |
| `Infrastructure/Images/ImageCacheService.cs` | Atomic temp-file-then-rename |
| `Infrastructure/Http/HttpClientConfiguration.cs` | Polly retry policy |
| `Infrastructure/Providers/EpicGamesProvider.cs` | `ProviderNames`, discount calc simplification |
| `Infrastructure/Providers/SteamProvider.cs` | `ProviderNames` |
| `Infrastructure/GameDealWatcher.Infrastructure.csproj` | Added `Microsoft.Extensions.Http.Polly` |
| `App/App.xaml.cs` | Crash log + `Environment.Exit(1)` |
| `App/FileLoggerProvider.cs` | `BeginScope` XML doc |
| `App/ViewModels/DashboardViewModel.cs` | `InitializeAsync`, `IsLoading` |
| `App/ViewModels/EpicViewModel.cs` | `InitializeAsync`, `IsLoading`, CTS disposal |
| `App/ViewModels/SteamViewModel.cs` | `InitializeAsync`, `IsLoading`, CTS disposal |
| `App/ViewModels/SettingsViewModel.cs` | `InitializeAsync` |
| `App/Views/DashboardView.xaml.cs` | `Loaded` → `InitializeAsync` |
| `App/Views/EpicView.xaml.cs` | `Loaded` → `InitializeAsync` |
| `App/Views/SteamView.xaml.cs` | `Loaded` → `InitializeAsync` |
| `App/Views/SettingsView.xaml.cs` | `Loaded` → `InitializeAsync` |
| `Tests/GameDealWatcher.Tests.csproj` | Added Moq, logging abstractions, Application ref |
| `Tests/Infrastructure/Database/GameDealRepositoryTests.cs` | Async setup, `DeleteDealsNotSeen` tests |

### Files Created (3)

| File | Purpose |
|------|---------|
| `Tests/Application/Services/GameDealServiceTests.cs` | 8 tests for core business logic |
| `Tests/Infrastructure/Database/JsonSettingsRepositoryTests.cs` | 4 tests for settings repo |
| `CHANGELOG.md` | This file |

### Note

The .NET 9 SDK was not available in the development environment. All changes were verified via static analysis. Run `dotnet build` and `dotnet test` locally to confirm compilation and test results.
