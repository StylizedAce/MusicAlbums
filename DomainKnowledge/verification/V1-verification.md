# V1 Verification Record

Date: 2026-09-28 (UTC)
Environment: Windows, .NET SDK 10.0.108, EF Core 10.0.12, xunit 2.9.3
Scope: V1 - Core contract/models/services, EF Core persistence, real Deezer
provider, fake Spotify provider, minimal API, test suite.

## Automated test suite

Command:

```powershell
dotnet test --logger "trx;LogFileName=V1-automated-tests.trx" --results-directory DomainKnowledge/verification
```

Result: **Passed - 27/27, 0 failed, 0 skipped** (~1-2 s).

Evidence artifacts:

- [V1-automated-tests.trx](V1-automated-tests.trx) - machine-readable TRX report
- [V1-automated-tests-console.txt](V1-automated-tests-console.txt) - console output

Coverage by area:

| Area | Tests | Key assertions |
| --- | --- | --- |
| Provider factory | 4 | case-insensitive resolution, default, unknown provider, listing |
| Deezer adapter | 6 | search mapping + `limit`/`index` params, clamping, detail + track mapping, position fallback, error envelope 800 -> 404, HTTP failure -> 502, invalid date |
| Spotify adapter | 4 | second strategy honors the same contract |
| Library use cases | 7 | user auto-creation, snapshot with tracks, deterministic `SavedAt`, duplicate -> conflict, per-user isolation, unknown provider/album/user, delete |
| HTTP E2E (WebApplicationFactory) | 6 | full lifecycle over the real pipeline, default-provider routing, ProblemDetails mapping |

## Live E2E smoke test (real Deezer, real SQLite, real Kestrel)

Command: `dotnet run --project src/MusicAlbums.Api --launch-profile http`
plus the request sequence in `src/MusicAlbums.Api/MusicAlbums.Api.http`.
Raw output: [V1-live-smoke.txt](V1-live-smoke.txt)

```text
=== MusicAlbums V1 live E2E smoke test ===
Date (UTC): 2026-09-28 07:44:00
SDK: 10.0.108

GET /api/providers -> deezer(default=True), spotify(default=False)
GET /api/albums/search?q=daft punk (deezer) -> total=95, first='Discovery' id=302127
POST /api/users/smoke-991bc934.../library -> 201 title='Discovery' release=2001-03-07 artist='Daft Punk' tracks=14
GET /api/users/smoke-991bc934.../library -> count=1 firstTrack='One More Time' trackCount=14
POST duplicate -> 409 (expected 409)
DELETE album -> 204; GET library after delete -> count=0
GET unknown provider -> 400 (expected 400)
DB file created: True
RESULT: ALL LIVE CHECKS PASSED
```

Notes:

- Deezer required no credentials; real catalogue data (95 hits for "daft punk";
  Discovery with 14 tracks) flowed through the adapter, mapper, service, EF Core,
  and API unchanged.
- `musicalbums.db` is created automatically at startup and is gitignored.

## Reproduce

```powershell
dotnet build
dotnet test
dotnet run --project src/MusicAlbums.Api --launch-profile http
# then run the requests from src/MusicAlbums.Api/MusicAlbums.Api.http
```
