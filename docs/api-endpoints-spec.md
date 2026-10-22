# API Endpoints Specification

This document describes the public endpoints that the **GameDealWatcher** application uses to fetch deals from Steam and Epic Games Store.

## 1. Steam – discounted store search

| Property | Value |
|----------|-------|
| Method | GET |
| URL | `https://store.steampowered.com/search/results/?query&start=0&count=100&specials=1&infinite=1&cc=us&l=english` |
| Authentication | None required |
| Rate‑limit | Steam's public API limit (≈ 200 requests per second). |
| Expected Response | JSON object with `success`, `results_html` (one `<a>` element per result row) and `total_count` (number of matching specials; ≈19k for `specials=1`). The provider requests `start=0,100,...` until `total_count` is reached, capped at 10 pages (1,000 deals), and parses app rows only:

- `href` – `https://store.steampowered.com/app/{appid}/...`; app ID used to build store URL and deal identifier (`steam_{appid}`)
- `<span class="title">` – string; game title (HTML-decoded)
- `data-price-final` – integer; discounted price in hundredths, regardless of currency
- `data-discount` – integer; discount percentage (0–100)
- `<img src>` inside `search_capsule` – primary image URL

| Notes | Prices are divided by 100. The original price is derived from the discount percentage because the search response does not include it, so it can be off by one minor unit; it is not displayed when the result is zero. A 100%-off row is marked currently free. Store URLs are constructed as `https://store.steampowered.com/app/{appid}/`. Bundles and packages (`/bundle/`, `/sub/`) are skipped. A missing `success`/`total_count`/`results_html`, or an empty page before `total_count` is reached, throws so the cached deals are kept instead of being pruned.

## 2. Epic Games Store – `freeGamesPromotions`

| Property | Value |
|----------|-------|
| Method | GET |
| URL | `https://store-site-backend-static.ak.epicgames.com/freeGamesPromotions?locale=en-US&country=US&allowCountries=US` |
| Authentication | None required |
| Expected Response | JSON object with nested structure. The relevant data is located at `data.Catalog.searchStore.elements[]`. Each element includes the following properties used by the provider:

- `id` – string; offer identifier, used to build deal ID (`epic_{id}`). If missing, a deterministic fallback ID is generated from SHA256 of the title.
- `title` – string; game title
- `offerType` – string; only items with value `BASE_GAME` are included (DLCs, soundtracks, bundles are filtered out)
- `price.totalPrice.originalPrice` – decimal; original price in the currency's minor unit
- `price.totalPrice.discountPrice` – decimal; discounted price in the currency's minor unit
- `price.totalPrice.currencyInfo.decimals` – integer; number of decimals for the currency (0 for JPY/KRW, 2 otherwise)
- `promotions.promotionalOffers[].promotionalOffers[]` – nested array of active promotional offers; each contains:
  - `startDate` – ISO‑8601 date string; start of the promotion
  - `endDate` – ISO‑8601 date string; end of the promotion
- `promotions.upcomingPromotionalOffers[].promotionalOffers[]` – nested array of upcoming promotional offers with same structure as above, including `discountSetting.discountPercentage` (share of the original price still to be paid; `0` means free)
- `keyImages[?].type` – array of image objects filtered by type:
  - `OfferImageWide` or `VaultClosed` → used for main image URL
  - `Thumbnail` → used for thumbnail URL
- `productSlug` – string; preferred slug for store URL construction
- `catalogNs.mappings[0].pageSlug` – string; fallback slug if productSlug is absent

| Notes | Prices are scaled by `price.totalPrice.currencyInfo.decimals` (JPY/KRW report 0, other supported currencies 2). Cover images on `epicgames.com`/`unrealengine.com` hosts get `?w=460&resize=1` appended. Store URLs follow the pattern `https://www.epicgames.com/store/en-US/p/{slug}` with multi-level fallback: `productSlug` → `catalogNs.mappings[0].pageSlug` → raw `url` field → constructed URL from offer ID. A deal is marked as currently free when an active promotional offer's date range includes the current time and price is zero; it is marked upcoming only when a future promotional offer is a giveaway (`discountPercentage` 0), has not yet started, and the game is not already discounted.

## 3. Image Cache Endpoint

The image cache layer does not expose an API; it only uses a named client `ImageCache` configured with a 15‑second timeout. The endpoint URL is whatever the provider's image URLs point to (e.g., `https://cdn1.epicgames.com/...`). Cached files older than 30 days are pruned on startup; there is no size cap.

## 4. Summary

Both endpoints are public and do not require authentication, making them safe for use in the application without special keys. If either service changes its JSON shape or URL structure, adjust the parsing logic in `SteamProvider` or `EpicGamesProvider`. The database schema already matches the data model; no further action is required.
