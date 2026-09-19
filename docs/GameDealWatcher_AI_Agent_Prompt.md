# AI Agent Prompt — Native Windows Free Games & Steam Discounts Tracker

You are a **senior Windows software engineer and software architect**. Build a production-quality native Windows desktop application that automatically collects and displays:

1. **Free games on the Epic Games Store**
2. **Discounted games on Steam**

The application should refresh its data automatically every day and provide a clean desktop UI for browsing current and historical deals.

Do not create a prototype or proof of concept. Build the project as a maintainable application that can realistically be packaged and distributed to Windows users.

---

## 1. Platform & Technology

Target:

- Windows 10/11
- 64-bit
- Native Windows desktop application
- No Electron
- No browser-based desktop wrapper
- No requirement for a local web server

Preferred technology:

- **C#**
- **.NET 10 LTS or the latest stable LTS available**
- **WinUI 3 / Windows App SDK**
- MVVM architecture
- Dependency Injection
- `HttpClient` for network communication
- `System.Text.Json` for JSON serialization
- SQLite for local persistence

If there is a strong technical reason to use another native Windows technology, explain the reason before changing the stack.

---

## 2. Application Name

Use a temporary working name:

**GameDealWatcher**

The architecture should make it easy to rename the application later.

---

## 3. Core Features

## Epic Games — Free Games

Retrieve currently free games from the Epic Games Store.

For each game, collect:

- Game title
- Description
- Publisher
- Developer
- Cover image
- Thumbnail
- Store URL
- Original price
- Current price
- Discount percentage
- Free start date
- Free end date
- Game ID / product ID
- Genres/categories if available
- Platform
- Whether the game is currently free
- Whether the offer is an upcoming giveaway

The UI should clearly distinguish:

- Currently free
- Upcoming free games
- Expired giveaways

The primary view should make the current free games immediately visible.

---

## 4. Steam Discounts

Retrieve discounted games from Steam.

For each game, collect:

- App ID
- Game title
- Header image
- Capsule image
- Store URL
- Original price
- Discounted price
- Discount percentage
- Currency
- Discount start date
- Discount end date
- Developer
- Publisher
- Review score if available
- Review count if available
- Release date if available
- Genres
- Whether the discount is active
- Whether the discount is newly detected

The application must support sorting/filtering by:

- Highest discount
- Lowest price
- Highest review score
- Recently discounted
- Ending soon
- Title
- Release date

---

## 5. Data Sources

Do NOT blindly scrape HTML pages if a stable API or structured endpoint is available.

Investigate and use appropriate public/official endpoints where possible.

Potential sources include:

### Steam

Investigate:

- Steam Store APIs
- Steam search endpoints
- Steam app details endpoints
- Steam sale/deal endpoints

### Epic Games Store

Investigate:

- Epic Games Store public/structured APIs
- Epic catalog endpoints
- Epic offer endpoints

Before implementing a data source:

1. Verify that the endpoint currently works.
2. Determine whether authentication is required.
3. Determine rate limits.
4. Determine whether commercial use is restricted.
5. Determine whether the data can legally be cached locally.
6. Implement a provider abstraction so the source can be replaced later.

Do not hardcode undocumented behavior into the UI layer.

---

## 6. Provider Architecture

Create a provider abstraction.

Example:

```csharp
public interface IGameDealProvider
{
    string ProviderName { get; }

    Task<IReadOnlyList<GameDeal>> GetDealsAsync(
        CancellationToken cancellationToken);
}
```

Implement:

```text
EpicGamesProvider
SteamProvider
```

The application should not directly depend on either provider.

Use a structure similar to:

```text
Application
 ├── Domain
 │    ├── Game
 │    ├── GameDeal
 │    ├── Store
 │    └── DealType
 │
 ├── Providers
 │    ├── IGameDealProvider
 │    ├── EpicGamesProvider
 │    └── SteamProvider
 │
 ├── Infrastructure
 │    ├── Http
 │    ├── Database
 │    ├── ImageCache
 │    └── Logging
 │
 └── Presentation
      ├── Views
      ├── ViewModels
      └── Services
```

Keep provider-specific models separate from domain models.

---

## 7. Local Database

Use SQLite.

Create tables for at least:

```text
Games
GameDeals
Stores
DealHistory
RefreshHistory
Settings
```

Store historical deal information.

For example, if a Steam game changes:

```text
$59.99 → $29.99 → 50% off
```

the application should be able to retain the historical state.

Do not unnecessarily download the same information repeatedly.

Use:

- Primary keys
- Foreign keys
- Indexes
- Unique constraints
- Database migrations

The database layer must be asynchronous.

---

## 8. Daily Refresh

The application must automatically refresh once per day.

Requirements:

- Refresh automatically when the application starts if the cached data is stale.
- Allow the user to manually refresh.
- Allow configurable refresh time.
- Default refresh interval: **24 hours**
- Store the last successful refresh timestamp.
- Do not consider a failed request a successful refresh.
- Retry transient network failures.
- Use exponential backoff.
- Do not continuously retry an unavailable service.

Example:

```text
Application starts
        ↓
Check LastSuccessfulRefresh
        ↓
Older than 24h?
   ┌────┴────┐
   │         │
  Yes        No
   │         │
Refresh    Use cache
   │
   ↓
Steam + Epic
   │
   ↓
Validate
   │
   ↓
Save to SQLite
   │
   ↓
Update UI
```

---

## 9. Windows Background Refresh

The application should be capable of refreshing data even when the main window is not open, if Windows permits it.

Investigate an appropriate native Windows mechanism such as:

- Windows App SDK background tasks
- Windows Task Scheduler
- Startup task + lightweight refresh
- Appropriate Windows notification/background APIs

Prefer a robust solution that works reliably on normal Windows 10/11 installations.

Do not keep the entire application running in the background just to perform a daily refresh.

The refresh mechanism should:

1. Start
2. Fetch data
3. Update SQLite
4. Record refresh result
5. Optionally display a Windows notification
6. Exit

---

## 10. Notifications

Provide optional Windows notifications.

Examples:

```text
Epic Games Store

New free game:
GAME NAME

Free until:
September 25
```

and:

```text
Steam Deal

GAME NAME is now 80% off.

$49.99 → $9.99
```

Allow the user to disable notifications.

Do not spam notifications.

Only notify about newly detected deals or newly detected free games.

---

## 11. User Interface

Create a modern Windows-native UI.

Main navigation:

```text
┌─────────────────────────────────────────────┐
│ GameDealWatcher                             │
├──────────────┬──────────────────────────────┤
│              │                              │
│ Dashboard    │                              │
│ Epic Free    │      Content                 │
│ Steam Deals  │                              │
│ History      │                              │
│ Settings     │                              │
│              │                              │
└──────────────┴──────────────────────────────┘
```

## Dashboard

Show:

```text
Today's Deals

Epic Games
[ Game ] [ Game ] [ Game ]

Steam
[ Game ] [ Game ] [ Game ]

Last updated:
Today 08:30
```

---

## 12. Game Cards

Create reusable game cards.

Example:

```text
┌─────────────────────────────┐
│                             │
│       GAME IMAGE            │
│                             │
├─────────────────────────────┤
│ Cyberpunk Example           │
│                             │
│ Steam                       │
│ 80% OFF                     │
│                             │
│ $49.99  →  $9.99            │
│                             │
│ Ends in 2 days              │
│                             │
│ [ Open Store ]              │
└─────────────────────────────┘
```

For Epic:

```text
┌─────────────────────────────┐
│       GAME IMAGE            │
├─────────────────────────────┤
│ Example Game                │
│                             │
│ EPIC GAMES                  │
│                             │
│ FREE                        │
│                             │
│ Free until Sep 25           │
│                             │
│ [ Claim Game ]              │
└─────────────────────────────┘
```

---

## 13. Filtering

Provide filters.

Epic:

```text
[ All ] [ Free Now ] [ Upcoming ]
```

Steam:

```text
Minimum discount:
[ 0% ───────────── 100% ]

Price:
[ Any ]

Genres:
[ All ]

Sort:
[ Highest Discount ▼ ]
```

Search:

```text
Search games...
```

Search should operate against locally cached data first.

---

## 14. Store Integration

Clicking:

```text
Open Store
```

must open the game's official store page using the default Windows browser.

Do not embed a browser unnecessarily.

Store URLs should be generated/stored from provider data rather than constructed from user input.

---

## 15. Image Caching

Do not download images every time the user opens the application.

Implement an image cache.

Example:

```text
%LOCALAPPDATA%\GameDealWatcher\Cache\Images\
```

Use:

- Stable image identifiers
- HTTP cache headers where practical
- Local file caching
- Maximum cache size
- Cleanup of old images

Images should load asynchronously.

The UI must remain responsive while images download.

---

## 16. Network Layer

Create a reusable HTTP client layer.

Requirements:

- `IHttpClientFactory`
- CancellationToken support
- Request timeout
- Retry for transient failures
- Exponential backoff
- User-Agent
- Structured logging
- JSON parsing
- HTTP status handling

Do not use:

```csharp
new HttpClient()
```

throughout the application.

Use dependency injection.

---

## 17. Error Handling

The application must remain usable if:

- Steam is unavailable.
- Epic is unavailable.
- Internet is disconnected.
- An API changes.
- An individual game contains malformed data.
- An image fails to download.
- SQLite temporarily fails.
- A provider returns HTTP 429.
- A provider returns an unexpected JSON structure.

One provider failing must not prevent the other provider from updating.

Example:

```text
Steam       ✓ Updated
Epic Games  ✗ Failed

Epic Games could not be refreshed.
Showing previously cached data.
```

---

## 18. Offline Mode

The application must remain useful without internet access.

When offline:

- Show cached games.
- Show the timestamp of the last successful refresh.
- Disable/indicate unavailable actions appropriately.
- Do not delete existing data.

Example:

```text
Offline

Showing cached data.
Last updated:
September 18, 2026 08:32
```

---

## 19. Settings

Create a Settings page.

Options:

```text
General

[✓] Start with Windows

Refresh

Refresh interval:
[ 24 hours ]

Preferred refresh time:
[ 08:00 ]

Notifications

[✓] Notify about free Epic games
[✓] Notify about Steam discounts

Steam

Minimum discount:
[ 50% ]

Epic Games

[✓] Show upcoming giveaways

Storage

Cache size:
124 MB

[ Clear image cache ]

[ Clear database ]
```

Do not expose unnecessary technical configuration to normal users.

---

## 20. Logging

Use structured logging.

Store logs under:

```text
%LOCALAPPDATA%\GameDealWatcher\Logs\
```

Log:

- Application startup
- Provider requests
- Refresh start/end
- Number of games retrieved
- Database operations
- Retry attempts
- HTTP errors
- Parsing errors
- Notification errors

Never log:

- Passwords
- API keys
- Tokens
- Sensitive personal information

Use log levels:

```text
Trace
Debug
Information
Warning
Error
Critical
```

---

## 21. Configuration

Use a strongly typed settings model.

Example:

```csharp
public sealed class AppSettings
{
    public TimeSpan RefreshInterval { get; set; }

    public TimeOnly RefreshTime { get; set; }

    public bool StartWithWindows { get; set; }

    public bool EpicNotifications { get; set; }

    public bool SteamNotifications { get; set; }

    public int MinimumSteamDiscount { get; set; }
}
```

Do not scatter settings throughout the codebase.

---

## 22. MVVM

Use strict MVVM.

Views should not contain business logic.

Avoid:

```csharp
button.Click += ...
```

for application logic where a command can be used.

Use:

- ViewModels
- Commands
- Observable properties
- Dependency injection
- Services

Example:

```text
DashboardView
      ↓
DashboardViewModel
      ↓
GameDealService
      ↓
IGameDealProvider
      ↓
SteamProvider / EpicGamesProvider
      ↓
Repository
      ↓
SQLite
```

---

## 23. Dependency Injection

Register services centrally.

Example:

```csharp
services.AddSingleton<IGameDealRepository, GameDealRepository>();
services.AddSingleton<IGameDealService, GameDealService>();

services.AddTransient<SteamProvider>();
services.AddTransient<EpicGamesProvider>();

services.AddHttpClient();
```

Avoid service locator patterns.

Avoid static global state.

---

## 24. Testing

Create automated tests.

At minimum:

### Unit tests

Test:

- Steam JSON parsing
- Epic JSON parsing
- Price parsing
- Discount calculation
- Date handling
- Deal expiration
- Duplicate detection
- Refresh logic
- Notification detection
- Database repository behavior

### Integration tests

Test:

```text
Provider
   ↓
Domain mapping
   ↓
Repository
   ↓
SQLite
```

Use mocked HTTP responses where appropriate.

Do not make normal unit tests depend on live Steam/Epic APIs.

---

## 25. Data Normalization

Different providers will return different structures.

Normalize them into a common domain model.

For example:

```csharp
public sealed class GameDeal
{
    public string Id { get; init; }

    public string Store { get; init; }

    public string Title { get; init; }

    public decimal OriginalPrice { get; init; }

    public decimal CurrentPrice { get; init; }

    public int DiscountPercentage { get; init; }

    public DealType DealType { get; init; }

    public DateTimeOffset? StartsAt { get; init; }

    public DateTimeOffset? EndsAt { get; init; }

    public string StoreUrl { get; init; }

    public string? ImageUrl { get; init; }
}
```

Do not let Steam/Epic-specific DTOs leak into the UI.

---

## 26. Duplicate Detection

The application must correctly identify the same game across refreshes.

Do not rely solely on the game title.

Use:

```text
Provider + ProviderGameId
```

as the preferred identity.

Example:

```text
Steam: 730
Epic: abc123
```

are separate provider identities even if the title is identical.

---

## 27. Historical Data

Maintain deal history.

For example:

```text
Game: Example Game

Sep 10
$59.99 → $29.99
50% off

Sep 15
$59.99 → $19.99
67% off

Sep 19
$59.99 → $14.99
75% off
```

This can later support features such as:

- Lowest historical price
- Highest discount
- Price history graph

Implement the database structure now even if the graph is initially optional.

---

## 28. Currency

Steam prices depend on region/currency.

Do not assume USD.

The application should detect or allow configuration of the user's Steam region/currency where practical.

Store:

```text
Currency
Amount
```

separately.

Never parse currency strings as the canonical numeric value.

Use:

```csharp
decimal
```

for monetary values.

---

## 29. Time Zones

Use:

```text
DateTimeOffset
```

for timestamps.

Store UTC internally.

Convert to the user's local Windows time zone for display.

Pay special attention to:

- Daylight saving time
- Offer expiration
- Daily refresh
- Epic giveaway start/end times
- Steam discount end times

Never compare local time strings directly.

---

## 30. Performance

The UI must remain responsive.

Do not perform network or database operations on the UI thread.

Use asynchronous APIs throughout.

The application should start quickly even if the network is unavailable.

Startup should roughly follow:

```text
Start application
      ↓
Load cached SQLite data
      ↓
Display UI
      ↓
Check refresh status
      ↓
Refresh in background if necessary
```

Do NOT make the user wait for network requests before showing the UI.

---

## 31. Security

Follow secure coding practices.

Do not:

- Execute downloaded files.
- Automatically install games.
- Automatically claim Epic games.
- Automatically purchase games.
- Store unnecessary credentials.
- Disable TLS certificate validation.
- Execute provider-provided URLs as commands.

Opening store pages should use normal browser navigation.

---

## 32. Windows Integration

Support:

- Windows 10/11
- Windows notifications
- Start with Windows
- Application icon
- Windows App SDK packaging
- Proper application data directories
- Graceful application shutdown

Prefer MSIX packaging if appropriate.

Also investigate whether an unpackaged/self-contained distribution would be useful.

---

## 33. Architecture Quality

Follow these principles:

- SOLID
- Dependency inversion
- Separation of concerns
- DRY
- Explicit domain models
- Small services
- Testable code
- No giant ViewModels
- No giant service classes
- No static mutable application state
- No business logic in XAML code-behind

Avoid overengineering.

Do not introduce unnecessary abstractions simply for the sake of abstraction.

---

## 34. Project Structure

Aim for something similar to:

```text
GameDealWatcher/
│
├── src/
│   ├── GameDealWatcher.App/
│   │   ├── Views/
│   │   ├── ViewModels/
│   │   ├── Converters/
│   │   └── Resources/
│   │
│   ├── GameDealWatcher.Domain/
│   │   ├── Entities/
│   │   ├── Enums/
│   │   └── Interfaces/
│   │
│   ├── GameDealWatcher.Application/
│   │   ├── Services/
│   │   ├── DTOs/
│   │   └── Mappings/
│   │
│   ├── GameDealWatcher.Infrastructure/
│   │   ├── Database/
│   │   ├── Providers/
│   │   ├── Http/
│   │   ├── Images/
│   │   └── Notifications/
│   │
│   └── GameDealWatcher.Tests/
│
├── docs/
│
└── README.md
```

Adjust the structure if the chosen framework has a better established convention.

---

## 35. Development Process

Work incrementally.

### Phase 1 — Research

Before writing implementation code:

1. Investigate current Steam data sources.
2. Investigate current Epic Games Store data sources.
3. Determine what information each source actually provides.
4. Identify API limitations.
5. Identify rate limits.
6. Document the chosen endpoints.
7. Explain any assumptions.

Do not invent APIs.

### Phase 2 — Architecture

Create:

- Solution
- Projects
- Domain models
- Provider interfaces
- Repository interfaces
- Dependency injection
- Configuration
- Logging

Make sure the project builds.

### Phase 3 — Database

Implement:

- SQLite
- Migrations
- Entities
- Repositories
- Deal history
- Refresh history

Add tests.

### Phase 4 — Providers

Implement:

```text
SteamProvider
EpicGamesProvider
```

Each provider should:

1. Request data.
2. Validate response.
3. Parse DTOs.
4. Map to domain models.
5. Return normalized deals.

Add tests using recorded/mock responses.

### Phase 5 — Refresh Engine

Implement:

```text
RefreshService
```

Responsibilities:

- Determine whether refresh is required.
- Execute providers.
- Handle provider failures independently.
- Persist successful results.
- Record refresh status.
- Detect newly discovered deals.
- Trigger notifications.

### Phase 6 — UI

Implement:

1. Main window
2. Navigation
3. Dashboard
4. Epic Free Games
5. Steam Discounts
6. Search
7. Filtering
8. Game detail
9. History
10. Settings

Use real cached data as soon as the backend is available.

Do not build a fake UI disconnected from the actual architecture.

### Phase 7 — Windows Integration

Implement:

- Startup
- Notifications
- Background refresh
- Application packaging
- Local application data
- Logging

### Phase 8 — Testing

Run:

```text
dotnet build
dotnet test
```

Fix all warnings/errors that are relevant to production quality.

Test:

- No internet
- Steam unavailable
- Epic unavailable
- HTTP 429
- Invalid JSON
- Empty result
- Duplicate games
- Expired deals
- Application restart
- Database migration
- First launch
- Existing database
- Large number of deals

---

## 36. Important API Rule

Do not make assumptions about undocumented APIs.

If an endpoint cannot reliably provide the required information:

1. Document the limitation.
2. Look for another legitimate structured source.
3. Adapt the provider.
4. Keep the provider interface stable.

The rest of the application must not care where the data comes from.

---

## 37. Deliverables

Produce:

1. Complete Visual Studio solution.
2. Compilable source code.
3. SQLite database implementation.
4. Steam provider.
5. Epic Games provider.
6. Daily refresh system.
7. Windows notifications.
8. Native Windows UI.
9. Automated tests.
10. Logging.
11. Configuration/settings.
12. README.
13. Architecture documentation.
14. Build instructions.
15. Packaging instructions.
16. Example configuration.
17. Troubleshooting guide.

---

## 38. README Requirements

The README must explain:

- What the application does.
- Supported Windows versions.
- Technology stack.
- Architecture.
- Data sources.
- API limitations.
- Installation.
- Development setup.
- Building.
- Testing.
- Packaging.
- Database location.
- Cache location.
- Log location.
- How daily refresh works.
- How notifications work.
- Known limitations.

---

## 39. Final Acceptance Criteria

The project is considered complete only when all of these work:

### Epic

- [ ] Current free games appear.
- [ ] Upcoming giveaways can appear.
- [ ] Game images load.
- [ ] Free expiration date is displayed.
- [ ] Store button opens Epic.
- [ ] Historical data is stored.

### Steam

- [ ] Discounted games appear.
- [ ] Prices are displayed correctly.
- [ ] Discount percentage is displayed.
- [ ] Expiration dates are displayed when available.
- [ ] Sorting works.
- [ ] Filtering works.
- [ ] Search works.
- [ ] Store button opens Steam.
- [ ] Historical data is stored.

### Refresh

- [ ] First launch refresh works.
- [ ] Daily refresh works.
- [ ] Manual refresh works.
- [ ] Failed refresh does not overwrite valid cached data.
- [ ] Steam failure does not prevent Epic refresh.
- [ ] Epic failure does not prevent Steam refresh.

### Windows

- [ ] Application starts normally.
- [ ] UI remains responsive during refresh.
- [ ] Windows notifications work.
- [ ] Startup option works.
- [ ] Background refresh works or has a documented Windows limitation.
- [ ] Application can be packaged and installed.

### Reliability

- [ ] Offline mode works.
- [ ] API failures are handled.
- [ ] Rate limiting is handled.
- [ ] Database survives application restart.
- [ ] Duplicate deals are not created on every refresh.
- [ ] Tests pass.

---

## 40. How You Should Work

You are the implementation agent.

Do not simply describe how the application could be built.

Actually build it.

Before making architectural decisions that significantly affect the project:

1. Research current APIs and Windows capabilities.
2. State the decision briefly.
3. Implement the solution.
4. Build/test it.
5. Fix errors.
6. Continue until the acceptance criteria are satisfied.

When something cannot be implemented exactly as requested because of an API or Windows limitation, **do not fake the functionality**. Implement the closest reliable solution and clearly document the limitation.

Prioritize:

**Correctness → Reliability → Maintainability → Performance → UI polish**
