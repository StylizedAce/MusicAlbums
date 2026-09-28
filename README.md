# MusicAlbums

A per-user music album library backend that imports album metadata from
third-party catalogues (Deezer and Spotify) through a pluggable provider
strategy. Built with .NET 10, EF Core 10, minimal APIs and xunit.

- **Deezer**: real integration via the public REST API - no API key required.
- **Spotify**: contract-first fake provider for now; a real OAuth client-credentials
  adapter can be dropped in behind the same interface.
- **Library**: albums are snapshotted into SQLite, so reads never hit a third party.

## Structure

```
src/MusicAlbums.Core            abstractions, domain models, use cases (no dependencies)
src/MusicAlbums.Infrastructure  EF Core persistence + provider adapters (Deezer, Spotify)
src/MusicAlbums.Api             minimal API endpoints and composition root
tests/MusicAlbums.Tests         unit + E2E integration tests (27)
DomainKnowledge/                architecture, contracts, decisions, integration notes
```

## Quick start

```powershell
dotnet tool restore      # restores the pinned dotnet-ef tool
dotnet build
dotnet test
dotnet run --project src/MusicAlbums.Api --launch-profile http
# -> http://localhost:5080 (OpenAPI document at /openapi/v1.json)
```

The SQLite database is created and migrated automatically on startup.

## Example flow

```powershell
Invoke-RestMethod "http://localhost:5080/api/albums/search?q=daft%20punk"
Invoke-RestMethod -Method Post -Uri "http://localhost:5080/api/users/alice/library" `
  -ContentType "application/json" -Body '{"provider":"deezer","providerAlbumId":"302127"}'
Invoke-RestMethod "http://localhost:5080/api/users/alice/library"
```

More requests: `src/MusicAlbums.Api/MusicAlbums.Api.http`.

## API summary

| Method | Route |
| --- | --- |
| GET | `/api/providers` |
| GET | `/api/albums/search?q=&provider=&limit=&offset=` |
| GET | `/api/albums/{provider}/{providerAlbumId}` |
| GET/POST | `/api/users/{userName}/library` |
| GET/DELETE | `/api/users/{userName}/library/{albumId}` |

Users are created on the fly on first save; providers default to
`AlbumProviders:Default` (`deezer`) when omitted. Errors are RFC 9457
ProblemDetails.

## Documentation

See [DomainKnowledge/README.md](DomainKnowledge/README.md) for the architecture,
the `IAlbumProvider` contract, Deezer quirks, persistence details, testing
strategy, and the running decision log.
