# Testing Strategy

Principles:

- Test **meaningful behavior**, not implementation details. No tests that assert
  the framework works.
- The real Deezer adapter is exercised end-to-end, but **never against the live
  network** in the test suite: a `StubHttpMessageHandler` serves canned Deezer
  payloads (captured from the real API).
- Persistence tests run against **real SQLite in-memory** with the real
  migrations applied; no EF InMemory provider, no mock repositories.
- The library service is tested with stubbed providers, so failures point at the
  service logic, not the network.

Run everything:

```powershell
dotnet test
```

## Suite map (27 tests)

`tests/MusicAlbums.Tests`

| Area | File | What it proves |
| --- | --- | --- |
| Provider factory | `Unit/AlbumProviderFactoryTests.cs` | Case-insensitive resolution, default provider, unknown provider throws, listing |
| Deezer adapter | `Unit/DeezerAlbumProviderTests.cs` | Search mapping + real Deezer paging params (`limit`/`index`), clamping, detail + track mapping, position fallback, error-envelope code 800 -> not found, HTTP failure -> unavailable, invalid release date |
| Spotify adapter | `Unit/SpotifyAlbumProviderTests.cs` | The second strategy honors the same contract (search, paging, detail, not found) |
| Library use cases | `Unit/LibraryServiceTests.cs` | User auto-creation, snapshot with tracks (including `SavedAt` from `TimeProvider`), duplicate save, per-user isolation, unknown provider/album/user, delete semantics |
| HTTP E2E | `Integration/LibraryApiTests.cs` | Full lifecycle through the real pipeline: providers list, search via real Deezer adapter over stubbed HTTP (default provider), save -> 201 + Location, library read with tracks, duplicate -> 409, delete -> 204, plus 400/404 error mapping and ProblemDetails |

## How the E2E tests replace infrastructure

`Integration/MusicAlbumsApiFactory.cs` (a `WebApplicationFactory<Program>`):

1. Removes the registered `DbContext` registrations and re-adds SQLite using a
   single kept-open `:memory:` connection, so each factory gets a fresh,
   migrated database.
2. Replaces the primary `HttpMessageHandler` of the typed `DeezerAlbumProvider`
   client with `StubHttpMessageHandler`, so the real adapter, JSON mapping, and
   exception behavior run unchanged.

Test classes are independent: integration tests use unique user names, and unit
tests get their own in-memory database (created with `Database.Migrate()`, which
also validates the migrations).

## Live smoke test (manual, optional)

Because Deezer needs no credentials, the real integration can be verified by
hand:

```powershell
dotnet run --project src/MusicAlbums.Api --launch-profile http
# then hit /api/albums/search?q=daft punk, save album 302127, read the library
```

The same flow is captured in `MusicAlbums.Api.http`. A recorded run on
2026-09-28 returned 95 search hits, saved Discovery with 14 real tracks, a 409 on
duplicate save, and an empty library after delete.

## Planned test upgrades

- Spotify live adapter tests (mirroring the Deezer handler fake) once real
  Spotify credentials exist.
- A contract test suite run against every `IAlbumProvider` implementation
  (search/detail invariants) to make provider additions cheaper.
