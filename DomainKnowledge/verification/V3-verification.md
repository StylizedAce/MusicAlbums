# V3 Verification Record

Date: 2026-09-28 (UTC)
Environment: Windows, .NET SDK 10.0.108, real Deezer API, user-secrets configured
Scope: V3 - provider album URLs, real Spotify adapter, preview URLs, policy quirks.

## Automated test suite

Command:

```powershell
dotnet test tests/MusicAlbums.Tests --logger "trx;LogFileName=V3-automated-tests.trx" --results-directory DomainKnowledge/verification
```

Result: **Passed - 43/43** (V2's 30 + preview/URL asserts + real Spotify adapter,
token-cache, mode and full-stack stubbed-transport E2E tests).

Artifacts: [V3-automated-tests.trx](V3-automated-tests.trx),
[V3-automated-tests-console.txt](V3-automated-tests-console.txt).

## Live E2E (native, real Deezer)

```text
=== MusicAlbums V3 live E2E (native, real Deezer) ===
Date (UTC): 2026-09-28 09:04:29
Smoke testing http://localhost:5080 (provider=deezer, album=302127)
[ok] providers: deezer, spotify
[ok] search 'daft punk' -> total=95, first='Discovery'
[ok] saved 'Discovery' with 14 tracks for smoke-2fe8e9702dfe4f35920c528fe411919f
[ok] library contains 'Discovery'
[ok] duplicate save -> 409
[ok] delete -> library empty
SMOKE TEST PASSED (http://localhost:5080)
externalUrl: https://www.deezer.com/album/302127
previewUrl sample: https://cdnt-preview.dzcdn.net/api/1/1/f/8/c/0/f8c5dc3837912...
RESULT: V3 LIVE E2E PASSED
```

Raw log: [V3-live-smoke.txt](V3-live-smoke.txt).

## Spotify: adapter verified, live access blocked by account policy

- The real `SpotifyAlbumProvider` (client-credentials token cache, search, album
  detail, 404/403/error handling) is covered end-to-end by
  `SpotifyApiModeEndToEndTests.SearchSaveAndDelete_RunThroughTheRealSpotifyAdapter_WithStubbedTransport`
  (real DI, adapter, token flow, EF Core and endpoints; canned Spotify transport).
- Live attempt on 2026-09-28: token acquisition succeeds
  (`POST https://accounts.spotify.com/api/token` -> 200) but every data endpoint
  returns a **bodyless HTTP 403** (`/v1/search`, `/v1/albums/{id}`), including
  with `limit` within the new maximum of 10.
- Cause per Spotify's official February 2026 Dev Mode migration guide: all
  Development Mode apps require the app owner to have an active **Premium**
  subscription; search `limit` is capped at 10.
  (<https://developer.spotify.com/documentation/web-api/tutorials/february-2026-migration-guide>)
- Handling: provider clamps search to 10 and turns bodyless 403s into an
  explanatory message (`...requires the app owner to have an active Premium
  subscription...`). Decision: keep `Mode=Fake` as the demo default and
  **revisit** the live check when Premium is active on the app-owner account
  (recorded in the decision log, row 30).

## Reproduce

```powershell
dotnet test tests/MusicAlbums.Tests
dotnet run --project src/MusicAlbums.Api --launch-profile http
.\scripts\smoke.ps1
```
