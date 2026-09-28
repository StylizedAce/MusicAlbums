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
dotnet test                                           # 64 tests: 50 unit/integration + 14 BDD
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

> **Cluster choice.** We used the Kubernetes built into Docker Desktop (a
> single-node local cluster). The manifests are standard Kubernetes 1.28+ YAML and
> work unchanged on Minikube, kind or MicroK8s - the only difference is making the
> image visible to the nodes (`minikube image load musicalbums-api:local`, or push
> to a registry the cluster can pull from).

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

```powershell
dotnet test                              # everything: 50 unit/integration + 14 BDD
dotnet test tests/MusicAlbums.Tests      # unit + integration only
dotnet test tests/MusicAlbums.ApiTests   # the API acceptance suite (Part 2 deliverable)
```

| Project | Type | Coverage |
| --- | --- | --- |
| `tests/MusicAlbums.Tests` | unit + integration (xunit) | provider factory, adapters/mappers, token cache, library use cases, HTTP E2E, health, frontend shell |
| `tests/MusicAlbums.ApiTests` | acceptance (Reqnroll BDD) | Gherkin features for providers, search, library lifecycle, health |
| `tests/MusicAlbums.TestSupport` | shared harness | stubbed provider transports, SQLite in-memory, WebApplicationFactory |

### Testing approach: what we made testable, and why

We did not chase coverage; we chose seams.

- **`MusicAlbums.Core` has zero dependencies**, so the use cases (`LibraryService`,
  `AlbumCatalogService`) can be unit-tested against **real SQLite** and **stubbed
  providers** - no mocking framework, no in-memory fake of EF.
- **Providers are behind `IAlbumProvider`**, so both real adapters are tested by
  replacing the `HttpMessageHandler` with canned payloads captured from the live APIs.
  The adapter, its mapper, its error handling and its query building are all under
  test; only the network is gone. That is also why the suite is deterministic and
  never fails because a third party had a bad day.
- **Time is injected (`TimeProvider`)**, so `SavedAt` and Spotify token expiry are
  asserted exactly, with no sleeping and no clock flakiness.
- **The composition root is a real host** (`WebApplicationFactory<Program>`), so
  integration and BDD tests exercise routing, DI, EF migrations, the exception
  handler and the response shapes - not just services.
- **Live verification is a separate gate** (`scripts/smoke.ps1`) against real Deezer
  at each deployment level, so network flakiness never contaminates CI while real
  behaviour still gets proven.

## Architectural decisions (short version)

One deployable, three assemblies, one-way dependencies: `Api -> Infrastructure ->
Core`. The core abstraction is `IAlbumProvider` (Strategy), resolved by a factory by
name, so a third catalogue is *one class + one DI registration* - endpoints, use
cases, response shapes and the database schema are all provider-agnostic. Libraries
are local snapshots, so reads never depend on a third party. Errors are domain
exceptions translated once into RFC 9457 ProblemDetails.

Full reasoning, alternatives considered and reversals (35 entries) live in
[`DomainKnowledge/07-decision-log.md`](DomainKnowledge/07-decision-log.md); the
request flow is in [`10-code-walkthrough.md`](DomainKnowledge/10-code-walkthrough.md)
and the "how would you scale it" answers in
[`11-architecture-explained.md`](DomainKnowledge/11-architecture-explained.md).

## Scaling and adding providers

- **Provider boundary:** `IAlbumProvider` in Core, implemented in Infrastructure. The
  API and use cases never see provider-specific shapes; each adapter normalizes into
  `ProviderAlbum`/`ProviderTrack` through its own mapper (which absorbs that
  provider's quirks - Deezer's HTTP-200 error envelope and signed previews, Spotify's
  `release_date_precision`).
- **What stays the same when a third arrives:** endpoints, DTOs, use cases, the
  database schema, the uniqueness constraint, the frontend, and this test suite.
- **Data differences:** handled by the mapper layer, tabulated in
  [`03-deezer-integration.md`](DomainKnowledge/03-deezer-integration.md).
- **Rate limits (our next delivery, design in
  [`11-architecture-explained.md`](DomainKnowledge/11-architecture-explained.md#7-rate-limits-what-the-assignment-asks-and-what-we-do-today)):**
  documented limits (Spotify search clamped to 10) and use per-call timeouts.
  **Implemented now:** every provider call goes through `ThrottledAlbumProvider`,
  which caps in-flight requests per provider and opens a circuit after repeated
  upstream failures (fails fast instead of stacking 15s timeouts) - configured
  under `AlbumProviders:Throttling`. Still next: jittered retries honouring
  `Retry-After`, a short-lived per-provider cache, and provider call/latency
  metrics. Each provider's limits are configuration, not code.
- **Scaling the deployment:** the persistence layer is provider-swappable, so moving
  SQLite -> Postgres/SQL Server is one `UseSqlite` call plus a migration, which is
  what would unlock horizontal replicas (today: 1 replica, because SQLite has a
  single writer).

## What we deliberately left out

| Left out | Why | Revisit when |
| --- | --- | --- |
| **Authentication** | Out of scope for the exercise; identity is the `{userName}` route segment and the auth story is documented rather than faked. The honest middle ground is an `X-Api-Key` filter. | A real multi-user product ships. |
| **Client-side rate limiting / circuit breaking** | Real complexity guarding a problem we have not observed; Deezer has never throttled us in live testing. | Provider throttling or latency appears in metrics. |
| **Concurrent-save conflict polish** | Correctness is already guaranteed by the unique index; the loser currently gets a 500 instead of a 409. | Before adding retries, or on the first user complaint. |
| **GitHub Actions CI** | Deliberate scope call; the same commands are documented for manual runs. | When more than one person pushes. |
| **Library pagination** | Returns everything for a user; fine at demo scale. | Any user with a large library. |
| **Bulk/import endpoints** | Add is one call per album; no batch operation was needed. | Importing a user's existing collection. |
| **A separate frontend deployment** | The UI is static files inside the API image: one artifact, same origin, no CORS. | The UI grows into a real SPA with its own release cycle. |
| **Apple Music / Last.fm providers** | The contract is proven with two; a third is ~150 lines. | Someone asks for one. |
| **Deezer's authenticated endpoints** | Only public catalogue reads are used, which need no login. | Personalisation features. |

## Documentation

Start at [DomainKnowledge/README.md](DomainKnowledge/README.md): architecture,
provider contract, Deezer quirks, persistence, API reference, testing strategy,
decision log, deployment levels, and the full
[code walkthrough](DomainKnowledge/10-code-walkthrough.md).
