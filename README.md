# MusicAlbums

A per-user music album library backend that imports album metadata from
third-party catalogues (Deezer and Spotify) through a pluggable provider
strategy. Built with .NET 10, EF Core 10, minimal APIs, xunit and Reqnroll.

- **Deezer**: real integration via the public REST API - no API key required.
- **Spotify**: real client-credentials adapter (search + album details, cached
  token) plus a deterministic fake provider selected by configuration.
- **Library**: albums are snapshotted into SQLite with tracks, 30-second preview
  URLs and provider links, so reads never hit a third party.
- **Demo UI**: a single-page frontend is served by the API at `/`.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (the repo pins everything
  else: packages via central package management, `dotnet-ef` via a local tool
  manifest)
- PowerShell 5.1+ for the helper scripts (`scripts/smoke.ps1`) - the API itself
  and `dotnet test` work on any platform
- Optional: Docker Desktop for the container level
- Optional: Kubernetes enabled in Docker Desktop for the cluster level

No paths, usernames, or machine-specific settings are required; every command is
relative to the repository root.

## Quick start (local)

```powershell
dotnet tool restore                                   # restores the pinned dotnet-ef tool
dotnet build                                          # 0 warnings (warnings are errors)
dotnet test                                           # 56 tests: 43 unit/integration + 13 BDD
dotnet run --project src/MusicAlbums.Api --launch-profile http
```

Then open:

- UI: <http://localhost:5080>
- OpenAPI document: <http://localhost:5080/openapi/v1.json>
- Health: <http://localhost:5080/health/live> and `/health/ready`

The SQLite database (`src/MusicAlbums.Api/musicalbums.db`) is created and
migrated automatically on startup and is gitignored.

Run the live end-to-end smoke test against real Deezer (works against any host):

```powershell
.\scripts\smoke.ps1                                          # native
.\scripts\smoke.ps1 -BaseUrl http://localhost:30080          # kubernetes
```

## Docker

```powershell
docker compose up --build
# -> http://localhost:5080, SQLite persisted in the named volume musicalbums-data
docker compose down --volumes
```

## Kubernetes (Docker Desktop)

```powershell
docker build -t musicalbums-api:local .
kubectl apply -f deploy/k8s/namespace.yaml
kubectl apply -f deploy/k8s/configmap.yaml -f deploy/k8s/pvc.yaml -f deploy/k8s/deployment.yaml -f deploy/k8s/service.yaml
kubectl -n musicalbums rollout status deployment/musicalbums-api
# -> UI + API on http://localhost:30080
```

Full guide: [deploy/k8s/README.md](deploy/k8s/README.md). The app boots with no
credentials (Spotify stays in `Fake` mode); create the optional Secret to switch
to the real Spotify adapter.

## Example flow

```powershell
Invoke-RestMethod "http://localhost:5080/api/albums/search?q=daft%20punk"           # search (default provider)
Invoke-RestMethod -Method Post -Uri "http://localhost:5080/api/users/alice/library" `
  -ContentType "application/json" -Body '{"provider":"deezer","providerAlbumId":"302127"}'
Invoke-RestMethod "http://localhost:5080/api/users/alice/library"                    # library snapshot
```

More requests: `src/MusicAlbums.Api/MusicAlbums.Api.http`.

## API summary

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/` | Demo frontend |
| GET | `/health/live`, `/health/ready` | Probes (readiness checks SQLite) |
| GET | `/api/providers` | Registered strategies + default |
| GET | `/api/albums/search?q=&provider=&limit=&offset=` | Search by album or artist name |
| GET | `/api/albums/{provider}/{providerAlbumId}` | Album details with tracks |
| GET/POST | `/api/users/{userName}/library` | List / save (auto-creates the user) |
| GET/DELETE | `/api/users/{userName}/library/{albumId}` | Fetch / remove a saved album |

Users are created on first save; providers default to `AlbumProviders:Default`
(`deezer`) when omitted. Errors are RFC 9457 ProblemDetails. There is **no auth**:
identity is the `{userName}` route segment, documented as a deliberate scope
decision (see `DomainKnowledge/07-decision-log.md`).

## Configuration and secrets

| Setting | Default | Notes |
| --- | --- | --- |
| `ConnectionStrings:Library` | `Data Source=musicalbums.db` | container/K8s override to `/data/musicalbums.db` |
| `AlbumProviders:Default` | `deezer` | provider used when a request omits one |
| `AlbumProviders:Spotify:Mode` | `Fake` | `Api` enables the real adapter (needs credentials) |
| `AlbumProviders:Spotify:ClientId` / `ClientSecret` | - | local: user-secrets; containers: env vars; K8s: Secret |

## Tests

| Project | Type | Coverage |
| --- | --- | --- |
| `tests/MusicAlbums.Tests` | unit + integration (xunit) | provider factory, adapters/mappers, token cache, library use cases, HTTP E2E, health, frontend shell |
| `tests/MusicAlbums.ApiTests` | acceptance (Reqnroll BDD) | Gherkin features for providers, search, library lifecycle, health |
| `tests/MusicAlbums.TestSupport` | shared harness | stubbed provider transports, SQLite in-memory, WebApplicationFactory |

## Documentation

Start at [DomainKnowledge/README.md](DomainKnowledge/README.md): architecture,
provider contract, Deezer quirks, persistence, API reference, testing strategy,
decision log, deployment levels, and the full
[code walkthrough](DomainKnowledge/10-code-walkthrough.md).
