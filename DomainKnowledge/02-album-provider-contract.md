# The Album Provider Contract

Every third-party music catalogue is adapted behind one interface in
`MusicAlbums.Core.Abstractions`:

```csharp
public interface IAlbumProvider
{
    string Name { get; }  // stable, lowercase key: "deezer", "spotify"

    Task<AlbumSearchResult> SearchAlbumsAsync(AlbumSearchQuery query, CancellationToken cancellationToken = default);

    Task<ProviderAlbum> GetAlbumAsync(string providerAlbumId, CancellationToken cancellationToken = default);
}
```

## Supporting models (Core.Models)

- `AlbumSearchQuery(Text, Limit = 25, Offset = 0)` - `MaxLimit` is 100.
- `AlbumSearchResult(Albums, Total, Query)` - `Query` echoes the effective
  (clamped) paging values.
- `ProviderAlbum(ProviderName, ProviderAlbumId, Title, ArtistName, ReleaseDate,
  CoverImageUrl, TrackCount, Tracks)` - normalized album data.
  `Tracks` is empty for search results when the provider search response does not
  include a tracklist.
- `ProviderTrack(Position, Title, DurationSeconds)`.

## Contract rules

1. `Name` must be stable and lowercase; it is the public `provider` value on the
   API and is used as the factory key (resolution is case-insensitive).
2. `SearchAlbumsAsync` must clamp paging to `[1..MaxLimit]` and never return
   `null` collections.
3. `GetAlbumAsync` throws `AlbumProviderNotFoundException` when the id does not
   exist in the catalogue, and `AlbumProviderUnavailableException` when the
   upstream system cannot be reached or misbehaves.
4. Providers are stateless; infrastructure concerns (HTTP clients, credentials)
   are injected via constructors.

## How to add a third provider (the point of the Strategy pattern)

1. Implement `IAlbumProvider` in `MusicAlbums.Infrastructure/Providers/<Name>/`.
2. Register it in `DependencyInjection.AddInfrastructure` (typed `HttpClient` for
   HTTP providers, singleton for stateless ones).
3. Optionally point `AlbumProviders:Default` at it.

Nothing else changes: no endpoint, service, or test infrastructure edits are
required. `GET /api/providers` automatically lists the new provider.

## Strategy status

| Provider | Status | Notes |
| --- | --- | --- |
| `deezer` | Real | Public REST API, no credentials (see 03-deezer-integration.md) |
| `spotify` | Fake | Contract-first in-memory catalogue; deterministic for demos and tests |

## Spotify roadmap (when credentials are available)

Spotify requires OAuth 2.0 **client credentials**:

1. Create an app at https://developer.spotify.com/dashboard (free).
2. Copy `ClientId` / `ClientSecret` (store in user-secrets, never in git).
3. Implement `SpotifyAlbumProvider` with a typed `HttpClient`:
   - Token: `POST https://accounts.spotify.com/api/token` with
     `grant_type=client_credentials` (Basic auth) - cache the token until expiry.
   - Search: `GET https://api.spotify.com/v1/search?q={q}&type=album&limit&offset`
   - Detail: `GET https://api.spotify.com/v1/albums/{id}`
4. Map the payloads to `ProviderAlbum` and keep the fake for tests via DI.
