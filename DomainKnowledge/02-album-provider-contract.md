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
| `spotify` | Fake, switch ready | `FakeSpotifyAlbumProvider` behind `AlbumProviders:Spotify:Mode`; real adapter in V3 |

## Spotify provider mode switch (V2)

Configuration (`AlbumProviders:Spotify`):

```json
{
  "Mode": "Fake",
  "AccountsBaseUrl": "https://accounts.spotify.com/",
  "ApiBaseUrl": "https://api.spotify.com/v1/"
}
```

- `Mode=Fake` (default): the deterministic in-memory catalogue serves requests.
  Credentials are not required.
- `Mode=Api`: startup validation requires `ClientId`/`ClientSecret`, and then the
  app **fails fast** with a clear message - the real adapter arrives in V3. There
  is deliberately no silent fallback to the fake.

Credentials never live in git. Local development uses user-secrets on the API
project (already initialized with a `UserSecretsId`):

```powershell
dotnet user-secrets set "AlbumProviders:Spotify:ClientId" "<id>" --project src/MusicAlbums.Api
dotnet user-secrets set "AlbumProviders:Spotify:ClientSecret" "<secret>" --project src/MusicAlbums.Api
```

Containers/host: `AlbumProviders__Spotify__ClientId` and
`AlbumProviders__Spotify__ClientSecret` environment variables (V3 syncs a
gitignored `.env` through TuxComp).

## Spotify roadmap (V3, credentials available)

Spotify requires OAuth 2.0 **client credentials**:

1. App registration on the Spotify dashboard (already done; copy the secret into
   user-secrets/env, never into the repo).
2. Implement the real `SpotifyAlbumProvider` with a typed `HttpClient`:
   - Token: `POST {AccountsBaseUrl}api/token` with `grant_type=client_credentials`
     (Basic auth) - cache the token until expiry.
   - Search: `GET {ApiBaseUrl}search?q={q}&type=album&limit&offset`
   - Detail: `GET {ApiBaseUrl}albums/{id}`
3. Replace the fail-fast branch in `DependencyInjection` with the typed-client
   registration and update the fail-fast test in `SpotifyConfigurationTests`.
4. Map payloads to `ProviderAlbum`; keep the fake for tests via DI.
