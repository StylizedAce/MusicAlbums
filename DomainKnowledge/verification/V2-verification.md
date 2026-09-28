# V2 Verification Record

Date: 2026-09-28 (UTC)
Environment: Windows, .NET SDK 10.0.108, Docker Desktop 28.0.4 (linux/amd64),
EF Core 10.0.12, xunit 2.9.3
Scope: V2 - Spotify options/mode switch, health endpoints, Docker artifacts,
smoke script.

## Automated test suite

Command:

```powershell
dotnet test --logger "trx;LogFileName=V2-automated-tests.trx" --results-directory DomainKnowledge/verification
```

Result: **Passed - 30/30, 0 failed, 0 skipped** (27 from V1 + 3 new).

Evidence artifacts:

- [V2-automated-tests.trx](V2-automated-tests.trx)
- [V2-automated-tests-console.txt](V2-automated-tests-console.txt)

New in V2:

| Test | Proves |
| --- | --- |
| `SpotifyConfigurationTests.SpotifyApiMode_FailsFast_UntilTheRealAdapterShips` | `Mode=Api` fails at startup with the explicit "not implemented yet" message - no silent fake fallback |
| `HealthEndpointsTests.Live_ReturnsHealthy_WithoutCheckingDependencies` | `/health/live` is independent of the database |
| `HealthEndpointsTests.Ready_ReturnsHealthy_WithDatabaseCheck` | `/health/ready` runs the SQLite check and reports `Healthy` |

## Native E2E (same smoke script, real Deezer)

```text
=== MusicAlbums V2 native E2E (same smoke script, real Deezer) ===
Date (UTC): 2026-09-28 08:07:36
Smoke testing http://localhost:5080 (provider=deezer, album=302127)
[ok] providers: deezer, spotify
[ok] search 'daft punk' -> total=95, first='Discovery'
[ok] saved 'Discovery' with 14 tracks for smoke-e1a0c361f88345e7b194f19c37edbb9d
[ok] library contains 'Discovery'
[ok] duplicate save -> 409
[ok] delete -> library empty
SMOKE TEST PASSED (http://localhost:5080)
RESULT: NATIVE E2E PASSED
```

## Container E2E (`docker compose`, real Deezer, persisted volume)

Image: `musicalbums-api:local`, ~410MB, `linux/amd64`, runs as user `app`,
exposes `8080`, healthcheck via `curl -fsS /health/live`.

```text
=== MusicAlbums V2 container E2E ===
Date (UTC): 2026-09-28 08:07:17
Image: musicalbums-api:local
 Container musicalbums-api  Started
health ready: HTTP 200 'Healthy'
docker health status: healthy

Smoke testing http://localhost:5080 (provider=deezer, album=302127)
[ok] providers: deezer, spotify
[ok] search 'daft punk' -> total=95, first='Discovery'
[ok] saved 'Discovery' with 14 tracks for smoke-10ba84df3e97472fbb58f0a8b2b70e13
[ok] library contains 'Discovery'
[ok] duplicate save -> 409
[ok] delete -> library empty
SMOKE TEST PASSED (http://localhost:5080)

=== restart persistence check ===
before restart: user=persist-36f514f2509d4f10a2911478e09d5392 albumId=b1c4739a-cf3c-458e-9218-5e5151f7722d title='Discovery'
after restart: ready=True
after restart: library count=1 title='Discovery' tracks=14
db file in volume: -rw-r--r-- 1 app app 53248 Sep 28 08:07 /data/musicalbums.db
cleanup delete -> ok
RESULT: CONTAINER E2E PASSED
```

Notes:

- The restart proves the named volume `musicalbums-data` keeps the SQLite snapshot
  across container restarts, owned by `app` (`-rw-r--r-- app app`).
- The stack was cleaned up afterwards with `docker compose down --volumes`.
- Raw log: [V2-live-container-smoke.txt](V2-live-container-smoke.txt)

## Reproduce

```powershell
dotnet build
dotnet test
# native
dotnet run --project src/MusicAlbums.Api --launch-profile http
.\scripts\smoke.ps1

# container
docker compose up --build
.\scripts\smoke.ps1
docker compose down --volumes
```
