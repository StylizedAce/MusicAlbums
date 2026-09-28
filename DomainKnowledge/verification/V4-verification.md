# V4 Verification Record

Date: 2026-09-28 (UTC)
Environment: Windows, .NET SDK 10.0.108, Reqnroll 3.3.4 + xunit 2.9.3
Scope: V4 - automated API acceptance suite (Reqnroll/Gherkin).

## Acceptance suite

Command:

```powershell
dotnet test tests/MusicAlbums.ApiTests --logger "trx;LogFileName=V4-automated-tests.trx" --results-directory DomainKnowledge/verification
```

Result: **Passed - 13/13 scenarios**.

Artifacts: [V4-automated-tests.trx](V4-automated-tests.trx),
[V4-automated-tests-console.txt](V4-automated-tests-console.txt).

## Coverage

| Feature | Scenarios |
| --- | --- |
| `Providers.feature` | list, default flagged, spotify registered |
| `AlbumSearch.feature` | search by artist, search by album name, fake-Spotify strategy, unknown provider -> 400, missing query -> 400 |
| `Library.feature` | save creates user + snapshots 4 stubbed tracks, duplicate -> 409, remove -> 204 + empty, unknown album -> 404, per-user isolation -> 404 |
| `Health.feature` | `/health/live` independent of dependencies, `/health/ready` includes SQLite |

All scenarios run the real HTTP pipeline (`WebApplicationFactory<Program>`, real
adapters, real DI, real EF Core with migrated SQLite in-memory) and replace only
the third-party transports with canned payloads, so the suite is deterministic and
network-free. Live verification remains the `scripts/smoke.ps1` gate against real
Deezer (see V3/V5 records).

## How it fits the workflow

- Separate project: `tests/MusicAlbums.ApiTests`, sharing the deterministic
  harness from `tests/MusicAlbums.TestSupport`.
- Plain `dotnet test` runs everything (unit/integration + acceptance) - no extra
  runtime, plugin server, or external tooling.

## Reproduce

```powershell
dotnet test tests/MusicAlbums.ApiTests
```
