# Deezer Integration (real, no API key)

Verified on 2026-09-28: Deezer's public REST API works **without any
authentication** for catalogue reads. No app registration, client id, or secret
is required. We only use read-only endpoints.

## Endpoints used

| Purpose | Request | Notes |
| --- | --- | --- |
| Album search | `GET https://api.deezer.com/search/album?q={q}&limit={n}&index={offset}` | Paging uses `index`, not `offset` |
| Album detail | `GET https://api.deezer.com/album/{id}` | Includes `tracks.data` |

Base URL and timeout come from configuration (`AlbumProviders:Deezer`), bound to
`DeezerOptions` and validated at startup.

## Observed quirks and how we handle them

| Quirk | Handling |
| --- | --- |
| Errors use HTTP 200 with body `{"error":{"type","message","code"}}` | `DeezerAlbumProvider` inspects the envelope; code `800` means "no data" and maps to `AlbumProviderNotFoundException`, other codes map to `AlbumProviderUnavailableException` |
| Search results do **not** include `release_date` | Release date arrives only on the detail call, which is exactly why saving fetches details first |
| Album detail tracks have an empty `track_position` | Mapper falls back to the 1-based array index (`DeezerAlbumMapper`) |
| `release_date` can be `0000-00-00` or malformed | `ParseReleaseDate` returns `null` instead of throwing |
| Covers come as multiple fields | Prefer `cover_xl`, fall back to `cover_big` |
| Field names use snake_case (`nb_tracks`, `cover_xl`) | DTOs in `DeezerApiContracts.cs` use explicit `[JsonPropertyName]` |
| Unknown album ids still return HTTP 200 with the error envelope | Same code-800 handling as above |
| Rate limiting (roughly 50 requests / 5 seconds per IP) | Fine for this API's usage; a resilience/rate-limit layer is a planned upgrade |

## Mapping to the domain model

| Deezer JSON | `ProviderAlbum` |
| --- | --- |
| `id` (long) | `ProviderAlbumId` (string, invariant culture) |
| `title` | `Title` (falls back to empty) |
| `artist.name` | `ArtistName` (falls back to "Unknown Artist") |
| `release_date` | `ReleaseDate` (`DateOnly?`, `yyyy-MM-dd`) |
| `cover_xl` / `cover_big` | `CoverImageUrl` |
| `nb_tracks` | `TrackCount` |
| `tracks.data[]` | `Tracks` (`title`, `duration` seconds, position fallback) |

## Test data

Canned payloads that mirror the real API (including the error envelope) live in
`tests/MusicAlbums.Tests/Support/DeezerTestData.cs`. The live smoke test procedure
is documented in 06-testing-strategy.md.
