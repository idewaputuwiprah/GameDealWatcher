# GameDealWatcher Implementation Plan

## Architectural Review Summary
This plan addresses API reliability, rate-limiting, database schema requirements, image caching locking hazards, and native Windows integration constraints.

---

## Phase 1: Core Domain, Database & Settings

### 1.1 Database Schema & Repositories
- **Tables**:
  - `GameDeals`: `Id`, `ProviderName`, `ProviderGameId`, `Title`, `OriginalPrice`, `CurrentPrice`, `DiscountPercentage`, `Currency`, `StoreUrl`, `ImageUrl`, `ThumbnailUrl`, `StartsAt`, `EndsAt`, `IsCurrentlyFree`, `IsUpcoming`, `LastUpdated`
  - `DealHistory`: `Id`, `DealId`, `OriginalPrice`, `CurrentPrice`, `DiscountPercentage`, `RecordedAt`
  - `RefreshHistory`: `Id`, `ProviderName`, `RefreshedAt`, `Success`, `ItemCount`, `ErrorMessage`
- **Logic**:
  - Support `UPSERT` operations for deals.
  - Automatically log price changes to `DealHistory`.
  - Maintain historical metrics for lowest price / highest discount calculations.

### 1.2 Configuration & Settings
- **Storage**: `%LOCALAPPDATA%\GameDealWatcher\settings.json`
- **Fields**:
  - `RefreshInterval`: Frequency of background updates (default: 24h).
  - `PreferredRefreshTime`: Target time of day (default: 08:00).
  - `StartWithWindows`: Auto-start registry configuration.
  - `EpicNotifications`: Enable alerts for new Epic free games.
  - `SteamNotifications`: Enable alerts for new Steam deals.
  - `MinimumSteamDiscount`: Minimum discount percentage filter for alerts.

---

## Phase 2: Store Providers

### 2.1 Steam Provider (`SteamProvider`)
- **Endpoints**:
  - `https://store.steampowered.com/api/featuredcategories/?cc=us&l=english`
- **Capabilities**:
  - Extract `specials` category for active top discounts.
  - Parse original price, final price, discount percentage, currency, app ID, and header images.
  - Implement retry with exponential backoff on HTTP 429/5xx responses.

### 2.2 Epic Games Store Provider (`EpicGamesProvider`)
- **Endpoints**:
  - `https://store-site-backend-static.ak.epicgames.com/freeGamesPromotions?locale=en-US&country=US&allowCountries=US`
- **Capabilities**:
  - Parse nested `promotionalOffers` and `upcomingPromotionalOffers`.
  - Distinguish active giveaways vs. upcoming giveaways.
  - Extract start/end dates, store slug/page URLs, and key images (`Thumbnail`, `OfferImageWide`, `VaultClosed`).

---

## Phase 3: Refresh Engine & Background Services

### 3.1 Refresh Engine (`RefreshService`)
- Execute providers concurrently with independent failure boundaries (`Task.WhenAll` with error isolation).
- Evaluate changes to identify newly detected deals and price drops.
- Update `RefreshHistory` with diagnostic details.

### 3.2 Image Caching Service (`ImageCacheService`)
- **Directory**: `%LOCALAPPDATA%\GameDealWatcher\Cache\Images\`
- Use SHA256 hashes of URLs as file keys.
- Load image streams into memory with `BitmapCacheOption.OnLoad` to prevent locking disk files.
- Provide cleanup routines for clearing cache and removing stale assets.

---

## Phase 4: UI & MVVM Presentation

### 4.1 ViewModels
- `MainViewModel`: Navigation shell, offline status indicator, manual refresh command.
- `DashboardViewModel`: Overview showing active Epic free games and top Steam deals.
- `EpicViewModel`: Filter by All, Free Now, and Upcoming.
- `SteamViewModel`: Filter by minimum discount %, price threshold, and title search; sort by discount %, price, and title.
- `SettingsViewModel`: App configuration, storage cache management, manual database clearing.

### 4.2 Views & Components
- Reusable `GameCard` user control with cover art, badges, price comparison, countdown timers, and "Open Store" buttons.
- Modern Windows desktop styling with dark/light mode compatibility.

---

## Phase 5: Windows Integration & Delivery

### 5.1 Toast Notifications
- Implement toast notifications via Windows native APIs (`Microsoft.Toolkit.Uwp.Notifications`).
- Send notifications only for newly detected free games or deals meeting user thresholds.

### 5.2 Startup & Background Refresh
- Implement launch arguments (`--background` / `--silent`) to run headless refresh and exit.
- Provide Task Scheduler registration for non-packaged background execution.

### 5.3 Automated Testing
- Unit tests for provider DTO deserialization and domain mapping.
- Integration tests for SQLite repository CRUD, history tracking, and upsert logic.
- Mocked HTTP tests for resilient error handling (429, timeouts, malformed payloads).
