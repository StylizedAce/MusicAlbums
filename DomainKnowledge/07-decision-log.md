# Decision Log

Running log of decisions, newest last. Format: decision - rationale -
alternatives considered.

| # | Decision | Rationale | Alternatives |
| --- | --- | --- | --- |
| 1 | Greenfield scaffold (repo was empty) | Nothing existed in-repo; external systems are Deezer/Spotify | Wait for an existing API contract |
| 2 | `.slnx` solution format | SDK 10 default; cleaner XML | `.sln` (force with `dotnet new sln -f sln`) |
| 3 | Central Package Management (`Directory.Packages.props`) | Single source for versions; no drift across 4 projects | Per-project versions |
| 4 | `TreatWarningsAsErrors` + `Nullable` + `ImplicitUsings` | Keeps the codebase clean from day one | Loosen later if it gets noisy |
| 5 | Core has zero package dependencies | Enforces the dependency rule; abstractions and use cases only | Add `Microsoft.Extensions.DependencyInjection.Abstractions` to host an `AddCore()` |
| 6 | Minimal APIs with endpoint groups, not controllers | .NET 10 default; less ceremony; typed results | Controllers |
| 7 | EF Core inside `MusicAlbums.Infrastructure` | Infrastructure owns all I/O (DB + third parties); keeps project count low | Separate `MusicAlbums.Data` project |
| 8 | SQLite for persistence | Zero-setup local dev/demo; provider swap is a one-line change | SQL Server, PostgreSQL |
| 9 | Users identified by `{userName}` route segment, auto-created on first save | No auth scope for now; keeps demo flow frictionless; unique NOCASE index gives case-insensitive identity | JWT claims, ASP.NET Identity, Guid-only routes |
| 10 | Library is a persisted snapshot; provider only used for search + save-time details | Reads never depend on third parties; supports offline history | Fetch-through proxy; TTL cache |
| 11 | Deezer is a real integration with a typed `HttpClient` | Public API needs no credentials (verified 2026-09-28); gives real data to demo | Fake like Spotify |
| 12 | Spotify is a fake provider behind the same interface | Contract-first: proves the Strategy pattern; real OAuth flow deferred until credentials exist | Implement Spotify first (needs client credentials) |
| 13 | Deezer JSON mapped manually through DTOs + mapper | Snake_case fields, error-envelope quirk, `track_position` absence need explicit handling | Source-generated JSON, AutoMapper |
| 14 | Domain exceptions translated to ProblemDetails in one handler | Controllers/services stay transport-agnostic; single place for status mapping | Status codes returned from endpoints; `Results.Problem` per endpoint |
| 15 | `TimeProvider` injected into `LibraryService` | Deterministic `SavedAt` in tests without clock mocking | `DateTimeOffset.UtcNow`, `FakeTimeProvider` package |
| 16 | `DateTimeOffset` stored as `UtcTicks` via value converter | SQLite cannot `ORDER BY DateTimeOffset` (EF throws); ticks are order-preserving and keep "newest first" in SQL | `DateTime` UTC property; client-side ordering |
| 17 | Single xunit test project with `Unit/` + `Integration/` | Matches "one xunit project" request; integration tests share one factory fixture; avoids project sprawl | Per-layer test projects |
| 18 | E2E tests stub only the HTTP transport of the real Deezer adapter | Exercises adapter, mapping, service, EF, API, error mapping in one flow without network flakiness | Mock `IAlbumProvider`; WireMock; live calls |
| 19 | `dotnet-ef` pinned as a repo-local tool (`dotnet-tools.json`) | Reproducible migrations; no machine-level install | Global tool install |
| 20 | `Microsoft.OpenApi` pinned to 2.12.2 | Transitive 2.0.0 from the template had advisory GHSA-v5pm-xwqc-g5wc; 2.x line keeps compatibility | Upgrade to 3.x (breaking) |
| 21 | Auto-apply migrations at startup | Fresh clone runs with one command; fine for a SQLite-backed service | `dotnet ef database update` step in deployment |
| 22 | Spotify provider chosen via `AlbumProviders:Spotify:Mode` with credentials from user-secrets/env | One codebase demonstrates both strategies; secrets never in git | Separate builds/configs per provider |
| 23 | `Mode=Api` fails fast at startup until the V3 adapter exists | Fail loudly beats silently serving fake data | Fallback to fake with a warning; implement real Spotify in V2 |
| 24 | Health split into `/health/live` and `/health/ready` (ready checks SQLite) | Orchestrator-friendly: liveness never depends on the DB | Single `/health` with DB check |
| 25 | Container runtime: non-root `app`, curl installed for healthchecks, `/data` pre-owned by `app` | Least privilege + a healthcheck that actually validates HTTP, named volume writable by non-root | Root user; bash `/dev/tcp` check; bind mount |
| 26 | Compose project name pinned to `musicalbums`, host port `5080:8080` | Stable container/volume names for TuxComp; matches local docs | Directory-derived project name; port 8080 on host |
| 27 | Tiny-delivery commits: each commit independently verified (build + tests; container E2E for the Docker delivery) | Interview-ready workflow; no untested code in history | One big-bang commit per version |
| 28 | Auth: none; identity is the `{userName}` route segment, documented | Fits the interview scope; no false sense of security | API key filter; JWT bearer |
| 29 | Album `externalUrl` exposed (Deezer `link`, Spotify `external_urls.spotify`) | Users need a jump to the provider page; cheap to map | Omit |
| 30 | Spotify live blocked by the February 2026 Premium policy; `Fake` stays the demo default; policy + dated 403 evidence documented | Account-level restriction, not a code defect; adapter covered by stubbed-transport E2E | Wait for Premium; drop Spotify |
| 31 | Demo frontend served from the API's `wwwroot` instead of its own Deployment | One container, same origin, zero CORS; conventional for backend-hosted UIs | nginx pod + Ingress + CORS |
| 32 | Kubernetes: 1 replica, `Recreate`, PVC, NodePort 30080, optional Secret reference | SQLite single-writer; containers boot self-contained without credentials | Multi-replica + Postgres; Ingress |
| 33 | Shared test harness extracted to `MusicAlbums.TestSupport` | One deterministic harness serves unit, integration and BDD suites | Duplicated fakes; mocking libraries |
| 34 | Reqnroll BDD as the API acceptance layer | Living documentation, runs in `dotnet test`, no extra runtime/tooling | Playwright .NET; Postman+Newman; Testcontainers |
| 35 | Deployment levels: local -> Docker -> Kubernetes; TuxComp phone path dropped | Reproducible anywhere; phone deployment added no interview value | TuxComp/termux deployment |
| 36 | Provider resilience as a decorator (`ThrottledAlbumProvider`): per-provider concurrency cap + circuit breaker, configured via `AlbumProviders:Throttling` | Answers "how do you handle rate limits" concretely; decorators keep the strategy contract untouched; no new packages | Polly/resilience handlers; a retry-only approach; nothing |
| 37 | Library reads return an empty list for unknown users (was 404) | A fresh install showed a red 404 before the first save; reads are now idempotent, writes still auto-create users, and per-album reads/deletes keep 404 semantics | Keep 404 and hide it in the UI |
