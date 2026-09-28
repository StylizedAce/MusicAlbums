# API Reference

Base URL in development: `http://localhost:5080`. OpenAPI document:
`/openapi/v1.json` (Development only). Ready-to-run requests:
`src/MusicAlbums.Api/MusicAlbums.Api.http`.

There is no authentication. The user is the `{userName}` route segment; unknown
users are created automatically on the first save. "Provider" is optional
everywhere; when omitted the configured default (`AlbumProviders:Default`) is used.

## Endpoints

| Method | Route | Purpose | Success | Errors |
| --- | --- | --- | --- | --- |
| GET | `/health/live` | Liveness probe, no dependencies | 200 `Healthy` | - |
| GET | `/health/ready` | Readiness probe incl. SQLite connectivity | 200 `Healthy` | 503 when the database is unreachable |
| GET | `/api/providers` | List registered providers | 200 | - |
| GET | `/api/albums/search?q&provider&limit&offset` | Search a provider catalogue | 200 | 400 missing `q` / unknown provider, 502 provider down |
| GET | `/api/albums/{provider}/{providerAlbumId}` | Fetch album details | 200 | 400 unknown provider, 404 unknown album, 502 provider down |
| GET | `/api/users/{userName}/library` | List a user's saved albums | 200 | 404 unknown user |
| GET | `/api/users/{userName}/library/{albumId}` | Get one saved album | 200 | 404 unknown user/album |
| POST | `/api/users/{userName}/library` | Save an album (fetches details from the provider, then snapshots it) | 201 + `Location` | 400 invalid body/unknown provider, 404 album missing in provider, 409 already saved, 502 provider down |
| DELETE | `/api/users/{userName}/library/{albumId}` | Remove a saved album | 204 | 404 unknown user/album |

All errors are RFC 9457 ProblemDetails (`application/problem+json`) produced by
`MusicAlbumsExceptionHandler`.

## Examples

```powershell
# Search the real Deezer catalogue (no credentials needed)
Invoke-RestMethod "http://localhost:5080/api/albums/search?q=daft%20punk&provider=deezer&limit=3"

# Save Discovery into alice's library (creates alice on first save)
Invoke-RestMethod -Method Post -Uri "http://localhost:5080/api/users/alice/library" `
  -ContentType "application/json" -Body '{"provider":"deezer","providerAlbumId":"302127"}'

# Read the library
Invoke-RestMethod "http://localhost:5080/api/users/alice/library"
```

## Response shapes

`SearchAlbumsResponse`

```json
{
  "albums": [
    { "provider": "deezer", "providerAlbumId": "302127", "title": "Discovery",
      "artist": "Daft Punk", "releaseDate": null, "coverImageUrl": "...",
      "trackCount": 14, "tracks": [] }
  ],
  "total": 95, "limit": 3, "offset": 0
}
```

`SavedAlbumResponse` adds `id` (GUID, used for delete/get), `savedAt` and
`externalUrl` (provider page), and its `tracks` array is populated from the
provider detail call. Each track carries `previewUrl` when the provider exposes a
30-second preview (Deezer does; Spotify often returns null).

## Demo frontend

`GET /` serves a single-page demo UI from `wwwroot` (same origin, no CORS):
provider selector, search, save/remove, expandable tracklists with preview
playback and provider links. It ships inside the API container and the
Kubernetes deployment - no separate web container.

## Acceptance tests

`tests/MusicAlbums.ApiTests` (Reqnroll/Gherkin) covers these endpoints over the
real HTTP pipeline with deterministic provider transports; run with `dotnet test`.

## Error mapping

| Domain exception | HTTP |
| --- | --- |
| `UnknownAlbumProviderException` | 400 |
| `ArgumentException` (invalid input) | 400 |
| `AlbumProviderNotFoundException` | 404 |
| `UserNotFoundException` | 404 |
| `AlbumNotInLibraryException` | 404 |
| `AlbumAlreadyInLibraryException` | 409 |
| `AlbumProviderUnavailableException` | 502 |
| anything else | 500 |
