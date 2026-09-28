# Code Walkthrough (big to small)

How a request travels from HTTP to the database and a third-party catalogue, and
back. Follow along in the source; file references are relative to the repo root.

## 1. The shape of the solution

```
src/MusicAlbums.Api            composition root + HTTP endpoints + DTOs + demo frontend (wwwroot)
        |            \
        v             v
src/MusicAlbums.Infrastructure  providers (Deezer, Spotify), EF Core, repositories, DI wiring
        |
        v
src/MusicAlbums.Core            abstractions, domain models, use-case services (no dependencies)
```

Dependency rule: `Api -> Infrastructure -> Core`, and `Api -> Core`. Core knows
nothing about EF Core, HTTP, or ASP.NET Core - it is plain C#. That is what makes
the use cases unit-testable and the third-party integrations swappable.

## 2. Startup: the composition root

`src/MusicAlbums.Api/Program.cs` is the only place that knows about everything:

1. `builder.Services.AddInfrastructure(builder.Configuration)` registers:
   - `MusicAlbumsDbContext` (SQLite) and the repositories + unit of work;
   - provider options with **startup validation**;
   - typed `HttpClient`s for Deezer and Spotify (accounts client for tokens);
   - the provider strategies and `IAlbumProviderFactory`.
2. The application services from Core are registered (`ILibraryService`,
   `IAlbumCatalogService`), plus `TimeProvider.System` (for deterministic clocks).
3. `AddExceptionHandler<MusicAlbumsExceptionHandler>()` translates domain
   exceptions to RFC 9457 ProblemDetails.
4. After `app.Build()`, migrations are applied, static files are enabled
   (`wwwroot` = the demo UI), and the endpoint groups are mapped.

Lifetime summary:

| Service | Lifetime | Why |
| --- | --- | --- |
| `IAlbumProviderFactory` | transient | cheap; composes the current provider set |
| `DeezerAlbumProvider`, `SpotifyAlbumProvider` | transient | typed `HttpClient` convention |
| `FakeSpotifyAlbumProvider` | singleton | stateless in-memory catalogue |
| `SpotifyTokenProvider` | singleton | one cached token + single-flight refresh for the process |
| `IUserRepository`, `ISavedAlbumRepository`, `ILibraryService`, DbContext, `IUnitOfWork` | scoped | one unit of work per request |
| `TimeProvider` | singleton | system clock (fake in tests) |

The mode switch lives here too: `AlbumProviders:Spotify:Mode=Fake` registers the
fake strategy, `Mode=Api` registers the real one.

## 3. Endpoints: thin HTTP boundary

`src/MusicAlbums.Api/Endpoints`:

| File | Routes |
| --- | --- |
| `ProviderEndpoints.cs` | `GET /api/providers` |
| `CatalogEndpoints.cs` | `GET /api/albums/search`, `GET /api/albums/{provider}/{id}` |
| `LibraryEndpoints.cs` | `GET/POST /api/users/{userName}/library`, `GET/DELETE .../{albumId}` |

Endpoints only bind input, delegate to a Core service, and map the result to a
response DTO (`src/MusicAlbums.Api/Contracts`). No business logic, no EF, no HTTP
client code.

## 4. Use cases: Core services

`src/MusicAlbums.Core/Application/LibraryService.cs` orchestrates a save:

1. Resolve the provider through `IAlbumProviderFactory` (default or requested).
2. Find the user by name (`IUserRepository`); create it in memory if missing -
   auto-provisioning happens only on write.
3. Fetch the album **detail** from the provider (`IAlbumProvider.GetAlbumAsync`) -
   this is where the full tracklist and preview URLs come from.
4. Reject duplicates through `ISavedAlbumRepository.ExistsAsync`.
5. Snapshot with `SavedAlbum.FromProvider(...)` (domain factory) and persist via
   `IUnitOfWork.SaveChangesAsync`.

Search is similarly thin: `AlbumCatalogService` resolves the provider and delegates
`SearchAlbumsAsync`, which normalizes third-party payloads into `ProviderAlbum`.

## 5. Abstractions: the seams

`src/MusicAlbums.Core/Abstractions`:

- `IAlbumProvider` - the **Strategy** contract: `Name`, `SearchAlbumsAsync`,
  `GetAlbumAsync`. Every catalogue integration implements exactly this.
- `IAlbumProviderFactory` - the **Factory/registry**: resolves by name
  (case-insensitive) or the configured default; unknown names throw
  `UnknownAlbumProviderException`.
- `IUserRepository`, `ISavedAlbumRepository` - **Repository** abstractions.
- `IUnitOfWork` - commit boundary, implemented by the DbContext.
- `ILibraryService`, `IAlbumCatalogService` - the use-case contracts the API calls.

The domain models (`ProviderAlbum`, `ProviderTrack`, `SavedAlbum`, ...) are plain
records/classes in `Core.Models`; EF Core maps them directly (no separate entity
duplication).

## 6. Infrastructure: the adapters

`src/MusicAlbums.Infrastructure/Providers`.

**Deezer** (`Deezer/`): typed `HttpClient` with `BaseAddress` from options.
`DeezerAlbumProvider` calls `search/album?q=&limit=&index=` and `album/{id}`,
deserializes snake_case DTOs, and maps them through `DeezerAlbumMapper`. It knows
Deezer's quirks: error envelopes with HTTP 200 (`{"error":{"code":800}}` -> not
found), missing `track_position` -> array index, `0000-00-00` dates -> null,
`cover_xl` preferred, `preview` captured for 30-second audio, `link` as the album
URL.

**Spotify** (`Spotify/`): `SpotifyTokenProvider` (singleton) obtains a
client-credentials token with Basic auth and caches it until 30 seconds before
expiry, using a `SemaphoreSlim` so concurrent requests trigger a single token
call. `SpotifyAlbumProvider` attaches the bearer token per request, calls
`search?type=album` (limit clamped to 10 per the February 2026 Dev Mode policy)
and `albums/{id}`, maps `images` (largest first), `external_urls.spotify`,
`release_date` + precision, `duration_ms` and `preview_url`. Bodyless 403s get an
explanatory Premium-policy message.

`AlbumProviderFactory` is the registry, built from `IEnumerable<IAlbumProvider>` -
adding a provider means writing a class and one DI registration.

## 7. Persistence: EF Core

`src/MusicAlbums.Infrastructure/Persistence`:

- `MusicAlbumsDbContext` exposes `Users`, `SavedAlbums`, `SavedTracks` and
  implements `IUnitOfWork`, so application services commit explicitly.
- Entity configurations set table names, lengths, the case-insensitive unique
  user name (`NOCASE`), the unique `(UserId, ProviderName, ProviderAlbumId)`
  index, cascade deletes, and UTC-ticks value converters for `DateTimeOffset`
  (SQLite cannot `ORDER BY` that type).
- Repositories use `AsNoTracking` for reads and ordered `Include` for tracks.
- Migrations live next to the context and are applied automatically at startup.

## 8. Response shaping and error translation

- `ContractMappings.cs` converts domain objects to response DTOs
  (`AlbumResponse`, `SavedAlbumResponse`, `TrackResponse`, ...).
- `MusicAlbumsExceptionHandler.cs` maps domain exceptions to status codes:
  unknown provider/argument -> 400, not-found family -> 404, duplicate -> 409,
  upstream provider failure -> 502, anything else -> 500 - always as
  `application/problem+json`.

## 9. Two traces

**Search** `GET /api/albums/search?q=daft punk&provider=deezer`:

```
CatalogEndpoints -> validates q -> IAlbumCatalogService.SearchAlbumsAsync
  -> IAlbumProviderFactory.GetRequired("deezer") -> DeezerAlbumProvider
     -> HttpClient GET https://api.deezer.com/search/album?q=daft%20punk&limit=25&index=0
     -> Deezer DTOs -> DeezerAlbumMapper -> ProviderAlbum[]
  -> SearchAlbumsResponse -> 200 JSON
```

**Save** `POST /api/users/alice/library {"provider":"deezer","providerAlbumId":"302127"}`:

```
LibraryEndpoints -> IUserRepository.FindByNameAsync("alice")   (not found: create in memory)
  -> DeezerAlbumProvider.GetAlbumAsync("302127")               (detail + tracks + previews)
  -> ISavedAlbumRepository.ExistsAsync(...)                    (duplicate: 409)
  -> SavedAlbum.FromProvider(user, album, timeProvider.GetUtcNow())
  -> ISavedAlbumRepository.Add + IUnitOfWork.SaveChangesAsync
  -> 201 Created + Location + SavedAlbumResponse
```

Delete and health follow the same shape: endpoint -> Core service or health
check -> repository/DbContext -> 204/200.

## 10. Where the patterns live

| Pattern | Location |
| --- | --- |
| Strategy | `IAlbumProvider` implementations |
| Factory/registry | `AlbumProviderFactory` |
| Repository | `IUserRepository`, `ISavedAlbumRepository` + EF implementations |
| Unit of Work | `IUnitOfWork` implemented by `MusicAlbumsDbContext` |
| Options (validated) | `DeezerOptions`, `SpotifyOptions`, `AlbumProviderOptions` |
| Composition root | `src/MusicAlbums.Api/Program.cs` |
| Adapter + mapper | Deezer/Spotify DTOs + mappers |
| Domain factory | `SavedAlbum.FromProvider` |
| Exception translator | `MusicAlbumsExceptionHandler` |
| Test double seam | `HttpMessageHandler` replacement + `WebApplicationFactory` |

## 11. Adding a third provider

1. Implement `IAlbumProvider` under `Providers/<Name>/` (typed `HttpClient` if it
   talks HTTP, otherwise a stateless singleton).
2. Register it in `AddInfrastructure` (one `AddHttpClient` + one
   `AddTransient<IAlbumProvider>` line).
3. Optionally set `AlbumProviders:Default`.

No endpoint, service, or schema changes; `GET /api/providers` lists it
automatically and the library uniqueness constraint already covers it.

## 12. How tests intercept the outside world

`tests/MusicAlbums.TestSupport` builds a `WebApplicationFactory<Program>` that
- swaps the SQLite database for an open `:memory:` connection with migrations;
- replaces the primary `HttpMessageHandler`s of the Deezer/Spotify clients with
  canned payload responders (captured from the real APIs), so the **real**
  adapters, DI, EF Core, and endpoints run unchanged.

`tests/MusicAlbums.Tests` holds unit + integration tests; `tests/MusicAlbums.ApiTests`
holds the Reqnroll/Gherkin acceptance suite. `scripts/smoke.ps1` is the live E2E
gate against real Deezer (native, Docker, or Kubernetes).
