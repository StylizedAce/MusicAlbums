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
| `spotify` | Real + Fake | Real adapter with client-credentials token cache; the fake serves deterministic demos/tests; selected by `AlbumProviders:Spotify:Mode` |

## Spotify provider mode switch

Configuration (`AlbumProviders:Spotify`):

```json
{
  "Mode": "Fake",
  "AccountsBaseUrl": "https://accounts.spotify.com/",
  "ApiBaseUrl": "https://api.spotify.com/v1/",
  "TimeoutSeconds": 15
}
```

- `Mode=Fake` (default): the in-memory catalogue serves requests; no credentials.
- `Mode=Api`: the real adapter is registered and startup validation requires
  `ClientId`/`ClientSecret`. Requests acquire a cached token (client credentials,
  refreshed 30 seconds before expiry, single-flight locked).

### Spotify policy quirks (February 2026 Dev Mode changes)

- Search `limit` is capped at **10** for Development Mode apps; the provider clamps.
- All Development Mode apps require the app owner to have an active **Premium**
  subscription. Without it, data endpoints return a **bodyless HTTP 403** even
  though token acquisition succeeds; the provider surfaces an explanatory message.
- Live verification of the real adapter is currently blocked by account state (see
  `verification/V3-verification.md`); the adapter is verified end-to-end with the
  stubbed-transport E2E test instead.

Credentials never live in git:

- Local dev: user-secrets on `MusicAlbums.Api` (already initialized).
- Docker/Kubernetes: `AlbumProviders__Spotify__ClientId` / `...__ClientSecret`
  environment variables (Kubernetes: a Secret, see 09-deployment.md).

## Why the fake provider stays

`FakeSpotifyAlbumProvider` is the deterministic fixture for unit and BDD tests
(no network, stable payloads). Both providers share the same contract, so the
same features run against either strategy by changing configuration.
