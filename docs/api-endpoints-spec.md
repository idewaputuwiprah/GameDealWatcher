# API Endpoints Specification

This document describes the public endpoints that the **GameDealWatcher** application uses to fetch deals from Steam and Epic Games Store.

## 1. Steam – `featuredcategories`

| Property | Value |
|----------|-------|
| Method | GET |
| URL | `https://store.steampowered.com/api/featuredcategories/?cc=us&l=english` |
| Authentication | None required |
| Rate‑limit | Steam's public API limit (≈ 200 requests per second). |
| Expected Response | JSON object with a top‑level key `specials` containing an object. The relevant data is located in `specials.items[]`. Each item includes the following properties used by the provider:

- `id` – integer; Steam App ID, used to build store URL and deal identifier (`steam_{id}`)
- `name` – string; game title
- `discount_percent` – integer; discount percentage (0–100)
- `final_price` – decimal; discounted price in cents
- `original_price` – decimal; original price in cents (if missing, derived from final_price and discount_percent when discount > 0)
- `header_image` – string; primary image URL (preferred)
- `large_capsule_image` – string; fallback image URL if header_image is absent

| Notes | Prices are stored in cents and converted to dollars by dividing by 100. Store URLs are constructed as `https://store.steampowered.com/app/{id}/`. The provider does not filter by offer type; all items with valid data are included.

## 2. Epic Games Store – `freeGamesPromotions`

| Property | Value |
|----------|-------|
| Method | GET |
| URL | `https://store-site-backend-static.ak.epicgames.com/freeGamesPromotions?locale=en-US&country=US&allowCountries=US` |
| Authentication | None required |
| Expected Response | JSON object with nested structure. The relevant data is located at `data.Catalog.searchStore.elements[]`. Each element includes the following properties used by the provider:

- `id` – string; offer identifier, used to build deal ID (`epic_{id}`). If missing, a deterministic fallback ID is generated from SHA256 of the title.
- `title` – string; game title
- `offerType` – string; only items with value `BASEGAME` are included (DLCs, soundtracks, bundles are filtered out)
- `price.totalPrice.originalPrice` – decimal; original price in cents
- `price.totalPrice.discountPrice` – decimal; discounted price in cents
- `promotions.promotionalOffers[].promotionalOffers[]` – nested array of active promotional offers; each contains:
  - `startDate` – ISO‑8601 date string; start of the promotion
  - `endDate` – ISO‑8601 date string; end of the promotion
- `promotions.upcomingPromotionalOffers[].promotionalOffers[]` – nested array of upcoming promotional offers with same structure as above
- `keyImages[?].type` – array of image objects filtered by type:
  - `OfferImageWide` or `VaultClosed` → used for main image URL
  - `Thumbnail` → used for thumbnail URL
- `productSlug` – string; preferred slug for store URL construction
- `catalogNs.mappings[0].pageSlug` – string; fallback slug if productSlug is absent

| Notes | Prices are stored in cents and converted to dollars by dividing by 100. Store URLs follow the pattern `https://www.epicgames.com/store/en-US/p/{slug}` with multi-level fallback: `productSlug` → `catalogNs.mappings[0].pageSlug` → raw `url` field → constructed URL from offer ID. A deal is marked as currently free when an active promotional offer's date range includes the current time and price is zero; it is marked upcoming when a future promotional offer exists and has not yet started.

## 3. Image Cache Endpoint

The image cache layer does not expose an API; it only uses a named client `ImageCache` configured with a 15‑second timeout. The endpoint URL is whatever the provider's image URLs point to (e.g., `https://cdn1.epicgames.com/...`).

## 4. Summary

Both endpoints are public and do not require authentication, making them safe for use in the application without special keys. If either service changes its JSON shape or URL structure, adjust the parsing logic in `SteamProvider` or `EpicGamesProvider`. The database schema already matches the data model; no further action is required.
